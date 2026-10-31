import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthSessionService } from '../auth/auth-session.service';
import { RoutesEnum } from '../routes/routes.enum';
import { routePath } from '../routes/routes.util';

export const authGuard: CanActivateFn = () => {
  const session = inject(AuthSessionService);
  const router = inject(Router);

  return session.isAuthenticated() || router.createUrlTree([routePath(RoutesEnum.LOGIN)]);
};
