import {
  ChangeDetectionStrategy,
  Component,
  DestroyRef,
  OnInit,
  computed,
  inject,
  signal,
} from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router } from '@angular/router';
import { AbstractControl, FormControl, FormGroup, ReactiveFormsModule, ValidationErrors, Validators } from '@angular/forms';
import { Store } from '@ngrx/store';
import { MessageService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { ToastModule } from 'primeng/toast';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { Subject, catchError, debounceTime, distinctUntilChanged, finalize, of, switchMap } from 'rxjs';
import { WellsApiService } from '@wells/services';
import { WellsActions } from '@wells/store';
import { selectCurrentUser } from '@core/auth/store';
import { selectIsSaving } from '@wells/store';
import type {
  Campo,
  Clasificacion,
  Cluster,
  Contrato,
  Departamento,
  Municipio,
  SubClasificacion,
  TipoUbicacion,
  UwiPreviewComponentsDTO,
} from '@wells/models';
import { generateUwiPreview } from '../../domain/uwi.generator';
import { generateWellName } from '../../domain/well-name.generator';
import { isCampoRequired } from '../../domain/well-form.validators';
import { WELL_CREATE_LOCALE } from './locale';
import { SectionContractInfoComponent } from './components/section-contract-info/section-contract-info.component';
import { SectionTechnicalDataComponent } from './components/section-technical-data/section-technical-data.component';
import { SectionLocationComponent } from './components/section-location/section-location.component';
import { UwiPreviewPanelComponent } from './components/uwi-preview-panel/uwi-preview-panel.component';

// ─── Tipos del formulario ─────────────────────────────────────────────────────

interface WellCreateFormControls {
  contratoId:        FormControl<number | null>;
  campoId:           FormControl<number | null>;
  tipoTrayectoria:   FormControl<string>;
  clasificacion:     FormControl<string>;
  subClasificacion:  FormControl<string | null>;
  denominacion:      FormControl<string>;
  consecutivo:       FormControl<number | null>;
  tipoUbicacion:     FormControl<string>;
  tipoAngulo:        FormControl<string>;
  tipoObjetivo:      FormControl<string>;
  tipoTerminacion:   FormControl<string>;
  departamentoId:    FormControl<number | null>;
  municipioId:       FormControl<number | null>;
  clusterId:         FormControl<number | null>;
}

interface UwiPreviewTrigger {
  codigoDaneDpto:  string;
  codigoDaneMpio:  string;
  denominacion:    string;
  consecutivo:     number;
  clusterNombre:   string | null;
  tipoAngulo:      string;
  tipoTrayectoria: string;
  tipoObjetivo:    string;
  tipoTerminacion: string;
}

// ─── Validator: denominación (RN-07) ─────────────────────────────────────────

function denominacionValidator(ctrl: AbstractControl): ValidationErrors | null {
  const val = ctrl.value as string;
  if (!val) return null;
  if (!/^[A-Za-záéíóúÁÉÍÓÚñÑ\s\-]{1,50}$/.test(val)) return { pattern: true };
  return null;
}

@Component({
  selector: 'app-well-create',
  templateUrl: './well-create.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    ButtonModule,
    ToastModule,
    ProgressSpinnerModule,
    SectionContractInfoComponent,
    SectionTechnicalDataComponent,
    SectionLocationComponent,
    UwiPreviewPanelComponent,
  ],
})
export class WellCreateComponent implements OnInit {
  // ─── Servicios ───────────────────────────────────────────────────────────────
  private readonly wellsApi   = inject(WellsApiService);
  private readonly route      = inject(ActivatedRoute);
  private readonly router     = inject(Router);
  private readonly store      = inject(Store);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly locale = WELL_CREATE_LOCALE;

  // ─── Store ────────────────────────────────────────────────────────────────
  protected readonly currentUser = toSignal(this.store.select(selectCurrentUser));
  protected readonly isSaving    = toSignal(this.store.select(selectIsSaving), { initialValue: false });

  protected readonly isAnh = computed(() => {
    const tenant = this.currentUser()?.tenantName ?? '';
    return tenant.toLowerCase().includes('anh');
  });

