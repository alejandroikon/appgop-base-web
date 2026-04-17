import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TagModule } from 'primeng/tag';
import { WELL_FORM_LOCALE } from '../../locale';

/** Datos planos del formulario para mostrar en el resumen read-only */
export interface WellFormSummaryData {
  contratoId:      number | null;
  campoId:         number | null;
  tipoTrayectoria: string;
  clasificacion:   string;
  denominacion:    string;
  consecutivo:     string;
  tipoUbicacion:   string;
  tipoAngulo:      string;
  tipoObjetivo:    string;
  tipoTerminacion: string;
  departamentoId:  number | null;
  municipioId:     number | null;
  clusterId:       number | null;
}

@Component({
  selector: 'app-step-summary',
  templateUrl: './step-summary.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TagModule],
})
export class StepSummaryComponent {
  formData          = input.required<WellFormSummaryData>();
  cuenca            = input('');
  tipoContrato      = input('');
  nombrePozo        = input('');
  contratoNombre    = input('');
  campoNombre       = input('');
  departamentoNombre = input('');
  municipioNombre   = input('');
  clusterNombre     = input('');

  protected readonly locale = WELL_FORM_LOCALE;
}
