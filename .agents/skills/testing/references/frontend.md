# Teste do frontend

Modelo. Quando o repositório já tiver um exemplo real, siga o exemplo real.

Vitest com jsdom, pelo `ng test`. `npm test` roda uma vez e termina; `npm run test:watch` fica observando os arquivos. O arquivo `*.spec.ts` fica ao lado do código testado. As funções do Vitest (`describe`, `it`, `expect` e `vi`) são globais, sem import.

## Service da API

Exemplos reais: `frontend/src/app/core/api/auth-api.service.spec.ts` e, com parâmetros na query string, `frontend/src/app/core/api/vaccine-api.service.spec.ts`. Uma URL com query string se confere com uma função em `http.expectOne`, pelo `url` da requisição, e os parâmetros por `request.request.params`.

## Pipe

Exemplo real: `frontend/src/app/shared/pipes/dose-label.pipe.spec.ts`. O pipe nasce com `new DoseLabelPipe()`, sem `TestBed`.

## ThemeService

Exemplo real: `frontend/src/app/core/services/theme.service.spec.ts`. O jsdom não implementa `matchMedia`: o teste fornece um stub antes de criar o service e limpa o `localStorage` entre os casos. Todo teste que sobe o `ThemeService`, inclusive pelo `appConfig` (`frontend/src/app/app.config.spec.ts`), precisa desse stub.

## Conversão de erros

Exemplo real: `frontend/src/app/core/utils/api-error.spec.ts`. O erro nasce de `new HttpErrorResponse({ status, error: body })`, sem `HttpClient`, e o toast é um objeto com `error: vi.fn<(message: string) => void>()`.

## Toast

Exemplo real: `frontend/src/app/shared/components/toast/toast.service.spec.ts`. O teste chama o service, roda `TestBed.tick()` e confere o `mat-snack-bar-container` no `document`.

## Sessão

Exemplos reais: `frontend/src/app/core/auth/auth-session.service.spec.ts`, `frontend/src/app/core/interceptors/auth.interceptor.spec.ts` e `frontend/src/app/core/guards/auth.guard.spec.ts`.

- O interceptor sobe com `provideHttpClient(withInterceptors([authInterceptor]))`, `provideHttpClientTesting()` e `provideRouter([])`. A navegação se confere com `vi.spyOn(router, 'navigate').mockResolvedValue(true)`.
- `http.expectOne` prova o refresh único, e `http.expectNone` prova que não houve outro refresh.
- O guard roda em `TestBed.runInInjectionContext`. Um teste com `provideRouter(routes)` e `router.navigateByUrl` prova que as rotas usam o guard.
- Todo teste que sobe o `appConfig` soma `provideHttpClientTesting()` aos providers (`frontend/src/app/app.config.spec.ts`), porque o `hydrate()` chama o refresh na inicialização.

## Campo e formulário

Exemplos reais: `frontend/src/app/shared/components/inputs/email-input/email-input.spec.ts` e `frontend/src/app/modules/auth/pages/login/login.spec.ts`.

- O campo sobe com `fixture.componentRef.setInput`. O teste muda o `FormControl`, espera `fixture.whenStable()` e confere o `mat-error`, que só fica no DOM com o campo em erro.
- O formulário recebe texto por `input.value` e pelo evento `input`, e o envio é um `click()` no botão, que dispara o `submit` no jsdom.
- O `ToastService` entra como um objeto com `vi.fn()`, e o `Router` com o `spyOn` de `navigate`.
- O cursor se confere por `selectionStart`, com um valor que a máscara muda: o jsdom só move o cursor quando o valor muda (`frontend/src/app/shared/components/inputs/cpf-input/cpf-input.spec.ts`).

## Formulário em dialog

Exemplo real: `frontend/src/app/modules/main/pages/vaccine-list/components/vaccine-form/vaccine-form.spec.ts`. O componente sobe direto pelo `TestBed`, sem abrir um dialog: o `MatDialogRef` entra como um objeto com `close: vi.fn()` e `disableClose`.

Com lista e data, o exemplo real é `frontend/src/app/modules/main/pages/vaccination-card/components/vaccination-form/vaccination-form.spec.ts`:

- O `MAT_DIALOG_DATA` entra por um `configureTestingModule` dentro da função que abre o formulário, porque muda a cada caso.
- O `MatSelectHarness` abre a lista, escolhe a opção e lê o valor com `getValueText`.
- Um campo de data pede `provideDateFnsAdapter()` e `MAT_DATE_LOCALE`. O teste digita no `input`, sem abrir o calendário.

## Lista com busca

Exemplo real: `frontend/src/app/modules/main/pages/vaccine-list/vaccine-list.spec.ts`.

