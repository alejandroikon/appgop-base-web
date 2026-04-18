import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { FormGroup, ReactiveFormsModule } from '@angular/forms';
import { SelectModule } from 'primeng/select';
import { MessageModule } from 'primeng/message';
import type { Campo, Contrato } from '@wells/models';
import { WELL_CREATE_LOCALE } from '../../locale';

@Component({
  selector: 'app-section-contract-info',
  templateUrl: './section-contract-info.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, SelectModule, MessageModule],
})
export class SectionContractInfoComponent {
  formGroup  = input.required<FormGroup>();
  contratos  = input.required<Contrato[]>();
  campos     = input.required<Campo[]>();
  operadora  = input('');
  cuenca     = input('');
  tipoContrato = input('');
  isAnh      = input(false);

  contratoChange = output<number | null>();
  campoChange    = output<number | null>();

  protected readonly locale = WELL_CREATE_LOCALE;

  protected readonly ubicacionDefaultFromContrato = computed<string>(() => '');

  isFieldInvalid(controlName: string): boolean {
    const ctrl = this.formGroup().get(controlName);
    return !!(ctrl?.invalid && ctrl.touched);
  }

  hasError(controlName: string, errorType: string): boolean {
    return !!this.formGroup().get(controlName)?.hasError(errorType);
  }
}
