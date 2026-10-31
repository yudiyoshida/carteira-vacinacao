import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';
import { AuthApiService } from '../../../../core/api/auth-api.service';
import { AuthSessionService } from '../../../../core/auth/auth-session.service';
import { RoutesEnum } from '../../../../core/routes/routes.enum';
import { routePath } from '../../../../core/routes/routes.util';
import { applyApiError } from '../../../../core/utils/api-error';
import { EmailInput } from '../../../../shared/components/inputs/email-input/email-input';
import { PasswordInput } from '../../../../shared/components/inputs/password-input/password-input';
import { SubmitButton } from '../../../../shared/components/submit-button/submit-button';
import { ToastService } from '../../../../shared/components/toast/toast.service';

@Component({
  selector: 'app-login',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './login.html',
  styleUrl: './login.scss',
  imports: [ReactiveFormsModule, EmailInput, PasswordInput, SubmitButton],
})
export class Login {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly authApi = inject(AuthApiService);
  private readonly session = inject(AuthSessionService);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);

  protected readonly loading = signal(false);
  protected readonly form = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]],
  });

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    // A login has no success message that replaces the error of the previous attempt.
    this.toast.dismiss();
    this.loading.set(true);
    this.authApi
      .login(this.form.getRawValue())
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: ({ accessToken }) => {
          this.session.login(accessToken);
          void this.router.navigate([routePath(RoutesEnum.PEOPLE)]);
        },
        error: (error: unknown) => applyApiError(error, this.form, this.toast),
      });
  }
}
