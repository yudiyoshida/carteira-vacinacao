import { HttpRequest, provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
  TestRequest,
} from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { Subject } from 'rxjs';
import { IPagination } from '../../../../core/models/pagination.model';
import { ProblemDetails } from '../../../../core/models/problem-details.model';
import { VaccineDto } from '../../../../core/models/vaccine.model';
import { ToastService } from '../../../../shared/components/toast/toast.service';
import { VaccineForm } from './components/vaccine-form/vaccine-form';
import { VaccineList } from './vaccine-list';

const VACCINES_URL = '/api/v1/vaccines';
const DEBOUNCE_MS = 300;

const BCG: VaccineDto = { id: 'bcg-id', name: 'BCG', primaryDoses: 1, boosterDoses: 0 };
const DTP: VaccineDto = { id: 'dtp-id', name: 'DTP', primaryDoses: 3, boosterDoses: 2 };

function makePage(data: VaccineDto[]): IPagination<VaccineDto> {
  return {
    currentPage: 1,
    itemsPerPage: data.length,
    totalItems: data.length,
    totalPages: data.length > 0 ? 1 : 0,
    data,
  };
}

function isVaccineList(request: HttpRequest<unknown>): boolean {
  return request.method === 'GET' && request.url === VACCINES_URL;
}

