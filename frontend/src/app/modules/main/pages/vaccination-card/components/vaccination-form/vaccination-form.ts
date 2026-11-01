import { ChangeDetectionStrategy, Component, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed, toSignal } from '@angular/core/rxjs-interop';
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
import { VaccineApiService } from '../../../../../../core/api/vaccine-api.service';
import { DoseDto, VaccinationCardEntryDto } from '../../../../../../core/models/vaccination.model';
import { VaccineDto } from '../../../../../../core/models/vaccine.model';
import { toApiDate } from '../../../../../../core/utils/api-date';
import { applyApiError, toApiError } from '../../../../../../core/utils/api-error';
import { orderedDoses, sameDose } from '../../../../../../core/utils/dose';
import { DateInput } from '../../../../../../shared/components/inputs/date-input/date-input';
import {
  SelectInput,
  SelectOption,
} from '../../../../../../shared/components/inputs/select-input/select-input';
import { SubmitButton } from '../../../../../../shared/components/submit-button/submit-button';
import { ToastService } from '../../../../../../shared/components/toast/toast.service';
import { doseLabel } from '../../../../../../shared/pipes/dose-label.pipe';

const FIRST_DOSE: DoseDto = { type: 'Primary', number: 1 };

export interface VaccinationFormData {
  personId: string;
  // The vaccines on the card, the only ones with a next expected dose from the API.
  cardVaccines: VaccinationCardEntryDto[];
  // The vaccine of the block that opened the form, or null from the button of the page.
  vaccineId: string | null;
}

@Component({
  selector: 'app-vaccination-form',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './vaccination-form.html',
  styleUrl: './vaccination-form.scss',
  imports: [
    ReactiveFormsModule,
    MatButton,
    MatDialogActions,
    MatDialogClose,
    MatDialogContent,
    MatDialogTitle,
    DateInput,
    SelectInput,
    SubmitButton,
  ],
})
export class VaccinationForm {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly vaccineApi = inject(VaccineApiService);
  private readonly vaccinationApi = inject(VaccinationApiService);
  private readonly toast = inject(ToastService);
  private readonly dialogRef = inject<MatDialogRef<VaccinationForm, boolean>>(MatDialogRef);
  private readonly data = inject<VaccinationFormData>(MAT_DIALOG_DATA);

  private readonly vaccines = signal<VaccineDto[]>([]);

  protected readonly sameDose = sameDose;
  protected readonly loading = signal(false);
  protected readonly form = this.fb.group({
    vaccineId: [this.data.vaccineId ?? '', Validators.required],
    dose: this.fb.control(this.expectedDose(this.data.vaccineId), Validators.required),
    appliedOn: this.fb.control<Date | null>(null, Validators.required),
  });

  protected readonly vaccineErrors = { required: 'A vacina é obrigatória.' };
  protected readonly doseErrors = { required: 'A dose é obrigatória.' };
  protected readonly appliedOnErrors = { required: 'A data de aplicação é obrigatória.' };

  private readonly vaccineId = toSignal(this.form.controls.vaccineId.valueChanges, {
    initialValue: this.form.controls.vaccineId.value,
  });

  protected readonly vaccineOptions = computed<SelectOption<string>[]>(() =>
    this.vaccines().map(({ id, name }) => ({ value: id, label: name })),
  );
  protected readonly doseOptions = computed<SelectOption<DoseDto | null>[]>(() => {
    const vaccine = this.vaccines().find(({ id }) => id === this.vaccineId());

    return vaccine
      ? orderedDoses(vaccine).map((dose) => ({ value: dose, label: doseLabel(dose, vaccine) }))
      : [];
  });

  constructor() {
    this.form.controls.vaccineId.valueChanges
      .pipe(takeUntilDestroyed())
      .subscribe((vaccineId) => this.form.controls.dose.setValue(this.expectedDose(vaccineId)));

    this.vaccineApi
      .findAll()
      .pipe(takeUntilDestroyed())
      .subscribe({
        next: ({ data }) => this.vaccines.set(data),
        // The registration depends on the list (TechSpec §11.3).
        error: (error: unknown) => {
          this.toast.error(toApiError(error).message);
          this.dialogRef.close();
        },
      });
  }

  protected submit(): void {
    const { vaccineId, dose, appliedOn } = this.form.getRawValue();

    if (this.form.invalid || dose === null || appliedOn === null) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    // The card reloads with the result of the dialog, which would be lost if it closed now.
    this.dialogRef.disableClose = true;
    this.vaccinationApi
      .register(this.data.personId, { vaccineId, dose, appliedOn: toApiDate(appliedOn) })
      .pipe(
        finalize(() => {
          this.loading.set(false);
          this.dialogRef.disableClose = false;
        }),
      )
      .subscribe({
        next: () => {
          this.toast.success('Vacinação registrada com sucesso.');
          this.dialogRef.close(true);
        },
        error: (error: unknown) => applyApiError(error, this.form, this.toast),
      });
  }

  private expectedDose(vaccineId: string | null): DoseDto | null {
    if (!vaccineId) {
      return null;
    }

    const entry = this.data.cardVaccines.find((vaccine) => vaccine.vaccineId === vaccineId);
    // A vaccine off the card has no vaccination, so it starts at the first dose (RN10, RN17).
    return entry ? entry.nextExpectedDose : FIRST_DOSE;
  }
}
