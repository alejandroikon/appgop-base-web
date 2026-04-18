import {
  ChangeDetectionStrategy,
  Component,
  OnInit,
  inject,
  signal,
} from '@angular/core';
import { Router, ActivatedRoute, RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { Store } from '@ngrx/store';
import { MessageService } from 'primeng/api';
import { CardModule } from 'primeng/card';
import { ButtonModule } from 'primeng/button';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { ToastModule } from 'primeng/toast';
import { TagModule } from 'primeng/tag';
import { WellsApiService } from '@wells/services';
import type { Well } from '@wells/models';
import { selectCurrentUser } from '@core/auth/store/auth.selectors';
import { WellStatusBadgeComponent } from '../../components/well-status-badge/well-status-badge.component';
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
    TagModule,
    WellStatusBadgeComponent,
  ],
})
export class WellDetailComponent implements OnInit {
  private readonly wellsApi        = inject(WellsApiService);
  private readonly route           = inject(ActivatedRoute);
  private readonly router          = inject(Router);
  private readonly store           = inject(Store);
  private readonly messageService  = inject(MessageService);

  protected readonly locale = WELL_DETAIL_LOCALE;

  protected readonly currentUser = toSignal(this.store.select(selectCurrentUser));

  protected readonly well          = signal<Well | null>(null);
  protected readonly isLoadingWell = signal(true);

  private wellId = '';

  ngOnInit(): void {
    this.wellId = this.route.snapshot.paramMap.get('id') ?? '';
    if (!this.wellId) {
      this.router.navigate(['/wells/manage']);
      return;
    }
    this.loadWell();
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

  protected onEdit(): void {
    this.router.navigate(['/wells', this.wellId, 'edit']);
  }

  protected onDelete(): void {
    this.wellsApi.deleteWell(this.wellId).subscribe({
      next: () => {
        this.messageService.add({
          severity: 'success',
          summary: this.locale.actions.deleted,
          detail:  this.locale.messages.deleteSuccess,
        });
        this.router.navigate(['/wells/manage']);
      },
    });
  }
}
