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
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MessageService } from 'primeng/api';
import { Subject, catchError, debounceTime, distinctUntilChanged, finalize, map, of, switchMap } from 'rxjs';
import { StepperModule } from 'primeng/stepper';
import { ButtonModule } from 'primeng/button';
import { ToastModule } from 'primeng/toast';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { WellsApiService } from '@wells/services';
import {
  CLASIFICACION_OPTIONS,
  TIPO_ANGULO_OPTIONS,
  TIPO_OBJETIVO_OPTIONS,
  TIPO_TERMINACION_OPTIONS,
  TIPO_TRAYECTORIA_OPTIONS,
  TIPO_UBICACION_OPTIONS,
} from '@wells/models';
import type { Campo, Cluster, Contrato, Departamento, Municipio, WellNamePreviewDTO } from '@wells/models';
import { WELL_FORM_LOCALE } from './locale';
import { StepContractInfoComponent } from './components/step-contract-info/step-contract-info.component';
import { StepTechnicalDataComponent } from './components/step-technical-data/step-technical-data.component';
import { StepLocationComponent } from './components/step-location/step-location.component';
import { StepSummaryComponent } from './components/step-summary/step-summary.component';
import type { WellFormSummaryData } from './components/step-summary/step-summary.component';

// ─── Tipos del formulario ─────────────────────────────────────────────────────

interface WellFormControls {
  contratoId:      FormControl<number | null>;
  campoId:         FormControl<number | null>;
  tipoTrayectoria: FormControl<string>;
  clasificacion:   FormControl<string>;
  denominacion:    FormControl<string>;
  consecutivo:     FormControl<string>;
  tipoUbicacion:   FormControl<string>;
  tipoAngulo:      FormControl<string>;
  tipoObjetivo:    FormControl<string>;
  tipoTerminacion: FormControl<string>;
  departamentoId:  FormControl<number | null>;
  municipioId:     FormControl<number | null>;
  clusterId:       FormControl<number | null>;
}

interface PreviewParams {
  contratoId:   number;
  denominacion: string;
  consecutivo:  string;
}

@Component({
  selector: 'app-well-form',
  templateUrl: './well-form.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    ReactiveFormsModule,
    StepperModule,
    ButtonModule,
    ToastModule,
    ProgressSpinnerModule,
    StepContractInfoComponent,
    StepTechnicalDataComponent,
    StepLocationComponent,
    StepSummaryComponent,
  ],
})
export class WellFormComponent implements OnInit {
  // ─── Servicios ───────────────────────────────────────────────────────────────
  private readonly wellsApi    = inject(WellsApiService);
  private readonly route       = inject(ActivatedRoute);
  private readonly router      = inject(Router);
  private readonly msgService  = inject(MessageService);
  private readonly destroyRef  = inject(DestroyRef);

  protected readonly locale = WELL_FORM_LOCALE;

  // ─── Constantes de opciones para dropdowns ────────────────────────────────
  protected readonly tipoTrayectoriaOpts  = TIPO_TRAYECTORIA_OPTIONS;
  protected readonly clasificacionOpts    = CLASIFICACION_OPTIONS;
  protected readonly tipoUbicacionOpts    = TIPO_UBICACION_OPTIONS;
  protected readonly tipoAnguloOpts       = TIPO_ANGULO_OPTIONS;
  protected readonly tipoObjetivoOpts     = TIPO_OBJETIVO_OPTIONS;
  protected readonly tipoTerminacionOpts  = TIPO_TERMINACION_OPTIONS;

