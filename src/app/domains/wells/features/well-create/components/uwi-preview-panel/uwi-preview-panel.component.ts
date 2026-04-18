import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { TagModule } from 'primeng/tag';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import type { UwiPreviewComponentsDTO } from '@wells/models';
import { WELL_CREATE_LOCALE } from '../../locale';

@Component({
  selector: 'app-uwi-preview-panel',
  templateUrl: './uwi-preview-panel.component.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
  imports: [TagModule, ProgressSpinnerModule],
})
export class UwiPreviewPanelComponent {
  uwi        = input<string | null>(null);
  components = input<UwiPreviewComponentsDTO | null>(null);
  isUnique   = input<boolean | null>(null);
  isChecking = input(false);

  protected readonly locale = WELL_CREATE_LOCALE.uwi;
}
