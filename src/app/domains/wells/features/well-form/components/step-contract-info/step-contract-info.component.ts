import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { FormGroup, ReactiveFormsModule } from '@angular/forms';
import { SelectModule } from 'primeng/select';
import { InputTextModule } from 'primeng/inputtext';
import { MessageModule } from 'primeng/message';
import type { Campo, Cluster, Contrato } from '@wells/models';
import { WellNamePreviewComponent } from '../well-name-preview/well-name-preview.component';
import type { WellNamePreviewDTO } from '@wells/models';
import { WELL_FORM_LOCALE } from '../../locale';

@Component({
  selector: 'app-step-contract-info',
  templateUrl: './step-contract-info.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, SelectModule, InputTextModule, MessageModule, WellNamePreviewComponent],
})
export class StepContractInfoComponent {
  formGroup        = input.required<FormGroup>();
  contratos        = input.required<Contrato[]>();
  campos           = input.required<Campo[]>();
  clusters         = input.required<Cluster[]>();
  clasificacionOpts = input.required<{ label: string; value: string }[]>();
  cuenca           = input('');
  tipoContrato     = input('');
  namePreview      = input<WellNamePreviewDTO | null>(null);
  namePreviewLoading = input(false);

  contratoChanged = output<number | null>();
  campoChanged    = output<number | null>();

  protected readonly locale = WELL_FORM_LOCALE;

  isFieldInvalid(controlName: string): boolean {
    const ctrl = this.formGroup().get(controlName);
    return !!(ctrl?.invalid && ctrl.touched);
  }

  hasError(controlName: string, errorType: string): boolean {
    return !!this.formGroup().get(controlName)?.hasError(errorType);
  }
}