  // ─── Formulario reactivo ──────────────────────────────────────────────────
  readonly wellForm = new FormGroup<WellFormControls>({
    contratoId:      new FormControl<number | null>(null, Validators.required),
    campoId:         new FormControl<number | null>(null, Validators.required),
    tipoTrayectoria: new FormControl('', { nonNullable: true, validators: Validators.required }),
    clasificacion:   new FormControl('', { nonNullable: true, validators: Validators.required }),
    denominacion:    new FormControl('', {
      nonNullable: true,
      validators: [
        Validators.required,
        Validators.maxLength(50),
        Validators.pattern(/^[A-Za-záéíóúÁÉÍÓÚñÑ ]+$/),
      ],
    }),
    consecutivo:     new FormControl('', {
      nonNullable: true,
      validators: [Validators.required, Validators.pattern(/^\d{2}$/)],
    }),
    tipoUbicacion:   new FormControl('', { nonNullable: true, validators: Validators.required }),
    tipoAngulo:      new FormControl('', { nonNullable: true, validators: Validators.required }),
    tipoObjetivo:    new FormControl('', { nonNullable: true, validators: Validators.required }),
    tipoTerminacion: new FormControl('', { nonNullable: true, validators: Validators.required }),
    departamentoId:  new FormControl<number | null>(null, Validators.required),
    municipioId:     new FormControl<number | null>(null, Validators.required),
    clusterId:       new FormControl<number | null>(null),
  });

  // ─── Estado de UI ─────────────────────────────────────────────────────────
  readonly isLoading         = signal(false);
  readonly isSaving          = signal(false);
  readonly isEditMode        = signal(false);
  readonly contratos         = signal<Contrato[]>([]);
  readonly campos            = signal<Campo[]>([]);
  readonly departamentos     = signal<Departamento[]>([]);
  readonly municipios        = signal<Municipio[]>([]);
  readonly clusters          = signal<Cluster[]>([]);
  readonly activeStep        = signal<number>(1);
  readonly selectedContrato  = signal<Contrato | null>(null);
  readonly namePreview       = signal<WellNamePreviewDTO | null>(null);
  readonly namePreviewLoading = signal(false);

  // ─── Tracking de cambios del formulario (para computed reactivos) ─────────
  private readonly formStatus = toSignal(
    this.wellForm.statusChanges,
    { initialValue: this.wellForm.status },
  );
  private readonly formValues = toSignal(
    this.wellForm.valueChanges.pipe(map(() => this.wellForm.getRawValue())),
    { initialValue: this.wellForm.getRawValue() },
  );

  // ─── Computed signals ─────────────────────────────────────────────────────
  readonly cuencaDisplay       = computed(() => this.selectedContrato()?.cuenca ?? '');
  readonly tipoContratoDisplay = computed(() => this.selectedContrato()?.tipo ?? '');

  readonly isStep1Valid = computed(() => {
    this.formStatus(); // tracking
    const controls = ['contratoId', 'campoId', 'clasificacion', 'denominacion', 'consecutivo'];
    return controls.every(name => this.wellForm.get(name)?.valid === true);
  });

  readonly isStep2Valid = computed(() => {
    this.formStatus();
    const controls = ['tipoTrayectoria', 'tipoUbicacion', 'tipoAngulo', 'tipoObjetivo', 'tipoTerminacion'];
    return controls.every(name => this.wellForm.get(name)?.valid === true);
  });

  readonly isStep3Valid = computed(() => {
    this.formStatus();
    return (
      this.wellForm.get('departamentoId')?.valid === true &&
      this.wellForm.get('municipioId')?.valid === true
    );
  });

  readonly canSave = computed(() => this.isStep1Valid() && this.isStep2Valid() && this.isStep3Valid());

  // ─── Computed para resumen (Paso 4) ──────────────────────────────────────
  readonly summaryFormData = computed((): WellFormSummaryData => this.formValues());
  readonly summaryNombrePozo = computed(() => this.namePreview()?.nombrePozo ?? '');
  readonly summaryContratoNombre = computed(() => {
    const id = this.formValues().contratoId;
    return this.contratos().find(c => c.id === id)?.nombre ?? '';
  });
  readonly summaryCampoNombre = computed(() => {
    const id = this.formValues().campoId;
    return this.campos().find(c => c.id === id)?.nombre ?? '';
  });
  readonly summaryDepartamentoNombre = computed(() => {
    const id = this.formValues().departamentoId;
    return this.departamentos().find(d => d.id === id)?.nombre ?? '';
  });
  readonly summaryMunicipioNombre = computed(() => {
    const id = this.formValues().municipioId;
    return this.municipios().find(m => m.id === id)?.nombre ?? '';
  });
  readonly summaryClusterNombre = computed(() => {
    const id = this.formValues().clusterId;
    return this.clusters().find(c => c.id === id)?.nombre ?? '';
  });

