import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { FormGroup, ReactiveFormsModule } from '@angular/forms';
import { SelectModule } from 'primeng/select';
import { InputTextModule } from 'primeng/inputtext';
import { InputNumberModule } from 'primeng/inputnumber';
import { MessageModule } from 'primeng/message';
import {
  CLASIFICACION_OPTIONS,
  SUB_CLASIFICACION_OPTIONS,
  TIPO_ANGULO_OPTIONS,
  TIPO_OBJETIVO_OPTIONS,
  TIPO_TERMINACION_OPTIONS,
  TIPO_TRAYECTORIA_OPTIONS,
  TIPO_UBICACION_OPTIONS,
} from '@wells/models';
import type { Clasificacion, SubClasificacion, TipoUbicacion } from '@wells/models';
import { WELL_CREATE_LOCALE } from '../../locale';

@Component({
  selector: 'app-section-technical-data',
  templateUrl: './section-technical-data.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, SelectModule, InputTextModule, InputNumberModule, MessageModule],
})
export class SectionTechnicalDataComponent {
  formGroup        = input.required<FormGroup>();
  isAnh            = input(false);
  ubicacionDefault = input<TipoUbicacion | null>(null);

  clasificacionChange = output<Clasificacion | null>();
  subClasificacionChange = output<SubClasificacion | null>();
  terminacionChange   = output<string | null>();

  protected readonly locale = WELL_CREATE_LOCALE;

  // Dropdown options
  protected readonly tipoTrayectoriaOpts  = TIPO_TRAYECTORIA_OPTIONS;
  protected readonly tipoAnguloOpts       = TIPO_ANGULO_OPTIONS;
  protected readonly tipoObjetivoOpts     = TIPO_OBJETIVO_OPTIONS;
  protected readonly tipoTerminacionOpts  = TIPO_TERMINACION_OPTIONS;
  protected readonly tipoUbicacionOpts    = TIPO_UBICACION_OPTIONS;
  protected readonly subClasificacionOpts = SUB_CLASIFICACION_OPTIONS;

  /** Opciones de clasificación filtradas: ANH solo ve ESTRATIGRAFICO (RN-15) */
  protected readonly clasificacionOpts = computed(() =>
    this.isAnh()
      ? CLASIFICACION_OPTIONS.filter((o) => o.value === 'ESTRATIGRAFICO')
      : CLASIFICACION_OPTIONS,
  );

  /** Mostrar sublista de sub-clasificación cuando Clasificación = EXPLORATORIO */
  protected readonly showSubClasificacion = computed(() => {
    const val = this.formGroup().get('clasificacion')?.value as Clasificacion | null;
    return val === 'EXPLORATORIO';
  });

  /** Advertencia RN-23: Terminación = OH */
  protected readonly showOhWarning = computed(() => {
    return this.formGroup().get('tipoTerminacion')?.value === 'OH';
  });

  isFieldInvalid(controlName: string): boolean {
    const ctrl = this.formGroup().get(controlName);
    return !!(ctrl?.invalid && ctrl.touched);
  }

  hasError(controlName: string, errorType: string): boolean {
    return !!this.formGroup().get(controlName)?.hasError(errorType);
  }
}
