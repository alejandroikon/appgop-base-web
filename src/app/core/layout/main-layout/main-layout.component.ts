import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { Store } from '@ngrx/store';
import { toSignal } from '@angular/core/rxjs-interop';
import { Drawer } from 'primeng/drawer';
import { SidebarComponent } from '@core/layout/sidebar/sidebar.component';
import { TopHeaderComponent } from '@core/layout/top-header/top-header.component';
import { AuthActions, selectCurrentUser } from '@core/auth/store';
import { NAV_ITEMS, NavItem } from '@core/layout/sidebar/nav-items';
import { UserRole } from '@shared/models';
import { APP_LOCALE } from '@shared/locale/locale';

@Component({
  selector: 'app-main-layout',
  templateUrl: './main-layout.component.html',
  imports: [RouterOutlet, Drawer, SidebarComponent, TopHeaderComponent],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class MainLayoutComponent {
  private readonly store = inject(Store);

  readonly currentUser = toSignal(this.store.select(selectCurrentUser), { initialValue: null });
  readonly sidebarCollapsed = signal(false);
  readonly mobileDrawerOpen = signal(false);
  readonly locale = APP_LOCALE.topHeader;

  readonly filteredNavItems = computed(() => {
    const user = this.currentUser();
    if (!user) return [];
    return this.filterByRole(NAV_ITEMS, user.role);
  });

  onLogout(): void {
    this.store.dispatch(AuthActions.logout());
  }

  onToggleSidebar(): void {
    this.sidebarCollapsed.update(v => !v);
  }

  onToggleMobile(): void {
    this.mobileDrawerOpen.update(v => !v);
  }

  onMobileNavigated(): void {
    this.mobileDrawerOpen.set(false);
  }

  private filterByRole(items: NavItem[], role: UserRole): NavItem[] {
    return items
      .filter(item => item.roles.includes(role))
      .map(item => ({
        ...item,
        children: item.children
          ? item.children.filter(child => child.roles.includes(role))
          : undefined,
      }))
      .filter(item => !item.children || item.children.length > 0);
  }
}