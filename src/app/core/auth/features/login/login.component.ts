import { ChangeDetectionStrategy, Component, inject, OnInit, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { Store } from '@ngrx/store';
import { MessageService } from 'primeng/api';
import { AuthActions, selectAuthError, selectAuthIsLoading } from '@core/auth/store';
import { LOGIN_LOCALE } from './locale';
import { APP_LOCALE } from '@shared/locale/locale';

@Component({
  selector: 'app-login',
  templateUrl: './login.component.html',
  imports: [ReactiveFormsModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class LoginComponent implements OnInit {
  private readonly store = inject(Store);
  private readonly route = inject(ActivatedRoute);
  private readonly messageService = inject(MessageService);

  readonly locale = LOGIN_LOCALE;
  readonly isLoading = this.store.selectSignal(selectAuthIsLoading);
  readonly authError = this.store.selectSignal(selectAuthError);
  readonly showPassword = signal(false);

  readonly loginForm = new FormGroup({
    email: new FormControl('', {
      validators: [Validators.required, Validators.email],
      nonNullable: true,
    }),
    password: new FormControl('', {
      validators: [Validators.required],
      nonNullable: true,
    }),
  });

  ngOnInit(): void {
    const reason = this.route.snapshot.queryParamMap.get('reason');
    if (reason === 'session_expired') {
      this.messageService.add({
        severity: 'warn',
        summary: APP_LOCALE.auth.sessionExpiredTitle,
        detail: APP_LOCALE.auth.sessionExpiredMessage,
      });
    }
  }

  onSubmit(): void {
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }

    const email = this.loginForm.controls.email.value.trim().toLowerCase();
    const password = this.loginForm.controls.password.value;

    this.store.dispatch(AuthActions.login({ credentials: { email, password } }));
  }

  onFieldFocus(): void {
    if (this.authError()) {
      this.store.dispatch(AuthActions.clearAuthError());
    }
  }

  togglePassword(): void {
    this.showPassword.update((v) => !v);
  }
}
