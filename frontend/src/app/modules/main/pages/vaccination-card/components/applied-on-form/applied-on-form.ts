import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { NonNullableFormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButton } from '@angular/material/button';
import {
  MAT_DIALOG_DATA,
  MatDialogActions,
  MatDialogClose,
  MatDialogContent,
  MatDialogRef,
  MatDialogTitle,
} from '@angular/material/dialog';
import { finalize } from 'rxjs';
import { VaccinationApiService } from '../../../../../../core/api/vaccination-api.service';
import { fromApiDate, toApiDate } from '../../../../../../core/utils/api-date';
import { applyApiError } from '../../../../../../core/utils/api-error';
import { DateInput } from '../../../../../../shared/components/inputs/date-input/date-input';
import { SubmitButton } from '../../../../../../shared/components/submit-button/submit-button';
import { ToastService } from '../../../../../../shared/components/toast/toast.service';

export interface AppliedOnFormData {
  personId: string;
  vaccinationId: string;
  vaccineName: string;
  doseLabel: string;
  // In yyyy-MM-dd.
  appliedOn: string;
}

@Component({
  selector: 'app-applied-on-form',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './applied-on-form.html',
  styleUrl: './applied-on-form.scss',
  imports: [
    ReactiveFormsModule,
    MatButton,
    MatDialogActions,
    MatDialogClose,
    MatDialogContent,
    MatDialogTitle,
    DateInput,
    SubmitButton,
  ],
})
export class AppliedOnForm {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly vaccinationApi = inject(VaccinationApiService);
  private readonly toast = inject(ToastService);
  private readonly dialogRef = inject<MatDialogRef<AppliedOnForm, boolean>>(MatDialogRef);

  protected readonly data = inject<AppliedOnFormData>(MAT_DIALOG_DATA);
  protected readonly loading = signal(false);
  protected readonly form = this.fb.group({
    appliedOn: this.fb.control<Date | null>(fromApiDate(this.data.appliedOn), Validators.required),
  });

  protected readonly appliedOnErrors = { required: 'A data de aplicação é obrigatória.' };

  protected submit(): void {
    const { appliedOn } = this.form.getRawValue();

    if (this.form.invalid || appliedOn === null) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    // The card reloads with the result of the dialog, which would be lost if it closed now.
    this.dialogRef.disableClose = true;
    this.vaccinationApi
      .correctDate(this.data.personId, this.data.vaccinationId, {
        appliedOn: toApiDate(appliedOn),
      })
      .pipe(
        finalize(() => {
          this.loading.set(false);
          this.dialogRef.disableClose = false;
        }),
      )
      .subscribe({
        next: ({ message }) => {
          this.toast.success(message);
          this.dialogRef.close(true);
        },
        error: (error: unknown) => applyApiError(error, this.form, this.toast),
      });
  }
}
