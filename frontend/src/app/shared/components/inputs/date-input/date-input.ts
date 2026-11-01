import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import {
  MatDatepicker,
  MatDatepickerInput,
  MatDatepickerIntl,
  MatDatepickerToggle,
} from '@angular/material/datepicker';
import { MatError, MatFormField, MatLabel, MatSuffix } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';
import { fieldError } from '../field-error';
import { DatepickerIntl } from './datepicker-intl';

const INVALID_DATE_MESSAGE = 'Informe a data no formato dd/mm/aaaa.';

@Component({
  selector: 'app-date-input',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './date-input.html',
  styleUrl: '../input.scss',
  providers: [{ provide: MatDatepickerIntl, useClass: DatepickerIntl }],
  imports: [
    ReactiveFormsModule,
    MatDatepicker,
    MatDatepickerInput,
    MatDatepickerToggle,
    MatError,
    MatFormField,
    MatInput,
    MatLabel,
    MatSuffix,
  ],
})
export class DateInput {
  public readonly label = input.required<string>();
  public readonly control = input.required<FormControl<Date | null>>();
  // The messages come from the page, because this field serves any date.
  public readonly errorMessages = input<Record<string, string>>({});

  // A method: the errors of a control are not a signal, so a computed would keep the first value.
  protected errorMessage(): string | null {
    // First: text that is not a date also leaves the value empty, which a required field refuses.
    return fieldError(this.control(), {
      matDatepickerParse: INVALID_DATE_MESSAGE,
      ...this.errorMessages(),
    });
  }
}
