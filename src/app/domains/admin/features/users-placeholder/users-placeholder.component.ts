import { ChangeDetectionStrategy, Component } from '@angular/core';
import { APP_LOCALE } from '@shared/locale/locale';

@Component({
  selector: 'app-users-placeholder',
  template: `
    <div class="flex items-center justify-center h-full">
      <div class="text-center">
        <i class="pi pi-wrench text-4xl text-text-secondary mb-4 block"></i>
        <h2 class="text-xl font-semibold text-text-primary mb-2">{{ locale.title }}</h2>
        <p class="text-text-secondary">{{ locale.subtitle }}</p>
      </div>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class UsersPlaceholderComponent {
  readonly locale = APP_LOCALE.placeholders;
}