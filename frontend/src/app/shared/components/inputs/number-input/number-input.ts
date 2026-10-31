import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatError, MatFormField, MatLabel } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';
import { fieldError } from '../field-error';

@Component({
  selector: 'app-number-input',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './number-input.html',
  styleUrl: '../input.scss',
  imports: [ReactiveFormsModule, MatFormField, MatLabel, MatError, MatInput],
})
export class NumberInput {
  public readonly label = input.required<string>();
  public readonly control = input.required<FormControl<number>>();
  public readonly min = input<number | null>(null);
  public readonly max = input<number | null>(null);
  // The messages come from the page, because this field serves any quantity.
  public readonly errorMessages = input<Record<string, string>>({});

  // A method: the errors of a control are not a signal, so a computed would keep the first value.
  protected errorMessage(): string | null {
    return fieldError(this.control(), this.errorMessages());
  }
}
