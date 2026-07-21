import { NzModalService } from 'ng-zorro-antd/modal';

/**
 * Resolves true only when the user confirms. Dismissing the dialog any other
 * way (cancel, backdrop, escape) resolves false.
 */
export function confirmAction(modal: NzModalService, title: string, content: string): Promise<boolean> {
  return new Promise(resolve => {
    let resolved = false;
    const finish = (value: boolean) => {
      if (!resolved) {
        resolved = true;
        resolve(value);
      }
    };

    const modalRef = modal.confirm({
      nzTitle: title,
      nzContent: `${content} Are you sure you want to continue?`,
      nzOkText: 'Continue',
      nzCancelText: 'Cancel',
      nzOnOk: () => finish(true),
      nzOnCancel: () => finish(false)
    });

    modalRef.afterClose.subscribe(() => finish(false));
  });
}
