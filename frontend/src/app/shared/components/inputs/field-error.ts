import { AbstractControl } from '@angular/forms';
import { API_ERROR_KEY } from '../../../core/utils/api-error';

// The message of the API comes first, and the messages of the field follow the order of their keys.
export function fieldError(
  control: AbstractControl,
  messages: Record<string, string>,
): string | null {
  const { errors } = control;

  if (errors === null) {
    return null;
  }

  if (errors[API_ERROR_KEY]) {
    return errors[API_ERROR_KEY];
  }

  const validator = Object.keys(messages).find((key) => key in errors);
  return validator ? messages[validator] : null;
}
