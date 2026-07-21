import { Injectable, signal } from '@angular/core';
import { HubConnection, HubConnectionBuilder, HubConnectionState, LogLevel } from '@microsoft/signalr';
import { RealtimeConnectionState } from './realtime-connection-state';

/**
 * Generic SignalR hub connection lifecycle: connect, reconnect, state, invoke.
 *
 * Knows nothing about any particular hub's events or groups. Feature services
 * configure it with a hub URL and register a resubscribe callback that replays
 * their own group memberships after a reconnect.
 */
@Injectable({ providedIn: 'root' })
export class SignalrConnectionService {
  readonly state = signal<RealtimeConnectionState>('Disconnected');

  private connection: HubConnection | null = null;
  private startPromise: Promise<void> | null = null;
  private hubUrlFactory: (() => string) | null = null;
  private accessTokenFactory: (() => string) | null = null;
  private readonly eventHandlers = new Map<string, Set<(payload: never) => void>>();
  private readonly resubscribeCallbacks = new Set<() => Promise<void>>();
  private readonly reconnectedCallbacks = new Set<() => void>();

  /** Must be called before start(). Safe to call repeatedly with the same values. */
  configure(hubUrlFactory: () => string, accessTokenFactory: () => string): void {
    this.hubUrlFactory = hubUrlFactory;
    this.accessTokenFactory = accessTokenFactory;
  }

  get isConnected(): boolean {
    return this.connection?.state === HubConnectionState.Connected;
  }

  on<TPayload>(eventName: string, handler: (payload: TPayload) => void): () => void {
    let handlers = this.eventHandlers.get(eventName);
    if (!handlers) {
      handlers = new Set();
      this.eventHandlers.set(eventName, handlers);
      this.connection?.on(eventName, payload => this.dispatch(eventName, payload));
    }

    handlers.add(handler as (payload: never) => void);
    return () => handlers.delete(handler as (payload: never) => void);
  }

  onReconnected(callback: () => void): () => void {
    this.reconnectedCallbacks.add(callback);
    return () => this.reconnectedCallbacks.delete(callback);
  }

  /** Registers work that replays group membership after connect and reconnect. */
  registerResubscribe(callback: () => Promise<void>): () => void {
    this.resubscribeCallbacks.add(callback);
    return () => this.resubscribeCallbacks.delete(callback);
  }

  async start(): Promise<void> {
    const connection = this.ensureConnection();
    if (connection.state === HubConnectionState.Connected) {
      this.state.set('Connected');
      return;
    }

    if (this.startPromise) {
      return this.startPromise;
    }

    this.state.set('Connecting');
    this.startPromise = connection.start()
      .then(async () => {
        this.state.set('Connected');
        await this.resubscribe();
      })
      .catch(error => {
        this.state.set('Disconnected');
        console.warn('Realtime connection failed.', error);
      })
      .finally(() => {
        this.startPromise = null;
      });

    return this.startPromise;
  }

  async stop(): Promise<void> {
    if (!this.connection) {
      this.state.set('Disconnected');
      return;
    }

    await this.connection.stop();
    this.state.set('Disconnected');
  }

  async invoke(methodName: string, ...args: unknown[]): Promise<void> {
    try {
      await this.connection?.invoke(methodName, ...args);
    } catch (error) {
      console.warn(`Realtime ${methodName} failed.`, error);
    }
  }

  private ensureConnection(): HubConnection {
    if (this.connection) {
      return this.connection;
    }

    if (!this.hubUrlFactory || !this.accessTokenFactory) {
      throw new Error('SignalrConnectionService.configure() must be called before start().');
    }

    const accessTokenFactory = this.accessTokenFactory;
    this.connection = new HubConnectionBuilder()
      .withUrl(this.hubUrlFactory(), { accessTokenFactory: () => accessTokenFactory() })
      .withAutomaticReconnect([0, 2_000, 10_000, 30_000])
      .configureLogging(LogLevel.Information)
      .build();

    this.eventHandlers.forEach((_, eventName) => {
      this.connection?.on(eventName, payload => this.dispatch(eventName, payload));
    });

    this.connection.onreconnecting(() => this.state.set('Reconnecting'));
    this.connection.onreconnected(() => {
      this.state.set('Connected');
      void this.resubscribe();
      this.reconnectedCallbacks.forEach(callback => callback());
    });
    this.connection.onclose(() => this.state.set('Disconnected'));

    return this.connection;
  }

  private dispatch(eventName: string, payload: unknown): void {
    this.eventHandlers.get(eventName)?.forEach(handler => (handler as (value: unknown) => void)(payload));
  }

  private async resubscribe(): Promise<void> {
    if (!this.isConnected) {
      return;
    }

    for (const callback of this.resubscribeCallbacks) {
      await callback();
    }
  }
}
