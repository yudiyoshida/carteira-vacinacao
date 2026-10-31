# Feature

Modelo. Quando o repositório já tiver um exemplo real, siga o exemplo real. A T23 criou a base: a conversão de erros (`frontend/src/app/core/utils/api-error.ts`), o toast (`frontend/src/app/shared/components/toast/toast.service.ts`), as rotas (`frontend/src/app/core/routes/` e `frontend/src/app/modules/main/main.routes.ts`) e os layouts (`frontend/src/app/modules/auth/auth.ts` e `frontend/src/app/modules/main/main.ts`). A T24 criou o primeiro formulário (`frontend/src/app/modules/auth/pages/login/`), os primeiros campos (`frontend/src/app/shared/components/inputs/`) e o botão de envio (`frontend/src/app/shared/components/submit-button/`). A T25 criou a primeira tela de dados (`frontend/src/app/modules/main/pages/vaccine-list/`) e os componentes de `frontend/src/app/shared/components/` que ela usa. A T26 criou a primeira lista paginada (`frontend/src/app/modules/main/pages/people-list/`), o campo com máscara (`frontend/src/app/shared/components/inputs/cpf-input/`) e o modal de confirmação (`frontend/src/app/shared/components/confirm-modal/`). O modelo abaixo cobre o que ainda não existe: a rota do cartão.

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

Exemplo real: `frontend/src/app/modules/main/pages/people-list/people-list.ts`.

- O `data-table` recebe `totalItems`, `pageIndex` e `pageSize` e emite `pageChange`. Sem `totalItems`, o paginador não aparece, como na lista de vacinas.
- Busca, página e tamanho ficam num só signal (`query`), para uma busca nova voltar à primeira página com uma única requisição. O `load$` dispara a carga, sempre com o valor atual do `query`, e o `switchMap` cancela a anterior.
- A busca compara o termo com `query().search`, depois do `trim`. Só espaços não repetem a requisição.
- `loadedQuery` guarda a consulta das linhas na tela: o HTML usa esse termo para distinguir a lista vazia da busca sem resultado.
- Uma resposta vazia numa página além da primeira leva à última página existente, sem trocar as linhas antes. A API responde `200` com `data` vazio, e a tela diria que a lista está vazia.
- Uma falha devolve o `query` à página e ao tamanho de `loadedQuery`. O `MatPaginator` muda a própria página no clique, e só uma mudança no valor do binding o corrige. Por isso, o `query` muda no clique e volta na falha.

### Ação destrutiva

Exemplos reais: `frontend/src/app/shared/components/confirm-modal/confirm-modal.ts` e, na página, `confirmRemoval` em `frontend/src/app/modules/main/pages/people-list/people-list.ts`.

- `openConfirmModal` abre um alert dialog com o título como nome e a mensagem como descrição.
- "Cancelar" vem primeiro: o dialog põe o foco inicial nele, e um Enter não confirma a ação.
- O modal só devolve o resultado. A página faz a chamada com `filter(Boolean)` e `switchMap`, mostra o `message` da API no toast e recarrega a lista.
- A mensagem diz o que será apagado, com o nome do registro.

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

### Campo com máscara

Exemplos reais: `frontend/src/app/shared/components/inputs/cpf-input/cpf-input.ts` e `frontend/src/app/core/utils/cpf-mask.ts`.

- O `(input)` aplica a máscara, grava com `control.setValue` e devolve o cursor com `setSelectionRange`. Reescrever o valor manda o cursor para o fim, o que atrapalharia a correção de um dígito no meio.
- O campo é `type="text"` com `inputmode="numeric"`: `setSelectionRange` não funciona em `type="number"`, e o celular abre o teclado numérico.
- A máscara limita os dígitos, sem `maxlength`, que cortaria um valor colado com espaços em volta.
- "Mascarado" é o CPF das listas, `***.456.789-**`, que vem da API (RN26). A máscara de digitação produz o CPF formatado.

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
