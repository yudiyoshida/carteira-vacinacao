import { HttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { LOCALE_ID } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { DateAdapter, MAT_DATE_FORMATS } from '@angular/material/core';
import { appConfig } from './app.config';
import { AuthSessionService } from './core/auth/auth-session.service';

describe('appConfig', () => {
  beforeEach(() => {
    vi.stubGlobal(
      'matchMedia',
      vi.fn().mockReturnValue({ matches: false, addEventListener: vi.fn() }),
    );
    TestBed.configureTestingModule({
      providers: [...appConfig.providers, provideHttpClientTesting()],
    });
  });

  afterEach(() => vi.unstubAllGlobals());

  it('uses the pt-BR locale', () => {
    expect(TestBed.inject(LOCALE_ID)).toBe('pt-BR');
  });

  it('shows dates as dd/MM/yyyy in the date picker', () => {
    const adapter = TestBed.inject(DateAdapter);
    const formats = TestBed.inject(MAT_DATE_FORMATS);

    expect(adapter.format(new Date(2024, 0, 5), formats.display.dateInput)).toBe('05/01/2024');
  });

  it('reads dates typed as dd/MM/yyyy in the date picker', () => {
    const adapter = TestBed.inject(DateAdapter);
    const formats = TestBed.inject(MAT_DATE_FORMATS);

    const parsed = adapter.parse('05/01/2024', formats.parse.dateInput) as Date;

    expect([parsed.getFullYear(), parsed.getMonth(), parsed.getDate()]).toEqual([2024, 0, 5]);
  });

  it('RN23: restores the session on startup', () => {
    const http = TestBed.inject(HttpTestingController);

    http.expectOne('/api/v1/auth/refresh').flush({ accessToken: 'renewed-token' });

    expect(TestBed.inject(AuthSessionService).accessToken()).toBe('renewed-token');
  });

  it('sends the access token through the interceptor', () => {
    const http = TestBed.inject(HttpTestingController);
    TestBed.inject(AuthSessionService).login('access-token');

    TestBed.inject(HttpClient).get('/api/v1/people').subscribe();

    const request = http.expectOne('/api/v1/people');
    expect(request.request.headers.get('Authorization')).toBe('Bearer access-token');
  });
});
