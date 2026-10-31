import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { AuthApiService } from './auth-api.service';

describe('AuthApiService', () => {
  let service: AuthApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(AuthApiService);
    http = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('sends the credentials on login', () => {
    const credentials = { email: 'operador@demo.local', password: 'senha-do-teste' };
    let accessToken: string | undefined;

    service.login(credentials).subscribe((response) => (accessToken = response.accessToken));

    const request = http.expectOne('/api/v1/auth/login');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual(credentials);
    request.flush({ accessToken: 'access-token' });
    expect(accessToken).toBe('access-token');
  });

  it('renews the session without a body, because the browser sends the cookie', () => {
    let accessToken: string | undefined;

    service.refresh().subscribe((response) => (accessToken = response.accessToken));

    const request = http.expectOne('/api/v1/auth/refresh');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toBeNull();
    request.flush({ accessToken: 'renewed-token' });
    expect(accessToken).toBe('renewed-token');
  });

  it('ends the session without a body', () => {
    let message: string | undefined;

    service.logout().subscribe((response) => (message = response.message));

    const request = http.expectOne('/api/v1/auth/logout');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toBeNull();
    request.flush({ message: 'Sessão encerrada com sucesso.' });
    expect(message).toBe('Sessão encerrada com sucesso.');
  });
});
