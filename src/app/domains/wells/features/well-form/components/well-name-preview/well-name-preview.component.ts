import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TagModule } from 'primeng/tag';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { WELL_FORM_LOCALE } from '../../locale';

@Component({
  selector: 'app-well-name-preview',
  templateUrl: './well-name-preview.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TagModule, ProgressSpinnerModule],
})
export class WellNamePreviewComponent {
  nombrePozo = input<string | null>(null);
  available  = input<boolean | null>(null);
  loading    = input(false);

  protected readonly locale = WELL_FORM_LOCALE.preview;
}
