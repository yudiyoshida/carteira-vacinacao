import { ChangeDetectionStrategy, Component, computed, input } from '@angular/core';

@Component({
  selector: 'app-skeleton',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './skeleton.html',
  styleUrl: './skeleton.scss',
})
export class Skeleton {
  public readonly rows = input(5);

  protected readonly items = computed(() =>
    Array.from({ length: this.rows() }, (_, index) => index),
  );
}
