# Teste do frontend

Modelo. Quando o repositório já tiver um exemplo real, siga o exemplo real.

Vitest com jsdom, pelo `ng test`. `npm test` roda uma vez e termina; `npm run test:watch` fica observando os arquivos. O arquivo `*.spec.ts` fica ao lado do código testado. As funções do Vitest (`describe`, `it`, `expect` e `vi`) são globais, sem import.

## Service da API

Exemplo real: `frontend/src/app/core/api/auth-api.service.spec.ts`. Uma busca com parâmetros segue este modelo:

```ts
describe('VaccineApiService', () => {
  let service: VaccineApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(VaccineApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('sends the search term as a query parameter', () => {
    service.findAll('hepatite').subscribe();

    const request = http.expectOne((r) => r.url === '/api/v1/vaccines');
    expect(request.request.params.get('search')).toBe('hepatite');
    request.flush({ currentPage: 1, itemsPerPage: 1, totalItems: 0, totalPages: 0, data: [] });
  });
});
```

## Pipe

```ts
describe('DoseLabelPipe', () => {
  const pipe = new DoseLabelPipe();

  it('RN18: labels the dose of a 1 + 0 scheme as "Dose única"', () => {
    expect(pipe.transform({ type: 'Primary', number: 1 }, { primaryDoses: 1, boosterDoses: 0 })).toBe('Dose única');
  });
});
```

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

## Layout

Exemplo real: `frontend/src/app/modules/main/main.spec.ts`. O teste fornece um `BreakpointObserver` com `observe` e `isMatched` e repete os casos no desktop e no celular com `describe.each`. O `ThemeToggle` da navegação pede o stub de `matchMedia`.

## Componente

- O componente sobe com `TestBed.createComponent`, com os dados vindos do service mockado, e o teste espera `fixture.whenStable()`.
- O teste confere o DOM pelo texto visível e pelos papéis (`button`, `row`), independente das classes CSS.
