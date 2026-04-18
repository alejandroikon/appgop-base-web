import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { FormGroup, ReactiveFormsModule } from '@angular/forms';
import { SelectModule } from 'primeng/select';
import { InputTextModule } from 'primeng/inputtext';
import { MessageModule } from 'primeng/message';
import { ButtonModule } from 'primeng/button';
import type { Cluster, Departamento, Municipio } from '@wells/models';
import { WELL_CREATE_LOCALE } from '../../locale';

@Component({
  selector: 'app-section-location',
  templateUrl: './section-location.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ReactiveFormsModule, SelectModule, InputTextModule, MessageModule, ButtonModule],
})
export class SectionLocationComponent {
  formGroup    = input.required<FormGroup>();
  departamentos = input.required<Departamento[]>();
  municipios   = input.required<Municipio[]>();
  clusters     = input.required<Cluster[]>();

  departamentoChange = output<number | null>();
  createClusterRequest = output<string>();

  protected readonly locale = WELL_CREATE_LOCALE;

  /** Código DANE del departamento seleccionado */
  protected readonly selectedDeptoDane = computed(() => {
    const id = this.formGroup().get('departamentoId')?.value as number | null;
    return id ? (this.departamentos().find((d) => d.id === id)?.codigoDane ?? '') : '';
  });

  /** Código DANE (5 dígitos) del municipio seleccionado */
  protected readonly selectedMpioDane = computed(() => {
    const id = this.formGroup().get('municipioId')?.value as number | null;
    return id ? (this.municipios().find((m) => m.id === id)?.codigoDane ?? '') : '';
  });

  /** Parte municipal del código DANE (3 últimos dígitos) */
  protected readonly selectedMpioCode = computed(() => {
    const dane = this.selectedMpioDane();
    return dane.length === 5 ? dane.slice(2) : dane.slice(-3);
  });

  isFieldInvalid(controlName: string): boolean {
    const ctrl = this.formGroup().get(controlName);
    return !!(ctrl?.invalid && ctrl.touched);
  }
}
