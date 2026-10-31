import { ChangeDetectionStrategy, Component } from '@angular/core';
import { MatAnchor } from '@angular/material/button';
import { MatIcon } from '@angular/material/icon';
import { RouterLink } from '@angular/router';
import { RoutesEnum } from '../../../core/routes/routes.enum';
import { routePath } from '../../../core/routes/routes.util';

@Component({
  selector: 'app-not-found',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './not-found.html',
  styleUrl: './not-found.scss',
  imports: [RouterLink, MatAnchor, MatIcon],
})
export class NotFound {
  protected readonly homePath = routePath(RoutesEnum.PEOPLE);
}
