import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { FormGroup, ReactiveFormsModule } from '@angular/forms';
import { SelectModule } from 'primeng/select';
import { MessageModule } from 'primeng/message';
import { WELL_FORM_LOCALE } from '../../locale';

@Component({
  selector: 'app-step-technical-data',
  templateUrl: './step-technical-data.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, SelectModule, MessageModule],
})
export class StepTechnicalDataComponent {
  formGroup          = input.required<FormGroup>();
  tipoTrayectoriaOpts = input.required<{ label: string; value: string }[]>();
  tipoUbicacionOpts  = input.required<{ label: string; value: string }[]>();
  tipoAnguloOpts     = input.required<{ label: string; value: string }[]>();
  tipoObjetivoOpts   = input.required<{ label: string; value: string }[]>();
  tipoTerminacionOpts = input.required<{ label: string; value: string }[]>();

  protected readonly locale = WELL_FORM_LOCALE;

  isFieldInvalid(controlName: string): boolean {
    const ctrl = this.formGroup().get(controlName);
    return !!(ctrl?.invalid && ctrl.touched);
  }
}
