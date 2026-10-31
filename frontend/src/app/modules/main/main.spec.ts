import { BreakpointObserver } from '@angular/cdk/layout';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of } from 'rxjs';
import { AuthSessionService } from '../../core/auth/auth-session.service';
import { ToastService } from '../../shared/components/toast/toast.service';
import { MainLayout } from './main';

const LOGOUT_URL = '/api/v1/auth/logout';

describe.each([
  { layout: 'desktop', isHandset: false, nav: 'app-side-nav' },
  { layout: 'handset', isHandset: true, nav: 'app-bottom-nav' },
])('MainLayout on $layout', ({ isHandset, nav }) => {
  let fixture: ComponentFixture<MainLayout>;
  let http: HttpTestingController;
  let session: AuthSessionService;
  let router: Router;
  let toast: { success: (message: string) => void; error: (message: string) => void };

  function signOutButton(): HTMLButtonElement {
    const buttons: HTMLButtonElement[] = Array.from(
      fixture.nativeElement.querySelectorAll(`${nav} button`),
    );
    return buttons.find((button) => button.textContent?.includes('Sair')) as HTMLButtonElement;
  }

  beforeEach(async () => {
    vi.stubGlobal(
      'matchMedia',
      vi.fn().mockReturnValue({ matches: false, addEventListener: vi.fn() }),
    );
    toast = { success: vi.fn(), error: vi.fn() };
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: ToastService, useValue: toast },
        {
          provide: BreakpointObserver,
          useValue: {
            observe: () => of({ matches: isHandset, breakpoints: {} }),
            isMatched: () => isHandset,
          },
        },
      ],
    });
    http = TestBed.inject(HttpTestingController);
    session = TestBed.inject(AuthSessionService);
    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigate').mockResolvedValue(true);
    session.login('access-token');
    fixture = TestBed.createComponent(MainLayout);
    await fixture.whenStable();
  });

  afterEach(() => {
    http.verify();
    vi.unstubAllGlobals();
  });

  it('RN23: ends the session, shows the message of the API and goes to the login', () => {
    signOutButton().click();

    http.expectOne(LOGOUT_URL).flush({ message: 'Sessão encerrada com sucesso.' });

    expect(session.isAuthenticated()).toBe(false);
    expect(toast.success).toHaveBeenCalledExactlyOnceWith('Sessão encerrada com sucesso.');
    expect(router.navigate).toHaveBeenCalledWith(['/login']);
  });

  it('keeps the session and shows the error when the logout fails', () => {
    signOutButton().click();

    http.expectOne(LOGOUT_URL).error(new ProgressEvent('error'));

    expect(session.isAuthenticated()).toBe(true);
    expect(toast.error).toHaveBeenCalledExactlyOnceWith(
      'Não foi possível conectar ao servidor. Tente de novo.',
    );
    expect(router.navigate).not.toHaveBeenCalled();
  });
});
