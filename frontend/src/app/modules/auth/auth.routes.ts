import { Routes } from '@angular/router';
import { RoutesEnum } from '../../core/routes/routes.enum';
import { pageTitle } from '../../core/routes/routes.util';

export const routes: Routes = [
  {
    path: RoutesEnum.LOGIN,
    loadComponent: () => import('./auth').then((m) => m.AuthLayout),
    children: [
      {
        path: '',
        title: pageTitle('Entrar'),
        loadComponent: () => import('./pages/login/login').then((m) => m.Login),
      },
    ],
  },
];
