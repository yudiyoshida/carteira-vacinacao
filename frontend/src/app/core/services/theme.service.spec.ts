import { TestBed } from '@angular/core/testing';
import { ThemeService } from './theme.service';

describe('ThemeService', () => {
  let systemListeners: ((event: { matches: boolean }) => void)[];

  function stubSystemPreference(prefersDark: boolean): void {
    vi.stubGlobal(
      'matchMedia',
      vi.fn().mockReturnValue({
        matches: prefersDark,
        addEventListener: (_: string, listener: (event: { matches: boolean }) => void) =>
          systemListeners.push(listener),
      }),
    );
  }

  function changeSystemPreference(prefersDark: boolean): void {
    systemListeners.forEach((listener) => listener({ matches: prefersDark }));
  }

  function appliedTheme(): string | null {
    return document.documentElement.getAttribute('data-theme');
  }

  beforeEach(() => {
    localStorage.clear();
    systemListeners = [];
  });

  afterEach(() => {
    vi.unstubAllGlobals();
    vi.restoreAllMocks();
    document.documentElement.removeAttribute('data-theme');
  });

  it('follows the system preference when the operator has not chosen a theme', () => {
    stubSystemPreference(true);

    const theme = TestBed.inject(ThemeService);

    expect(theme.isDark()).toBe(true);
    expect(appliedTheme()).toBe('dark');
  });

  it('follows a change of the system preference while the operator has not chosen a theme', () => {
    stubSystemPreference(true);
    const theme = TestBed.inject(ThemeService);

    changeSystemPreference(false);

    expect(theme.isDark()).toBe(false);
    expect(appliedTheme()).toBe('light');
  });

  it('prefers the saved choice over the system preference', () => {
    localStorage.setItem('theme', 'light');
    stubSystemPreference(true);

    const theme = TestBed.inject(ThemeService);

    expect(theme.isDark()).toBe(false);
    expect(appliedTheme()).toBe('light');
  });

  it('ignores an unknown saved value', () => {
    localStorage.setItem('theme', 'sepia');
    stubSystemPreference(true);

    const theme = TestBed.inject(ThemeService);

    expect(theme.isDark()).toBe(true);
  });

  it('applies and saves the theme chosen with the toggle', () => {
    stubSystemPreference(false);
    const theme = TestBed.inject(ThemeService);

    theme.toggle();

    expect(theme.isDark()).toBe(true);
    expect(appliedTheme()).toBe('dark');
    expect(localStorage.getItem('theme')).toBe('dark');
  });

  it('ignores a change of the system preference after the operator chooses a theme', () => {
    stubSystemPreference(false);
    const theme = TestBed.inject(ThemeService);

    theme.toggle();
    changeSystemPreference(false);

    expect(theme.isDark()).toBe(true);
    expect(appliedTheme()).toBe('dark');
  });

  it('keeps working when the browser blocks the storage', () => {
    stubSystemPreference(false);
    const blocked = (): never => {
      throw new DOMException('Blocked', 'SecurityError');
    };
    vi.spyOn(Storage.prototype, 'getItem').mockImplementation(blocked);
    vi.spyOn(Storage.prototype, 'setItem').mockImplementation(blocked);
    const theme = TestBed.inject(ThemeService);

    theme.toggle();
    changeSystemPreference(false);

    expect(theme.isDark()).toBe(true);
    expect(appliedTheme()).toBe('dark');
  });
});
