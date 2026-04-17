import {
  ChangeDetectionStrategy,
  Component,
  computed,
  input,
  output,
  signal,
} from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DialogModule } from 'primeng/dialog';
import { ButtonModule } from 'primeng/button';
import { TextareaModule } from 'primeng/textarea';
import type { TransitionAction } from '@wells/models';
import { WELL_DETAIL_LOCALE } from '../../locale';

@Component({
  selector: 'app-transition-confirm-dialog',
  templateUrl: './transition-confirm-dialog.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [DialogModule, ButtonModule, TextareaModule, FormsModule],
})
export class TransitionConfirmDialogComponent {
  visible   = input(false);
  action    = input<TransitionAction | null>(null);
  isLoading = input(false);

  confirm = output<{ action: TransitionAction; comment?: string }>();
  cancel  = output();

  protected readonly locale = WELL_DETAIL_LOCALE;

  // Estado local del textarea de devolución
  protected readonly comment = signal('');

  protected readonly isDevolver = computed(() => this.action() === 'DEVOLVER');
  protected readonly canConfirm = computed(
    () => !this.isDevolver() || this.comment().trim().length >= 10,
  );
  protected readonly dialogHeader = computed(() =>
    this.isDevolver()
      ? this.locale.dialog.devolverTitle
      : this.locale.dialog.confirmTitle,
  );

  protected onConfirm(): void {
    const act = this.action();
    if (!act) return;
    this.confirm.emit(
      this.isDevolver()
        ? { action: act, comment: this.comment().trim() }
        : { action: act },
    );
    this.comment.set('');
  }

  protected onCancel(): void {
    this.comment.set('');
    this.cancel.emit();
  }

  protected onHide(): void {
    if (!this.isLoading()) {
      this.onCancel();
    }
  }
}
