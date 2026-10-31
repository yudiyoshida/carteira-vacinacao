import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { AuthSessionService } from '../../../../core/auth/auth-session.service';
import { ProblemDetails } from '../../../../core/models/problem-details.model';
import { ToastService } from '../../../../shared/components/toast/toast.service';
import { Login } from './login';

const LOGIN_URL = '/api/v1/auth/login';

function makeProblem(status: number, code: string, detail: string): ProblemDetails {
  return { title: 'Erro', status, detail, instance: LOGIN_URL, code, traceId: '00-abc' };
}

describe('Login', () => {
  let fixture: ComponentFixture<Login>;
  let http: HttpTestingController;
  let session: AuthSessionService;
  let router: Router;
  let toast: { error: (message: string) => void; dismiss: () => void };

  function element(): HTMLElement {
    return fixture.nativeElement;
  }

  function submitButton(): HTMLButtonElement {
    return element().querySelector('button[type="submit"]') as HTMLButtonElement;
  }

  function errorMessages(): (string | undefined)[] {
    return Array.from(element().querySelectorAll('mat-error')).map((error) =>
      error.textContent?.trim(),
    );
  }

  function type(selector: string, value: string): void {
    const input = element().querySelector(selector) as HTMLInputElement;
    input.value = value;
    input.dispatchEvent(new Event('input'));
  }

  async function submitCredentials(): Promise<void> {
    type('input[type="email"]', 'operador@demo.local');
    type('input[type="password"]', 'senha-do-teste');
    submitButton().click();
    await fixture.whenStable();
  }

  beforeEach(async () => {
    toast = { error: vi.fn(), dismiss: vi.fn() };
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        { provide: ToastService, useValue: toast },
      ],
    });
    http = TestBed.inject(HttpTestingController);
    session = TestBed.inject(AuthSessionService);
    router = TestBed.inject(Router);
    vi.spyOn(router, 'navigate').mockResolvedValue(true);
    fixture = TestBed.createComponent(Login);
    await fixture.whenStable();
  });

  afterEach(() => http.verify());

  it('shows the field errors and sends nothing when the form is invalid', async () => {
    submitButton().click();
    await fixture.whenStable();

    http.expectNone(LOGIN_URL);
    expect(errorMessages()).toEqual(['O e-mail é obrigatório.', 'A senha é obrigatória.']);
  });

  it('opens the session and goes to the people list when the credentials are valid', async () => {
    await submitCredentials();

    const request = http.expectOne(LOGIN_URL);
    expect(request.request.body).toEqual({
      email: 'operador@demo.local',
      password: 'senha-do-teste',
    });
    request.flush({ accessToken: 'access-token' });

    expect(session.accessToken()).toBe('access-token');
    expect(router.navigate).toHaveBeenCalledWith(['/people']);
  });

  it('RN22: shows the generic message in a toast and stays on the page', async () => {
    await submitCredentials();

    http
      .expectOne(LOGIN_URL)
      .flush(makeProblem(401, 'auth.invalid_credentials', 'E-mail ou senha inválidos.'), {
        status: 401,
        statusText: 'Unauthorized',
      });
    await fixture.whenStable();

    expect(toast.error).toHaveBeenCalledExactlyOnceWith('E-mail ou senha inválidos.');
    expect(session.isAuthenticated()).toBe(false);
    expect(router.navigate).not.toHaveBeenCalled();
  });

  it('RN24: shows the refusal of the attempt limit in a toast', async () => {
    await submitCredentials();

    http
      .expectOne(LOGIN_URL)
      .flush(makeProblem(429, 'auth.too_many_attempts', 'Muitas tentativas de login.'), {
        status: 429,
        statusText: 'Too Many Requests',
      });
    await fixture.whenStable();

    expect(toast.error).toHaveBeenCalledExactlyOnceWith('Muitas tentativas de login.');
  });

  it('shows a validation error of the API on the field, without a toast', async () => {
    await submitCredentials();

    http.expectOne(LOGIN_URL).flush(
      {
        ...makeProblem(400, 'validation.failed', 'Um ou mais campos são inválidos.'),
        errors: { email: ['O e-mail informado é inválido.'] },
      },
      { status: 400, statusText: 'Bad Request' },
    );
    await fixture.whenStable();

    expect(errorMessages()).toEqual(['O e-mail informado é inválido.']);
    expect(toast.error).not.toHaveBeenCalled();
  });

  it('closes the message of the previous attempt when the operator submits again', async () => {
    await submitCredentials();

    expect(toast.dismiss).toHaveBeenCalledOnce();
    http.expectOne(LOGIN_URL).flush({ accessToken: 'access-token' });
  });

  it('blocks a second submission during the request and releases it after a failure', async () => {
    await submitCredentials();

    expect(submitButton().disabled).toBe(true);

    http
      .expectOne(LOGIN_URL)
      .flush(makeProblem(401, 'auth.invalid_credentials', 'E-mail ou senha inválidos.'), {
        status: 401,
        statusText: 'Unauthorized',
      });
    await fixture.whenStable();

    expect(submitButton().disabled).toBe(false);
  });
});