  // ─── Subject para debounce de preview ────────────────────────────────────
  private readonly previewTrigger$ = new Subject<PreviewParams | null>();

  private editId: string | null = null;

  // ─── Lifecycle ────────────────────────────────────────────────────────────

  ngOnInit(): void {
    this.editId = this.route.snapshot.paramMap.get('id');
    this.isEditMode.set(!!this.editId);

    this.loadContratos();
    this.loadDepartamentos();

    if (this.editId) {
      this.loadWellForEdit(this.editId);
    }

    this.setupPreviewDebounce();
    this.subscribeFormForPreview();
  }

  // ─── Handlers de navegación por pasos ────────────────────────────────────

  nextStep(): void {
    this.activeStep.update(v => Math.min(v + 1, 4));
  }

  prevStep(): void {
    this.activeStep.update(v => Math.max(v - 1, 1));
  }

  // ─── Handlers de outputs de step components ───────────────────────────────

  onContratoChanged(contratoId: number | null): void {
    const contrato = contratoId
      ? this.contratos().find(c => c.id === contratoId) ?? null
      : null;
    this.selectedContrato.set(contrato);

    if (!contratoId) {
      this.campos.set([]);
      this.wellForm.controls.campoId.setValue(null);
      this.clusters.set([]);
      return;
    }
    this.wellsApi.getCampos(contratoId).subscribe({
      next: (data) => {
        this.campos.set(data);
        const currentCampoId = this.wellForm.controls.campoId.value;
        if (!data.some(c => c.id === currentCampoId)) {
          this.wellForm.controls.campoId.setValue(null);
          this.clusters.set([]);
        }
      },
    });
  }

  onCampoChanged(campoId: number | null): void {
    if (!campoId) {
      this.clusters.set([]);
      this.wellForm.controls.clusterId.setValue(null);
      return;
    }
    this.wellsApi.getClusters(campoId).subscribe({
      next: (data) => {
        this.clusters.set(data);
        const currentClusterId = this.wellForm.controls.clusterId.value;
        if (!data.some(c => c.id === currentClusterId)) {
          this.wellForm.controls.clusterId.setValue(null);
        }
      },
    });
  }

  onDepartamentoChanged(departamentoId: number | null): void {
    if (!departamentoId) {
      this.municipios.set([]);
      this.wellForm.controls.municipioId.setValue(null);
      return;
    }
    this.wellsApi.getMunicipios(departamentoId).subscribe({
      next: (data) => {
        this.municipios.set(data);
        const currentMunicipioId = this.wellForm.controls.municipioId.value;
        if (!data.some(m => m.id === currentMunicipioId)) {
          this.wellForm.controls.municipioId.setValue(null);
        }
      },
    });
  }

  // ─── Envío del formulario ─────────────────────────────────────────────────