- `vi.useFakeTimers({ toFake: ['setInterval', 'clearInterval', 'Date'] })` controla o debounce, porque o RxJS agenda com `setInterval` e mede com `Date`. O `setTimeout` fica real, porque a detecção de mudanças que o `fixture.whenStable()` espera roda nele. O `afterEach` chama `vi.useRealTimers()`.
- `vi.advanceTimersByTime` encerra o debounce, e `http.expectNone` prova que nada saiu antes disso.
- `request.cancelled` prova que o `switchMap` descartou a busca antiga.
- O `MatDialog` entra como um objeto cujo `open` devolve `{ afterClosed: () => subject }`, e o teste decide o resultado com `subject.next(...)`.

## Lista paginada

Exemplo real: `frontend/src/app/modules/main/pages/people-list/people-list.spec.ts`.

- `page` e `size` se conferem em `request.request.params`, e a faixa do paginador pelo `role="status"` dele.
- A página muda pelo clique no botão com `aria-label` "Próxima página". O tamanho muda pelo `pageChange` do `DataTable`, que `By.directive` encontra.
- Numa falha de troca de página, o teste espera `fixture.whenStable()` antes do `flush`: o paginador só volta se o binding já tiver visto a página nova.

## Modal de confirmação

Exemplo real: `frontend/src/app/shared/components/confirm-modal/confirm-modal.spec.ts`. O componente sobe direto pelo `TestBed`, com `MAT_DIALOG_DATA` e `MatDialogRef` nos providers. `openConfirmModal` usa o `MatDialog` real, e o teste lê `role`, `aria-labelledby` e `aria-describedby` do `mat-dialog-container` depois de `ApplicationRef.whenStable()`, porque o título registra o id num microtask. O jsdom não informa a visibilidade dos elementos, então o foco inicial se confere no teste manual.

## Tabela

Exemplo real: `frontend/src/app/shared/components/data-table/data-table.spec.ts`. Um componente `Host` no próprio spec projeta as colunas, como uma página faz, e o teste muda os dados por um signal do `Host`. O `MatPaginatorHarness` troca a página e o tamanho, inclusive pelo select do paginador.

## Layout

Exemplo real: `frontend/src/app/modules/main/main.spec.ts`. O teste fornece um `BreakpointObserver` com `observe` e `isMatched` e repete os casos no desktop e no celular com `describe.each`. O `ThemeToggle` da navegação pede o stub de `matchMedia`.

## Página com parâmetro de rota

Exemplo real: `frontend/src/app/modules/main/pages/vaccination-card/vaccination-card.spec.ts`.

- O parâmetro entra com `fixture.componentRef.setInput('id', ...)`, antes do `fixture.whenStable()` que dispara a carga.
- Um `RouterLink` na página pede `provideRouter([])`.
- As ações de um componente filho disparam pelo `output`, que `By.directive` encontra, como `block.remove.emit()`. O `output` pula o HTML do filho, então pelo menos um teste de cada ação clica no elemento real.
- Um `mat-menu` se confere pelo `MatMenuHarness`. O `selector` encontra o gatilho pelo `aria-label`, `getItems` lê os itens no overlay e `clickItem` clica num deles. O texto de um item inclui a ligadura do ícone (`editCorrigir data`), então o teste usa `expect.stringContaining` ou um filtro `text` com regex.

## Componente

- O componente sobe com `TestBed.createComponent`, com os dados vindos do service mockado, e o teste espera `fixture.whenStable()`.
- O teste confere o DOM pelo texto visível e pelos papéis (`button`, `row`), independente das classes CSS.

## E2E smoke

Exemplo real: `frontend/e2e/smoke.spec.ts`, com a configuração em `frontend/playwright.config.ts`. O teste é Playwright, não Vitest, e o `ng test` não o coleta, porque procura só em `src/`.

- O Compose precisa estar no ar, com os dados de demonstração: o teste não sobe nada. `npx playwright install chromium`, dentro de `frontend/`, baixa o navegador uma vez.
- Os localizadores usam papel e rótulo (`getByRole`, `getByLabel`), com os nomes acessíveis da tela. Uma `section` com `aria-label` tem o papel `region`.
- A data é fixa e no passado, para não depender do relógio real.
- Cada execução gasta um login do limite de 5 por minuto (RN24), e por isso o teste não tem `retries`.
- O teste devolve os dados ao estado do seed pela própria interface, para rodar de novo no mesmo volume.
- O Playwright não confere os tipos ao rodar: `npx tsc -p tsconfig.e2e.json --noEmit`, dentro de `frontend/`, confere.
