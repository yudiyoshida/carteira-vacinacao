import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideDateFnsAdapter } from '@angular/material-date-fns-adapter';
import { MAT_DATE_LOCALE } from '@angular/material/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { ptBR } from 'date-fns/locale/pt-BR';
import { ProblemDetails } from '../../../../../../core/models/problem-details.model';
import { ToastService } from '../../../../../../shared/components/toast/toast.service';
import { AppliedOnForm, AppliedOnFormData } from './applied-on-form';

const VACCINATION_URL = '/api/v1/people/maria-id/vaccinations/vaccination-id';

const DATA: AppliedOnFormData = {
  personId: 'maria-id',
  vaccinationId: 'vaccination-id',
  vaccineName: 'Hepatite B',
  doseLabel: '2ª dose',
  appliedOn: '2023-05-10',
};

function makeProblem(status: number, code: string, detail: string): ProblemDetails {
  return { title: 'Erro', status, detail, instance: VACCINATION_URL, code, traceId: '00-abc' };
}

describe('AppliedOnForm', () => {
  let fixture: ComponentFixture<AppliedOnForm>;
  let http: HttpTestingController;
  let toast: { success: (message: string) => void; error: (message: string) => void };
  let dialogRef: { close: (result?: unknown) => void; disableClose: boolean };

  function element(): HTMLElement {
    return fixture.nativeElement;
  }

  function input(): HTMLInputElement {
    return element().querySelector('input') as HTMLInputElement;
  }

  function submitButton(): HTMLButtonElement {
    return element().querySelector('button[type="submit"]') as HTMLButtonElement;
  }

  function cancelButton(): HTMLButtonElement {
    return Array.from(element().querySelectorAll('button')).find((button) =>
      button.textContent?.includes('Cancelar'),
    ) as HTMLButtonElement;
  }

  function type(value: string): void {
    input().value = value;
    input().dispatchEvent(new Event('input'));
  }

  function errorMessage(): string | undefined {
    return element().querySelector('mat-error')?.textContent?.trim();
  }

  async function submit(): Promise<void> {
    submitButton().click();
    await fixture.whenStable();
  }

  beforeEach(async () => {
    toast = { success: vi.fn(), error: vi.fn() };
    dialogRef = { close: vi.fn(), disableClose: false };
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideDateFnsAdapter(),
        { provide: MAT_DATE_LOCALE, useValue: ptBR },
        { provide: ToastService, useValue: toast },
        { provide: MatDialogRef, useValue: dialogRef },
        { provide: MAT_DIALOG_DATA, useValue: DATA },
      ],
    });
    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(AppliedOnForm);
    await fixture.whenStable();
  });

  afterEach(() => http.verify());

  it('RN18: shows the vaccine and the label of the dose being corrected', () => {
    expect(element().textContent).toContain('Hepatite B · 2ª dose');
  });

  it('starts with the current applied-on date', () => {
    expect(input().value).toBe('10/05/2023');
  });

  it('RN12: asks for the date and sends nothing when it is empty', async () => {
    type('');

    await submit();

    http.expectNone(VACCINATION_URL);
    expect(errorMessage()).toBe('A data de aplicação é obrigatória.');
  });

  it('corrects the date, shows the message of the API and closes with the result', async () => {
    type('11/05/2023');

    await submit();

    const request = http.expectOne(VACCINATION_URL);
    expect(request.request.method).toBe('PATCH');
    expect(request.request.body).toEqual({ appliedOn: '2023-05-11' });
    request.flush({ message: 'Data de aplicação corrigida com sucesso.' });
    await fixture.whenStable();

    expect(toast.success).toHaveBeenCalledExactlyOnceWith(
      'Data de aplicação corrigida com sucesso.',
    );
    expect(dialogRef.close).toHaveBeenCalledExactlyOnceWith(true);
  });

  it('shows a validation error of the API on the field and stays open', async () => {
    await submit();

    http.expectOne(VACCINATION_URL).flush(
      {
        ...makeProblem(400, 'validation.failed', 'Um ou mais campos são inválidos.'),
        errors: { appliedOn: ['O valor informado é inválido.'] },
      },
      { status: 400, statusText: 'Bad Request' },
    );
    await fixture.whenStable();

    expect(errorMessage()).toBe('O valor informado é inválido.');
    expect(toast.error).not.toHaveBeenCalled();
    expect(dialogRef.close).not.toHaveBeenCalled();
  });

  it('RN14: shows a date out of order in a toast and stays open', async () => {
    await submit();

    const detail = 'A nova data deve ficar entre as datas da dose anterior e da dose seguinte.';
    http
      .expectOne(VACCINATION_URL)
      .flush(makeProblem(422, 'vaccination.applied_on_out_of_order', detail), {
        status: 422,
        statusText: 'Unprocessable Entity',
      });
    await fixture.whenStable();

    expect(toast.error).toHaveBeenCalledExactlyOnceWith(detail);
    expect(dialogRef.close).not.toHaveBeenCalled();
  });

  it('cannot be closed during the request and is released after a failure', async () => {
    await submit();

    expect(submitButton().disabled).toBe(true);
    expect(cancelButton().disabled).toBe(true);
    expect(dialogRef.disableClose).toBe(true);

    http
      .expectOne(VACCINATION_URL)
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
