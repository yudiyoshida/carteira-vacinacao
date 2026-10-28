# Feature

Modelo. Quando o repositório já tiver um exemplo real, siga o exemplo real. Os nomes da conversão de erros (`core/utils/api-error.ts`) saem da T23.

## Contrato e service

```ts
// core/models/vaccine.model.ts
export interface VaccineDto {
  id: string;
  name: string;
  primaryDoses: number;
  boosterDoses: number;
}

export interface CreateVaccineRequest {
  name: string;
  primaryDoses: number;
  boosterDoses: number;
}
```

```ts
// core/api/vaccine-api.service.ts
@Injectable({ providedIn: 'root' })
export class VaccineApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/vaccines`;

  public findAll(search = ''): Observable<IPagination<VaccineDto>> {
    const params = search ? new HttpParams().set('search', search) : undefined;
    return this.http.get<IPagination<VaccineDto>>(this.base, { params });
  }

  public create(body: CreateVaccineRequest): Observable<{ id: string }> {
    return this.http.post<{ id: string }>(this.base, body);
  }
}
```

## Lista com paginação no servidor

```ts
// modules/main/pages/people-list/people-list.ts
@Component({
  selector: 'app-people-list',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './people-list.html',
  styleUrl: './people-list.scss',
  imports: [MatTableModule, PageHeader, TextInput, DataTable],
})
export class PeopleList {
  private readonly personApi = inject(PersonApiService);

  protected readonly searchControl = new FormControl('', { nonNullable: true });
  protected readonly columns = ['name', 'cpf', 'actions'];
  protected readonly rows = signal<PersonSummaryDto[]>([]);
  protected readonly totalItems = signal(0);
  protected readonly pageIndex = signal(0);
  protected readonly pageSize = signal(20);

  private readonly search = toSignal(
    this.searchControl.valueChanges.pipe(debounceTime(300), startWith(this.searchControl.value)),
    { initialValue: '' },
  );

  constructor() {
    let previousSearch = '';
    effect(() => {
      const search = this.search();
      if (search !== previousSearch) {
        previousSearch = search;
        this.pageIndex.set(0);
      }
      this.fetch(search, this.pageIndex(), this.pageSize());
    });
  }

  protected onPage(event: PageEvent): void {
    this.pageIndex.set(event.pageIndex);
    this.pageSize.set(event.pageSize);
  }

  private fetch(search: string, pageIndex: number, size: number): void {
    this.personApi.findAll({ search, page: pageIndex + 1, size }).subscribe({
      next: (page) => {
        this.rows.set(page.data);
        this.totalItems.set(page.totalItems);
      },
    });
  }
}
```

```html
<!-- modules/main/pages/people-list/people-list.html -->
<app-page-header title="Pessoas" />
<app-text-input label="Buscar por nome" type="search" [control]="searchControl" />

<app-data-table
  [data]="rows()"
  [columns]="columns"
  [totalItems]="totalItems()"
  [pageIndexInput]="pageIndex()"
  [pageSizeInput]="pageSize()"
  ariaLabel="Paginação de pessoas"
  (pageChange)="onPage($event)"
>
  <ng-container matColumnDef="name">
    <th mat-header-cell *matHeaderCellDef>Nome</th>
    <td mat-cell *matCellDef="let person">{{ person.name }}</td>
  </ng-container>
  <!-- colunas cpf e actions -->
</app-data-table>
```

## Formulário

```ts
export class VaccineForm {
  private readonly fb = inject(FormBuilder);
  private readonly vaccineApi = inject(VaccineApiService);
  private readonly toast = inject(ToastService);

  protected readonly loading = signal(false);
  protected readonly form = this.fb.nonNullable.group({
    name: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
    primaryDoses: [1, [Validators.required, Validators.min(1), Validators.max(10)]],
    boosterDoses: [0, [Validators.required, Validators.min(0), Validators.max(5)]],
  });

  protected submit(): void {
    if (this.form.invalid) {
      this.form.markAllAsTouched();
      return;
    }

    this.loading.set(true);
    this.vaccineApi.create(this.form.getRawValue()).subscribe({
      next: () => {
        this.loading.set(false);
        this.toast.success('Vacina cadastrada.');
      },
      error: (error: unknown) => {
        this.loading.set(false);
        applyApiError(error, this.form, this.toast);
      },
    });
  }
}
```

```html
<form [formGroup]="form" (ngSubmit)="submit()">
  <app-text-input label="Nome" [control]="form.controls.name" [disabled]="loading()" />
  <!-- campos das doses -->
  <app-submit-button label="Cadastrar" [loading]="loading()" [disabled]="form.invalid || loading()" />
</form>
```

`applyApiError` representa a conversão de `core/utils/api-error.ts`: aplica os `errors` do ProblemDetails aos campos e mostra o resto no toast.

## Rotas

```ts
// core/routes/routes.enum.ts
export enum RoutesEnum {
  LOGIN = 'login',
  PEOPLE = 'people',
  CARD = 'card',
  VACCINES = 'vaccines',
}
```

```ts
// modules/main/main.routes.ts
export const routes: Routes = [
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./main').then((m) => m.MainLayout),
    children: [
      { path: '', redirectTo: RoutesEnum.PEOPLE, pathMatch: 'full' },
      {
        path: RoutesEnum.PEOPLE,
        loadComponent: () => import('./pages/people-list/people-list').then((m) => m.PeopleList),
      },
      {
        path: `${RoutesEnum.PEOPLE}/:id/${RoutesEnum.CARD}`,
        loadComponent: () =>
          import('./pages/vaccination-card/vaccination-card').then((m) => m.VaccinationCard),
      },
      {
        path: RoutesEnum.VACCINES,
        loadComponent: () => import('./pages/vaccine-list/vaccine-list').then((m) => m.VaccineList),
      },
    ],
  },
];
```

A navegação usa `routePath()`: `this.router.navigate([routePath(RoutesEnum.PEOPLE), id, RoutesEnum.CARD])`.
