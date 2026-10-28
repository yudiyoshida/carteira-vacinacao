# Sessão no frontend

Modelo. Quando o repositório já tiver um exemplo real, siga o exemplo real.

## Regras

- O access token fica só num signal do `AuthService`; `localStorage` e `sessionStorage` ficam vazios.
- O navegador envia o cookie do refresh token sozinho, porque interface e API estão na mesma origem. O código nunca lê esse cookie.
- Um `401` do login ou do refresh encerra o fluxo, sem disparar outro refresh.
- O logout é uma rota protegida comum: com o access token vencido, o interceptor renova a sessão e repete o logout.

## AuthService

```ts
@Injectable({ providedIn: 'root' })
export class AuthService {
  private readonly http = inject(HttpClient);
  private readonly token = signal<string | null>(null);
  private refreshing: Observable<string> | null = null;

  readonly accessToken = this.token.asReadonly();
  readonly isAuthenticated = computed(() => this.token() !== null);

  signIn(email: string, password: string): Observable<void> {
    return this.http
      .post<{ accessToken: string }>('/api/v1/auth/login', { email, password })
      .pipe(map(({ accessToken }) => this.token.set(accessToken)));
  }

  // Requisições que recebem 401 ao mesmo tempo compartilham um único refresh.
  refresh(): Observable<string> {
    this.refreshing ??= this.http.post<{ accessToken: string }>('/api/v1/auth/refresh', null).pipe(
      map(({ accessToken }) => accessToken),
      tap((accessToken) => this.token.set(accessToken)),
      finalize(() => (this.refreshing = null)),
      shareReplay(1),
    );
    return this.refreshing;
  }

  clear(): void {
    this.token.set(null);
  }
}
```

## authInterceptor

```ts
const SESSION_ROUTES = ['/api/v1/auth/login', '/api/v1/auth/refresh'];

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const auth = inject(AuthService);
  const router = inject(Router);
  const send = (token: string | null) =>
    next(token ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : request);

  if (SESSION_ROUTES.includes(request.url)) {
    return next(request);
  }

  return send(auth.accessToken()).pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse) || error.status !== HttpStatusCode.Unauthorized) {
        return throwError(() => error);
      }

      return auth.refresh().pipe(
        catchError((refreshError: unknown) => {
          auth.clear();
          void router.navigate(['/login']);
          return throwError(() => refreshError);
        }),
        switchMap((token) => send(token)),
      );
    }),
  );
};
```

O `catchError` fica antes do `switchMap`: ele trata só a falha do refresh, e o erro da requisição repetida segue para quem chamou.

## Inicialização

```ts
provideAppInitializer(() => inject(AuthService).refresh().pipe(catchError(() => of(null)))),
provideHttpClient(withInterceptors([authInterceptor])),
```

Sem cookie válido, o refresh falha em silêncio, e o `authGuard` leva o operador ao `/login`.
