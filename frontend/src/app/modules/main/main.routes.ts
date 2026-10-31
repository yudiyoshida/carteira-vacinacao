import { Routes } from '@angular/router';
import { authGuard } from '../../core/guards/auth.guard';
import { RoutesEnum } from '../../core/routes/routes.enum';

export const routes: Routes = [
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./main').then((m) => m.MainLayout),
    children: [
      { path: '', redirectTo: RoutesEnum.PEOPLE, pathMatch: 'full' },
      {
        path: RoutesEnum.PEOPLE,
        loadComponent: () => import('./pages/people-list/people-list').then((m) => m.PeopleList),
      },
      {
        path: RoutesEnum.VACCINES,
        loadComponent: () => import('./pages/vaccine-list/vaccine-list').then((m) => m.VaccineList),
      },
    ],
  },
];
