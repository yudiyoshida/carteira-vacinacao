import { ApplicationRef } from '@angular/core';
import { ComponentFixture, TestBed } from '@angular/core/testing';
import { MAT_DIALOG_DATA, MatDialog, MatDialogRef } from '@angular/material/dialog';
import { ConfirmModal, ConfirmModalData, openConfirmModal } from './confirm-modal';

const DATA: ConfirmModalData = {
  heading: 'Remover pessoa',
  message: 'Remover Maria? A remoção é definitiva.',
  confirmLabel: 'Remover',
};

describe('ConfirmModal', () => {
  let fixture: ComponentFixture<ConfirmModal>;
  let dialogRef: { close: (result?: unknown) => void };

  function buttons(): HTMLButtonElement[] {
    return Array.from(fixture.nativeElement.querySelectorAll('button'));
  }

  function button(label: string): HTMLButtonElement {
    return buttons().find((candidate) =>
      candidate.textContent?.includes(label),
    ) as HTMLButtonElement;
  }

  beforeEach(async () => {
    dialogRef = { close: vi.fn() };
    TestBed.configureTestingModule({
      providers: [
        { provide: MAT_DIALOG_DATA, useValue: DATA },
        { provide: MatDialogRef, useValue: dialogRef },
      ],
    });
    fixture = TestBed.createComponent(ConfirmModal);
    await fixture.whenStable();
  });

  it('shows the texts the page gave', () => {
    const text = fixture.nativeElement.textContent;

    expect(text).toContain('Remover pessoa');
    expect(text).toContain('Remover Maria? A remoção é definitiva.');
    expect(button('Remover').textContent?.trim()).toBe('Remover');
  });

  it('puts the way out first, where the dialog places the initial focus', () => {
    expect(buttons().map((candidate) => candidate.textContent?.trim())).toEqual([
      'Cancelar',
      'Remover',
    ]);
  });

  it('closes with the confirmation', () => {
    button('Remover').click();

    expect(dialogRef.close).toHaveBeenCalledExactlyOnceWith(true);
  });

  it('closes without the confirmation when the operator cancels', () => {
    button('Cancelar').click();

    expect(dialogRef.close).toHaveBeenCalledOnce();
    expect(dialogRef.close).not.toHaveBeenCalledWith(true);
  });
});

describe('openConfirmModal', () => {
  afterEach(() => TestBed.inject(MatDialog).closeAll());

  it('opens an alert dialog named by the heading and described by the message', async () => {
    openConfirmModal(TestBed.inject(MatDialog), DATA);
    await TestBed.inject(ApplicationRef).whenStable();

    const container = document.querySelector('mat-dialog-container') as HTMLElement;
    const describedBy = container.getAttribute('aria-describedby') as string;
    const labelledBy = container.getAttribute('aria-labelledby') as string;

    expect(container.getAttribute('role')).toBe('alertdialog');
    expect(document.getElementById(describedBy)?.textContent).toContain(DATA.message);
    expect(document.getElementById(labelledBy)?.textContent).toContain(DATA.heading);
  });
});