describe('VaccineList', () => {
  let fixture: ComponentFixture<VaccineList>;
  let http: HttpTestingController;
  let toast: { error: (message: string) => void };
  let dialog: { open: (component: unknown) => unknown };
  let afterClosed: Subject<boolean | undefined>;

  function element(): HTMLElement {
    return fixture.nativeElement;
  }

  function texts(selector: string): (string | undefined)[] {
    return Array.from(element().querySelectorAll(selector)).map((cell) => cell.textContent?.trim());
  }

  function button(label: string): HTMLButtonElement | undefined {
    return Array.from(element().querySelectorAll('button')).find((candidate) =>
      candidate.textContent?.includes(label),
    );
  }

  function skeleton(): Element | null {
    return element().querySelector('app-skeleton');
  }

  function emptyState(): string | undefined {
    return element().querySelector('app-empty-state')?.textContent?.trim();
  }

  function searchInput(): HTMLInputElement {
    return element().querySelector('input[type="search"]') as HTMLInputElement;
  }

  function search(term: string): void {
    searchInput().value = term;
    searchInput().dispatchEvent(new Event('input'));
    vi.advanceTimersByTime(DEBOUNCE_MS);
  }

  async function answer(request: TestRequest, data: VaccineDto[]): Promise<void> {
    request.flush(makePage(data));
    await fixture.whenStable();
  }

  async function failWith(request: TestRequest): Promise<void> {
    const problem: ProblemDetails = {
      title: 'Erro inesperado',
      status: 500,
      detail: 'Ocorreu um erro inesperado.',
      instance: VACCINES_URL,
      code: 'internal.unexpected',
      traceId: '00-abc',
    };
    request.flush(problem, { status: 500, statusText: 'Internal Server Error' });
    await fixture.whenStable();
  }

  beforeEach(async () => {
    // RxJS schedules the debounce with setInterval and measures it with Date. The others stay real,
    // because the change detection that whenStable awaits runs on setTimeout.
    vi.useFakeTimers({ toFake: ['setInterval', 'clearInterval', 'Date'] });
    afterClosed = new Subject();
    toast = { error: vi.fn() };
    dialog = { open: vi.fn(() => ({ afterClosed: () => afterClosed })) };
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: ToastService, useValue: toast },
        { provide: MatDialog, useValue: dialog },
      ],
    });
    http = TestBed.inject(HttpTestingController);
    fixture = TestBed.createComponent(VaccineList);
    await fixture.whenStable();
  });

  afterEach(() => {
    http.verify();
    vi.useRealTimers();
  });

  it('RN20: lists every vaccine on opening, in the order of the API', async () => {
    const request = http.expectOne(isVaccineList);
    expect(request.request.params.keys()).toEqual([]);

    await answer(request, [BCG, DTP]);

    expect(texts('th')).toEqual(['Nome', 'Doses primárias', 'Doses de reforço']);
    expect(texts('td')).toEqual(['BCG', '1', '0', 'DTP', '3', '2']);
  });

  it('shows the skeleton until the first response arrives', async () => {
    expect(skeleton()).not.toBeNull();

    await answer(http.expectOne(isVaccineList), [BCG]);

    expect(skeleton()).toBeNull();
  });

  it('limits the search to the length the API accepts', () => {
    expect(searchInput().getAttribute('maxlength')).toBe('100');

    http.expectOne(isVaccineList).flush(makePage([]));
  });

  it('RN20: searches by the trimmed term, only after the operator stops typing', async () => {
    await answer(http.expectOne(isVaccineList), [BCG, DTP]);

    searchInput().value = '  hepa ';
    searchInput().dispatchEvent(new Event('input'));
    vi.advanceTimersByTime(DEBOUNCE_MS - 1);
    http.expectNone(isVaccineList);
    vi.advanceTimersByTime(1);

    const request = http.expectOne(isVaccineList);
    expect(request.request.params.get('search')).toBe('hepa');
    await answer(request, [DTP]);

    expect(texts('td')).toEqual(['DTP', '3', '2']);
  });

  it('does not search again when only the spaces of the term change', async () => {
    await answer(http.expectOne(isVaccineList), [BCG]);

    search('   ');

    http.expectNone(isVaccineList);
  });

  it('drops the response of an older search', async () => {
    await answer(http.expectOne(isVaccineList), [BCG, DTP]);

    search('b');
    const older = http.expectOne(isVaccineList);
    search('d');
    const newer = http.expectOne(isVaccineList);

    expect(older.cancelled).toBe(true);
    await answer(newer, [DTP]);
    expect(texts('td')).toEqual(['DTP', '3', '2']);
  });

  it('keeps the rows on the screen during a new search', async () => {
    await answer(http.expectOne(isVaccineList), [BCG, DTP]);

    search('d');
    await fixture.whenStable();

    expect(skeleton()).toBeNull();
    expect(texts('td')).toEqual(['BCG', '1', '0', 'DTP', '3', '2']);

    await answer(http.expectOne(isVaccineList), [DTP]);
  });

  it('invites the operator to register when there is no vaccine', async () => {
    await answer(http.expectOne(isVaccineList), []);

    expect(emptyState()).toContain('Nenhuma vacina cadastrada.');

    button('Cadastrar vacina')?.click();

    expect(dialog.open).toHaveBeenCalledExactlyOnceWith(VaccineForm);
  });

  it('tells that the search found nothing, without the invitation', async () => {
    await answer(http.expectOne(isVaccineList), [BCG]);

    search('xyz');
    await answer(http.expectOne(isVaccineList), []);

    expect(emptyState()).toContain('Nenhuma vacina encontrada.');
    expect(button('Cadastrar vacina')).toBeUndefined();
  });

  it('shows a failure in a toast and offers a new attempt', async () => {
    await failWith(http.expectOne(isVaccineList));

    expect(toast.error).toHaveBeenCalledExactlyOnceWith('Ocorreu um erro inesperado.');
    expect(emptyState()).toContain('Não foi possível carregar as vacinas.');

    button('Tentar de novo')?.click();
    await fixture.whenStable();

    expect(skeleton()).not.toBeNull();
    await answer(http.expectOne(isVaccineList), [BCG]);
    expect(texts('td')).toEqual(['BCG', '1', '0']);
  });

  it('keeps the rows when a new search fails', async () => {
    await answer(http.expectOne(isVaccineList), [BCG]);

    search('d');
    await failWith(http.expectOne(isVaccineList));

    expect(toast.error).toHaveBeenCalledOnce();
    expect(texts('td')).toEqual(['BCG', '1', '0']);
  });

  it('keeps searching after a failed search', async () => {
    await failWith(http.expectOne(isVaccineList));

    search('bcg');

    await answer(http.expectOne(isVaccineList), [BCG]);
    expect(emptyState()).toBeUndefined();
    expect(texts('td')).toEqual(['BCG', '1', '0']);
  });

  it('reloads the list with the current search when the form registers a vaccine', async () => {
    await answer(http.expectOne(isVaccineList), [BCG]);
    search('d');
    await answer(http.expectOne(isVaccineList), [DTP]);

    button('Nova vacina')?.click();
    expect(dialog.open).toHaveBeenCalledExactlyOnceWith(VaccineForm);
    afterClosed.next(true);

    const request = http.expectOne(isVaccineList);
    expect(request.request.params.get('search')).toBe('d');
    await answer(request, [DTP]);
  });

  it('keeps the list when the form closes without a registration', async () => {
    await answer(http.expectOne(isVaccineList), [BCG]);

    button('Nova vacina')?.click();
    afterClosed.next(undefined);

    http.expectNone(isVaccineList);
  });
});
