import { HarnessLoader } from '@angular/cdk/testing';
import { TestbedHarnessEnvironment } from '@angular/cdk/testing/testbed';
import { provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
  TestRequest,
} from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { MatMenuHarness } from '@angular/material/menu/testing';
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

// One row with every kind of block: applied doses, the next expected dose and empty blocks.
const DTP: VaccinationCardEntryDto = {
  vaccineId: 'dtp-id',
  vaccineName: 'DTP',
  scheme: { primaryDoses: 3, boosterDoses: 2 },
  doses: [
    {
      vaccinationId: 'dtp-1',
      dose: { type: 'Primary', number: 1 },
      appliedOn: '2023-04-15',
    },
    {
      vaccinationId: 'dtp-2',
      dose: { type: 'Primary', number: 2 },
      appliedOn: '2023-06-20',
    },
  ],
  progress: {
    primaryApplied: 2,
    primaryTotal: 3,
    primarySeriesComplete: false,
    boostersApplied: 0,
    boostersTotal: 2,
  },
  nextExpectedDose: { type: 'Primary', number: 3 },
};

const DTP_AFTER_DELETION: VaccinationCardEntryDto = {
  ...DTP,
  doses: DTP.doses.slice(0, 1),
  progress: { ...DTP.progress, primaryApplied: 1 },
  nextExpectedDose: { type: 'Primary', number: 2 },
};

const PERSON = { id: 'maria-id', name: 'Maria Demonstração', cpf: '123.456.789-09' };
const CARD: VaccinationCardDto = { person: PERSON, vaccines: [BCG, HEPATITIS_B] };
const GRID_CARD: VaccinationCardDto = { person: PERSON, vaccines: [BCG, DTP, HEPATITIS_B] };
const EMPTY_CARD: VaccinationCardDto = { person: PERSON, vaccines: [] };

function makeProblem(status: number, code: string, detail: string): ProblemDetails {
  return { title: 'Erro', status, detail, instance: CARD_URL, code, traceId: '00-abc' };
}

