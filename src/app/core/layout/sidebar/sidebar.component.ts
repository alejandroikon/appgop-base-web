import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { PanelMenu } from 'primeng/panelmenu';
import { Avatar } from 'primeng/avatar';
import { Tooltip } from 'primeng/tooltip';
import { MenuItem } from 'primeng/api';
import { AuthUser } from '@shared/models';
import { APP_LOCALE } from '@shared/locale/locale';
import { NavItem } from './nav-items';

@Component({
  selector: 'app-sidebar',
  templateUrl: './sidebar.component.html',
  styleUrl: './sidebar.component.css',
  imports: [PanelMenu, Avatar, Tooltip, RouterLink, RouterLinkActive],
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'flex-1 flex flex-col min-h-0' },
})
export class SidebarComponent {
  readonly navItems = input.required<NavItem[]>();
  readonly collapsed = input(false);
  readonly user = input.required<AuthUser | null>();

  readonly logout = output<void>();
  readonly navigated = output<void>();

  readonly locale = APP_LOCALE.sidebar;
  readonly topHeaderLocale = APP_LOCALE.topHeader;
  readonly brandLocale = APP_LOCALE.brand;

  readonly initials = computed(() => {
    const name = this.user()?.name ?? '';
    return name
      .split(' ')
      .map(w => w[0])
      .join('')
      .toUpperCase()
      .slice(0, 2);
  });

  readonly menuModel = computed<MenuItem[]>(() =>
    this.navItems().map(item => this.toMenuItem(item))
  );

  getLabel(key: string): string {
    return (this.locale as Record<string, string>)[key] ?? key;
  }

  onNavigated(): void {
    this.navigated.emit();
  }

  private toMenuItem(item: NavItem): MenuItem {
    const label = this.locale[item.key as keyof typeof this.locale] as string;

    if (item.children?.length) {
      return {
        label,
        icon: item.icon,
        items: item.children.map(child => ({
          label: this.locale[child.key as keyof typeof this.locale] as string,
          routerLink: child.route,
          command: () => this.onNavigated(),
        })),
      };
    }

    return {
      label,
      icon: item.icon,
      routerLink: item.route,
      command: () => this.onNavigated(),
    };
  }
}
