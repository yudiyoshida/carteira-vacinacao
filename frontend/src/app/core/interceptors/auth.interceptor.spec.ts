import {
  HttpClient,
  HttpErrorResponse,
  provideHttpClient,
  withInterceptors,
} from '@angular/common/http';
import {
  HttpTestingController,
  provideHttpClientTesting,
  TestRequest,
} from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { AuthSessionService } from '../auth/auth-session.service';
import { authInterceptor } from './auth.interceptor';

const PEOPLE_URL = '/api/v1/people';
const VACCINES_URL = '/api/v1/vaccines';
const LOGIN_URL = '/api/v1/auth/login';
const REFRESH_URL = '/api/v1/auth/refresh';
const LOGOUT_URL = '/api/v1/auth/logout';

describe('authInterceptor', () => {
  let http: HttpTestingController;
  let session: AuthSessionService;
  let client: HttpClient;
  let router: Router;

  function respondUnauthorized(request: TestRequest): void {
    request.flush(null, { status: 401, statusText: 'Unauthorized' });
  }

  function authorizationOf(request: TestRequest): string | null {
    return request.request.headers.get('Authorization');
  }

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(withInterceptors([authInterceptor])),
        provideHttpClientTesting(),
        provideRouter([]),
      ],
    });
    http = TestBed.inject(HttpTestingController);
    session = TestBed.inject(AuthSessionService);
    client = TestBed.inject(HttpClient);
    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigate').mockResolvedValue(true);
  });

  afterEach(() => http.verify());

  it('adds the access token to the request', () => {
    session.login('access-token');

    client.get(PEOPLE_URL).subscribe();

    const request = http.expectOne(PEOPLE_URL);
    expect(authorizationOf(request)).toBe('Bearer access-token');
    request.flush([]);
  });

  it('sends the request without the header when there is no session', () => {
    client.get(PEOPLE_URL).subscribe();

    const request = http.expectOne(PEOPLE_URL);
    expect(request.request.headers.has('Authorization')).toBe(false);
    request.flush([]);
  });

  it('RN23: renews the session once when several requests receive 401 and repeats each one', () => {
    session.login('expired-token');
    const responses: unknown[] = [];
    client.get(PEOPLE_URL).subscribe((response) => responses.push(response));
    client.get(VACCINES_URL).subscribe((response) => responses.push(response));

    respondUnauthorized(http.expectOne(PEOPLE_URL));
    respondUnauthorized(http.expectOne(VACCINES_URL));
    http.expectOne(REFRESH_URL).flush({ accessToken: 'renewed-token' });

    const repeatedPeople = http.expectOne(PEOPLE_URL);
    const repeatedVaccines = http.expectOne(VACCINES_URL);
    expect(authorizationOf(repeatedPeople)).toBe('Bearer renewed-token');
    expect(authorizationOf(repeatedVaccines)).toBe('Bearer renewed-token');
    repeatedPeople.flush('people');
    repeatedVaccines.flush('vaccines');
    expect(responses).toEqual(['people', 'vaccines']);
  });

  it('RN23: repeats a request whose 401 arrives after the renewal, without another refresh', () => {
    session.login('expired-token');
    client.get(PEOPLE_URL).subscribe();
    client.get(VACCINES_URL).subscribe();
    const vaccines = http.expectOne(VACCINES_URL);

    respondUnauthorized(http.expectOne(PEOPLE_URL));
    http.expectOne(REFRESH_URL).flush({ accessToken: 'renewed-token' });
    http.expectOne(PEOPLE_URL).flush([]);
    respondUnauthorized(vaccines);

    http.expectNone(REFRESH_URL);
    const repeated = http.expectOne(VACCINES_URL);
    expect(authorizationOf(repeated)).toBe('Bearer renewed-token');
    repeated.flush([]);
  });

  it('RN23: clears the session and goes to the login when the refresh fails', () => {
    session.login('expired-token');
    const errors: HttpErrorResponse[] = [];
    client.get(PEOPLE_URL).subscribe({ error: (error) => errors.push(error) });
    client.get(VACCINES_URL).subscribe({ error: (error) => errors.push(error) });

    respondUnauthorized(http.expectOne(PEOPLE_URL));
    respondUnauthorized(http.expectOne(VACCINES_URL));
    respondUnauthorized(http.expectOne(REFRESH_URL));

    expect(session.isAuthenticated()).toBe(false);
    expect(router.navigate).toHaveBeenCalledWith(['/login']);
    expect(errors.map((error) => error.url)).toEqual([REFRESH_URL, REFRESH_URL]);
    http.expectNone(PEOPLE_URL);
    http.expectNone(VACCINES_URL);
  });

  it('RN23: does not renew the session when the login responds 401', () => {
    let status: number | undefined;
    client
      .post(LOGIN_URL, { email: 'operador@demo.local', password: 'errada' })
      .subscribe({ error: (error: HttpErrorResponse) => (status = error.status) });

    const request = http.expectOne(LOGIN_URL);
    expect(request.request.headers.has('Authorization')).toBe(false);
    respondUnauthorized(request);

    http.expectNone(REFRESH_URL);
    expect(status).toBe(401);
    expect(router.navigate).not.toHaveBeenCalled();
  });

  it('RN23: does not start another refresh when the refresh responds 401', () => {
    let status: number | undefined;
    client
      .post(REFRESH_URL, null)
      .subscribe({ error: (error: HttpErrorResponse) => (status = error.status) });

    respondUnauthorized(http.expectOne(REFRESH_URL));

    http.expectNone(REFRESH_URL);
    expect(status).toBe(401);
    expect(router.navigate).not.toHaveBeenCalled();
  });

  it('RN23: passes the 401 of the repeated request to the caller, without another refresh', () => {
    session.login('expired-token');
    let status: number | undefined;
    client
      .get(PEOPLE_URL)
      .subscribe({ error: (error: HttpErrorResponse) => (status = error.status) });

    respondUnauthorized(http.expectOne(PEOPLE_URL));
    http.expectOne(REFRESH_URL).flush({ accessToken: 'renewed-token' });
    respondUnauthorized(http.expectOne(PEOPLE_URL));

    http.expectNone(REFRESH_URL);
    expect(status).toBe(401);
    expect(session.accessToken()).toBe('renewed-token');
    expect(router.navigate).not.toHaveBeenCalled();
  });

  it('RN23: renews the session and repeats the logout when the access token expired', () => {
    session.login('expired-token');
    let response: unknown;
    client.post(LOGOUT_URL, null).subscribe((body) => (response = body));

    respondUnauthorized(http.expectOne(LOGOUT_URL));
    http.expectOne(REFRESH_URL).flush({ accessToken: 'renewed-token' });

    const repeated = http.expectOne(LOGOUT_URL);
    expect(authorizationOf(repeated)).toBe('Bearer renewed-token');
    repeated.flush({ message: 'Sessão encerrada com sucesso.' });
    expect(response).toEqual({ message: 'Sessão encerrada com sucesso.' });
  });

  it('does not renew the session on another error', () => {
    session.login('access-token');
    let status: number | undefined;
    client
      .get(PEOPLE_URL)
      .subscribe({ error: (error: HttpErrorResponse) => (status = error.status) });

    http.expectOne(PEOPLE_URL).flush(null, { status: 500, statusText: 'Server Error' });

    http.expectNone(REFRESH_URL);
    expect(status).toBe(500);
    expect(session.accessToken()).toBe('access-token');
  });
});