describe('VaccinationCard', () => {
  let fixture: ComponentFixture<VaccinationCard>;
  let loader: HarnessLoader;
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

  function row(vaccineName: string): HTMLElement {
    return element().querySelector(`section[aria-label="${vaccineName}"]`) as HTMLElement;
  }

  function doseBlocks(vaccineName: string): HTMLElement[] {
    return Array.from(row(vaccineName).querySelectorAll('app-dose-block'));
  }

  // The label is the first text of a block, whatever the kind of the block.
  function doseLabels(vaccineName: string): (string | undefined)[] {
    return doseBlocks(vaccineName).map((dose) => dose.querySelector('span')?.textContent?.trim());
  }

  // An empty block has no action, so it has no name in this list.
  function actionNames(vaccineName: string): (string | null)[] {
    return Array.from(row(vaccineName).querySelectorAll('button')).map((action) =>
      action.getAttribute('aria-label'),
    );
  }

  // The menu of an applied dose, by the start of the accessible name of its block.
  function doseMenu(doseName: string): Promise<MatMenuHarness> {
    return loader.getHarness(MatMenuHarness.with({ selector: `[aria-label^="${doseName},"]` }));
  }

  async function menuActions(doseName: string): Promise<string[]> {
    const menu = await doseMenu(doseName);
    await menu.open();
    const items = await menu.getItems();

    return Promise.all(items.map((item) => item.getText()));
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
    loader = TestbedHarnessEnvironment.loader(fixture);
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

  it('RN17: shows one row per vaccine', async () => {
    await answer(http.expectOne(CARD_URL), GRID_CARD);

    const names = Array.from(element().querySelectorAll('section h2')).map(
      (name) => name.textContent,
    );
    expect(names).toEqual(['BCG', 'DTP', 'Hepatite B']);
  });

  it('RN17, RN18: shows one block per dose of the scheme, in the order of the sequence', async () => {
    await answer(http.expectOne(CARD_URL), GRID_CARD);

    expect(doseLabels('BCG')).toEqual(['Dose única']);
    expect(doseLabels('DTP')).toEqual([
      '1ª dose',
      '2ª dose',
      '3ª dose',
      '1º reforço',
      '2º reforço',
    ]);
    expect(doseLabels('Hepatite B')).toEqual(['1ª dose', '2ª dose', '3ª dose']);
  });

  it('RN17: shows the date of each applied dose', async () => {
    await answer(http.expectOne(CARD_URL), GRID_CARD);

    const [firstDose, secondDose] = doseBlocks('DTP');
    expect(firstDose.textContent).toContain('15/04/2023');
    expect(secondDose.textContent).toContain('20/06/2023');
  });

  it('names the actions of the applied doses and of the next expected dose', async () => {
    await answer(http.expectOne(CARD_URL), GRID_CARD);

    expect(actionNames('DTP')).toEqual([
      '1ª dose de DTP, aplicada em 15/04/2023',
      '2ª dose de DTP, aplicada em 20/06/2023',
      'Registrar 3ª dose de DTP',
    ]);
  });

  it('offers no registration on a vaccine with the scheme complete', async () => {
    await answer(http.expectOne(CARD_URL), GRID_CARD);

    expect(actionNames('BCG')).toEqual(['Dose única de BCG, aplicada em 10/03/2023']);
  });

  it('shows the doses after the next one as empty blocks, without an action', async () => {
    await answer(http.expectOne(CARD_URL), GRID_CARD);

    const emptyBlocks = doseBlocks('DTP').slice(3);
    expect(emptyBlocks.map((dose) => dose.textContent?.trim())).toEqual([
      '1º reforço',
      '2º reforço',
    ]);
    expect(emptyBlocks.map((dose) => dose.querySelector('button'))).toEqual([null, null]);
  });

  it('F07: registers the next expected dose by a click on its block', async () => {
    await answer(http.expectOne(CARD_URL), GRID_CARD);

    (row('DTP').querySelector('[aria-label="Registrar 3ª dose de DTP"]') as HTMLElement).click();

    expect(dialog.open).toHaveBeenCalledExactlyOnceWith(VaccinationForm, {
      data: { personId: 'maria-id', cardVaccines: GRID_CARD.vaccines, vaccineId: 'dtp-id' },
    });
  });

  it.each(['2ª dose de DTP', 'Dose única de BCG', '2ª dose de Hepatite B'])(
    'RN15: offers the correction and the deletion on the last dose: %s',
    async (doseName) => {
      await answer(http.expectOne(CARD_URL), GRID_CARD);

      expect(await menuActions(doseName)).toEqual([
        expect.stringContaining('Corrigir data'),
        expect.stringContaining('Excluir dose'),
      ]);
    },
  );

  it('RN15: offers only the correction on a dose before the last', async () => {
    await answer(http.expectOne(CARD_URL), GRID_CARD);

    expect(await menuActions('1ª dose de DTP')).toEqual([expect.stringContaining('Corrigir data')]);
  });

  it('F09: opens the correction from the menu of a dose before the last', async () => {
    await answer(http.expectOne(CARD_URL), GRID_CARD);

    const menu = await doseMenu('1ª dose de DTP');
    await menu.clickItem({ text: /Corrigir data/ });

    expect(dialog.open).toHaveBeenCalledExactlyOnceWith(AppliedOnForm, {
      data: {
        personId: 'maria-id',
        vaccinationId: 'dtp-1',
        vaccineName: 'DTP',
        doseLabel: '1ª dose',
        appliedOn: '2023-04-15',
      },
    });
  });

  it('F10: asks for confirmation from the menu of the last dose', async () => {
    await answer(http.expectOne(CARD_URL), GRID_CARD);

    const menu = await doseMenu('2ª dose de DTP');
    await menu.clickItem({ text: /Excluir dose/ });

    expect(dialog.open).toHaveBeenCalledExactlyOnceWith(
      ConfirmModal,
      expect.objectContaining({
        data: expect.objectContaining({
          message: 'Excluir a vacinação de DTP (2ª dose), aplicada em 20/06/2023?',
        }),
      }),
    );
  });

  it('F10: moves the deletion to the previous dose when the card reloads', async () => {
    await answer(http.expectOne(CARD_URL), GRID_CARD);

    const menu = await doseMenu('2ª dose de DTP');
    await menu.clickItem({ text: /Excluir dose/ });
    afterClosed.next(true);
    http
      .expectOne({ method: 'DELETE', url: `${VACCINATIONS_URL}/dtp-2` })
      .flush({ message: 'Vacinação excluída com sucesso.' });
    await answer(http.expectOne(CARD_URL), { person: PERSON, vaccines: [DTP_AFTER_DELETION] });

    expect(actionNames('DTP')).toEqual([
      '1ª dose de DTP, aplicada em 15/04/2023',
      'Registrar 2ª dose de DTP',
    ]);
    expect(await menuActions('1ª dose de DTP')).toEqual([
      expect.stringContaining('Corrigir data'),
      expect.stringContaining('Excluir dose'),
    ]);
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
