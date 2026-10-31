import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { FormControl, ReactiveFormsModule } from '@angular/forms';
import { MatError, MatFormField, MatLabel } from '@angular/material/form-field';
import { MatInput } from '@angular/material/input';
import { applyCpfMask, caretAfterCpfMask } from '../../../../core/utils/cpf-mask';
import { fieldError } from '../field-error';

const ERROR_MESSAGES = {
  required: 'O CPF é obrigatório.',
  pattern: 'O CPF informado é inválido.',
};

@Component({
  selector: 'app-cpf-input',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './cpf-input.html',
  styleUrl: '../input.scss',
  imports: [ReactiveFormsModule, MatFormField, MatLabel, MatError, MatInput],
})
export class CpfInput {
  public readonly label = input.required<string>();
  public readonly control = input.required<FormControl<string>>();

  // A method: the errors of a control are not a signal, so a computed would keep the first value.
  protected errorMessage(): string | null {
    return fieldError(this.control(), ERROR_MESSAGES);
  }

  protected applyMask(field: HTMLInputElement): void {
    const caret = caretAfterCpfMask(field.value, field.selectionStart ?? field.value.length);

    this.control().setValue(applyCpfMask(field.value));
    // Rewriting the value sends the caret to the end, which would disturb a fix in the middle.
    field.setSelectionRange(caret, caret);
  }
}
