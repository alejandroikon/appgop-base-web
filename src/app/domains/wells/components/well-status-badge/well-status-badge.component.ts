import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { Tag } from 'primeng/tag';

// Mapa de configuración: estado → { label, severity, icon }
const STATUS_CONFIG: Record<string, { label: string; severity: 'secondary' | 'warn' | 'info' | 'success'; icon: string }> = {
  BORRADOR:     { label: 'Borrador',       severity: 'secondary', icon: 'pi pi-pencil'       },
  PENDING_UWI:  { label: 'Pendiente UWI',  severity: 'warn',      icon: 'pi pi-clock'        },
  READY_FISCAL: { label: 'Listo Fiscal',   severity: 'info',      icon: 'pi pi-check-circle' },
  FISCALIZADO:  { label: 'Fiscalizado',    severity: 'success',   icon: 'pi pi-verified'     },
};

@Component({
  selector: 'app-well-status-badge',
  templateUrl: './well-status-badge.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [Tag],
})
export class WellStatusBadgeComponent {
  estado = input.required<string>();

  protected readonly config = computed(
    () => STATUS_CONFIG[this.estado()] ?? STATUS_CONFIG['BORRADOR'],
  );
}
