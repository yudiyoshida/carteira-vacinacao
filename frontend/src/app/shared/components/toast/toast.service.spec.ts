import { TestBed } from '@angular/core/testing';
import { ToastService } from './toast.service';

describe('ToastService', () => {
  let toast: ToastService;

  function visibleToast(): HTMLElement | null {
    return document.querySelector<HTMLElement>('mat-snack-bar-container');
  }

  beforeEach(() => {
    toast = TestBed.inject(ToastService);
  });

  it('shows a success message that the operator can close', () => {
    toast.success('Pessoa removida com sucesso.');
    TestBed.tick();

    expect(visibleToast()?.textContent).toContain('Pessoa removida com sucesso.');
    expect(visibleToast()?.querySelector('button')?.textContent).toContain('Fechar');
    expect(visibleToast()?.classList).toContain('app-toast--success');
  });

  it('shows an error message that the operator can close', () => {
    toast.error('Vacinação não encontrada.');
    TestBed.tick();

    expect(visibleToast()?.textContent).toContain('Vacinação não encontrada.');
    expect(visibleToast()?.querySelector('button')?.textContent).toContain('Fechar');
    expect(visibleToast()?.classList).toContain('app-toast--error');
  });
});
