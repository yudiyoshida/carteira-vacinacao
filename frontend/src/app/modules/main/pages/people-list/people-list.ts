import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl } from '@angular/forms';
import { MatButton, MatIconButton } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIcon } from '@angular/material/icon';
import { PageEvent } from '@angular/material/paginator';
import { MatTableModule } from '@angular/material/table';
import { RouterLink } from '@angular/router';
import {
  BehaviorSubject,
  catchError,
  debounceTime,
  EMPTY,
  filter,
  finalize,
  map,
  Observable,
  switchMap,
  tap,
} from 'rxjs';
import { PersonApiService } from '../../../../core/api/person-api.service';
import { IPagination } from '../../../../core/models/pagination.model';
import { PersonSummaryDto } from '../../../../core/models/person.model';
import { RoutesEnum } from '../../../../core/routes/routes.enum';
import { routePath } from '../../../../core/routes/routes.util';
import { toApiError } from '../../../../core/utils/api-error';
import { openConfirmModal } from '../../../../shared/components/confirm-modal/confirm-modal';
import { DataTable } from '../../../../shared/components/data-table/data-table';
import { EmptyState } from '../../../../shared/components/empty-state/empty-state';
import { TextInput } from '../../../../shared/components/inputs/text-input/text-input';
import { PageHeader } from '../../../../shared/components/page-header/page-header';
import { Skeleton } from '../../../../shared/components/skeleton/skeleton';
import { ToastService } from '../../../../shared/components/toast/toast.service';
import { PersonForm } from './components/person-form/person-form';

interface PeopleQuery {
  search: string;
  pageIndex: number;
  pageSize: number;
}

const SEARCH_DEBOUNCE_MS = 300;
const INITIAL_QUERY: PeopleQuery = { search: '', pageIndex: 0, pageSize: 20 };

@Component({
  selector: 'app-people-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './people-list.html',
  styleUrl: './people-list.scss',
  imports: [
    RouterLink,
    MatButton,
    MatIcon,
    MatIconButton,
    MatTableModule,
    DataTable,
    EmptyState,
    PageHeader,
    Skeleton,
    TextInput,
  ],
})
export class PeopleList {
  private readonly personApi = inject(PersonApiService);
  private readonly dialog = inject(MatDialog);
  private readonly toast = inject(ToastService);

  protected readonly searchControl = new FormControl('', { nonNullable: true });
  protected readonly searchMaxLength = 100;
  protected readonly columns = ['name', 'cpf', 'actions'];
  protected readonly peoplePath = routePath(RoutesEnum.PEOPLE);
  protected readonly cardSegment = RoutesEnum.CARD;
  protected readonly people = signal<PersonSummaryDto[]>([]);
  protected readonly totalItems = signal(0);
  protected readonly loading = signal(true);
  protected readonly loadFailed = signal(false);
  // One state for search, page and size: a new search goes back to the first page with one request.
  protected readonly query = signal(INITIAL_QUERY);
  // The query of the rows on the screen, which the search field and the paginator may be ahead of.
  protected readonly loadedQuery = signal(INITIAL_QUERY);

  private readonly load$ = new BehaviorSubject<void>(undefined);

  constructor() {
    this.searchControl.valueChanges
      .pipe(
        debounceTime(SEARCH_DEBOUNCE_MS),
        map((search) => search.trim()),
        filter((search) => search !== this.query().search),
        takeUntilDestroyed(),
      )
      .subscribe((search) => this.changeQuery({ search, pageIndex: 0 }));

    this.load$
      .pipe(
        // Cancels the previous request, whose response could arrive after the one of a newer query.
        switchMap(() => this.load(this.query())),
        takeUntilDestroyed(),
      )
      .subscribe();
  }

  protected reload(): void {
    this.load$.next();
  }

  protected onPage({ pageIndex, pageSize }: PageEvent): void {
    this.changeQuery({ pageIndex, pageSize });
  }

  protected openForm(): void {
    this.dialog
      .open<PersonForm, void, boolean>(PersonForm)
      .afterClosed()
      .pipe(filter(Boolean))
      .subscribe(() => this.reload());
  }

  protected confirmRemoval(person: PersonSummaryDto): void {
    openConfirmModal(this.dialog, {
      heading: 'Remover pessoa',
      message: `Remover ${person.name}? A pessoa, o cartão de vacinação e todas as vacinações serão apagados definitivamente.`,
      confirmLabel: 'Remover',
    })
      .afterClosed()
      .pipe(
        filter(Boolean),
        switchMap(() => this.personApi.delete(person.id)),
      )
      .subscribe({
        next: ({ message }) => {
          this.toast.success(message);
          this.reload();
        },
        error: (error: unknown) => this.toast.error(toApiError(error).message),
      });
  }

  private changeQuery(changes: Partial<PeopleQuery>): void {
    this.query.update((query) => ({ ...query, ...changes }));
    this.reload();
  }

  private load(query: PeopleQuery): Observable<IPagination<PersonSummaryDto>> {
    this.loading.set(true);
    this.loadFailed.set(false);

    return this.personApi
      .findAll({ search: query.search, page: query.pageIndex + 1, size: query.pageSize })
      .pipe(
        tap((page) => this.show(query, page)),
        // An inner catch keeps the list alive for the next query.
        catchError((error: unknown) => {
          this.loadFailed.set(true);
          this.toast.error(toApiError(error).message);
          // The rows stay, so the paginator, which moved on the click, goes back to them.
          const { pageIndex, pageSize } = this.loadedQuery();
          this.query.update((current) => ({ ...current, pageIndex, pageSize }));
          return EMPTY;
        }),
        finalize(() => this.loading.set(false)),
      );
  }

  private show(query: PeopleQuery, page: IPagination<PersonSummaryDto>): void {
    // A page past the last one has no rows (TechSpec §7.1), as after removing its only person.
    if (page.data.length === 0 && query.pageIndex > 0) {
      this.changeQuery({ pageIndex: Math.max(page.totalPages - 1, 0) });
      return;
    }

    this.people.set(page.data);
    this.totalItems.set(page.totalItems);
    this.loadedQuery.set(query);
  }
}
