import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatError, MatFormField, MatLabel } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';
import { fieldError } from '../field-error';

const ERROR_MESSAGES = {
  required: 'O e-mail é obrigatório.',
  email: 'O e-mail informado é inválido.',
};

@Component({
  selector: 'app-email-input',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './email-input.html',
  styleUrl: '../input.scss',
  imports: [ReactiveFormsModule, MatFormField, MatLabel, MatError, MatInput],
})
export class EmailInput {
  public readonly label = input.required<string>();
  public readonly control = input.required<FormControl<string>>();

  // A method: the errors of a control are not a signal, so a computed would keep the first value.
  protected errorMessage(): string | null {
    return fieldError(this.control(), ERROR_MESSAGES);
  }
}
