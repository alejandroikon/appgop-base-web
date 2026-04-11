import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { Toolbar } from 'primeng/toolbar';
import { Avatar } from 'primeng/avatar';
import { Badge } from 'primeng/badge';
import { AuthUser } from '@shared/models';
import { APP_LOCALE } from '@shared/locale/locale';

@Component({
  selector: 'app-top-header',
  templateUrl: './top-header.component.html',
  imports: [Toolbar, Avatar, Badge],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class TopHeaderComponent {
  readonly user = input.required<AuthUser | null>();

  readonly toggleSidebar = output<void>();
  readonly logout = output<void>();

  readonly locale = APP_LOCALE.topHeader;

  readonly initials = computed(() => {
    const name = this.user()?.name ?? '';
    return name
      .split(' ')
      .map(w => w[0])
      .join('')
      .toUpperCase()
      .slice(0, 2);
  });
}
