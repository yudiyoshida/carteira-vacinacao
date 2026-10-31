import { Routes } from '@angular/router';
import { routes as authRoutes } from './modules/auth/auth.routes';
import { routes as mainRoutes } from './modules/main/main.routes';

export const routes: Routes = [
  ...authRoutes,
  ...mainRoutes,
  {
    path: '**',
    loadComponent: () => import('./shared/pages/not-found/not-found').then((m) => m.NotFound),
  },
];
