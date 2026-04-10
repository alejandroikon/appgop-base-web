import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { environment } from '@env/environment';
import { APP_LOCALE } from '@shared/locale/locale';

@Component({
  selector: 'app-auth-layout',
  templateUrl: './auth-layout.component.html',
  imports: [RouterOutlet],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class AuthLayoutComponent {
  readonly loginBgUrl = environment.loginBgUrl;
  readonly locale = APP_LOCALE.brand;
}
