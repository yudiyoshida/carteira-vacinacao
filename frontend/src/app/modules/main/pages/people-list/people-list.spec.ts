import { HttpRequest, provideHttpClient } from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
  TestRequest,
} from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MatDialog } from '@angular/material/dialog';
import { By } from '@angular/platform-browser';
import { Subject } from 'rxjs';
import { IPagination } from '../../../../core/models/pagination.model';
import { PersonSummaryDto } from '../../../../core/models/person.model';
import { ProblemDetails } from '../../../../core/models/problem-details.model';
import { ConfirmModal } from '../../../../shared/components/confirm-modal/confirm-modal';
import { DataTable } from '../../../../shared/components/data-table/data-table';
import { ToastService } from '../../../../shared/components/toast/toast.service';
import { PersonForm } from './components/person-form/person-form';
import { PeopleList } from './people-list';

const PEOPLE_URL = '/api/v1/people';
const DEBOUNCE_MS = 300;

const ANA: PersonSummaryDto = { id: 'ana-id', name: 'Ana', cpf: '***.111.222-**' };
const MARIA: PersonSummaryDto = { id: 'maria-id', name: 'Maria', cpf: '***.456.789-**' };

function makePage(
  data: PersonSummaryDto[],
  page: Partial<IPagination<PersonSummaryDto>> = {},
): IPagination<PersonSummaryDto> {
  return {
    currentPage: 1,
    itemsPerPage: 20,
    totalItems: data.length,
    totalPages: data.length > 0 ? 1 : 0,
    data,
    ...page,
  };
}

function isPeopleList(request: HttpRequest<unknown>): boolean {
  return request.method === 'GET' && request.url === PEOPLE_URL;
}

