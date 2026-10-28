# Teste do frontend

Modelo. Quando o repositório já tiver um exemplo real, siga o exemplo real.

Vitest com jsdom, pelo `ng test`. O arquivo `*.spec.ts` fica ao lado do código testado.

## Service

```ts
describe('VaccinesService', () => {
  let service: VaccinesService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(VaccinesService);
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

## Componente

- O componente sobe com `TestBed.createComponent`, com os dados vindos do service mockado, e o teste espera `fixture.whenStable()`.
- O teste confere o DOM pelo texto visível e pelos papéis (`button`, `row`), independente das classes CSS.
