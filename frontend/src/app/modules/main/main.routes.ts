import { Routes } from '@angular/router';
import { authGuard } from '../../core/guards/auth.guard';
import { RoutesEnum } from '../../core/routes/routes.enum';
import { pageTitle } from '../../core/routes/routes.util';

export const routes: Routes = [
  {
    path: '',
    canActivate: [authGuard],
    loadComponent: () => import('./main').then((m) => m.MainLayout),
    children: [
      { path: '', redirectTo: RoutesEnum.PEOPLE, pathMatch: 'full' },
      {
        path: RoutesEnum.PEOPLE,
        title: pageTitle('Pessoas'),
        loadComponent: () => import('./pages/people-list/people-list').then((m) => m.PeopleList),
      },
      {
        path: `${RoutesEnum.PEOPLE}/:id/${RoutesEnum.CARD}`,
        // Without the name of the person, because the title stays in the browser history (RNF03).
        title: pageTitle('Cartão da pessoa'),
        loadComponent: () =>
          import('./pages/vaccination-card/vaccination-card').then((m) => m.VaccinationCard),
      },
      {
        path: RoutesEnum.VACCINES,
        title: pageTitle('Vacinas'),
        loadComponent: () => import('./pages/vaccine-list/vaccine-list').then((m) => m.VaccineList),
      },
    ],
  },
];
