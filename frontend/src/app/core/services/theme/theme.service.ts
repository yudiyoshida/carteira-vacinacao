import { computed, Injectable, signal } from '@angular/core';

type Theme = 'light' | 'dark';

const STORAGE_KEY = 'theme';

@Injectable({ providedIn: 'root' })
export class ThemeService {
  private readonly systemPreference = matchMedia('(prefers-color-scheme: dark)');
  private chosenTheme = this.readChosenTheme();
  private readonly theme = signal<Theme>(
    this.chosenTheme ?? this.systemTheme(this.systemPreference.matches),
  );

  public readonly isDark = computed(() => this.theme() === 'dark');

  constructor() {
    this.apply(this.theme());

    this.systemPreference.addEventListener('change', (event) => {
      if (this.chosenTheme === null) {
        this.apply(this.systemTheme(event.matches));
      }
    });
  }

  public toggle(): void {
    this.chosenTheme = this.isDark() ? 'light' : 'dark';
    this.apply(this.chosenTheme);
    this.saveChosenTheme(this.chosenTheme);
  }

  private apply(theme: Theme): void {
    this.theme.set(theme);
    document.documentElement.setAttribute('data-theme', theme);
  }

  private systemTheme(prefersDark: boolean): Theme {
    return prefersDark ? 'dark' : 'light';
  }

  private readChosenTheme(): Theme | null {
    try {
      const saved = localStorage.getItem(STORAGE_KEY);
      return saved === 'light' || saved === 'dark' ? saved : null;
    } catch {
      // The browser may block the storage; the choice then lasts until the page reloads.
      return null;
    }
  }

  private saveChosenTheme(theme: Theme): void {
    try {
      localStorage.setItem(STORAGE_KEY, theme);
    } catch {
      // The browser may block the storage; the choice then lasts until the page reloads.
    }
  }
}
