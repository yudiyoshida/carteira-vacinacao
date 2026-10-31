import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatDialogRef } from '@angular/material/dialog';
import { ProblemDetails } from '../../../../../../core/models/problem-details.model';
import { ToastService } from '../../../../../../shared/components/toast/toast.service';
import { PersonForm } from './person-form';

const PEOPLE_URL = '/api/v1/people';
const VALID_CPF = '123.456.789-09';

function makeProblem(status: number, code: string, detail: string): ProblemDetails {
  return { title: 'Erro', status, detail, instance: PEOPLE_URL, code, traceId: '00-abc' };
}

function makeValidationProblem(errors: Record<string, string[]>): ProblemDetails {
  return {
    ...makeProblem(400, 'validation.failed', 'Um ou mais campos são inválidos.'),
    errors,
  };
}

describe('PersonForm', () => {
  let fixture: ComponentFixture<PersonForm>;
  let http: HttpTestingController;
  let toast: { success: (message: string) => void; error: (message: string) => void };
  let dialogRef: { close: (result?: unknown) => void; disableClose: boolean };

  function element(): HTMLElement {
    return fixture.nativeElement;
  }

  function submitButton(): HTMLButtonElement {
    return element().querySelector('button[type="submit"]') as HTMLButtonElement;
  }

  function cancelButton(): HTMLButtonElement {
    return Array.from(element().querySelectorAll('button')).find((button) =>
      button.textContent?.includes('Cancelar'),
    ) as HTMLButtonElement;
  }

  function nameInput(): HTMLInputElement {
    return element().querySelector('app-text-input input') as HTMLInputElement;
  }

  function cpfInput(): HTMLInputElement {
    return element().querySelector('app-cpf-input input') as HTMLInputElement;
  }

  function type(input: HTMLInputElement, value: string): void {
    input.value = value;
    input.dispatchEvent(new Event('input'));
  }

  function errorMessages(): (string | undefined)[] {
    return Array.from(element().querySelectorAll('mat-error')).map((error) =>
      error.textContent?.trim(),
    );
  }

  async function submit(): Promise<void> {
    submitButton().click();
    await fixture.whenStable();
  }

  async function submitValidPerson(): Promise<void> {
    type(nameInput(), 'Maria');
    type(cpfInput(), VALID_CPF);
    await submit();
  }

  beforeEach(async () => {
    toast = { success: vi.fn(), error: vi.fn() };
    dialogRef = { close: vi.fn(), disableClose: false };
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: ToastService, useValue: toast },
        { provide: MatDialogRef, useValue: dialogRef },
      ],
    });
    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(PersonForm);
    await fixture.whenStable();
  });

  afterEach(() => http.verify());

  it('RN01, RN02: asks for the name and the CPF and sends nothing', async () => {
    await submit();

    http.expectNone(PEOPLE_URL);
    expect(errorMessages()).toEqual(['O nome é obrigatório.', 'O CPF é obrigatório.']);
  });

  it.each([
    ['shorter than 3', 'Al'],
    ['longer than 150', 'a'.repeat(151)],
  ])('RN01: refuses a name %s characters', async (_, name) => {
    type(nameInput(), name);
    type(cpfInput(), VALID_CPF);

    await submit();

    http.expectNone(PEOPLE_URL);
    expect(errorMessages()).toEqual(['O nome deve ter entre 3 e 150 caracteres.']);
  });

  it.each(['Ana', 'a'.repeat(150)])('RN01: accepts the name "%s"', async (name) => {
    type(nameInput(), name);
    type(cpfInput(), VALID_CPF);

    await submit();

    const request = http.expectOne(PEOPLE_URL);
    expect(request.request.body).toEqual({ name, cpf: VALID_CPF });
    request.flush({ id: 'person-id' }, { status: 201, statusText: 'Created' });
  });

  it('RN02: refuses an incomplete CPF', async () => {
    type(nameInput(), 'Maria');
    type(cpfInput(), '123.456.789');

    await submit();

    http.expectNone(PEOPLE_URL);
    expect(errorMessages()).toEqual(['O CPF informado é inválido.']);
  });

  it('RN02: registers a CPF typed without the mask and closes with the result', async () => {
    type(nameInput(), 'Maria');
    type(cpfInput(), '12345678909');

    await submit();

    const request = http.expectOne(PEOPLE_URL);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ name: 'Maria', cpf: VALID_CPF });
    request.flush({ id: 'person-id' }, { status: 201, statusText: 'Created' });
    await fixture.whenStable();

    expect(toast.success).toHaveBeenCalledExactlyOnceWith('Pessoa cadastrada com sucesso.');
    expect(dialogRef.close).toHaveBeenCalledExactlyOnceWith(true);
  });

  it('RN02: shows the check digits refused by the API on the field and stays open', async () => {
    await submitValidPerson();

    http
      .expectOne(PEOPLE_URL)
      .flush(makeValidationProblem({ cpf: ['O CPF informado é inválido.'] }), {
        status: 400,
        statusText: 'Bad Request',
      });
    await fixture.whenStable();

    expect(errorMessages()).toEqual(['O CPF informado é inválido.']);
    expect(toast.error).not.toHaveBeenCalled();
    expect(dialogRef.close).not.toHaveBeenCalled();
  });

  it('RN01: shows on the field a name that is too short without the spaces', async () => {
    type(nameInput(), '   ');
    type(cpfInput(), VALID_CPF);
    await submit();

    http
      .expectOne(PEOPLE_URL)
      .flush(makeValidationProblem({ name: ['O nome deve ter entre 3 e 150 caracteres.'] }), {
        status: 400,
        statusText: 'Bad Request',
      });
    await fixture.whenStable();

    expect(errorMessages()).toEqual(['O nome deve ter entre 3 e 150 caracteres.']);
    expect(dialogRef.close).not.toHaveBeenCalled();
  });

  it('RN03: shows a repeated CPF in a toast and stays open', async () => {
    await submitValidPerson();

    http
      .expectOne(PEOPLE_URL)
      .flush(
        makeProblem(
          409,
          'person.cpf_already_registered',
          'Já existe uma pessoa cadastrada com este CPF.',
        ),
        { status: 409, statusText: 'Conflict' },
      );
    await fixture.whenStable();

    expect(toast.error).toHaveBeenCalledExactlyOnceWith(
      'Já existe uma pessoa cadastrada com este CPF.',
    );
    expect(dialogRef.close).not.toHaveBeenCalled();
  });

  it('cannot be closed during the request and is released after a failure', async () => {
    await submitValidPerson();

    expect(submitButton().disabled).toBe(true);
    expect(cancelButton().disabled).toBe(true);
    expect(dialogRef.disableClose).toBe(true);

    http
      .expectOne(PEOPLE_URL)
      .flush(makeProblem(500, 'internal.unexpected', 'Ocorreu um erro inesperado.'), {
        status: 500,
        statusText: 'Internal Server Error',
      });
    await fixture.whenStable();

    expect(submitButton().disabled).toBe(false);
    expect(cancelButton().disabled).toBe(false);
    expect(dialogRef.disableClose).toBe(false);
  });

  it('closes without the result when the operator cancels', () => {
    cancelButton().click();

    expect(dialogRef.close).toHaveBeenCalledOnce();
    expect(dialogRef.close).not.toHaveBeenCalledWith(true);
  });
});
