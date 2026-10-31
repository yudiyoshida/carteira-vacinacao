# Feature

Modelo. Quando o repositório já tiver um exemplo real, siga o exemplo real. A T23 criou a base: a conversão de erros (`frontend/src/app/core/utils/api-error.ts`), o toast (`frontend/src/app/shared/components/toast/toast.service.ts`), as rotas (`frontend/src/app/core/routes/` e `frontend/src/app/modules/main/main.routes.ts`) e os layouts (`frontend/src/app/modules/auth/auth.ts` e `frontend/src/app/modules/main/main.ts`). A T24 criou o primeiro formulário (`frontend/src/app/modules/auth/pages/login/`), os primeiros campos (`frontend/src/app/shared/components/inputs/`) e o botão de envio (`frontend/src/app/shared/components/submit-button/`). A T25 criou a primeira tela de dados (`frontend/src/app/modules/main/pages/vaccine-list/`) e os componentes de `frontend/src/app/shared/components/` que ela usa. Os modelos abaixo cobrem o que ainda não existe: a paginação e a rota do cartão.

## Contrato e service

Exemplos reais: `frontend/src/app/core/models/vaccine.model.ts` e `frontend/src/app/core/api/vaccine-api.service.ts`.

- Um parâmetro de busca vazio fica fora da query string.
- O corpo e a resposta têm os mesmos campos da TechSpec §7.3.

## Lista

Exemplo real: `frontend/src/app/modules/main/pages/vaccine-list/vaccine-list.ts`.

- A busca passa por `debounceTime`, `trim` e `distinctUntilChanged`, e o campo de busca recebe o `maxLength` que a API aceita (TechSpec §7.1).
- A chamada fica num `switchMap`, que cancela a requisição anterior: sem isso, a resposta de uma busca antiga pode chegar depois da resposta da busca nova. O `effect` com um `subscribe` por chamada deixa essa corrida aberta.
- O `catchError` fica dentro do `switchMap`. Fora dele, a primeira falha encerraria a busca.
- Os estados seguem esta ordem no HTML: tabela quando há linhas, `skeleton` carregando sem linhas, falha de carga, busca sem resultado e lista vazia. As linhas ficam na tela durante uma nova busca, e assim a lista não pisca a cada tecla.
- O `rxResource` fica de fora enquanto for `@experimental`.

### Tabela

Exemplo real: `frontend/src/app/shared/components/data-table/data-table.ts`.

- A página declara as colunas como `matColumnDef` dentro do `<app-data-table>`, e o componente as registra com `addColumnDef`.
- `ariaLabel` dá o nome acessível da tabela.

### Paginação no servidor

Modelo, para a primeira lista paginada. O `data-table` ganha `totalItems`, `pageIndex`, `pageSize`, `pageChange` e os textos do paginador em português; sem `totalItems`, o paginador não aparece, como na lista de vacinas. Busca, página e tamanho ficam num só estado, para uma busca nova voltar à primeira página com uma única requisição:

```ts
// modules/main/pages/people-list/people-list.ts
export class PeopleList {
  private readonly query$ = new BehaviorSubject({ search: '', pageIndex: 0, pageSize: 20 });

  protected readonly searchControl = new FormControl('', { nonNullable: true });
  protected readonly rows = signal<PersonSummaryDto[]>([]);
  protected readonly totalItems = signal(0);

  constructor() {
    this.searchControl.valueChanges
      .pipe(
        debounceTime(SEARCH_DEBOUNCE_MS),
        map((search) => search.trim()),
        distinctUntilChanged(),
        takeUntilDestroyed(),
      )
      .subscribe((search) => this.query$.next({ ...this.query$.value, search, pageIndex: 0 }));

    this.query$
      .pipe(
        switchMap((query) => this.load(query)),
        takeUntilDestroyed(),
      )
      .subscribe();
  }

  protected onPage({ pageIndex, pageSize }: PageEvent): void {
    this.query$.next({ ...this.query$.value, pageIndex, pageSize });
  }
}
```

