import { HarnessLoader } from '@angular/cdk/testing';
import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideDateFnsAdapter } from '@angular/material-date-fns-adapter';
import { MAT_DATE_LOCALE } from '@angular/material/core';
import { MAT_DIALOG_DATA, MatDialogRef } from '@angular/material/dialog';
import { MatSelectHarness } from '@angular/material/select/testing';
import { ptBR } from 'date-fns/locale/pt-BR';
import { ProblemDetails } from '../../../../../../core/models/problem-details.model';
import { VaccinationCardEntryDto } from '../../../../../../core/models/vaccination.model';
import { VaccineDto } from '../../../../../../core/models/vaccine.model';
import { ToastService } from '../../../../../../shared/components/toast/toast.service';
import { VaccinationForm } from './vaccination-form';

const VACCINES_URL = '/api/v1/vaccines';
const VACCINATIONS_URL = '/api/v1/people/maria-id/vaccinations';

const BCG: VaccineDto = { id: 'bcg-id', name: 'BCG', primaryDoses: 1, boosterDoses: 0 };
const DTP: VaccineDto = { id: 'dtp-id', name: 'DTP', primaryDoses: 2, boosterDoses: 1 };
const HEPATITIS_B: VaccineDto = {
  id: 'hep-id',
  name: 'Hepatite B',
  primaryDoses: 2,
  boosterDoses: 0,
};

function makeEntry(
  { id, name, primaryDoses, boosterDoses }: VaccineDto,
  nextExpectedDose: VaccinationCardEntryDto['nextExpectedDose'],
): VaccinationCardEntryDto {
  return {
    vaccineId: id,
    vaccineName: name,
    scheme: { primaryDoses, boosterDoses },
    doses: [],
    progress: {
      primaryApplied: 0,
      primaryTotal: primaryDoses,
      primarySeriesComplete: false,
      boostersApplied: 0,
      boostersTotal: boosterDoses,
    },
    nextExpectedDose,
  };
}

// DTP goes on with its booster, and the scheme of Hepatite B is complete. BCG is off the card.
const CARD_VACCINES = [
  makeEntry(DTP, { type: 'Booster', number: 1 }),
  makeEntry(HEPATITIS_B, null),
];

function makeProblem(status: number, code: string, detail: string): ProblemDetails {
  return { title: 'Erro', status, detail, instance: VACCINATIONS_URL, code, traceId: '00-abc' };
}

