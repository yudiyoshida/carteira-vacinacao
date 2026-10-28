# Sessão no frontend

Modelo. Quando o repositório já tiver um exemplo real, siga o exemplo real.

## Regras

- O access token fica só num signal do `AuthSessionService`; `localStorage` e `sessionStorage` ficam sem token.
- O navegador envia o cookie do refresh token sozinho, porque interface e API estão na mesma origem. O código nunca lê esse cookie.
- Um `401` do login ou do refresh encerra o fluxo, sem disparar outro refresh.
- O logout é uma rota protegida comum: com o access token vencido, o interceptor renova a sessão e repete o logout. Depois dele, a sessão é limpa e o operador volta ao login.

## AuthApiService

```ts
// core/api/auth-api.service.ts
@Injectable({ providedIn: 'root' })
export class AuthApiService {
  private readonly http = inject(HttpClient);
  private readonly base = `${environment.apiUrl}/auth`;

  public login(body: LoginRequest): Observable<AccessTokenResponse> {
    return this.http.post<AccessTokenResponse>(`${this.base}/login`, body);
  }

  public refresh(): Observable<AccessTokenResponse> {
    return this.http.post<AccessTokenResponse>(`${this.base}/refresh`, null);
  }

  public logout(): Observable<SuccessMessage> {
    return this.http.post<SuccessMessage>(`${this.base}/logout`, null);
  }
}
```

## AuthSessionService

```ts
// core/auth/auth-session.service.ts
@Injectable({ providedIn: 'root' })
export class AuthSessionService {
  private readonly authApi = inject(AuthApiService);
  private readonly _accessToken = signal<string | null>(null);
  private refreshing: Observable<string> | null = null;

  public readonly accessToken = this._accessToken.asReadonly();
  public readonly isAuthenticated = computed(() => this._accessToken() !== null);

  public login(accessToken: string): void {
    this._accessToken.set(accessToken);
  }

  // Requisições que recebem 401 ao mesmo tempo compartilham um único refresh.
  public refresh(): Observable<string> {
    this.refreshing ??= this.authApi.refresh().pipe(
      map(({ accessToken }) => accessToken),
      tap((accessToken) => this._accessToken.set(accessToken)),
      finalize(() => (this.refreshing = null)),
      shareReplay(1),
    );
    return this.refreshing;
  }

  // Na inicialização: sem cookie válido, a sessão começa vazia.
  public hydrate(): Observable<unknown> {
    return this.refresh().pipe(catchError(() => of(null)));
  }

  public clear(): void {
    this._accessToken.set(null);
  }
}
```

## authInterceptor

```ts
// core/interceptors/auth.interceptor.ts
const SESSION_ROUTES = [`${environment.apiUrl}/auth/login`, `${environment.apiUrl}/auth/refresh`];

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const session = inject(AuthSessionService);
  const router = inject(Router);
  const send = (token: string | null) =>
    next(token ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : request);

  if (SESSION_ROUTES.includes(request.url)) {
    return next(request);
  }

  return send(session.accessToken()).pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse) || error.status !== HttpStatusCode.Unauthorized) {
        return throwError(() => error);
      }

      return session.refresh().pipe(
        catchError((refreshError: unknown) => {
          session.clear();
          void router.navigate([routePath(RoutesEnum.LOGIN)]);
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
// app.config.ts
provideAppInitializer(() => inject(AuthSessionService).hydrate()),
provideHttpClient(withInterceptors([authInterceptor])),
```

Sem cookie válido, o `hydrate()` termina sem sessão, e o `authGuard` leva o operador ao `/login`.
