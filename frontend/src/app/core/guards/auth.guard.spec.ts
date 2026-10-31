import { TestBed } from '@angular/core/testing';
import {
  ActivatedRouteSnapshot,
  provideRouter,
  Router,
  RouterStateSnapshot,
  UrlTree,
} from '@angular/router';
import { routes } from '../../app.routes';
import { AuthSessionService } from '../auth/auth-session.service';
import { authGuard } from './auth.guard';

describe('authGuard', () => {
  let router: Router;
  let session: AuthSessionService;

  function runGuard(): ReturnType<typeof authGuard> {
    return TestBed.runInInjectionContext(() =>
      authGuard({} as ActivatedRouteSnapshot, {} as RouterStateSnapshot),
    );
  }

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideRouter(routes)] });
    router = TestBed.inject(Router);
    session = TestBed.inject(AuthSessionService);
  });

  it('RN21: lets the operator in when there is a session', () => {
    session.login('access-token');

    expect(runGuard()).toBe(true);
  });

  it('RN21: sends the operator to the login when there is no session', () => {
    const result = runGuard() as UrlTree;

    expect(router.serializeUrl(result)).toBe('/login');
  });

  it.each(['/', '/people', '/vaccines'])(
    'RN21: protects the %s page of the main layout',
    async (url) => {
      await router.navigateByUrl(url);

      expect(router.url).toBe('/login');
    },
  );
});