describe('VaccinationForm', () => {
  let fixture: ComponentFixture<VaccinationForm>;
  let loader: HarnessLoader;
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

  function typeDate(value: string): void {
    const input = element().querySelector('input') as HTMLInputElement;
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

  async function vaccineSelect(): Promise<MatSelectHarness> {
    return (await loader.getAllHarnesses(MatSelectHarness))[0];
  }

  async function doseSelect(): Promise<MatSelectHarness> {
    return (await loader.getAllHarnesses(MatSelectHarness))[1];
  }

  async function chooseVaccine(name: string): Promise<void> {
    const select = await vaccineSelect();
    await select.open();
    await select.clickOptions({ text: name });
  }

  async function doseLabels(): Promise<string[]> {
    const select = await doseSelect();
    await select.open();
    const options = await select.getOptions();
    return Promise.all(options.map((option) => option.getText()));
  }

  // The form as the page opens it, with the list of vaccines already answered.
  async function open(vaccineId: string | null = null): Promise<void> {
    TestBed.configureTestingModule({
      providers: [
        {
          provide: MAT_DIALOG_DATA,
          useValue: { personId: 'maria-id', cardVaccines: CARD_VACCINES, vaccineId },
        },
      ],
    });
    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(VaccinationForm);
    loader = TestbedHarnessEnvironment.loader(fixture);
    http.expectOne(VACCINES_URL).flush({
      currentPage: 1,
      itemsPerPage: 3,
      totalItems: 3,
      totalPages: 1,
      data: [BCG, DTP, HEPATITIS_B],
    });
    await fixture.whenStable();
  }

  beforeEach(() => {
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
      ],
    });
  });

  afterEach(() => http.verify());

  it('lists every vaccine of the catalog, on the card or not', async () => {
    await open();

    const select = await vaccineSelect();
    await select.open();
    const options = await select.getOptions();

    expect(await Promise.all(options.map((option) => option.getText()))).toEqual([
      'BCG',
      'DTP',
      'Hepatite B',
    ]);
  });

  it('starts empty from the button of the page', async () => {
    await open();

    expect(await (await vaccineSelect()).getValueText()).toBe('');
    expect(await (await doseSelect()).getValueText()).toBe('');
  });

  it('F07: starts with the vaccine and the next dose of the block that opened it', async () => {
    await open('dtp-id');

    expect(await (await vaccineSelect()).getValueText()).toBe('DTP');
    expect(await (await doseSelect()).getValueText()).toBe('1º reforço');
  });

  it('F07: selects the next expected dose of the vaccine the operator chooses', async () => {
    await open();

    await chooseVaccine('DTP');

    expect(await (await doseSelect()).getValueText()).toBe('1º reforço');
  });

  it('RN10: starts a vaccine off the card at the first dose', async () => {
    await open();

    await chooseVaccine('BCG');

    expect(await (await doseSelect()).getValueText()).toBe('Dose única');
  });

  it('selects no dose for a vaccine with a complete scheme', async () => {
    await open();

    await chooseVaccine('Hepatite B');

    expect(await (await doseSelect()).getValueText()).toBe('');
  });

  it('RN18: lists the doses of the chosen scheme, with their labels', async () => {
    await open('dtp-id');

    expect(await doseLabels()).toEqual(['1ª dose', '2ª dose', '1º reforço']);
  });

  it('changes the doses and the selection when the vaccine changes', async () => {
    await open('dtp-id');

    await chooseVaccine('BCG');

    expect(await doseLabels()).toEqual(['Dose única']);
    expect(await (await doseSelect()).getValueText()).toBe('Dose única');
  });

  it('asks for every field and sends nothing when the form is invalid', async () => {
    await open();

    await submit();

    http.expectNone(VACCINATIONS_URL);
    expect(errorMessages()).toEqual([
      'A vacina é obrigatória.',
      'A dose é obrigatória.',
      'A data de aplicação é obrigatória.',
    ]);
  });

  it('registers the vaccination, shows the message and closes with the result', async () => {
    await open('dtp-id');
    typeDate('10/06/2024');

    await submit();

    const request = http.expectOne(VACCINATIONS_URL);
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({
      vaccineId: 'dtp-id',
      dose: { type: 'Booster', number: 1 },
      appliedOn: '2024-06-10',
    });
    request.flush({ id: 'vaccination-id' }, { status: 201, statusText: 'Created' });
    await fixture.whenStable();

    expect(toast.success).toHaveBeenCalledExactlyOnceWith('Vacinação registrada com sucesso.');
    expect(dialogRef.close).toHaveBeenCalledExactlyOnceWith(true);
  });

  it('shows a validation error of the API on the field and stays open', async () => {
    await open('dtp-id');
    typeDate('10/06/2024');
    await submit();

    http.expectOne(VACCINATIONS_URL).flush(
      {
        ...makeProblem(400, 'validation.failed', 'Um ou mais campos são inválidos.'),
        errors: { appliedOn: ['O valor informado é inválido.'] },
      },
      { status: 400, statusText: 'Bad Request' },
    );
    await fixture.whenStable();

    expect(errorMessages()).toEqual(['O valor informado é inválido.']);
    expect(toast.error).not.toHaveBeenCalled();
    expect(dialogRef.close).not.toHaveBeenCalled();
  });

  it.each([
    [
      'RN10',
      422,
      'vaccination.dose_out_of_sequence',
      'A dose informada não é a próxima dose esperada desta vacina.',
    ],
    [
      'RN11',
      409,
      'vaccination.dose_already_registered',
      'Esta dose já foi registrada para esta pessoa.',
    ],
    ['RN12', 422, 'vaccination.applied_on_in_future', 'A data de aplicação não pode ser futura.'],
  ])(
    '%s: shows the refusal of the API in a toast and stays open',
    async (_, status, code, detail) => {
      await open('dtp-id');
      typeDate('10/06/2024');
      await submit();

      http
        .expectOne(VACCINATIONS_URL)
        .flush(makeProblem(status, code, detail), { status, statusText: 'Refused' });
      await fixture.whenStable();

      expect(toast.error).toHaveBeenCalledExactlyOnceWith(detail);
      expect(toast.success).not.toHaveBeenCalled();
      expect(dialogRef.close).not.toHaveBeenCalled();
    },
  );

  it('cannot be closed during the request and is released after a failure', async () => {
    await open('dtp-id');
    typeDate('10/06/2024');
    await submit();

    expect(submitButton().disabled).toBe(true);
    expect(cancelButton().disabled).toBe(true);
    expect(dialogRef.disableClose).toBe(true);

    http
      .expectOne(VACCINATIONS_URL)
      .flush(makeProblem(500, 'internal.unexpected', 'Ocorreu um erro inesperado.'), {
        status: 500,
        statusText: 'Internal Server Error',
      });
    await fixture.whenStable();

    expect(submitButton().disabled).toBe(false);
    expect(cancelButton().disabled).toBe(false);
    expect(dialogRef.disableClose).toBe(false);
  });

  it('closes with the toast when the list of vaccines fails, because it depends on the list', async () => {
    TestBed.configureTestingModule({
      providers: [
        {
          provide: MAT_DIALOG_DATA,
          useValue: { personId: 'maria-id', cardVaccines: [], vaccineId: null },
        },
      ],
    });
    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(VaccinationForm);

    http
      .expectOne(VACCINES_URL)
      .flush(makeProblem(500, 'internal.unexpected', 'Ocorreu um erro inesperado.'), {
        status: 500,
        statusText: 'Internal Server Error',
      });

    expect(toast.error).toHaveBeenCalledExactlyOnceWith('Ocorreu um erro inesperado.');
    expect(dialogRef.close).toHaveBeenCalledOnce();
    expect(dialogRef.close).not.toHaveBeenCalledWith(true);
  });

  it('closes without the result when the operator cancels', async () => {
    await open();

    cancelButton().click();

    expect(dialogRef.close).toHaveBeenCalledOnce();
    expect(dialogRef.close).not.toHaveBeenCalledWith(true);
  });
});
