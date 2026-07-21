import { Injectable, inject } from '@angular/core';
import { SessionService } from '../../../core/auth/session.service';
import { ApiEndpointConfigurationService } from '../../../core/config/api-endpoint-configuration.service';
import { SignalrConnectionService } from '../../../core/realtime/signalr-connection.service';
import { OrderStatusChangedNotification } from '../models/order-status-changed.model';

export const orderRealtimeEvents = {
  statusChanged: 'OrderStatusChanged'
} as const;

/**
 * Order-specific realtime behaviour: which hub, which groups, which events.
 * Connection lifecycle and reconnection belong to SignalrConnectionService.
 */
@Injectable({ providedIn: 'root' })
export class OrderRealtimeService {
  private readonly connection = inject(SignalrConnectionService);
  private readonly endpoints = inject(ApiEndpointConfigurationService);
  private readonly session = inject(SessionService);

  readonly state = this.connection.state;

  private readonly subscribedOrderIds = new Set<string>();
  private orderListSubscribed = false;

  constructor() {
    this.connection.configure(
      () => `${this.endpoints.salesBase()}/hubs/orders`,
      () => this.session.accessToken());

    this.connection.registerResubscribe(() => this.resubscribe());
  }

  start(): Promise<void> {
    return this.connection.start();
  }

  async stop(): Promise<void> {
    this.subscribedOrderIds.clear();
    this.orderListSubscribed = false;
    await this.connection.stop();
  }

  onStatusChanged(handler: (notification: OrderStatusChangedNotification) => void): () => void {
    return this.connection.on<OrderStatusChangedNotification>(orderRealtimeEvents.statusChanged, handler);
  }

  onReconnected(handler: () => void): () => void {
    return this.connection.onReconnected(handler);
  }

  async subscribeToOrder(orderId: string): Promise<void> {
    this.subscribedOrderIds.add(orderId);
    await this.connection.start();
    if (this.connection.isConnected) {
      await this.connection.invoke('SubscribeToOrder', orderId);
    }
  }

  async unsubscribeFromOrder(orderId: string): Promise<void> {
    this.subscribedOrderIds.delete(orderId);
    if (this.connection.isConnected) {
      await this.connection.invoke('UnsubscribeFromOrder', orderId);
    }
  }

  async subscribeToOrderList(): Promise<void> {
    this.orderListSubscribed = true;
    await this.connection.start();
    if (this.connection.isConnected) {
      await this.connection.invoke('SubscribeToOrderList');
    }
  }

  async unsubscribeFromOrderList(): Promise<void> {
    this.orderListSubscribed = false;
    if (this.connection.isConnected) {
      await this.connection.invoke('UnsubscribeFromOrderList');
    }
  }

  private async resubscribe(): Promise<void> {
    if (this.orderListSubscribed) {
      await this.connection.invoke('SubscribeToOrderList');
    }

    for (const orderId of this.subscribedOrderIds) {
      await this.connection.invoke('SubscribeToOrder', orderId);
    }
  }
}
