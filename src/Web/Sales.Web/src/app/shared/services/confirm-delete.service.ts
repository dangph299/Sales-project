import { Injectable, inject } from '@angular/core';
import { NzModalService } from 'ng-zorro-antd/modal';

export interface ConfirmDeleteOptions {
  title: string;
  itemName: string;
  warningMessage?: string;
}

/**
 * Shared delete confirmation. Resolves true only when the user confirms;
 * resolves false on cancel, backdrop click or escape. Does not call any API -
 * the caller performs the delete and its own error handling after awaiting
 * this promise.
 */
@Injectable({ providedIn: 'root' })
export class ConfirmDeleteService {
  private readonly modal = inject(NzModalService);

  open(options: ConfirmDeleteOptions): Promise<boolean> {
    const content = options.warningMessage
      ? `${options.warningMessage} Delete "${options.itemName}"? This action cannot be undone.`
      : `Delete "${options.itemName}"? This action cannot be undone.`;

    return new Promise(resolve => {
      let resolved = false;
      const finish = (value: boolean) => {
        if (!resolved) {
          resolved = true;
          resolve(value);
        }
      };

      const modalRef = this.modal.confirm({
        nzTitle: options.title,
        nzContent: content,
        nzOkText: 'Delete',
        nzOkDanger: true,
        nzCancelText: 'Cancel',
        nzOnOk: () => finish(true),
        nzOnCancel: () => finish(false)
      });

      modalRef.afterClose.subscribe(() => finish(false));
    });
  }
}
