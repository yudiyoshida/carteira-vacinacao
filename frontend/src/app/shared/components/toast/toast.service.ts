import { inject, Injectable } from '@angular/core';
import { MatSnackBar, MatSnackBarConfig } from '@angular/material/snack-bar';

const SUCCESS_DURATION_MS = 4000;
const ERROR_DURATION_MS = 8000;

@Injectable({ providedIn: 'root' })
export class ToastService {
  private readonly snackBar = inject(MatSnackBar);

  public success(message: string): void {
    this.show(message, {
      duration: SUCCESS_DURATION_MS,
      politeness: 'polite',
      panelClass: 'app-toast--success',
    });
  }

  public error(message: string): void {
    this.show(message, {
      duration: ERROR_DURATION_MS,
      politeness: 'assertive',
      panelClass: 'app-toast--error',
    });
  }

  private show(message: string, config: MatSnackBarConfig): void {
    // At the top of the screen, so that the toast does not cover the bottom navigation.
    this.snackBar.open(message, 'Fechar', { ...config, verticalPosition: 'top' });
  }
}
