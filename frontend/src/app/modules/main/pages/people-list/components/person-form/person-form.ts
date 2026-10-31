import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButton } from '@angular/material/button';
import {
  MatDialogActions,
  MatDialogClose,
  MatDialogContent,
  MatDialogRef,
  MatDialogTitle,
} from '@angular/material/dialog';
import { finalize } from 'rxjs';
import { PersonApiService } from '../../../../../../core/api/person-api.service';
import { applyApiError } from '../../../../../../core/utils/api-error';
import { COMPLETE_CPF_PATTERN } from '../../../../../../core/utils/cpf-mask';
import { CpfInput } from '../../../../../../shared/components/inputs/cpf-input/cpf-input';
import { TextInput } from '../../../../../../shared/components/inputs/text-input/text-input';
import { SubmitButton } from '../../../../../../shared/components/submit-button/submit-button';
import { ToastService } from '../../../../../../shared/components/toast/toast.service';

const NAME_LENGTH_MESSAGE = 'O nome deve ter entre 3 e 150 caracteres.';

@Component({
  selector: 'app-person-form',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './person-form.html',
  styleUrl: './person-form.scss',
  imports: [
    ReactiveFormsModule,
    MatButton,
    MatDialogActions,
    MatDialogClose,
    MatDialogContent,
    MatDialogTitle,
    CpfInput,
    SubmitButton,
    TextInput,
  ],
})
export class PersonForm {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly personApi = inject(PersonApiService);
  private readonly toast = inject(ToastService);
  private readonly dialogRef = inject<MatDialogRef<PersonForm, boolean>>(MatDialogRef);

  protected readonly loading = signal(false);
  protected readonly form = this.fb.group({
    name: ['', [Validators.required, Validators.minLength(3), Validators.maxLength(150)]],
    cpf: ['', [Validators.required, Validators.pattern(COMPLETE_CPF_PATTERN)]],
  });

  protected readonly nameErrors = {
    required: 'O nome é obrigatório.',
    minlength: NAME_LENGTH_MESSAGE,
    maxlength: NAME_LENGTH_MESSAGE,
  };

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    // The list reloads with the result of the dialog, which would be lost if it closed now.
    this.dialogRef.disableClose = true;
    this.personApi
      .create(this.form.getRawValue())
      .pipe(
        finalize(() => {
          this.loading.set(false);
          this.dialogRef.disableClose = false;
        }),
      )
      .subscribe({
        next: () => {
          this.toast.success('Pessoa cadastrada com sucesso.');
          this.dialogRef.close(true);
        },
        error: (error: unknown) => applyApiError(error, this.form, this.toast),
      });
  }
}
