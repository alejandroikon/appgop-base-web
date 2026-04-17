import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { ButtonModule } from 'primeng/button';
import type { TransitionAction } from '@wells/models';
import { WELL_DETAIL_LOCALE } from '../../locale';

// Tabla de transiciones válidas: espeja la matriz del spec §2.2
interface TransitionEntry {
  from: string;
  action: TransitionAction;
  roles: string[];
  severity: 'primary' | 'success' | 'danger' | 'warn' | 'info' | 'secondary';
}

const TRANSITION_MATRIX: TransitionEntry[] = [
  {
    from:     'BORRADOR',
    action:   'ENVIAR',
    roles:    ['ADMIN', 'SUPERVISOR', 'OPERADOR'],
    severity: 'primary',
  },
  {
    from:     'PENDING_UWI',
    action:   'APROBAR_UWI',
    roles:    ['ADMIN', 'SUPERVISOR'],
    severity: 'success',
  },
  {
    from:     'PENDING_UWI',
    action:   'DEVOLVER',
    roles:    ['ADMIN', 'SUPERVISOR'],
    severity: 'danger',
  },
  {
    from:     'READY_FISCAL',
    action:   'FISCALIZAR',
    roles:    ['ADMIN', 'SUPERVISOR'],
    severity: 'success',
  },
  {
    from:     'READY_FISCAL',
    action:   'DEVOLVER',
    roles:    ['ADMIN', 'SUPERVISOR'],
    severity: 'danger',
  },
];

@Component({
  selector: 'app-well-transition-actions',
  templateUrl: './well-transition-actions.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [ButtonModule],
})
export class WellTransitionActionsComponent {
  estado    = input.required<string>();
  userRole  = input.required<string>();
  isLoading = input(false);

  transition = output<{ action: TransitionAction }>();

  protected readonly locale = WELL_DETAIL_LOCALE;

  protected readonly availableActions = computed(() => {
    const estado = this.estado();
    const role   = this.userRole();
    return TRANSITION_MATRIX.filter(
      (t) => t.from === estado && t.roles.includes(role),
    );
  });

  protected onTransitionClick(action: TransitionAction): void {
    this.transition.emit({ action });
  }
}
