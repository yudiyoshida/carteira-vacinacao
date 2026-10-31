# Feature

Modelo. Quando o repositório já tiver um exemplo real, siga o exemplo real. A T23 criou a base: a conversão de erros (`frontend/src/app/core/utils/api-error.ts`), o toast (`frontend/src/app/shared/components/toast/toast.service.ts`), as rotas (`frontend/src/app/core/routes/` e `frontend/src/app/modules/main/main.routes.ts`) e os layouts (`frontend/src/app/modules/auth/auth.ts` e `frontend/src/app/modules/main/main.ts`). A T24 criou o primeiro service da API (`frontend/src/app/core/api/auth-api.service.ts`), o primeiro formulário (`frontend/src/app/modules/auth/pages/login/`), os campos (`frontend/src/app/shared/components/inputs/`) e o botão de envio (`frontend/src/app/shared/components/submit-button/`). As telas deste modelo chegam nas tasks seguintes.

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

Exemplo real: `frontend/src/app/modules/auth/pages/login/login.ts`. Uma tela de cadastro segue o mesmo desenho, com a mensagem de sucesso no toast:

```ts
export class VaccineForm {
  private readonly fb = inject(NonNullableFormBuilder);
  private readonly vaccineApi = inject(VaccineApiService);
  private readonly toast = inject(ToastService);

  protected readonly loading = signal(false);
  protected readonly form = this.fb.group({
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
    this.vaccineApi
      .create(this.form.getRawValue())
      .pipe(finalize(() => this.loading.set(false)))
      .subscribe({
        next: () => this.toast.success('Vacina cadastrada.'),
        error: (error: unknown) => applyApiError(error, this.form, this.toast),
      });
  }
}
```

```html
<form [formGroup]="form" (ngSubmit)="submit()">
  <app-text-input label="Nome" [control]="form.controls.name" />
  <!-- campos das doses -->
  <app-submit-button label="Cadastrar" [loading]="loading()" />
</form>
```

- `applyApiError` (`core/utils/api-error.ts`) aplica os `errors` do ProblemDetails aos campos, com a mensagem em `control.errors[API_ERROR_KEY]`, e mostra o resto no toast. Para decidir por um erro específico, use `toApiError(error).code`, que é `null` numa resposta que não veio da API.
- O `submit-button` não recebe `disabled`: com o formulário inválido, o clique precisa chegar ao `markAllAsTouched`. Durante o `loading`, o botão fica desabilitado, e isso impede o envio duplo.
- Os campos não recebem `disabled`: o `matInput` ignora o `[disabled]` num `formControl`, e `control.enable()` roda os validadores de novo e apaga o erro da API. Os campos continuam editáveis durante o envio.
- Uma tela sem mensagem de sucesso, como o login, chama `toast.dismiss()` no envio. Sem isso, o erro da tentativa anterior continua na tela seguinte.

## Campo de formulário

Exemplo real: `frontend/src/app/shared/components/inputs/email-input/email-input.ts`.

- O campo recebe `label` e `control` e usa o estilo comum `shared/components/inputs/input.scss`.
- A mensagem de erro sai de `fieldError` (`shared/components/inputs/field-error.ts`), com a mensagem da API antes das mensagens do campo.
- `errorMessage()` é um método, porque `control.errors` não é signal: um `computed` guardaria o primeiro valor.
- Um campo `OnPush` atualiza sozinho com `markAllAsTouched` e `setErrors`, sem observar `control.events`, porque as classes de status do `formControl` leem os signals internos do controle. Uma troca de erro sem mudança de status, de inválido para inválido, só aparece na próxima edição do campo.

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

Uma rota de `main.routes.ts` ainda sem página fica com `children: []`, sem componente provisório; a task da página troca por `loadComponent`. `RoutesEnum.CARD` entra com a página do cartão (T28).

A navegação usa `routePath()`: `this.router.navigate([routePath(RoutesEnum.PEOPLE), id, RoutesEnum.CARD])`.
