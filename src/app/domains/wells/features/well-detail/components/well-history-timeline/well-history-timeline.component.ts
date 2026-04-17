import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TimelineModule } from 'primeng/timeline';
import { TagModule } from 'primeng/tag';
import { SkeletonModule } from 'primeng/skeleton';
import type { TransitionHistoryItem } from '@wells/models';
import { WELL_DETAIL_LOCALE } from '../../locale';

@Component({
  selector: 'app-well-history-timeline',
  templateUrl: './well-history-timeline.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TimelineModule, TagModule, SkeletonModule],
})
export class WellHistoryTimelineComponent {
  history   = input.required<TransitionHistoryItem[]>();
  isLoading = input(false);

  protected readonly locale = WELL_DETAIL_LOCALE;

  protected formatDate(isoDate: string): string {
    return new Date(isoDate).toLocaleString('es-CO', {
      day:    '2-digit',
      month:  '2-digit',
      year:   'numeric',
      hour:   '2-digit',
      minute: '2-digit',
    });
  }

  protected getActionSeverity(
    action: string,
  ): 'success' | 'danger' | 'info' | 'warn' | 'secondary' {
    const map: Record<string, 'success' | 'danger' | 'info' | 'warn' | 'secondary'> = {
      ENVIAR:      'info',
      APROBAR_UWI: 'success',
      DEVOLVER:    'danger',
      FISCALIZAR:  'success',
    };
    return map[action] ?? 'secondary';
  }

  protected getActionLabel(action: string): string {
    const map: Record<string, string> = {
      ENVIAR:      'Enviar',
      APROBAR_UWI: 'Aprobar UWI',
      DEVOLVER:    'Devolver',
      FISCALIZAR:  'Fiscalizar',
    };
    return map[action] ?? action;
  }

  protected getStateLabel(state: string): string {
    return this.locale.status[state] ?? state;
  }
}
