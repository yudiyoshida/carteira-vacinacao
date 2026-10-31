import { BreakpointObserver, Breakpoints } from '@angular/cdk/layout';
import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { Router, RouterOutlet } from '@angular/router';
import { map } from 'rxjs';
import { AuthApiService } from '../../core/api/auth-api.service';
import { AuthSessionService } from '../../core/auth/auth-session.service';
import { RoutesEnum } from '../../core/routes/routes.enum';
import { routePath } from '../../core/routes/routes.util';
import { toApiError } from '../../core/utils/api-error';
import { BottomNav } from '../../shared/components/navs/bottom-nav/bottom-nav';
import { NavItem } from '../../shared/components/navs/nav-item';
import { SideNav } from '../../shared/components/navs/side-nav/side-nav';
import { ToastService } from '../../shared/components/toast/toast.service';

@Component({
  selector: 'app-main',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './main.html',
  styleUrl: './main.scss',
  imports: [RouterOutlet, SideNav, BottomNav],
})
export class MainLayout {
  private readonly breakpoints = inject(BreakpointObserver);
  private readonly authApi = inject(AuthApiService);
  private readonly session = inject(AuthSessionService);
  private readonly router = inject(Router);
  private readonly toast = inject(ToastService);

  protected readonly isHandset = toSignal(
    this.breakpoints.observe(Breakpoints.Handset).pipe(map((state) => state.matches)),
    { initialValue: this.breakpoints.isMatched(Breakpoints.Handset) },
  );

  protected readonly navItems: NavItem[] = [
    { label: 'Pessoas', icon: 'group', path: routePath(RoutesEnum.PEOPLE) },
    { label: 'Vacinas', icon: 'vaccines', path: routePath(RoutesEnum.VACCINES) },
  ];

  protected signOut(): void {
    this.authApi.logout().subscribe({
      next: ({ message }) => {
        this.session.clear();
        this.toast.success(message);
        void this.router.navigate([routePath(RoutesEnum.LOGIN)]);
      },
      // The session stays, because the cookie is still valid on the server (TechSpec §11.2).
      error: (error: unknown) => this.toast.error(toApiError(error).message),
    });
  }
}
