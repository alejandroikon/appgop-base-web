import { ChangeDetectionStrategy, Component, OnInit, inject, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { MessageService, ConfirmationService } from 'primeng/api';
import { TableLazyLoadEvent } from 'primeng/table';
import { WellsApiService } from '@wells/services';
import type { Contrato, WellListItem, WellsQueryParams, WellStatus } from '@wells/models';
import { WELL_STATUS_OPTIONS } from '@wells/models';
import { WELL_MANAGE_LOCALE } from './locale';
import { WellStatusBadgeComponent } from '../../components/well-status-badge/well-status-badge.component';

// UI imports — PrimeNG 21 (componentes standalone)
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { TableModule } from 'primeng/table';
import { ButtonModule } from 'primeng/button';
import { SelectModule } from 'primeng/select';
import { InputTextModule } from 'primeng/inputtext';
import { TagModule } from 'primeng/tag';
import { ConfirmDialogModule } from 'primeng/confirmdialog';
import { ToastModule } from 'primeng/toast';
import { SkeletonModule } from 'primeng/skeleton';
import { IconFieldModule } from 'primeng/iconfield';
import { InputIconModule } from 'primeng/inputicon';

@Component({
  selector: 'app-well-manage',
  templateUrl: './well-manage.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [ConfirmationService],
  imports: [
    CommonModule,
    FormsModule,
    RouterLink,
    TableModule,
    ButtonModule,
    SelectModule,
    InputTextModule,
    TagModule,
    ConfirmDialogModule,
    ToastModule,
    SkeletonModule,
    IconFieldModule,
    InputIconModule,
    WellStatusBadgeComponent,
  ],
})
export class WellManageComponent implements OnInit {
  private readonly wellsApi             = inject(WellsApiService);
  private readonly router               = inject(Router);
  private readonly messageService       = inject(MessageService);
  private readonly confirmationService  = inject(ConfirmationService);

  protected readonly locale = WELL_MANAGE_LOCALE;

  // ─── Opciones de filtro de estado ─────────────────────────────────────────
  protected readonly estadoOpts = [
    ...WELL_STATUS_OPTIONS,
  ];

  // ─── Estado de UI ─────────────────────────────────────────────────────────
  readonly wells        = signal<WellListItem[]>([]);
  readonly totalRecords = signal(0);
  readonly isLoading    = signal(true);
  readonly filters      = signal<WellsQueryParams>({ page: 1, pageSize: 20 });
  readonly contratos    = signal<Contrato[]>([]);

  protected searchTerm = '';
  private searchTimer: ReturnType<typeof setTimeout> | null = null;
  protected selectedContratoId: number | null = null;
  protected selectedEstado: WellStatus | null = null;

  ngOnInit(): void {
    this.loadContratos();
    this.loadWells();
  }

  private loadContratos(): void {
    this.wellsApi.getContratos().subscribe({ next: (data) => this.contratos.set(data) });
  }

  loadWells(): void {
    this.isLoading.set(true);
    this.wellsApi.getWells(this.filters()).subscribe({
      next: (res) => {
        this.wells.set(res.items);
        this.totalRecords.set(res.total);
        this.isLoading.set(false);
      },
      error: () => this.isLoading.set(false),
    });
  }

  onLazyLoad(event: TableLazyLoadEvent): void {
    const page     = event.first !== undefined && event.rows ? Math.floor(event.first / event.rows) + 1 : 1;
    const pageSize = event.rows ?? 20;
    const sortBy   = Array.isArray(event.sortField) ? event.sortField[0] : (event.sortField ?? undefined);
    const sortDir: 'asc' | 'desc' | undefined = event.sortOrder === -1 ? 'desc' : event.sortOrder === 1 ? 'asc' : undefined;

    this.filters.update((f) => ({ ...f, page, pageSize, sortBy, sortDir }));
    this.loadWells();
  }

  onSearch(term: string): void {
    if (this.searchTimer) clearTimeout(this.searchTimer);
    this.searchTimer = setTimeout(() => {
      this.filters.update((f) => ({ ...f, search: term || undefined, page: 1 }));
      this.loadWells();
    }, 400);
  }

  onContratoFilterChange(contratoId: number | null): void {
    this.filters.update((f) => ({ ...f, contratoId: contratoId ?? undefined, page: 1 }));
    this.loadWells();
  }

  onEstadoFilterChange(estado: WellStatus | null): void {
    this.filters.update((f) => ({ ...f, estado: estado ?? undefined, page: 1 }));
    this.loadWells();
  }

  onCreate(): void {
    this.router.navigate(['/wells/create']);
  }

  onEdit(id: string): void {
    this.router.navigate(['/wells', id, 'edit']);
  }

  onDelete(well: WellListItem): void {
    this.confirmationService.confirm({
      message: this.locale.messages.deleteConfirm,
      header:  this.locale.actions.delete,
      icon:    'pi pi-exclamation-triangle',
      accept:  () => {
        this.wellsApi.deleteWell(well.id).subscribe({
          next: () => {
            this.messageService.add({ severity: 'success', summary: 'Eliminado', detail: this.locale.messages.deleteSuccess });
            this.loadWells();
          },
        });
      },
    });
  }

  formatDate(isoDate: string): string {
    return new Date(isoDate).toLocaleDateString('es-CO', {
      day:   '2-digit',
      month: '2-digit',
      year:  'numeric',
    });
  }
}