describe('PeopleList', () => {
  let fixture: ComponentFixture<PeopleList>;
  let http: HttpTestingController;
  let toast: { success: (message: string) => void; error: (message: string) => void };
  let dialog: { open: (component: unknown, config?: unknown) => unknown };
  let afterClosed: Subject<boolean | undefined>;

  function element(): HTMLElement {
    return fixture.nativeElement;
  }

  function names(): (string | undefined)[] {
    return Array.from(element().querySelectorAll('td:first-child')).map((cell) =>
      cell.textContent?.trim(),
    );
  }

  function button(label: string): HTMLButtonElement | undefined {
    return Array.from(element().querySelectorAll('button')).find((candidate) =>
      candidate.textContent?.includes(label),
    );
  }

  function namedButton(label: string): HTMLButtonElement {
    return element().querySelector(`button[aria-label="${label}"]`) as HTMLButtonElement;
  }

  function rangeLabel(): string | undefined {
    return element().querySelector('mat-paginator [role="status"]')?.textContent?.trim();
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

  function changePage(pageIndex: number, pageSize: number): void {
    const dataTable: DataTable<PersonSummaryDto> = fixture.debugElement.query(
      By.directive(DataTable),
    ).componentInstance;
    dataTable.pageChange.emit({ pageIndex, pageSize, length: 0 });
  }

  async function answer(
    request: TestRequest,
    data: PersonSummaryDto[],
    page: Partial<IPagination<PersonSummaryDto>> = {},
  ): Promise<void> {
    request.flush(makePage(data, page));
    await fixture.whenStable();
  }

  async function failWith(request: TestRequest): Promise<void> {
    const problem: ProblemDetails = {
      title: 'Erro inesperado',
      status: 500,
      detail: 'Ocorreu um erro inesperado.',
      instance: PEOPLE_URL,
      code: 'internal.unexpected',
      traceId: '00-abc',
    };
    request.flush(problem, { status: 500, statusText: 'Internal Server Error' });
    await fixture.whenStable();
  }

  // Two pages of people on the screen, on the second one.
  async function openSecondPage(): Promise<void> {
    await answer(http.expectOne(isPeopleList), [ANA], { totalItems: 21, totalPages: 2 });
    namedButton('Próxima página').click();
    await answer(http.expectOne(isPeopleList), [MARIA], {
      currentPage: 2,
      totalItems: 21,
      totalPages: 2,
    });
  }

  beforeEach(async () => {
    // RxJS schedules the debounce with setInterval and measures it with Date. The others stay real,
    // because the change detection that whenStable awaits runs on setTimeout.
    vi.useFakeTimers({ toFake: ['setInterval', 'clearInterval', 'Date'] });
    afterClosed = new Subject();
    toast = { success: vi.fn(), error: vi.fn() };
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
    fixture = TestBed.createComponent(PeopleList);
    await fixture.whenStable();
  });

  afterEach(() => {
    http.verify();
    vi.useRealTimers();
  });

  it('RN19: opens on the first page, with 20 people, in the order of the API', async () => {
    const request = http.expectOne(isPeopleList);
    expect(request.request.params.keys()).toEqual(['page', 'size']);
    expect(request.request.params.get('page')).toBe('1');
    expect(request.request.params.get('size')).toBe('20');

    await answer(request, [ANA, MARIA]);

    expect(names()).toEqual(['Ana', 'Maria']);
  });

  it('RN26: shows the CPF masked, as the API sends it', async () => {
    await answer(http.expectOne(isPeopleList), [MARIA]);

    expect(element().querySelector('td:nth-child(2)')?.textContent?.trim()).toBe('***.456.789-**');
  });

  it('names the remove button after the person', async () => {
    await answer(http.expectOne(isPeopleList), [MARIA]);

    expect(namedButton('Remover Maria')).not.toBeNull();
  });

  it('shows the skeleton until the first response arrives', async () => {
    expect(skeleton()).not.toBeNull();

    await answer(http.expectOne(isPeopleList), [ANA]);

    expect(skeleton()).toBeNull();
  });

  it('limits the search to the length the API accepts', () => {
    expect(searchInput().getAttribute('maxlength')).toBe('100');

    http.expectOne(isPeopleList).flush(makePage([]));
  });

  it('RN19: searches by the trimmed term, only after the operator stops typing', async () => {
    await answer(http.expectOne(isPeopleList), [ANA, MARIA]);

    searchInput().value = '  mar ';
    searchInput().dispatchEvent(new Event('input'));
    vi.advanceTimersByTime(DEBOUNCE_MS - 1);
    http.expectNone(isPeopleList);
    vi.advanceTimersByTime(1);

    const request = http.expectOne(isPeopleList);
    expect(request.request.params.get('search')).toBe('mar');
    await answer(request, [MARIA]);

    expect(names()).toEqual(['Maria']);
  });

  it('does not search again when only the spaces of the term change', async () => {
    await answer(http.expectOne(isPeopleList), [ANA]);

    search('   ');

    http.expectNone(isPeopleList);
  });

  it('drops the response of an older search', async () => {
    await answer(http.expectOne(isPeopleList), [ANA, MARIA]);

    search('a');
    const older = http.expectOne(isPeopleList);
    search('m');
    const newer = http.expectOne(isPeopleList);

    expect(older.cancelled).toBe(true);
    await answer(newer, [MARIA]);
    expect(names()).toEqual(['Maria']);
  });

  it('keeps the rows on the screen during a new search', async () => {
    await answer(http.expectOne(isPeopleList), [ANA, MARIA]);

    search('m');
    await fixture.whenStable();

    expect(skeleton()).toBeNull();
    expect(names()).toEqual(['Ana', 'Maria']);

    await answer(http.expectOne(isPeopleList), [MARIA]);
  });

  it('RN19: shows the range of the page and asks for the next one', async () => {
    await answer(http.expectOne(isPeopleList), [ANA], { totalItems: 42, totalPages: 3 });

    expect(rangeLabel()).toBe('1 – 20 de 42');

    namedButton('Próxima página').click();

    const request = http.expectOne(isPeopleList);
    expect(request.request.params.get('page')).toBe('2');
    expect(request.request.params.get('size')).toBe('20');
    await answer(request, [MARIA], { currentPage: 2, totalItems: 42, totalPages: 3 });

    expect(names()).toEqual(['Maria']);
    expect(rangeLabel()).toBe('21 – 40 de 42');
  });

  it('RN19: asks for the page and the size the paginator chooses', async () => {
    await answer(http.expectOne(isPeopleList), [ANA], { totalItems: 42, totalPages: 3 });

    changePage(1, 10);

    const request = http.expectOne(isPeopleList);
    expect(request.request.params.get('page')).toBe('2');
    expect(request.request.params.get('size')).toBe('10');
    await answer(request, [MARIA], { currentPage: 2, itemsPerPage: 10, totalItems: 42 });

    expect(rangeLabel()).toBe('11 – 20 de 42');
  });

  it('RN19: goes back to the first page on a new search, with one request', async () => {
    await openSecondPage();
    changePage(1, 10);
    await answer(http.expectOne(isPeopleList), [MARIA], { currentPage: 2, totalItems: 21 });

    search('mar');

    const request = http.expectOne(isPeopleList);
    expect(request.request.params.get('page')).toBe('1');
    expect(request.request.params.get('size')).toBe('10');
    expect(request.request.params.get('search')).toBe('mar');
    await answer(request, [MARIA]);

    expect(rangeLabel()).toBe('1 – 1 de 1');
  });

  it('keeps the search when the page changes', async () => {
    await answer(http.expectOne(isPeopleList), [ANA]);
    search('a');
    await answer(http.expectOne(isPeopleList), [ANA], { totalItems: 21, totalPages: 2 });

    namedButton('Próxima página').click();

    const request = http.expectOne(isPeopleList);
    expect(request.request.params.get('page')).toBe('2');
    expect(request.request.params.get('search')).toBe('a');
    await answer(request, [MARIA], { currentPage: 2, totalItems: 21, totalPages: 2 });
  });

  it('invites the operator to register when there is no person', async () => {
    await answer(http.expectOne(isPeopleList), []);

    expect(emptyState()).toContain('Nenhuma pessoa cadastrada.');
    expect(element().querySelector('mat-paginator')).toBeNull();

    button('Cadastrar pessoa')?.click();

    expect(dialog.open).toHaveBeenCalledExactlyOnceWith(PersonForm);
  });

  it('tells that the search found nothing, without the invitation', async () => {
    await answer(http.expectOne(isPeopleList), [ANA]);

    search('xyz');
    await answer(http.expectOne(isPeopleList), []);

    expect(emptyState()).toContain('Nenhuma pessoa encontrada.');
    expect(button('Cadastrar pessoa')).toBeUndefined();
  });

  it('shows a failure in a toast and offers a new attempt', async () => {
    await failWith(http.expectOne(isPeopleList));

    expect(toast.error).toHaveBeenCalledExactlyOnceWith('Ocorreu um erro inesperado.');
    expect(emptyState()).toContain('Não foi possível carregar as pessoas.');

    button('Tentar de novo')?.click();
    await fixture.whenStable();

    expect(skeleton()).not.toBeNull();
    await answer(http.expectOne(isPeopleList), [ANA]);
    expect(names()).toEqual(['Ana']);
  });

  it('keeps the rows when a new search fails', async () => {
    await answer(http.expectOne(isPeopleList), [ANA]);

    search('m');
    await failWith(http.expectOne(isPeopleList));

    expect(toast.error).toHaveBeenCalledOnce();
    expect(names()).toEqual(['Ana']);
  });

  it('keeps searching after a failed search', async () => {
    await failWith(http.expectOne(isPeopleList));

    search('ana');

    await answer(http.expectOne(isPeopleList), [ANA]);
    expect(emptyState()).toBeUndefined();
    expect(names()).toEqual(['Ana']);
  });

  it('keeps the rows and sends the paginator back to them when a page change fails', async () => {
    await answer(http.expectOne(isPeopleList), [ANA], { totalItems: 42, totalPages: 3 });

    namedButton('Próxima página').click();
    await fixture.whenStable();
    expect(rangeLabel()).toBe('21 – 40 de 42');

    await failWith(http.expectOne(isPeopleList));

    expect(toast.error).toHaveBeenCalledOnce();
    expect(names()).toEqual(['Ana']);
    expect(rangeLabel()).toBe('1 – 20 de 42');

    namedButton('Próxima página').click();

    const retry = http.expectOne(isPeopleList);
    expect(retry.request.params.get('page')).toBe('2');
    await answer(retry, [MARIA], { currentPage: 2, totalItems: 42, totalPages: 3 });
  });

  it('sends the paginator back to the rows when a search from another page fails', async () => {
    await openSecondPage();

    search('m');
    const request = http.expectOne(isPeopleList);
    expect(request.request.params.get('page')).toBe('1');
    await fixture.whenStable();
    expect(rangeLabel()).toBe('1 – 20 de 21');

    await failWith(request);

    expect(names()).toEqual(['Maria']);
    expect(rangeLabel()).toBe('21 – 21 de 21');
  });

  it('reloads the current search and page when the form registers a person', async () => {
    await answer(http.expectOne(isPeopleList), [ANA]);
    search('a');
    await answer(http.expectOne(isPeopleList), [ANA], { totalItems: 21, totalPages: 2 });
    namedButton('Próxima página').click();
    await answer(http.expectOne(isPeopleList), [MARIA], {
      currentPage: 2,
      totalItems: 21,
      totalPages: 2,
    });

    button('Nova pessoa')?.click();
    expect(dialog.open).toHaveBeenCalledExactlyOnceWith(PersonForm);
    afterClosed.next(true);

    const request = http.expectOne(isPeopleList);
    expect(request.request.params.get('search')).toBe('a');
    expect(request.request.params.get('page')).toBe('2');
    await answer(request, [MARIA], { currentPage: 2, totalItems: 22, totalPages: 2 });
  });

  it('keeps the list when the form closes without a registration', async () => {
    await answer(http.expectOne(isPeopleList), [ANA]);

    button('Nova pessoa')?.click();
    afterClosed.next(undefined);

    http.expectNone(isPeopleList);
  });

  it('RN04: warns that the removal is definitive, before sending anything', async () => {
    await answer(http.expectOne(isPeopleList), [MARIA]);

    namedButton('Remover Maria').click();

    expect(dialog.open).toHaveBeenCalledExactlyOnceWith(
      ConfirmModal,
      expect.objectContaining({
        data: {
          heading: 'Remover pessoa',
          message:
            'Remover Maria? A pessoa, o cartão de vacinação e todas as vacinações serão apagados definitivamente.',
          confirmLabel: 'Remover',
        },
      }),
    );
    http.expectNone({ method: 'DELETE', url: `${PEOPLE_URL}/maria-id` });
  });

  it.each([[false], [undefined]])(
    'keeps the person when the confirmation closes with %s',
    async (result) => {
      await answer(http.expectOne(isPeopleList), [MARIA]);

      namedButton('Remover Maria').click();
      afterClosed.next(result);

      http.expectNone({ method: 'DELETE', url: `${PEOPLE_URL}/maria-id` });
      http.expectNone(isPeopleList);
      expect(toast.success).not.toHaveBeenCalled();
    },
  );

  it('RN04: removes the person, shows the message of the API and reloads the list', async () => {
    await answer(http.expectOne(isPeopleList), [ANA, MARIA]);

    namedButton('Remover Maria').click();
    afterClosed.next(true);

    http
      .expectOne({ method: 'DELETE', url: `${PEOPLE_URL}/maria-id` })
      .flush({ message: 'Pessoa removida com sucesso.' });

    expect(toast.success).toHaveBeenCalledExactlyOnceWith('Pessoa removida com sucesso.');
    await answer(http.expectOne(isPeopleList), [ANA]);
    expect(names()).toEqual(['Ana']);
  });

  it('shows a failed removal in a toast and keeps the rows', async () => {
    await answer(http.expectOne(isPeopleList), [MARIA]);

    namedButton('Remover Maria').click();
    afterClosed.next(true);

    const problem: ProblemDetails = {
      title: 'Recurso não encontrado',
      status: 404,
      detail: 'Pessoa não encontrada.',
      instance: `${PEOPLE_URL}/maria-id`,
      code: 'person.not_found',
      traceId: '00-abc',
    };
    http
      .expectOne({ method: 'DELETE', url: `${PEOPLE_URL}/maria-id` })
      .flush(problem, { status: 404, statusText: 'Not Found' });
    await fixture.whenStable();

    expect(toast.error).toHaveBeenCalledExactlyOnceWith('Pessoa não encontrada.');
    expect(toast.success).not.toHaveBeenCalled();
    expect(names()).toEqual(['Maria']);
  });

  it('goes back to the last page when the removal empties the current one', async () => {
    await openSecondPage();

    namedButton('Remover Maria').click();
    afterClosed.next(true);
    http
      .expectOne({ method: 'DELETE', url: `${PEOPLE_URL}/maria-id` })
      .flush({ message: 'Pessoa removida com sucesso.' });

    // A page past the last one answers 200 with no rows (TechSpec §7.1).
    await answer(http.expectOne(isPeopleList), [], {
      currentPage: 2,
      totalItems: 20,
      totalPages: 1,
    });

    expect(emptyState()).toBeUndefined();
    expect(names()).toEqual(['Maria']);

    const lastPage = http.expectOne(isPeopleList);
    expect(lastPage.request.params.get('page')).toBe('1');
    await answer(lastPage, [ANA], { totalItems: 20, totalPages: 1 });

    expect(names()).toEqual(['Ana']);
    expect(rangeLabel()).toBe('1 – 20 de 20');
  });

  it('shows the empty list when no page is left after going back', async () => {
    await openSecondPage();

    namedButton('Remover Maria').click();
    afterClosed.next(true);
    http
      .expectOne({ method: 'DELETE', url: `${PEOPLE_URL}/maria-id` })
      .flush({ message: 'Pessoa removida com sucesso.' });
    await answer(http.expectOne(isPeopleList), [], { currentPage: 2 });

    const firstPage = http.expectOne(isPeopleList);
    expect(firstPage.request.params.get('page')).toBe('1');
    await answer(firstPage, []);

    expect(emptyState()).toContain('Nenhuma pessoa cadastrada.');
  });
});
