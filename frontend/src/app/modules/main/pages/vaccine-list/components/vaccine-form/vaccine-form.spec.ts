import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatDialogRef } from '@angular/material/dialog';
import { ProblemDetails } from '../../../../../../core/models/problem-details.model';
import { ToastService } from '../../../../../../shared/components/toast/toast.service';
import { VaccineForm } from './vaccine-form';

const VACCINES_URL = '/api/v1/vaccines';

function makeProblem(status: number, code: string, detail: string): ProblemDetails {
  return { title: 'Erro', status, detail, instance: VACCINES_URL, code, traceId: '00-abc' };
}

describe('VaccineForm', () => {
  let fixture: ComponentFixture<VaccineForm>;
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
    return element().querySelector('input[type="text"]') as HTMLInputElement;
  }

  function primaryDosesInput(): HTMLInputElement {
    return element().querySelectorAll<HTMLInputElement>('input[type="number"]')[0];
  }

  function boosterDosesInput(): HTMLInputElement {
    return element().querySelectorAll<HTMLInputElement>('input[type="number"]')[1];
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

  async function submitValidVaccine(): Promise<void> {
    type(nameInput(), 'DTP');
    type(primaryDosesInput(), '3');
    type(boosterDosesInput(), '2');
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
    fixture = TestBed.createComponent(VaccineForm);
    await fixture.whenStable();
  });

  afterEach(() => http.verify());

  it('RN06: starts with the smallest valid scheme', () => {
    expect(primaryDosesInput().value).toBe('1');
    expect(boosterDosesInput().value).toBe('0');
  });

  it('RN07: warns that the scheme does not change after the registration', () => {
    expect(element().textContent).toContain(
      'O esquema de doses não pode ser alterado depois do cadastro.',
    );
  });

  it('RN05: asks for the name and sends nothing when the form is invalid', async () => {
    await submit();

    http.expectNone(VACCINES_URL);
    expect(errorMessages()).toEqual(['O nome é obrigatório.']);
  });

  it.each([
    ['shorter than 2', 'B'],
    ['longer than 100', 'a'.repeat(101)],
  ])('RN05: refuses a name %s characters', async (_, name) => {
    type(nameInput(), name);

    await submit();

    http.expectNone(VACCINES_URL);
    expect(errorMessages()).toEqual(['O nome deve ter entre 2 e 100 caracteres.']);
  });

  it.each(['0', '11', ''])('RN06: refuses "%s" primary doses', async (doses) => {
    type(nameInput(), 'DTP');
    type(primaryDosesInput(), doses);

    await submit();

    http.expectNone(VACCINES_URL);
    expect(errorMessages()).toEqual(['A vacina deve ter de 1 a 10 doses primárias.']);
  });

  it.each(['-1', '6', ''])('RN06: refuses "%s" booster doses', async (doses) => {
    type(nameInput(), 'DTP');
    type(boosterDosesInput(), doses);

    await submit();

    http.expectNone(VACCINES_URL);
    expect(errorMessages()).toEqual(['A vacina deve ter de 0 a 5 doses de reforço.']);
  });

  it('RN06: accepts the largest scheme', async () => {
    type(nameInput(), 'DTP');
    type(primaryDosesInput(), '10');
    type(boosterDosesInput(), '5');

    await submit();

    const request = http.expectOne(VACCINES_URL);
    expect(request.request.body).toEqual({ name: 'DTP', primaryDoses: 10, boosterDoses: 5 });
    request.flush({ id: 'dtp-id' }, { status: 201, statusText: 'Created' });
  });

  it('registers the vaccine, shows the message and closes with the result', async () => {
    await submitValidVaccine();

    const request = http.expectOne(VACCINES_URL);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ name: 'DTP', primaryDoses: 3, boosterDoses: 2 });
    request.flush({ id: 'dtp-id' }, { status: 201, statusText: 'Created' });
    await fixture.whenStable();

    expect(toast.success).toHaveBeenCalledExactlyOnceWith('Vacina cadastrada com sucesso.');
    expect(dialogRef.close).toHaveBeenCalledExactlyOnceWith(true);
  });

  it('shows a validation error of the API on the field and stays open', async () => {
    await submitValidVaccine();

    http.expectOne(VACCINES_URL).flush(
      {
        ...makeProblem(400, 'validation.failed', 'Um ou mais campos são inválidos.'),
        errors: { primaryDoses: ['O valor informado é inválido.'] },
      },
      { status: 400, statusText: 'Bad Request' },
    );
    await fixture.whenStable();

    expect(errorMessages()).toEqual(['O valor informado é inválido.']);
    expect(toast.error).not.toHaveBeenCalled();
    expect(dialogRef.close).not.toHaveBeenCalled();
  });

  it('RN05: shows a repeated name in a toast and stays open', async () => {
    await submitValidVaccine();

    http
      .expectOne(VACCINES_URL)
      .flush(
        makeProblem(
          409,
          'vaccine.name_already_registered',
          'Já existe uma vacina cadastrada com este nome.',
        ),
        { status: 409, statusText: 'Conflict' },
      );
    await fixture.whenStable();

    expect(toast.error).toHaveBeenCalledExactlyOnceWith(
      'Já existe uma vacina cadastrada com este nome.',
    );
    expect(dialogRef.close).not.toHaveBeenCalled();
  });

  it('cannot be closed during the request and is released after a failure', async () => {
    await submitValidVaccine();

    expect(submitButton().disabled).toBe(true);
    expect(cancelButton().disabled).toBe(true);
    expect(dialogRef.disableClose).toBe(true);

    http
      .expectOne(VACCINES_URL)
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
