import { ChangeDetectionStrategy, Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormControl } from '@angular/forms';
import { MatButton } from '@angular/material/button';
import { MatDialog } from '@angular/material/dialog';
import { MatIcon } from '@angular/material/icon';
import { MatTableModule } from '@angular/material/table';
import {
  BehaviorSubject,
  catchError,
  combineLatest,
  debounceTime,
  distinctUntilChanged,
  EMPTY,
  filter,
  finalize,
  map,
  Observable,
  startWith,
  switchMap,
  tap,
} from 'rxjs';
import { VaccineApiService } from '../../../../core/api/vaccine-api.service';
import { IPagination } from '../../../../core/models/pagination.model';
import { VaccineDto } from '../../../../core/models/vaccine.model';
import { toApiError } from '../../../../core/utils/api-error';
import { DataTable } from '../../../../shared/components/data-table/data-table';
import { EmptyState } from '../../../../shared/components/empty-state/empty-state';
import { TextInput } from '../../../../shared/components/inputs/text-input/text-input';
import { PageHeader } from '../../../../shared/components/page-header/page-header';
import { Skeleton } from '../../../../shared/components/skeleton/skeleton';
import { ToastService } from '../../../../shared/components/toast/toast.service';
import { VaccineForm } from './components/vaccine-form/vaccine-form';

const SEARCH_DEBOUNCE_MS = 300;

@Component({
  selector: 'app-vaccine-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './vaccine-list.html',
  styleUrl: './vaccine-list.scss',
  imports: [
    MatButton,
    MatIcon,
    MatTableModule,
    DataTable,
    EmptyState,
    PageHeader,
    Skeleton,
    TextInput,
  ],
})
export class VaccineList {
  private readonly vaccineApi = inject(VaccineApiService);
  private readonly dialog = inject(MatDialog);
  private readonly toast = inject(ToastService);

  protected readonly searchControl = new FormControl('', { nonNullable: true });
  protected readonly searchMaxLength = 100;
  protected readonly columns = ['name', 'primaryDoses', 'boosterDoses'];
  protected readonly vaccines = signal<VaccineDto[]>([]);
  protected readonly loading = signal(true);
  protected readonly loadFailed = signal(false);
  // The term of the rows on the screen, which the search field may already be ahead of.
  protected readonly loadedSearch = signal('');

  private readonly reload$ = new BehaviorSubject<void>(undefined);

  constructor() {
    const search$ = this.searchControl.valueChanges.pipe(
      debounceTime(SEARCH_DEBOUNCE_MS),
      map((search) => search.trim()),
      startWith(''),
      distinctUntilChanged(),
    );

    combineLatest([search$, this.reload$])
      .pipe(
        // Cancels the previous request, whose response could arrive after the one of a newer search.
        switchMap(([search]) => this.load(search)),
        takeUntilDestroyed(),
      )
      .subscribe();
  }

  protected reload(): void {
    this.reload$.next();
  }

  protected openForm(): void {
    this.dialog
      .open<VaccineForm, void, boolean>(VaccineForm)
      .afterClosed()
      .pipe(filter(Boolean))
      .subscribe(() => this.reload());
  }

  private load(search: string): Observable<IPagination<VaccineDto>> {
    this.loading.set(true);
    this.loadFailed.set(false);

    return this.vaccineApi.findAll(search).pipe(
      tap((page) => {
        this.vaccines.set(page.data);
        this.loadedSearch.set(search);
      }),
      // An inner catch keeps the search alive for the next term.
      catchError((error: unknown) => {
        this.loadFailed.set(true);
        this.toast.error(toApiError(error).message);
        return EMPTY;
      }),
      finalize(() => this.loading.set(false)),
    );
  }
}
