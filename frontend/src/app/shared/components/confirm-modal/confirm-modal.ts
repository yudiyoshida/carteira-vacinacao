import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatButton } from '@angular/material/button';
import {
  MAT_DIALOG_DATA,
  MatDialog,
  MatDialogActions,
  MatDialogClose,
  MatDialogContent,
  MatDialogRef,
  MatDialogTitle,
} from '@angular/material/dialog';

const MESSAGE_ID = 'confirm-modal-message';

export interface ConfirmModalData {
  heading: string;
  message: string;
  confirmLabel: string;
}

@Component({
  selector: 'app-confirm-modal',
  changeDetection: ChangeDetectionStrategy.OnPush,
  templateUrl: './confirm-modal.html',
  styleUrl: './confirm-modal.scss',
  imports: [MatButton, MatDialogActions, MatDialogClose, MatDialogContent, MatDialogTitle],
})
export class ConfirmModal {
  protected readonly data = inject<ConfirmModalData>(MAT_DIALOG_DATA);
  protected readonly messageId = MESSAGE_ID;
}

// An alert dialog announces the message along with the heading, because the action is destructive.
export function openConfirmModal(
  dialog: MatDialog,
  data: ConfirmModalData,
): MatDialogRef<ConfirmModal, boolean> {
  return dialog.open<ConfirmModal, ConfirmModalData, boolean>(ConfirmModal, {
    data,
    role: 'alertdialog',
    ariaDescribedBy: MESSAGE_ID,
  });
}
