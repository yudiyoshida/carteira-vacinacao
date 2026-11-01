import { ChangeDetectionStrategy, Component, computed, input, output } from '@angular/core';
import { MatIcon } from '@angular/material/icon';
import { MatMenu, MatMenuItem, MatMenuTrigger } from '@angular/material/menu';
import { formatApiDate } from '../../../../../../core/utils/api-date';

export type DoseBlockKind = 'applied' | 'next' | 'pending';

@Component({
  selector: 'app-dose-block',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './dose-block.html',
  styleUrl: './dose-block.scss',
  imports: [MatIcon, MatMenu, MatMenuItem, MatMenuTrigger],
})
export class DoseBlock {
  public readonly label = input.required<string>();
  public readonly vaccineName = input.required<string>();
  public readonly kind = input.required<DoseBlockKind>();
  // In yyyy-MM-dd, only on an applied dose.
  public readonly appliedOn = input('');
  public readonly isLast = input(false);

  public readonly register = output<void>();
  public readonly correct = output<void>();
  public readonly remove = output<void>();

  protected readonly appliedOnText = computed(() => formatApiDate(this.appliedOn()));
}
