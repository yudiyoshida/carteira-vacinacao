import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
  TestRequest,
} from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { By } from '@angular/platform-browser';
import { provideRouter } from '@angular/router';
import { Subject } from 'rxjs';
import { ProblemDetails } from '../../../../core/models/problem-details.model';
import {
  VaccinationCardDto,
  VaccinationCardEntryDto,
} from '../../../../core/models/vaccination.model';
import { ConfirmModal } from '../../../../shared/components/confirm-modal/confirm-modal';
import { ToastService } from '../../../../shared/components/toast/toast.service';
import { AppliedOnForm } from './components/applied-on-form/applied-on-form';
import { DoseBlock } from './components/dose-block/dose-block';
import { VaccinationForm } from './components/vaccination-form/vaccination-form';
import { VaccinationCard } from './vaccination-card';

const CARD_URL = '/api/v1/people/maria-id/vaccination-card';
const VACCINATIONS_URL = '/api/v1/people/maria-id/vaccinations';

const BCG: VaccinationCardEntryDto = {
  vaccineId: 'bcg-id',
  vaccineName: 'BCG',
  scheme: { primaryDoses: 1, boosterDoses: 0 },
  doses: [
    {
      vaccinationId: 'bcg-1',
      dose: { type: 'Primary', number: 1 },
      appliedOn: '2023-03-10',
    },
  ],
  progress: {
    primaryApplied: 1,
    primaryTotal: 1,
    primarySeriesComplete: true,
    boostersApplied: 0,
    boostersTotal: 0,
  },
  nextExpectedDose: null,
};

const HEPATITIS_B: VaccinationCardEntryDto = {
  vaccineId: 'hep-id',
  vaccineName: 'Hepatite B',
  scheme: { primaryDoses: 3, boosterDoses: 0 },
  doses: [
    {
      vaccinationId: 'hep-1',
      dose: { type: 'Primary', number: 1 },
      appliedOn: '2023-03-10',
    },
    {
      vaccinationId: 'hep-2',
      dose: { type: 'Primary', number: 2 },
      appliedOn: '2023-05-10',
    },
  ],
  progress: {
    primaryApplied: 2,
    primaryTotal: 3,
    primarySeriesComplete: false,
    boostersApplied: 0,
    boostersTotal: 0,
  },
  nextExpectedDose: { type: 'Primary', number: 3 },
};

const PERSON = { id: 'maria-id', name: 'Maria Demonstração', cpf: '123.456.789-09' };
const CARD: VaccinationCardDto = { person: PERSON, vaccines: [BCG, HEPATITIS_B] };
const EMPTY_CARD: VaccinationCardDto = { person: PERSON, vaccines: [] };

function makeProblem(status: number, code: string, detail: string): ProblemDetails {
  return { title: 'Erro', status, detail, instance: CARD_URL, code, traceId: '00-abc' };
}

