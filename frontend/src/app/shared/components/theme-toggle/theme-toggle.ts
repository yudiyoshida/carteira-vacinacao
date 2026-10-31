import { ChangeDetectionStrategy, Component, computed, inject } from '@angular/core';
import { MatIconButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { ThemeService } from '../../../core/services/theme.service';

@Component({
  selector: 'app-theme-toggle',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './theme-toggle.html',
  imports: [MatIconButton, MatIcon],
})
export class ThemeToggle {
  private readonly theme = inject(ThemeService);

  protected readonly icon = computed(() => (this.theme.isDark() ? 'light_mode' : 'dark_mode'));
  protected readonly label = computed(() =>
    this.theme.isDark() ? 'Ativar tema claro' : 'Ativar tema escuro',
  );

  protected toggle(): void {
    this.theme.toggle();
  }
}
