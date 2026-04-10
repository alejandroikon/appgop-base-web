import { ChangeDetectionStrategy, Component } from '@angular/core';
import { APP_LOCALE } from '@shared/locale/locale';

@Component({
  selector: 'app-home',
  template: `
    <div class="flex items-center justify-center h-screen bg-gray-50">
      <div class="text-center">
        <h1 class="text-3xl font-bold text-gray-800 mb-2">{{ locale.title }}</h1>
        <p class="text-gray-500">{{ locale.subtitle }}</p>
      </div>
    </div>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class HomeComponent {
  readonly locale = APP_LOCALE.home;
}
