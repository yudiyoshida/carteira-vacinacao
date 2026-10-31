import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { RouterOutlet } from '@angular/router';
import { map } from 'rxjs';
import { RoutesEnum } from '../../core/routes/routes.enum';
import { routePath } from '../../core/routes/routes.util';
import { BottomNav } from '../../shared/components/navs/bottom-nav/bottom-nav';
import { NavItem } from '../../shared/components/navs/nav-item';
import { SideNav } from '../../shared/components/navs/side-nav/side-nav';

@Component({
  selector: 'app-main',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './main.html',
  styleUrl: './main.scss',
  imports: [RouterOutlet, SideNav, BottomNav],
})
export class MainLayout {
  private readonly breakpoints = inject(BreakpointObserver);

  protected readonly isHandset = toSignal(
    this.breakpoints.observe(Breakpoints.Handset).pipe(map((state) => state.matches)),
    { initialValue: this.breakpoints.isMatched(Breakpoints.Handset) },
  );

  protected readonly navItems: NavItem[] = [
    { label: 'Pessoas', icon: 'group', path: routePath(RoutesEnum.PEOPLE) },
    { label: 'Vacinas', icon: 'vaccines', path: routePath(RoutesEnum.VACCINES) },
  ];
}