  onSubmit(): void {
    if (this.wellForm.invalid) {
      this.wellForm.markAllAsTouched();
      return;
    }

    const raw = this.wellForm.getRawValue();
    const dto = {
      contratoId:      raw.contratoId!,
      campoId:         raw.campoId!,
      tipoTrayectoria: raw.tipoTrayectoria,
      clasificacion:   raw.clasificacion,
      denominacion:    raw.denominacion,
      consecutivo:     raw.consecutivo,
      tipoUbicacion:   raw.tipoUbicacion,
      tipoAngulo:      raw.tipoAngulo,
      tipoObjetivo:    raw.tipoObjetivo,
      tipoTerminacion: raw.tipoTerminacion,
      departamentoId:  raw.departamentoId!,
      municipioId:     raw.municipioId!,
      clusterId:       raw.clusterId,
    };

    this.isSaving.set(true);

    if (this.isEditMode() && this.editId) {
      this.wellsApi.updateWell(this.editId, dto).subscribe({
        next: () => {
          this.msgService.add({ severity: 'success', summary: 'Éxito', detail: this.locale.messages.updateSuccess });
          this.isSaving.set(false);
          this.router.navigate(['/wells/manage']);
        },
        error: () => this.isSaving.set(false),
      });
    } else {
      this.wellsApi.createWell(dto).subscribe({
        next: () => {
          this.msgService.add({ severity: 'success', summary: 'Éxito', detail: this.locale.messages.createSuccess });
          this.isSaving.set(false);
          this.router.navigate(['/wells/manage']);
        },
        error: () => this.isSaving.set(false),
      });
    }
  }

  onCancel(): void {
    this.router.navigate(['/wells/manage']);
  }

  // ─── Privados ─────────────────────────────────────────────────────────────

  private loadContratos(): void {
    this.wellsApi.getContratos().subscribe({
      next: (data) => this.contratos.set(data),
    });
  }

  private loadDepartamentos(): void {
    this.wellsApi.getDepartamentos().subscribe({
      next: (data) => this.departamentos.set(data),
    });
  }

  private loadWellForEdit(id: string): void {
    this.isLoading.set(true);
    this.wellsApi.getWell(id).subscribe({
      next: (well) => {
        // Reconstituir contrato seleccionado a partir de los datos del pozo
        this.selectedContrato.set({
          id:     well.contratoId,
          nombre: well.contrato,
          tipo:   well.tipoContrato,
          cuenca: well.cuenca,
        });

        // Cargar catálogos filtrados
        this.onContratoChanged(well.contratoId);
        this.onDepartamentoChanged(well.ubicacion.departamentoId);
        if (well.campoId) this.onCampoChanged(well.campoId);

        this.wellForm.patchValue({
          contratoId:      well.contratoId,
          campoId:         well.campoId,
          tipoTrayectoria: well.tipoTrayectoria,
          clasificacion:   well.clasificacion,
          denominacion:    well.denominacion,
          consecutivo:     well.consecutivo,
          tipoUbicacion:   well.tipoUbicacion,
          tipoAngulo:      well.tipoAngulo,
          tipoObjetivo:    well.tipoObjetivo,
          tipoTerminacion: well.tipoTerminacion,
          departamentoId:  well.ubicacion.departamentoId,
          municipioId:     well.ubicacion.municipioId,
          clusterId:       well.ubicacion.clusterId,
        });
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false),
    });
  }

  private setupPreviewDebounce(): void {
    this.previewTrigger$.pipe(
      debounceTime(500),
      distinctUntilChanged((a, b) =>
        a?.contratoId === b?.contratoId &&
        a?.denominacion === b?.denominacion &&
        a?.consecutivo === b?.consecutivo,
      ),
      switchMap((params) => {
        if (!params) {
          return of(null);
        }
        this.namePreviewLoading.set(true);
        return this.wellsApi
          .previewWellName(
            params.contratoId,
            params.denominacion,
            params.consecutivo,
            this.editId ?? undefined,
          )
          .pipe(
            finalize(() => this.namePreviewLoading.set(false)),
            // catchError solo mantiene el stream exterior vivo; el toast lo muestra errorInterceptor
            catchError(() => of(null)),
          );
      }),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe((preview) => {
      this.namePreview.set(preview);
    });
  }

  private subscribeFormForPreview(): void {
    this.wellForm.valueChanges
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => {
        const { contratoId, denominacion, consecutivo } = this.wellForm.getRawValue();
        if (contratoId && denominacion && consecutivo && /^\d{2}$/.test(consecutivo)) {
          this.previewTrigger$.next({ contratoId, denominacion: denominacion.trim(), consecutivo });
        } else {
          this.namePreview.set(null);
          this.previewTrigger$.next(null);
        }
      });
  }
}
