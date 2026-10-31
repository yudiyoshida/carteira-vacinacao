import { HttpErrorResponse, HttpInterceptorFn, HttpStatusCode } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, of, switchMap, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';
import { AuthSessionService } from '../auth/auth-session.service';
import { RoutesEnum } from '../routes/routes.enum';
import { routePath } from '../routes/routes.util';

// A 401 from these routes ends the flow, without another refresh.
const SESSION_ROUTES = [`${environment.apiUrl}/auth/login`, `${environment.apiUrl}/auth/refresh`];

export const authInterceptor: HttpInterceptorFn = (request, next) => {
  if (SESSION_ROUTES.includes(request.url)) {
    return next(request);
  }

  const session = inject(AuthSessionService);
  const router = inject(Router);
  const sentToken = session.accessToken();
  const send = (token: string | null) =>
    next(token ? request.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : request);

  return send(sentToken).pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse) || error.status !== HttpStatusCode.Unauthorized) {
        return throwError(() => error);
      }

      // A request that was in flight during a renewal only needs the new token (TechSpec §11.2).
      const currentToken = session.accessToken();
      const renewal =
        currentToken && currentToken !== sentToken ? of(currentToken) : session.refresh();

      return renewal.pipe(
        // Before the switchMap: only a failed refresh ends the session, not a failed repetition.
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
