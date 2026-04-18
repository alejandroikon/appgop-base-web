import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';
import { Tag } from 'primeng/tag';

// Mapa de configuración V2.0: solo BORRADOR y CREADO
const STATUS_CONFIG: Record<string, { label: string; severity: 'secondary' | 'success' }> = {
  BORRADOR: { label: 'Borrador', severity: 'secondary' },
  CREADO:   { label: 'Creado',   severity: 'success'   },
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
