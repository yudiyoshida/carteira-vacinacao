import { computed, inject, Injectable, signal } from '@angular/core';
import { catchError, finalize, map, Observable, of, shareReplay, tap } from 'rxjs';
import { AuthApiService } from '../api/auth-api.service';

@Injectable({ providedIn: 'root' })
export class AuthSessionService {
  private readonly authApi = inject(AuthApiService);
  // Only in memory: the browser storage would expose the token to an XSS (ADR-0007).
  private readonly token = signal<string | null>(null);
  private refreshing: Observable<string> | null = null;

  public readonly accessToken = this.token.asReadonly();
  public readonly isAuthenticated = computed(() => this.token() !== null);

  public login(accessToken: string): void {
    this.token.set(accessToken);
  }

  // Requests that receive a 401 at the same time share one refresh (TechSpec §11.2).
  public refresh(): Observable<string> {
    this.refreshing ??= this.authApi.refresh().pipe(
      map(({ accessToken }) => accessToken),
      tap((accessToken) => this.token.set(accessToken)),
      finalize(() => (this.refreshing = null)),
      shareReplay(1),
    );
    return this.refreshing;
  }

  // Without a valid cookie, the session starts empty and the authGuard leads to the login.
  public hydrate(): Observable<unknown> {
    return this.refresh().pipe(catchError(() => of(null)));
  }

  public clear(): void {
    this.token.set(null);
  }
}
