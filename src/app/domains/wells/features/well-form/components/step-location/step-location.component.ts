import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { FormGroup, ReactiveFormsModule } from '@angular/forms';
import { SelectModule } from 'primeng/select';
import { MessageModule } from 'primeng/message';
import type { Departamento, Municipio } from '@wells/models';
import { WELL_FORM_LOCALE } from '../../locale';

@Component({
  selector: 'app-step-location',
  templateUrl: './step-location.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, SelectModule, MessageModule],
})
export class StepLocationComponent {
  formGroup     = input.required<FormGroup>();
  departamentos = input.required<Departamento[]>();
  municipios    = input.required<Municipio[]>();

  departamentoChanged = output<number | null>();

  protected readonly locale = WELL_FORM_LOCALE;

  isFieldInvalid(controlName: string): boolean {
    const ctrl = this.formGroup().get(controlName);
    return !!(ctrl?.invalid && ctrl.touched);
  }
}
