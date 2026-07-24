import { CommonModule } from '@angular/common';
import { Component, EventEmitter, Input, Output } from '@angular/core';
import { NzButtonModule } from 'ng-zorro-antd/button';
import { NzModalModule } from 'ng-zorro-antd/modal';
import { StyleObjectLike } from 'ng-zorro-antd/modal/modal-types';

/**
 * Generic Create/Edit dialog shell: title, content projection, footer with
 * Cancel/Save, saving state and double-submit guard. Knows nothing about any
 * specific entity, API or form - the feature owns the form and validation.
 */
@Component({
  selector: 'app-form-dialog',
  standalone: true,
  imports: [CommonModule, NzModalModule, NzButtonModule],
  template: `
    <nz-modal
      [nzVisible]="open"
      [nzTitle]="title"
      [nzWidth]="width"
      [nzBodyStyle]="bodyStyle"
      [nzFooter]="footerTpl"
      [nzMaskClosable]="!saving"
      [nzClosable]="!saving"
      (nzOnCancel)="handleCancel()"
      (nzAfterOpen)="afterOpen.emit()"
      (nzAfterClose)="afterClose.emit()">
      <ng-container *nzModalContent>
        <ng-content></ng-content>
      </ng-container>
    </nz-modal>

    <ng-template #footerTpl>
      <button nz-button type="button" (click)="handleCancel()" [disabled]="saving">{{ cancelText }}</button>
      <button
        nz-button
        nzType="primary"
        type="button"
        (click)="handleSave()"
        [nzLoading]="saving"
        [disabled]="saving || saveDisabled">
        {{ saveText }}
      </button>
    </ng-template>
  `
})
export class FormDialogComponent {
  @Input({ required: true }) title = '';
  @Input() open = false;
  @Input() saving = false;
  @Input() saveDisabled = false;
  @Input() width = 640;
  @Input() bodyStyle: StyleObjectLike | undefined = undefined;
  @Input() saveText = 'Save';
  @Input() cancelText = 'Cancel';

  @Output() save = new EventEmitter<void>();
  @Output() cancel = new EventEmitter<void>();
  @Output() afterOpen = new EventEmitter<void>();
  @Output() afterClose = new EventEmitter<void>();

  handleCancel(): void {
    if (this.saving) {
      return;
    }

    this.cancel.emit();
  }

  handleSave(): void {
    if (this.saving || this.saveDisabled) {
      return;
    }

    this.save.emit();
  }
}
