import { ChangeDetectionStrategy, Component, input } from '@angular/core';

@Component({
  selector: 'app-page-header',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './page-header.html',
  styleUrl: './page-header.scss',
})
export class PageHeader {
  // Not "title": the attribute stays on the host and becomes a native tooltip.
  public readonly heading = input.required<string>();
  public readonly description = input<string>();
}