  // ─── Formulario reactivo ──────────────────────────────────────────────────
  readonly wellForm = new FormGroup<WellCreateFormControls>({
    contratoId:        new FormControl<number | null>(null, Validators.required),
    campoId:           new FormControl<number | null>(null),
    tipoTrayectoria:   new FormControl('', { nonNullable: true, validators: Validators.required }),
    clasificacion:     new FormControl('', { nonNullable: true, validators: Validators.required }),
    subClasificacion:  new FormControl<string | null>(null),
    denominacion:      new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.maxLength(50), denominacionValidator],
    }),
    consecutivo:       new FormControl<number | null>(null, [
      Validators.required,
      Validators.min(1),
      Validators.max(9999),
    ]),
    tipoUbicacion:     new FormControl('', { nonNullable: true, validators: Validators.required }),
    tipoAngulo:        new FormControl('', { nonNullable: true, validators: Validators.required }),
    tipoObjetivo:      new FormControl('', { nonNullable: true, validators: Validators.required }),
    tipoTerminacion:   new FormControl('', { nonNullable: true, validators: Validators.required }),
    departamentoId:    new FormControl<number | null>(null, Validators.required),
    municipioId:       new FormControl<number | null>(null, Validators.required),
    clusterId:         new FormControl<number | null>(null, Validators.required),
  });

  // ─── Estado de UI ─────────────────────────────────────────────────────────
  readonly isLoading         = signal(false);
  readonly isEditMode        = signal(false);
  readonly contratos         = signal<Contrato[]>([]);
  readonly campos            = signal<Campo[]>([]);
  readonly departamentos     = signal<Departamento[]>([]);
  readonly municipios        = signal<Municipio[]>([]);
  readonly clusters          = signal<Cluster[]>([]);
  readonly selectedContrato  = signal<Contrato | null>(null);
  readonly selectedCampo     = signal<Campo | null>(null);
  readonly selectedDepartamento = signal<Departamento | null>(null);
  readonly selectedMunicipio = signal<Municipio | null>(null);
  readonly selectedCluster   = signal<Cluster | null>(null);

  // UWI preview (computed client-side)
  readonly uwiPreview        = signal<string | null>(null);
  readonly uwiComponents     = signal<UwiPreviewComponentsDTO | null>(null);
  readonly uwiIsUnique       = signal<boolean | null>(null);
  readonly uwiChecking       = signal(false);

  // Nombre del pozo (computed client-side)
  readonly wellNamePreview   = signal<string>('');

  // ─── Tracking de formulario para computed ─────────────────────────────────
  private readonly formValues = toSignal(
    this.wellForm.valueChanges,
    { initialValue: this.wellForm.getRawValue() },
  );

  // ─── Computed signals ─────────────────────────────────────────────────────
  readonly cuencaDisplay      = computed(() => this.selectedContrato()?.cuenca ?? '');
  readonly tipoContratoDisplay = computed(() => this.selectedContrato()?.tipo ?? '');
  readonly ubicacionDefault    = computed((): TipoUbicacion | null =>
    this.selectedContrato()?.ubicacionDefault ?? null,
  );

  readonly campoRequired = computed(() => {
    this.formValues(); // tracking
    const clas = this.wellForm.get('clasificacion')?.value;
    return clas ? isCampoRequired(clas) : false;
  });

  // ─── Debounce para verificación de UWI ────────────────────────────────────
  private readonly uwiTrigger$ = new Subject<UwiPreviewTrigger | null>();

  private editId: string | null = null;

  // ─── Lifecycle ────────────────────────────────────────────────────────────

  ngOnInit(): void {
    this.editId = this.route.snapshot.paramMap.get('id');
    this.isEditMode.set(!!this.editId);

    this.loadContratos();
    this.loadDepartamentos();

    if (this.editId) this.loadWellForEdit(this.editId);

    this.setupUwiDebounce();
    this.subscribeFormForUwiAndName();
    this.syncClusterValidation();
  }

  // ─── Handlers de cascadas ──────────────────────────────────────────────────

  onContratoChanged(contratoId: number | null): void {
    const contrato = contratoId
      ? this.contratos().find((c) => c.id === contratoId) ?? null
      : null;
    this.selectedContrato.set(contrato);
    this.campos.set([]);
    this.wellForm.controls.campoId.setValue(null);
    this.selectedCampo.set(null);
    this.clusters.set([]);
    this.wellForm.controls.clusterId.setValue(null);
    this.selectedCluster.set(null);

    // Pre-seleccionar ubicación por defecto del contrato
    if (contrato?.ubicacionDefault) {
      this.wellForm.controls.tipoUbicacion.setValue(contrato.ubicacionDefault);
    }

    if (contratoId) {
      this.wellsApi.getCampos(contratoId).subscribe({
        next: (data) => this.campos.set(data),
      });
    }
  }

  onCampoChanged(campoId: number | null): void {
    const campo = campoId ? this.campos().find((c) => c.id === campoId) ?? null : null;
    this.selectedCampo.set(campo);
    this.clusters.set([]);
    this.wellForm.controls.clusterId.setValue(null);
    this.selectedCluster.set(null);

    if (campoId) {
      this.wellsApi.getClusters(campoId).subscribe({
        next: (data) => {
          this.clusters.set(data);
        },
      });
    }
  }

  onDepartamentoChanged(departamentoId: number | null): void {
    const dpto = departamentoId
      ? this.departamentos().find((d) => d.id === departamentoId) ?? null
      : null;
    this.selectedDepartamento.set(dpto);
    this.municipios.set([]);
    this.wellForm.controls.municipioId.setValue(null);
    this.selectedMunicipio.set(null);

    if (departamentoId) {
      this.wellsApi.getMunicipios(departamentoId).subscribe({
        next: (data) => this.municipios.set(data),
      });
    }
  }

  onClasificacionChanged(clasificacion: Clasificacion | null): void {
    // Limpiar campo y subclasificación al cambiar clasificación (RN-14)
    this.wellForm.controls.campoId.setValue(null);
    this.wellForm.controls.subClasificacion.setValue(null);
    this.selectedCampo.set(null);
    this.clusters.set([]);
    this.wellForm.controls.clusterId.setValue(null);

    // Si Exploratorio requiere subclasificación como obligatoria
    if (clasificacion === 'EXPLORATORIO') {
      this.wellForm.controls.subClasificacion.setValidators(Validators.required);
    } else {
      this.wellForm.controls.subClasificacion.clearValidators();
    }
    this.wellForm.controls.subClasificacion.updateValueAndValidity();
  }

  // ─── Submit ───────────────────────────────────────────────────────────────

  onSaveDraft(): void {
    const raw = this.wellForm.getRawValue();
    const hasData = Object.values(raw).some((v) => v !== null && v !== '');
    if (!hasData) return;

    this.store.dispatch(WellsActions.createWell({
      payload: {
        action: 'DRAFT',
        contratoId:      raw.contratoId,
        campoId:         raw.campoId,
        denominacion:    raw.denominacion || null,
        consecutivo:     raw.consecutivo,
        tipoTrayectoria: raw.tipoTrayectoria || null,
        clasificacion:   raw.clasificacion || null,
        subClasificacion: raw.subClasificacion || null,
        tipoUbicacion:   raw.tipoUbicacion || null,
        tipoAngulo:      raw.tipoAngulo || null,
        tipoObjetivo:    raw.tipoObjetivo || null,
        tipoTerminacion: raw.tipoTerminacion || null,
        departamentoId:  raw.departamentoId,
        municipioId:     raw.municipioId,
        clusterId:       raw.clusterId,
      },
    }));
  }

  onSaveDraftEdit(): void {
    if (!this.editId) return;
    const raw = this.wellForm.getRawValue();
    this.store.dispatch(WellsActions.updateWell({
      id: this.editId,
      payload: {
        action: 'SAVE',
        contratoId:      raw.contratoId,
        campoId:         raw.campoId,
        denominacion:    raw.denominacion || null,
        consecutivo:     raw.consecutivo,
        tipoTrayectoria: raw.tipoTrayectoria || null,
        clasificacion:   raw.clasificacion || null,
        subClasificacion: raw.subClasificacion || null,
        tipoUbicacion:   raw.tipoUbicacion || null,
        tipoAngulo:      raw.tipoAngulo || null,
        tipoObjetivo:    raw.tipoObjetivo || null,
        tipoTerminacion: raw.tipoTerminacion || null,
        departamentoId:  raw.departamentoId,
        municipioId:     raw.municipioId,
        clusterId:       raw.clusterId,
      },
    }));
  }

  onFinalize(): void {
    if (this.wellForm.invalid) {
      this.wellForm.markAllAsTouched();
      return;
    }
    const raw = this.wellForm.getRawValue();

    if (this.isEditMode() && this.editId) {
      this.store.dispatch(WellsActions.updateWell({
        id: this.editId,
        payload: {
          action: 'FINALIZE',
          contratoId:      raw.contratoId,
          campoId:         raw.campoId,
          denominacion:    raw.denominacion,
          consecutivo:     raw.consecutivo,
          tipoTrayectoria: raw.tipoTrayectoria,
          clasificacion:   raw.clasificacion,
          subClasificacion: raw.subClasificacion,
          tipoUbicacion:   raw.tipoUbicacion,
          tipoAngulo:      raw.tipoAngulo,
          tipoObjetivo:    raw.tipoObjetivo,
          tipoTerminacion: raw.tipoTerminacion,
          departamentoId:  raw.departamentoId,
          municipioId:     raw.municipioId,
          clusterId:       raw.clusterId,
        },
      }));
    } else {
      this.store.dispatch(WellsActions.createWell({
        payload: {
          action: 'FINALIZE',
          contratoId:      raw.contratoId,
          campoId:         raw.campoId,
          denominacion:    raw.denominacion,
          consecutivo:     raw.consecutivo,
          tipoTrayectoria: raw.tipoTrayectoria,
          clasificacion:   raw.clasificacion,
          subClasificacion: raw.subClasificacion,
          tipoUbicacion:   raw.tipoUbicacion,
          tipoAngulo:      raw.tipoAngulo,
          tipoObjetivo:    raw.tipoObjetivo,
          tipoTerminacion: raw.tipoTerminacion,
          departamentoId:  raw.departamentoId,
          municipioId:     raw.municipioId,
          clusterId:       raw.clusterId,
        },
      }));
    }
  }

  onCancel(): void {
    this.router.navigate(['/wells/manage']);
  }

  // ─── Privados ─────────────────────────────────────────────────────────────

  private loadContratos(): void {
    this.wellsApi.getContratos().subscribe({ next: (data) => this.contratos.set(data) });
  }

  private loadDepartamentos(): void {
    this.wellsApi.getDepartamentos().subscribe({ next: (data) => this.departamentos.set(data) });
  }

  private loadWellForEdit(id: string): void {
    this.isLoading.set(true);
    this.wellsApi.getWell(id).subscribe({
      next: (well) => {
        if (well.forma101Radicada) {
          this.router.navigate(['/wells/manage']);
          return;
        }
        this.selectedContrato.set(well.contratoId != null ? {
          id:               well.contratoId,
          nombre:           well.contrato ?? '',
          tipo:             well.tipoContrato ?? '',
          cuenca:           well.cuenca ?? '',
          ubicacionDefault: (well.tipoUbicacion ?? undefined) as TipoUbicacion | undefined,
        } : null);

        if (well.contratoId) this.onContratoChanged(well.contratoId);
        if (well.departamentoId) this.onDepartamentoChanged(well.departamentoId);
        if (well.campoId) this.onCampoChanged(well.campoId);

        this.wellForm.patchValue({
          contratoId:       well.contratoId,
          campoId:          well.campoId,
          tipoTrayectoria:  well.tipoTrayectoria ?? '',
          clasificacion:    well.clasificacion ?? '',
          subClasificacion: well.subClasificacion ?? null,
          denominacion:     well.denominacion ?? '',
          consecutivo:      well.consecutivo,
          tipoUbicacion:    well.tipoUbicacion ?? '',
          tipoAngulo:       well.tipoAngulo ?? '',
          tipoObjetivo:     well.tipoObjetivo ?? '',
          tipoTerminacion:  well.tipoTerminacion ?? '',
          departamentoId:   well.departamentoId,
          municipioId:      well.municipioId,
          clusterId:        well.clusterId,
        });
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
        this.router.navigate(['/wells/manage']);
      },
    });
  }

  /** Sincroniza la obligatoriedad del cluster con la clasificación */
  private syncClusterValidation(): void {
    this.wellForm.controls.clasificacion.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe((val) => {
        if (val === 'DESARROLLO') {
          this.wellForm.controls.clusterId.setValidators(Validators.required);
        } else {
          this.wellForm.controls.clusterId.clearValidators();
        }
        this.wellForm.controls.clusterId.updateValueAndValidity();
      });
  }

  private setupUwiDebounce(): void {
    this.uwiTrigger$.pipe(
      debounceTime(500),
      distinctUntilChanged((a, b) => JSON.stringify(a) === JSON.stringify(b)),
      switchMap((params) => {
        if (!params) {
          this.uwiPreview.set(null);
          this.uwiComponents.set(null);
          this.uwiIsUnique.set(null);
          return of(null);
        }

        // 1. Compute preview client-side (inmediato, ≤ 100ms)
        const cluster = this.selectedCluster();
        const result = generateUwiPreview({
          ...params,
          clusterAbreviatura: cluster?.abreviatura ?? null,
          isAnh:              this.isAnh(),
        });
        this.uwiPreview.set(result.uwi);
        this.uwiComponents.set(result.components);

        // 2. Verificar unicidad contra backend (debounceado)
        this.uwiChecking.set(true);
        return this.wellsApi.previewUwi({
          ...params,
          clusterNombre: params.clusterNombre ?? undefined,
          isAnh:         this.isAnh(),
          excludeWellId: this.editId ?? undefined,
        }).pipe(
          finalize(() => this.uwiChecking.set(false)),
          catchError(() => {
            this.uwiIsUnique.set(null);
            return of(null);
          }),
        );
      }),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe((resp) => {
      if (resp) this.uwiIsUnique.set(resp.isUnique);
    });
  }

  private subscribeFormForUwiAndName(): void {
    this.wellForm.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        const raw  = this.wellForm.getRawValue();
        const dpto = this.selectedDepartamento();
        const mpio = this.municipios().find((m) => m.id === raw.municipioId) ?? null;

        // Actualizar municipio seleccionado
        if (mpio && mpio.id !== this.selectedMunicipio()?.id) {
          this.selectedMunicipio.set(mpio);
        }

        // Actualizar cluster seleccionado
        const cluster = this.clusters().find((c) => c.id === raw.clusterId) ?? null;
        if (cluster?.id !== this.selectedCluster()?.id) {
          this.selectedCluster.set(cluster);
        }

        // Nombre del pozo (client-side, RN-16..22)
        const campo    = this.selectedCampo();
        const contrato = this.selectedContrato();
        if (raw.denominacion && raw.consecutivo) {
          const name = generateWellName(
            campo?.nombre ?? null,
            contrato?.nombre ?? null,
            raw.denominacion,
            raw.consecutivo,
          );
          this.wellNamePreview.set(name);
        } else {
          this.wellNamePreview.set('');
        }

        // Trigger UWI preview si todos los campos DANE + técnicos están listos
        if (
          dpto?.codigoDane && mpio?.codigoDane &&
          raw.denominacion && raw.consecutivo &&
          raw.tipoAngulo && raw.tipoTrayectoria &&
          raw.tipoObjetivo && raw.tipoTerminacion
        ) {
          this.uwiTrigger$.next({
            codigoDaneDpto:  dpto.codigoDane,
            codigoDaneMpio:  mpio.codigoDane,
            denominacion:    raw.denominacion.trim(),
            consecutivo:     raw.consecutivo,
            clusterNombre:   cluster?.nombre ?? null,
            tipoAngulo:      raw.tipoAngulo,
            tipoTrayectoria: raw.tipoTrayectoria,
            tipoObjetivo:    raw.tipoObjetivo,
            tipoTerminacion: raw.tipoTerminacion,
          });
        } else {
          this.uwiPreview.set(null);
          this.uwiComponents.set(null);
          this.uwiIsUnique.set(null);
          this.uwiTrigger$.next(null);
        }
      });
  }
}
