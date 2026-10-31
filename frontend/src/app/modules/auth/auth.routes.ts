import { Routes } from '@angular/router';
import { RoutesEnum } from '../../core/routes/routes.enum';

export const routes: Routes = [
  {
    path: RoutesEnum.LOGIN,
    loadComponent: () => import('./auth').then((m) => m.AuthLayout),
    children: [],
  },
];
