import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { MatButton } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';

@Component({
  selector: 'app-empty-state',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './empty-state.html',
  styleUrl: './empty-state.scss',
  imports: [MatButton, MatIcon],
})
export class EmptyState {
  public readonly icon = input.required<string>();
  // Not "title": the attribute stays on the host and becomes a native tooltip.
  public readonly heading = input.required<string>();
  public readonly description = input<string>();
  public readonly ctaLabel = input<string>();

  public readonly cta = output<void>();
}
