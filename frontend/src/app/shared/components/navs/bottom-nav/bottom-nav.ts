import { ChangeDetectionStrategy, Component, input, output } from '@angular/core';
import { MatIcon } from '@angular/material/icon';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { ThemeToggle } from '../../theme-toggle/theme-toggle';
import { NavItem } from '../nav-item';

@Component({
  selector: 'app-bottom-nav',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './bottom-nav.html',
  styleUrl: './bottom-nav.scss',
  imports: [RouterLink, RouterLinkActive, MatIcon, ThemeToggle],
})
export class BottomNav {
  public readonly items = input.required<NavItem[]>();
  public readonly signOut = output();
}
