/** Debounces a refresh callback so bursts of realtime events collapse into one call. */
export class DebouncedRefresh {
  private timer: ReturnType<typeof setTimeout> | null = null;

  constructor(private readonly delayMs: number, private readonly refresh: () => void) {}

  schedule(): void {
    if (this.timer) {
      clearTimeout(this.timer);
    }

    this.timer = setTimeout(() => {
      this.timer = null;
      this.refresh();
    }, this.delayMs);
  }

  clear(): void {
    if (this.timer) {
      clearTimeout(this.timer);
      this.timer = null;
    }
  }
}

/**
 * Polls while an order is `PendingInventory` and realtime isn't connected, so the UI keeps
 * moving even if the SignalR connection drops. Stops itself once connected or after the
 * time window expires, so it never polls forever.
 */
export class PendingOrderPoller {
  private timer: ReturnType<typeof setInterval> | null = null;
  private deadline = 0;

  constructor(
    private readonly intervalMs: number,
    private readonly windowMs: number,
    private readonly isRealtimeConnected: () => boolean,
    private readonly refresh: () => void
  ) {}

  ensureStarted(): void {
    if (this.timer || this.isRealtimeConnected()) {
      return;
    }

    this.deadline = Date.now() + this.windowMs;
    this.timer = setInterval(() => {
      if (this.isRealtimeConnected() || Date.now() > this.deadline) {
        this.stop();
        return;
      }

      this.refresh();
    }, this.intervalMs);
  }

  stop(): void {
    if (this.timer) {
      clearInterval(this.timer);
      this.timer = null;
    }
  }
}
