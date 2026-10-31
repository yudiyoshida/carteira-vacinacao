import { HttpErrorResponse } from '@angular/common/http';
import { AbstractControl } from '@angular/forms';
import { ProblemDetails } from '../models/problem-details.model';

// Key of the control error that holds the message sent by the API.
export const API_ERROR_KEY = 'api';

const UNEXPECTED_ERROR_MESSAGE = 'Ocorreu um erro inesperado.';
const CONNECTION_ERROR_MESSAGE = 'Não foi possível conectar ao servidor. Tente de novo.';

export interface ApiError {
  status: number;
  // Null when the response did not come from the API, as in a connection failure.
  code: string | null;
  message: string;
  fieldErrors: Record<string, string[]>;
}

export function toApiError(error: unknown): ApiError {
  if (!(error instanceof HttpErrorResponse)) {
    return { status: 0, code: null, message: UNEXPECTED_ERROR_MESSAGE, fieldErrors: {} };
  }

  if (isProblemDetails(error.error)) {
    return {
      status: error.status,
      code: error.error.code,
      message: error.error.detail,
      fieldErrors: error.error.errors ?? {},
    };
  }

  return {
    status: error.status,
    code: null,
    message: error.status === 0 ? CONNECTION_ERROR_MESSAGE : UNEXPECTED_ERROR_MESSAGE,
    fieldErrors: {},
  };
}

// The errors of a field go to its control, and every other error goes to the toast (TechSpec §11.3).
export function applyApiError(
  error: unknown,
  form: AbstractControl,
  toast: { error(message: string): void },
): void {
  const { message, fieldErrors } = toApiError(error);
  const fields = Object.entries(fieldErrors);

  if (fields.length === 0) {
    toast.error(message);
    return;
  }

  const messagesWithoutField: string[] = [];
  for (const [field, messages] of fields) {
    const control = form.get(field);
    if (control) {
      control.setErrors({ [API_ERROR_KEY]: messages.join(' ') });
      control.markAsTouched();
    } else {
      messagesWithoutField.push(...messages);
    }
  }

  if (messagesWithoutField.length > 0) {
    toast.error(messagesWithoutField.join(' '));
  }
}

function isProblemDetails(body: unknown): body is ProblemDetails {
  return typeof body === 'object' && body !== null && 'code' in body && 'detail' in body;
}
