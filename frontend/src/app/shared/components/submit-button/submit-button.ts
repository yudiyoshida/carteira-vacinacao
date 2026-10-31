import { ChangeDetectionStrategy, Component, input } from '@angular/core';
import { MatButton } from '@angular/material/button';
import { MatProgressSpinner } from '@angular/material/progress-spinner';

@Component({
  selector: 'app-submit-button',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './submit-button.html',
  styleUrl: './submit-button.scss',
  imports: [MatButton, MatProgressSpinner],
})
export class SubmitButton {
  public readonly label = input.required<string>();
  public readonly loading = input(false);
}
