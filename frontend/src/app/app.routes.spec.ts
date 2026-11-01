import { TestBed } from '@angular/core/testing';
import { Title } from '@angular/platform-browser';
import { provideRouter, Router } from '@angular/router';
import { routes } from './app.routes';
import { AuthSessionService } from './core/auth/auth-session.service';

describe('routes', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideRouter(routes)] });
    TestBed.inject(AuthSessionService).login('access-token');
  });

  it.each([
    ['/login', 'Entrar | Cartão de Vacinação'],
    ['/people', 'Pessoas | Cartão de Vacinação'],
    ['/people/0192f1a0-0000-7000-8000-000000000000/card', 'Cartão da pessoa | Cartão de Vacinação'],
    ['/vaccines', 'Vacinas | Cartão de Vacinação'],
    ['/does-not-exist', 'Página não encontrada | Cartão de Vacinação'],
  ])('gives the %s page the title "%s"', async (url, title) => {
    await TestBed.inject(Router).navigateByUrl(url);

    expect(TestBed.inject(Title).getTitle()).toBe(title);
  });
});
