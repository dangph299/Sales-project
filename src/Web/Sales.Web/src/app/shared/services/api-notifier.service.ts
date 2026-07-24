import { Injectable, inject } from '@angular/core';
import { NzNotificationService } from 'ng-zorro-antd/notification';
import { describeApiError } from '../utilities/describe-api-error';

/**
 * Single place to show success/error notifications for API operations, so
 * feature components don't each hand-roll `notification.error(title, ...)`
 * plus their own error-to-message mapping. Returns the resolved message so
 * callers can also store it in a local error signal if the page needs it.
 */
@Injectable({ providedIn: 'root' })
export class ApiNotifierService {
  private readonly notification = inject(NzNotificationService);

  success(title: string, message: string): void {
    this.notification.success(title, message);
  }

  error(title: string, error: unknown): string {
    const message = typeof error === 'string' ? error : describeApiError(error);
    this.notification.error(title, message);
    return message;
  }
}
