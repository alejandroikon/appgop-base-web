import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  inject,
  signal,
} from '@angular/core';
import { Router } from '@angular/router';
import { ActivatedRoute } from '@angular/router';
import { RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { Store } from '@ngrx/store';
import { MessageService } from 'primeng/api';
import { CardModule } from 'primeng/card';
import { ButtonModule } from 'primeng/button';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { ToastModule } from 'primeng/toast';
import { WellsApiService } from '@wells/services';
import type { TransitionAction, TransitionHistoryItem, Well } from '@wells/models';
import { selectCurrentUser } from '@core/auth/store/auth.selectors';
import { WellStatusBadgeComponent } from '../../components/well-status-badge/well-status-badge.component';
import { WellTransitionActionsComponent } from './components/well-transition-actions/well-transition-actions.component';
import { WellHistoryTimelineComponent } from './components/well-history-timeline/well-history-timeline.component';
import { TransitionConfirmDialogComponent } from './components/transition-confirm-dialog/transition-confirm-dialog.component';
import { WELL_DETAIL_LOCALE } from './locale';

@Component({
  selector: 'app-well-detail',
  templateUrl: './well-detail.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [MessageService],
  imports: [
    RouterLink,
    CardModule,
    ButtonModule,
    ProgressSpinnerModule,
    ToastModule,
    WellStatusBadgeComponent,
    WellTransitionActionsComponent,
    WellHistoryTimelineComponent,
    TransitionConfirmDialogComponent,
  ],
})
export class WellDetailComponent implements OnInit {
  private readonly wellsApi = inject(WellsApiService);
  private readonly route    = inject(ActivatedRoute);
  private readonly router   = inject(Router);
  private readonly store    = inject(Store);
  private readonly messageService = inject(MessageService);

  protected readonly locale = WELL_DETAIL_LOCALE;

  // Lee el usuario del store NgRx (solo se actualiza cuando cambia el store)
  protected readonly currentUser = toSignal(this.store.select(selectCurrentUser));

  // Signals de estado local
  protected readonly well              = signal<Well | null>(null);
  protected readonly history           = signal<TransitionHistoryItem[]>([]);
  protected readonly isLoadingWell     = signal(true);
  protected readonly isLoadingHistory  = signal(true);
  protected readonly isTransitioning   = signal(false);
  protected readonly dialogVisible     = signal(false);
  protected readonly dialogAction      = signal<TransitionAction | null>(null);

  private wellId = '';

  ngOnInit(): void {
    this.wellId = this.route.snapshot.paramMap.get('id') ?? '';
    if (!this.wellId) {
      this.router.navigate(['/wells/manage']);
      return;
    }
    this.loadWell();
    this.loadHistory();
  }

  private loadWell(): void {
    this.isLoadingWell.set(true);
    this.wellsApi.getWell(this.wellId).subscribe({
      next: (well) => {
        this.well.set(well);
        this.isLoadingWell.set(false);
      },
      error: () => {
        this.isLoadingWell.set(false);
        this.router.navigate(['/wells/manage']);
      },
    });
  }

  private loadHistory(): void {
    this.isLoadingHistory.set(true);
    this.wellsApi.getWellHistory(this.wellId).subscribe({
      next: (items) => {
        this.history.set(items);
        this.isLoadingHistory.set(false);
      },
      error: () => this.isLoadingHistory.set(false),
    });
  }

  protected onTransitionRequested(event: { action: TransitionAction }): void {
    this.dialogAction.set(event.action);
    this.dialogVisible.set(true);
  }

  protected onTransitionConfirmed(event: { action: TransitionAction; comment?: string }): void {
    this.isTransitioning.set(true);
    this.dialogVisible.set(false);

    this.wellsApi.transitionWell(this.wellId, event.action, event.comment).subscribe({
      next: (result) => {
        // Actualiza el signal del pozo con el nuevo estado y uwi
        this.well.update((w) =>
          w ? { ...w, estado: result.estado as Well['estado'], uwi: result.uwi } : w,
        );
        this.loadHistory();
        this.messageService.add({
          severity: 'success',
          summary: 'Éxito',
          detail: this.locale.messages.transitionSuccess,
        });
        this.isTransitioning.set(false);
      },
      error: () => this.isTransitioning.set(false),
    });
  }

  protected onDialogCancel(): void {
    this.dialogVisible.set(false);
    this.dialogAction.set(null);
  }
}
