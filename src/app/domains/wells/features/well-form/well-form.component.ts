import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { MessageService } from 'primeng/api';
import { WellsApiService } from '@wells/services';
import {
  CLASIFICACION_OPTIONS,
  TIPO_ANGULO_OPTIONS,
  TIPO_OBJETIVO_OPTIONS,
  TIPO_TERMINACION_OPTIONS,
  TIPO_TRAYECTORIA_OPTIONS,
  TIPO_UBICACION_OPTIONS,
} from '@wells/models';
import type { Campo, Cluster, Contrato, Departamento, Municipio } from '@wells/models';
import { WELL_FORM_LOCALE } from './locale';

// UI imports — PrimeNG 21
import { CommonModule } from '@angular/common';
import { ButtonModule } from 'primeng/button';
import { SelectModule } from 'primeng/select';
import { InputTextModule } from 'primeng/inputtext';
import { ToastModule } from 'primeng/toast';
import { CardModule } from 'primeng/card';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { MessageModule } from 'primeng/message';

// Tipos del formulario
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

@Component({
  selector: 'app-well-form',
  templateUrl: './well-form.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    ButtonModule,
    SelectModule,
    InputTextModule,
    ToastModule,
    CardModule,
    ProgressSpinnerModule,
    MessageModule,
  ],
})
export class WellFormComponent implements OnInit {
  private readonly wellsApi    = inject(WellsApiService);
  private readonly route       = inject(ActivatedRoute);
  private readonly router      = inject(Router);
  private readonly msgService  = inject(MessageService);

  protected readonly locale = WELL_FORM_LOCALE;

  // ─── Constantes de opciones para dropdowns ────────────────────────────────
  protected readonly tipoTrayectoriaOpts = TIPO_TRAYECTORIA_OPTIONS;
  protected readonly clasificacionOpts   = CLASIFICACION_OPTIONS;
  protected readonly tipoUbicacionOpts   = TIPO_UBICACION_OPTIONS;
  protected readonly tipoAnguloOpts      = TIPO_ANGULO_OPTIONS;
  protected readonly tipoObjetivoOpts    = TIPO_OBJETIVO_OPTIONS;
  protected readonly tipoTerminacionOpts = TIPO_TERMINACION_OPTIONS;

  // ─── Estado de UI ─────────────────────────────────────────────────────────
  readonly isLoading     = signal(false);
  readonly isSaving      = signal(false);
  readonly isEditMode    = signal(false);
  readonly contratos     = signal<Contrato[]>([]);
  readonly campos        = signal<Campo[]>([]);
  readonly departamentos = signal<Departamento[]>([]);
  readonly municipios    = signal<Municipio[]>([]);
  readonly clusters      = signal<Cluster[]>([]);

  private editId: string | null = null;

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

  ngOnInit(): void {
    this.editId = this.route.snapshot.paramMap.get('id');
    this.isEditMode.set(!!this.editId);

    // Cargar catálogos raíz siempre
    this.loadContratos();
    this.loadDepartamentos();

    if (this.editId) {
      this.loadWellForEdit(this.editId);
    }
  }

  // ─── Carga de catálogos ───────────────────────────────────────────────────

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
        // Cargar catálogos filtrados con los valores del pozo
        this.onContratoChange(well.contratoId);
        this.onDepartamentoChange(well.ubicacion.departamentoId);
        if (well.campoId) this.onCampoChange(well.campoId);

        // Patch del formulario con los valores del pozo
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

  // ─── Handlers de cambio en dropdowns cascada ──────────────────────────────

  onContratoChange(contratoId: number | null): void {
    if (!contratoId) {
      this.campos.set([]);
      this.wellForm.controls.campoId.setValue(null);
      return;
    }
    this.wellsApi.getCampos(contratoId).subscribe({
      next: (data) => {
        this.campos.set(data);
        // Solo limpiar si el campo actual no pertenece al nuevo contrato
        const currentCampoId = this.wellForm.controls.campoId.value;
        if (!data.some((c) => c.id === currentCampoId)) {
          this.wellForm.controls.campoId.setValue(null);
          this.clusters.set([]);
        }
      },
    });
  }

  onDepartamentoChange(departamentoId: number | null): void {
    if (!departamentoId) {
      this.municipios.set([]);
      this.wellForm.controls.municipioId.setValue(null);
      return;
    }
    this.wellsApi.getMunicipios(departamentoId).subscribe({
      next: (data) => {
        this.municipios.set(data);
        const currentMunicipioId = this.wellForm.controls.municipioId.value;
        if (!data.some((m) => m.id === currentMunicipioId)) {
          this.wellForm.controls.municipioId.setValue(null);
        }
      },
    });
  }

  onCampoChange(campoId: number | null): void {
    if (!campoId) {
      this.clusters.set([]);
      this.wellForm.controls.clusterId.setValue(null);
      return;
    }
    this.wellsApi.getClusters(campoId).subscribe({
      next: (data) => {
        this.clusters.set(data);
        const currentClusterId = this.wellForm.controls.clusterId.value;
        if (!data.some((c) => c.id === currentClusterId)) {
          this.wellForm.controls.clusterId.setValue(null);
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

  // ─── Helpers de validación (acceso en template) ───────────────────────────

  isFieldInvalid(controlName: keyof WellFormControls): boolean {
    const control = this.wellForm.get(controlName);
    return !!(control?.invalid && control.touched);
  }

  hasError(controlName: keyof WellFormControls, errorType: string): boolean {
    return !!this.wellForm.get(controlName)?.hasError(errorType);
  }
}
