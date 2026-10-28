# Feature

Modelo. Quando o repositório já tiver um exemplo real, siga o exemplo real. Os nomes de `core/` e `shared/` (tratamento de erro, notificação) saem da T23.

## Service

```ts
// features/vaccines/vaccines.service.ts
@Injectable({ providedIn: 'root' })
export class VaccinesService {
  private readonly http = inject(HttpClient);

  findAll(search = ''): Observable<Paginated<Vaccine>> {
    const params = search ? new HttpParams().set('search', search) : undefined;
    return this.http.get<Paginated<Vaccine>>('/api/v1/vaccines', { params });
  }

  create(body: CreateVaccine): Observable<{ id: string }> {
    return this.http.post<{ id: string }>('/api/v1/vaccines', body);
  }
}
```

## Lista

```ts
// features/vaccines/vaccine-list/vaccine-list.ts
@Component({
  selector: 'app-vaccine-list',
  imports: [ReactiveFormsModule, MatFormFieldModule, MatInputModule, MatTableModule],
  templateUrl: './vaccine-list.html',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class VaccineList {
  private readonly vaccines = inject(VaccinesService);

  protected readonly search = new FormControl('', { nonNullable: true });
  protected readonly page = toSignal(
    this.search.valueChanges.pipe(
      startWith(this.search.value),
      debounceTime(300),
      switchMap((search) => this.vaccines.findAll(search)),
    ),
  );
}
```

```html
<!-- features/vaccines/vaccine-list/vaccine-list.html -->
@if (page(); as page) {
  <table mat-table [dataSource]="page.data">
    <!-- colunas -->
  </table>
} @else {
  <p>Carregando vacinas...</p>
}
```

## Formulário

```ts
protected readonly form = inject(NonNullableFormBuilder).group({
  name: ['', [Validators.required, Validators.minLength(2), Validators.maxLength(100)]],
  primaryDoses: [1, [Validators.required, Validators.min(1), Validators.max(10)]],
  boosterDoses: [0, [Validators.required, Validators.min(0), Validators.max(5)]],
});

protected submit(): void {
  if (this.form.invalid) {
    return;
  }

  this.vaccines.create(this.form.getRawValue()).subscribe({
    next: () => this.dialogRef.close(true),
    error: (error: HttpErrorResponse) => this.problems.handle(error, this.form),
  });
}
```

`problems.handle` representa o tratamento de `core/http`: aplica os `errors` do ProblemDetails aos campos e mostra o resto no snackbar.

## Rota

```ts
// features/vaccines/vaccines.routes.ts
export const VACCINES_ROUTES: Routes = [{ path: '', component: VaccineList }];
```

```ts
// app.routes.ts, dentro do shell protegido
{ path: 'vaccines', loadChildren: () => import('./features/vaccines/vaccines.routes').then((m) => m.VACCINES_ROUTES) },
```