```html
<!-- modules/main/pages/people-list/people-list.html -->
<app-data-table
  ariaLabel="Pessoas cadastradas"
  [data]="rows()"
  [columns]="columns"
  [totalItems]="totalItems()"
  [pageIndex]="pageIndex()"
  [pageSize]="pageSize()"
  (pageChange)="onPage($event)"
>
  <!-- colunas, como na lista de vacinas -->
</app-data-table>
```

### Cabeçalho, estado vazio e skeleton

Exemplos reais: `frontend/src/app/shared/components/page-header/`, `frontend/src/app/shared/components/empty-state/` e `frontend/src/app/shared/components/skeleton/`.

- O título entra por `heading`, não por `title`. Um atributo estático continua no host, e o `title` viraria um tooltip nativo em toda a área do componente.
- As ações do cabeçalho entram com o atributo `actions`, como o botão "Nova vacina".

## Formulário

Exemplos reais: `frontend/src/app/modules/auth/pages/login/login.ts` e, num dialog, `frontend/src/app/modules/main/pages/vaccine-list/components/vaccine-form/vaccine-form.ts`.

- `applyApiError` (`core/utils/api-error.ts`) aplica os `errors` do ProblemDetails aos campos, com a mensagem em `control.errors[API_ERROR_KEY]`, e mostra o resto no toast. Para decidir por um erro específico, use `toApiError(error).code`, que é `null` numa resposta que não veio da API.
- O `submit-button` não recebe `disabled`: com o formulário inválido, o clique precisa chegar ao `markAllAsTouched`. Durante o `loading`, o botão fica desabilitado, e isso impede o envio duplo.
- Os campos não recebem `disabled`: o `matInput` ignora o `[disabled]` num `formControl`, e `control.enable()` roda os validadores de novo e apaga o erro da API. Os campos continuam editáveis durante o envio.
- Uma tela sem mensagem de sucesso, como o login, chama `toast.dismiss()` no envio. Sem isso, o erro da tentativa anterior continua na tela seguinte.
- As mensagens dos validadores repetem as da API. O que só a API recusa, como um nome feito de espaços ou uma dose decimal, volta pelo `400` e aparece no campo.

### Formulário em dialog

- A página abre o formulário com `MatDialog.open` e recarrega os dados só com o resultado `true`: `afterClosed().pipe(filter(Boolean))`.
- O formulário recebe o `MatDialogRef` por `inject` e, no sucesso, mostra a mensagem no toast e chama `close(true)`. Uma criação responde só `{ id }`, então a mensagem é da interface: "Vacina cadastrada com sucesso.".
- Durante o envio, `dialogRef.disableClose` fica `true` e o "Cancelar" fica desabilitado. Um dialog fechado no meio da requisição perderia o resultado, e a página não recarregaria.
- Um erro que não é de campo, como um nome repetido, vai para o toast, e o dialog continua aberto para a correção.
- O `:host` do formulário tem `width: var(--app-dialog-width)` e `max-width: 100%`, que respeita o limite do dialog no celular.

## Campo de formulário

Exemplos reais: `frontend/src/app/shared/components/inputs/email-input/email-input.ts` e, com as mensagens vindas da página, `frontend/src/app/shared/components/inputs/text-input/text-input.ts`.

- O campo recebe `label` e `control` e usa o estilo comum `shared/components/inputs/input.scss`.
- A mensagem de erro sai de `fieldError` (`shared/components/inputs/field-error.ts`), com a mensagem da API antes das mensagens do campo.
- `errorMessage()` é um método, porque `control.errors` não é signal: um `computed` guardaria o primeiro valor.
- Um campo `OnPush` atualiza sozinho com `markAllAsTouched` e `setErrors`, sem observar `control.events`, porque as classes de status do `formControl` leem os signals internos do controle. Uma troca de erro sem mudança de status, de inválido para inválido, só aparece na próxima edição do campo.
- `text-input` e `number-input` usam `subscriptSizing="dynamic"`: uma mensagem longa ocupa mais de uma linha e cobriria o campo seguinte. Sem o espaço reservado para o erro, quem usa o campo define o `gap`.
- `min`, `max` e `maxlength` entram por `[attr.*]`. Com o binding de propriedade, o Angular ativaria os validadores dele, e a regra deve ficar só no `FormControl`.

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
