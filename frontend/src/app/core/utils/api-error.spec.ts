import { HttpErrorResponse } from '@angular/common/http';
import { FormControl, FormGroup } from '@angular/forms';
import { ProblemDetails } from '../models/problem-details.model';
import { API_ERROR_KEY, applyApiError, toApiError } from './api-error';

function makeProblem(overrides: Partial<ProblemDetails> = {}): HttpErrorResponse {
  const body: ProblemDetails = {
    title: 'Conflito',
    status: 409,
    detail: 'Já existe uma vacina cadastrada com este nome.',
    instance: '/api/v1/vaccines',
    code: 'vaccine.name_already_registered',
    traceId: '00-abc',
    ...overrides,
  };
  return new HttpErrorResponse({ status: body.status, error: body });
}

function makeValidationProblem(errors: Record<string, string[]>): HttpErrorResponse {
  return makeProblem({
    title: 'Requisição inválida',
    status: 400,
    detail: 'Um ou mais campos são inválidos.',
    code: 'validation.failed',
    errors,
  });
}

describe('toApiError', () => {
  it('reads the status, code and detail of a ProblemDetails', () => {
    expect(toApiError(makeProblem())).toEqual({
      status: 409,
      code: 'vaccine.name_already_registered',
      message: 'Já existe uma vacina cadastrada com este nome.',
      fieldErrors: {},
    });
  });

  it('reads the field errors of a validation failure', () => {
    const errors = { name: ['O nome é obrigatório.'] };

    expect(toApiError(makeValidationProblem(errors)).fieldErrors).toEqual(errors);
  });

  it('reports a connection failure when the request does not reach the API', () => {
    const error = new HttpErrorResponse({ status: 0, error: new ProgressEvent('error') });

    expect(toApiError(error)).toEqual({
      status: 0,
      code: null,
      message: 'Não foi possível conectar ao servidor. Tente de novo.',
      fieldErrors: {},
    });
  });

  it('reports an unexpected error when the body is not a ProblemDetails', () => {
    const error = new HttpErrorResponse({ status: 502, error: '<html>Bad Gateway</html>' });

    expect(toApiError(error)).toEqual({
      status: 502,
      code: null,
      message: 'Ocorreu um erro inesperado.',
      fieldErrors: {},
    });
  });

  it('hides the message of an error that did not come from the API', () => {
    expect(toApiError(new Error('secret detail'))).toEqual({
      status: 0,
      code: null,
      message: 'Ocorreu um erro inesperado.',
      fieldErrors: {},
    });
  });
});

describe('applyApiError', () => {
  let toast: { error: (message: string) => void };
  let form: FormGroup<{
    name: FormControl<string>;
    dose: FormGroup<{ number: FormControl<number> }>;
  }>;

  beforeEach(() => {
    toast = { error: vi.fn<(message: string) => void>() };
    form = new FormGroup({
      name: new FormControl('', { nonNullable: true }),
      dose: new FormGroup({ number: new FormControl(1, { nonNullable: true }) }),
    });
  });

  it('shows the errors on the matching fields, without a toast', () => {
    const error = makeValidationProblem({
      name: ['O nome é obrigatório.'],
      'dose.number': ['A dose deve ser positiva.'],
    });

    applyApiError(error, form, toast);

    expect(form.controls.name.errors).toEqual({ [API_ERROR_KEY]: 'O nome é obrigatório.' });
    expect(form.controls.dose.controls.number.errors).toEqual({
      [API_ERROR_KEY]: 'A dose deve ser positiva.',
    });
    expect(toast.error).not.toHaveBeenCalled();
  });

  it('marks the field as touched, so that the error appears', () => {
    applyApiError(makeValidationProblem({ name: ['O nome é obrigatório.'] }), form, toast);

    expect(form.controls.name.touched).toBe(true);
  });

  it('joins the messages of a field', () => {
    const error = makeValidationProblem({ name: ['Primeira.', 'Segunda.'] });

    applyApiError(error, form, toast);

    expect(form.controls.name.errors).toEqual({ [API_ERROR_KEY]: 'Primeira. Segunda.' });
  });

  it('sends the errors of a key without a field to the toast', () => {
    const error = makeValidationProblem({
      name: ['O nome é obrigatório.'],
      body: ['O corpo da requisição é inválido.'],
    });

    applyApiError(error, form, toast);

    expect(form.controls.name.errors).toEqual({ [API_ERROR_KEY]: 'O nome é obrigatório.' });
    expect(toast.error).toHaveBeenCalledExactlyOnceWith('O corpo da requisição é inválido.');
  });

  it('sends an error without field errors to the toast', () => {
    applyApiError(makeProblem(), form, toast);

    expect(toast.error).toHaveBeenCalledExactlyOnceWith(
      'Já existe uma vacina cadastrada com este nome.',
    );
    expect(form.valid).toBe(true);
  });
});
