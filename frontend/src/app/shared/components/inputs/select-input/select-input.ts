import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatOption } from '@angular/material/core';
import { MatError, MatFormField, MatLabel } from '@angular/material/form-field';
import { MatSelect } from '@angular/material/select';
import { fieldError } from '../field-error';

export interface SelectOption<T> {
  value: T;
  label: string;
}

@Component({
  selector: 'app-select-input',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './select-input.html',
  styleUrl: '../input.scss',
  imports: [ReactiveFormsModule, MatError, MatFormField, MatLabel, MatOption, MatSelect],
})
export class SelectInput<T> {
  public readonly label = input.required<string>();
  public readonly control = input.required<FormControl<T>>();
  public readonly options = input.required<SelectOption<T>[]>();
  // An object as the value needs its own comparison, because the options may be rebuilt.
  public readonly compareWith = input<(first: T, second: T) => boolean>(
    (first, second) => first === second,
  );
  // The messages come from the page, because this field serves any list.
  public readonly errorMessages = input<Record<string, string>>({});

  // A method: the errors of a control are not a signal, so a computed would keep the first value.
  protected errorMessage(): string | null {
    return fieldError(this.control(), this.errorMessages());
  }
}
