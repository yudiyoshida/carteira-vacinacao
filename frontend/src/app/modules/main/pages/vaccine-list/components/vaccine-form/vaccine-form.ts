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
import { VaccineApiService } from '../../../../../../core/api/vaccine-api.service';
import { applyApiError } from '../../../../../../core/utils/api-error';
import { NumberInput } from '../../../../../../shared/components/inputs/number-input/number-input';
import { TextInput } from '../../../../../../shared/components/inputs/text-input/text-input';
import { SubmitButton } from '../../../../../../shared/components/submit-button/submit-button';
import { ToastService } from '../../../../../../shared/components/toast/toast.service';

const NAME_LENGTH_MESSAGE = 'O nome deve ter entre 2 e 100 caracteres.';
const PRIMARY_DOSES_MESSAGE = 'A vacina deve ter de 1 a 10 doses primárias.';
const BOOSTER_DOSES_MESSAGE = 'A vacina deve ter de 0 a 5 doses de reforço.';

@Component({
  selector: 'app-vaccine-form',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './vaccine-form.html',
  styleUrl: './vaccine-form.scss',
  imports: [
    ReactiveFormsModule,
    MatButton,
    MatDialogActions,
    MatDialogClose,
    MatDialogContent,
    MatDialogTitle,
    NumberInput,
    SubmitButton,
    TextInput,
  ],
})
export class VaccineForm {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly vaccineApi = inject(VaccineApiService);
  private readonly toast = inject(ToastService);
  private readonly dialogRef = inject<MatDialogRef<VaccineForm, boolean>>(MatDialogRef);

  protected readonly loading = signal(false);
  protected readonly form = this.fb.group({
    name: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
    primaryDoses: [1, [Validators.required, Validators.min(1), Validators.max(10)]],
    boosterDoses: [0, [Validators.required, Validators.min(0), Validators.max(5)]],
  });

  protected readonly nameErrors = {
    required: 'O nome é obrigatório.',
    minlength: NAME_LENGTH_MESSAGE,
    maxlength: NAME_LENGTH_MESSAGE,
  };
  protected readonly primaryDosesErrors = {
    required: PRIMARY_DOSES_MESSAGE,
    min: PRIMARY_DOSES_MESSAGE,
    max: PRIMARY_DOSES_MESSAGE,
  };
  protected readonly boosterDosesErrors = {
    required: BOOSTER_DOSES_MESSAGE,
    min: BOOSTER_DOSES_MESSAGE,
    max: BOOSTER_DOSES_MESSAGE,
  };

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    // The list reloads with the result of the dialog, which would be lost if it closed now.
    this.dialogRef.disableClose = true;
    this.vaccineApi
      .create(this.form.getRawValue())
      .pipe(
        finalize(() => {
          this.loading.set(false);
          this.dialogRef.disableClose = false;
        }),
      )
      .subscribe({
        next: () => {
          this.toast.success('Vacina cadastrada com sucesso.');
          this.dialogRef.close(true);
        },
        error: (error: unknown) => applyApiError(error, this.form, this.toast),
      });
  }
}
