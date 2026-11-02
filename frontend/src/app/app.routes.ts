import { Routes } from '@angular/router';
import { pageTitle } from './core/routes/routes.util';
import { routes as authRoutes } from './modules/auth/auth.routes';
import { routes as mainRoutes } from './modules/main/main.routes';

export const routes: Routes = [
  ...authRoutes,
  ...mainRoutes,
  {
    path: '**',
    title: pageTitle('Página não encontrada'),
    loadComponent: () => import('./shared/pages/not-found/not-found').then((m) => m.NotFound),
  },
];