describe('VaccinationCard', () => {
  let fixture: ComponentFixture<VaccinationCard>;
  let http: HttpTestingController;
  let toast: { success: (message: string) => void; error: (message: string) => void };
  let dialog: { open: (component: unknown, config?: unknown) => unknown };
  let afterClosed: Subject<boolean | undefined>;

  function element(): HTMLElement {
    return fixture.nativeElement;
  }

  function button(label: string): HTMLButtonElement | undefined {
    return Array.from(element().querySelectorAll('button')).find((candidate) =>
      candidate.textContent?.includes(label),
    );
  }

  function emptyState(): string | undefined {
    return element().querySelector('app-empty-state')?.textContent?.trim();
  }

  // The block of a dose, in the order the grid shows them: BCG, then Hepatite B 1ª, 2ª and 3ª.
  function block(index: number): DoseBlock {
    return fixture.debugElement.queryAll(By.directive(DoseBlock))[index].componentInstance;
  }

  async function answer(request: TestRequest, card: VaccinationCardDto): Promise<void> {
    request.flush(card);
    await fixture.whenStable();
  }

  async function failWith(request: TestRequest): Promise<void> {
    request.flush(makeProblem(500, 'internal.unexpected', 'Ocorreu um erro inesperado.'), {
      status: 500,
      statusText: 'Internal Server Error',
    });
    await fixture.whenStable();
  }

  beforeEach(async () => {
    afterClosed = new Subject();
    toast = { success: vi.fn(), error: vi.fn() };
    dialog = { open: vi.fn(() => ({ afterClosed: () => afterClosed })) };
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: ToastService, useValue: toast },
        { provide: MatDialog, useValue: dialog },
      ],
    });
    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(VaccinationCard);
    fixture.componentRef.setInput('id', 'maria-id');
    await fixture.whenStable();
  });

  afterEach(() => http.verify());

  it('RN26: shows the name and the formatted CPF of the person', async () => {
    await answer(http.expectOne(CARD_URL), CARD);

    expect(element().querySelector('h1')?.textContent).toContain('Maria Demonstração');
    expect(element().textContent).toContain('CPF 123.456.789-09');
  });

  it('links back to the list of people', async () => {
    await answer(http.expectOne(CARD_URL), CARD);

    const link = element().querySelector('a[aria-label="Voltar para Pessoas"]');
    expect(link?.getAttribute('href')).toBe('/people');
  });

  it('RN17: shows the progress of each vaccine', async () => {
    await answer(http.expectOne(CARD_URL), CARD);

    const summaries = Array.from(element().querySelectorAll('section')).map((section) =>
      section.textContent?.replace(/\s+/g, ' '),
    );
    expect(summaries[0]).toContain('BCG Primária 1/1 · Reforços 0/0 Série completa');
    expect(summaries[1]).toContain('Hepatite B Primária 2/3 · Reforços 0/0 Série incompleta');
  });

  it('shows the skeleton until the first response arrives', async () => {
    expect(element().querySelector('app-skeleton')).not.toBeNull();

    await answer(http.expectOne(CARD_URL), CARD);

    expect(element().querySelector('app-skeleton')).toBeNull();
  });

  it('invites the operator to register when the person has no vaccination', async () => {
    await answer(http.expectOne(CARD_URL), EMPTY_CARD);

    expect(emptyState()).toContain('Nenhuma vacinação registrada.');
    expect(element().querySelector('section')).toBeNull();

    (element().querySelector('app-empty-state button') as HTMLButtonElement).click();

    expect(dialog.open).toHaveBeenCalledExactlyOnceWith(VaccinationForm, {
      data: { personId: 'maria-id', cardVaccines: [], vaccineId: null },
    });
  });

  it('shows a failure in a toast and offers a new attempt', async () => {
    await failWith(http.expectOne(CARD_URL));

    expect(toast.error).toHaveBeenCalledExactlyOnceWith('Ocorreu um erro inesperado.');
    expect(emptyState()).toContain('Não foi possível carregar o cartão.');

    button('Tentar de novo')?.click();
    await fixture.whenStable();

    expect(element().querySelector('app-skeleton')).not.toBeNull();
    await answer(http.expectOne(CARD_URL), CARD);
    expect(emptyState()).toBeUndefined();
    expect(element().querySelector('h1')?.textContent).toContain('Maria Demonstração');
  });

  it('keeps the card on the screen during a reload and after its failure', async () => {
    await answer(http.expectOne(CARD_URL), CARD);

    button('Registrar dose')?.click();
    afterClosed.next(true);
    await fixture.whenStable();

    expect(element().querySelector('app-skeleton')).toBeNull();
    expect(element().querySelector('section')).not.toBeNull();

    await failWith(http.expectOne(CARD_URL));

    expect(toast.error).toHaveBeenCalledOnce();
    expect(element().querySelector('section')).not.toBeNull();
  });

  it('opens the registration from the button of the page, without a vaccine', async () => {
    await answer(http.expectOne(CARD_URL), CARD);

    button('Registrar dose')?.click();

    expect(dialog.open).toHaveBeenCalledExactlyOnceWith(VaccinationForm, {
      data: { personId: 'maria-id', cardVaccines: CARD.vaccines, vaccineId: null },
    });
  });

  it('F07: opens the registration from the block with the vaccine of the block', async () => {
    await answer(http.expectOne(CARD_URL), CARD);

    block(3).register.emit();

    expect(dialog.open).toHaveBeenCalledExactlyOnceWith(VaccinationForm, {
      data: { personId: 'maria-id', cardVaccines: CARD.vaccines, vaccineId: 'hep-id' },
    });
  });

  it('reloads the card only when the registration succeeds', async () => {
    await answer(http.expectOne(CARD_URL), CARD);

    button('Registrar dose')?.click();
    afterClosed.next(undefined);
    http.expectNone(CARD_URL);

    afterClosed.next(true);
    await answer(http.expectOne(CARD_URL), CARD);
  });

  it('RN18: opens the correction with the vaccination and the label of the dose', async () => {
    await answer(http.expectOne(CARD_URL), CARD);

    block(2).correct.emit();

    expect(dialog.open).toHaveBeenCalledExactlyOnceWith(AppliedOnForm, {
      data: {
        personId: 'maria-id',
        vaccinationId: 'hep-2',
        vaccineName: 'Hepatite B',
        doseLabel: '2ª dose',
        appliedOn: '2023-05-10',
      },
    });
  });

  it('reloads the card only when the correction succeeds', async () => {
    await answer(http.expectOne(CARD_URL), CARD);

    block(2).correct.emit();
    afterClosed.next(undefined);
    http.expectNone(CARD_URL);

    afterClosed.next(true);
    await answer(http.expectOne(CARD_URL), CARD);
  });

  it('asks for confirmation before deleting, with the vaccine, the dose and the date', async () => {
    await answer(http.expectOne(CARD_URL), CARD);

    block(2).remove.emit();

    expect(dialog.open).toHaveBeenCalledExactlyOnceWith(
      ConfirmModal,
      expect.objectContaining({
        data: {
          heading: 'Excluir vacinação',
          message: 'Excluir a vacinação de Hepatite B (2ª dose), aplicada em 10/05/2023?',
          confirmLabel: 'Excluir',
        },
      }),
    );
    http.expectNone({ method: 'DELETE', url: `${VACCINATIONS_URL}/hep-2` });
  });

  it('F10: warns that the vaccine leaves the card with its only vaccination', async () => {
    await answer(http.expectOne(CARD_URL), CARD);

    block(0).remove.emit();

    expect(dialog.open).toHaveBeenCalledExactlyOnceWith(
      ConfirmModal,
      expect.objectContaining({
        data: expect.objectContaining({
          message:
            'Excluir a vacinação de BCG (Dose única), aplicada em 10/03/2023? A vacina sairá do cartão.',
        }),
      }),
    );
  });

  it('keeps the vaccination when the confirmation closes without a yes', async () => {
    await answer(http.expectOne(CARD_URL), CARD);

    block(2).remove.emit();
    afterClosed.next(undefined);

    http.expectNone({ method: 'DELETE', url: `${VACCINATIONS_URL}/hep-2` });
    http.expectNone(CARD_URL);
  });

  it('F10: deletes the vaccination, shows the message of the API and reloads', async () => {
    await answer(http.expectOne(CARD_URL), CARD);

    block(2).remove.emit();
    afterClosed.next(true);

    http
      .expectOne({ method: 'DELETE', url: `${VACCINATIONS_URL}/hep-2` })
      .flush({ message: 'Vacinação excluída com sucesso.' });

    expect(toast.success).toHaveBeenCalledExactlyOnceWith('Vacinação excluída com sucesso.');
    await answer(http.expectOne(CARD_URL), EMPTY_CARD);
    expect(emptyState()).toContain('Nenhuma vacinação registrada.');
  });

  it('RN15: shows a refused deletion in a toast and keeps the card', async () => {
    await answer(http.expectOne(CARD_URL), CARD);

    block(2).remove.emit();
    afterClosed.next(true);

    const detail = 'Só a última dose desta vacina pode ser excluída.';
    http
      .expectOne({ method: 'DELETE', url: `${VACCINATIONS_URL}/hep-2` })
      .flush(makeProblem(422, 'vaccination.not_last_dose', detail), {
        status: 422,
        statusText: 'Unprocessable Entity',
      });
    await fixture.whenStable();

    expect(toast.error).toHaveBeenCalledExactlyOnceWith(detail);
    expect(toast.success).not.toHaveBeenCalled();
    expect(element().querySelector('section')).not.toBeNull();
  });
});
