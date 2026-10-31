import { Routes } from '@angular/router';
import { RoutesEnum } from '../../core/routes/routes.enum';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./main').then((m) => m.MainLayout),
    children: [
      { path: '', redirectTo: RoutesEnum.PEOPLE, pathMatch: 'full' },
      { path: RoutesEnum.PEOPLE, children: [] },
      { path: RoutesEnum.VACCINES, children: [] },
    ],
  },
];
