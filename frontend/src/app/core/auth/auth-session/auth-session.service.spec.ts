import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AuthSessionService } from './auth-session.service';

const REFRESH_URL = '/api/v1/auth/refresh';

describe('AuthSessionService', () => {
  let session: AuthSessionService;
  let http: HttpTestingController;

  beforeEach(() => {
    localStorage.clear();
    sessionStorage.clear();
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    session = TestBed.inject(AuthSessionService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('starts without a session', () => {
    expect(session.accessToken()).toBeNull();
    expect(session.isAuthenticated()).toBe(false);
  });

  it('opens the session with the access token', () => {
    session.login('access-token');

    expect(session.accessToken()).toBe('access-token');
    expect(session.isAuthenticated()).toBe(true);
  });

  it('clears the session', () => {
    session.login('access-token');

    session.clear();

    expect(session.accessToken()).toBeNull();
    expect(session.isAuthenticated()).toBe(false);
  });

  it('keeps the access token out of the browser storage', () => {
    session.login('access-token');
    session.refresh().subscribe();
    http.expectOne(REFRESH_URL).flush({ accessToken: 'renewed-token' });

    expect(localStorage.length).toBe(0);
    expect(sessionStorage.length).toBe(0);
  });

  it('RN23: restores the session when the refresh token cookie is valid', () => {
    let finished = false;

    session.hydrate().subscribe({ complete: () => (finished = true) });
    http.expectOne(REFRESH_URL).flush({ accessToken: 'renewed-token' });

    expect(session.accessToken()).toBe('renewed-token');
    expect(finished).toBe(true);
  });

  it('RN23: starts without a session when the refresh token cookie is not valid', () => {
    let finished = false;

    session.hydrate().subscribe({ complete: () => (finished = true) });
    http.expectOne(REFRESH_URL).flush(null, { status: 401, statusText: 'Unauthorized' });

    expect(session.isAuthenticated()).toBe(false);
    expect(finished).toBe(true);
  });

  it('RN23: shares one refresh among the requests that ask for it at the same time', () => {
    const tokens: string[] = [];

    session.refresh().subscribe((token) => tokens.push(token));
    session.refresh().subscribe((token) => tokens.push(token));
    http.expectOne(REFRESH_URL).flush({ accessToken: 'renewed-token' });

    expect(tokens).toEqual(['renewed-token', 'renewed-token']);
  });

  it('starts a new refresh after the previous one ends', () => {
    session.refresh().subscribe();
    http.expectOne(REFRESH_URL).flush({ accessToken: 'first-token' });

    session.refresh().subscribe();
    http.expectOne(REFRESH_URL).flush({ accessToken: 'second-token' });

    expect(session.accessToken()).toBe('second-token');
  });

  it('starts a new refresh after the previous one fails', () => {
    session.refresh().subscribe({ error: () => undefined });
    http.expectOne(REFRESH_URL).flush(null, { status: 500, statusText: 'Server Error' });

    session.refresh().subscribe();
    http.expectOne(REFRESH_URL).flush({ accessToken: 'renewed-token' });

    expect(session.accessToken()).toBe('renewed-token');
  });
});
