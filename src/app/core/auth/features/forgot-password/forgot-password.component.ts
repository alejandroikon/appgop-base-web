import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { FormControl, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { AuthService } from '@core/auth/auth.service';
import { FORGOT_PASSWORD_LOCALE } from './locale';

@Component({
  selector: 'app-forgot-password',
  templateUrl: './forgot-password.component.html',
  imports: [ReactiveFormsModule, RouterLink],
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ForgotPasswordComponent {
  private readonly authService = inject(AuthService);

  readonly locale = FORGOT_PASSWORD_LOCALE;
  readonly isLoading = signal(false);
  readonly isConfirmed = signal(false);

  readonly forgotForm = new FormGroup({
    email: new FormControl('', {
      validators: [Validators.required, Validators.email],
      nonNullable: true,
    }),
  });

  onSubmit(): void {
    if (this.forgotForm.invalid) {
      this.forgotForm.markAllAsTouched();
      return;
    }

    const email = this.forgotForm.controls.email.value.trim().toLowerCase();

    this.isLoading.set(true);
    this.authService.forgotPassword(email).subscribe({
      next: () => {
        this.isLoading.set(false);
        this.isConfirmed.set(true);
      },
      error: () => {
        this.isLoading.set(false);
        this.isConfirmed.set(true);
      },
    });
  }
}
