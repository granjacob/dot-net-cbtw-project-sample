import * as signalR from "@microsoft/signalr";
import { eventBus, realtimeEventNames } from "./eventBus";
import { requestStore } from "../stores/requestStore";
import type { RealtimeEvent } from "../types";

export type ConnectionState = "disconnected" | "connecting" | "connected" | "reconnecting";

class RealtimeService {
  private connection: signalR.HubConnection | null = null;
  private token: string | null = null;
  private state: ConnectionState = "disconnected";
  private retryTimer: number | null = null;
  private retryAttempt = 0;
  private readonly stateListeners = new Set<(state: ConnectionState) => void>();

  subscribeState(listener: (state: ConnectionState) => void): () => void {
    this.stateListeners.add(listener);
    listener(this.state);
    return () => this.stateListeners.delete(listener);
  }

  private setState(state: ConnectionState): void {
    this.state = state;
    this.stateListeners.forEach((listener) => listener(state));
  }

  async connect(token: string): Promise<void> {
    if (this.connection && this.token === token && this.connection.state !== signalR.HubConnectionState.Disconnected) {
      return;
    }

    await this.disconnect();
    this.token = token;
    this.setState("connecting");
    const connection = new signalR.HubConnectionBuilder()
      .withUrl("/hubs/notifications", { accessTokenFactory: () => this.token ?? "" })
      .withAutomaticReconnect([0, 2_000, 5_000, 10_000, 30_000])
      .configureLogging(signalR.LogLevel.Warning)
      .build();

    realtimeEventNames.forEach((eventName) => {
      connection.on(eventName, (payload: RealtimeEvent) => {
        const event = payload ?? {};
        requestStore.apply(eventName, event);
        eventBus.publish(eventName, event);
      });
    });

    connection.onreconnecting(() => this.setState("reconnecting"));
    connection.onreconnected(() => {
      this.retryAttempt = 0;
      this.setState("connected");
    });
    connection.onclose(() => {
      this.setState("disconnected");
      this.scheduleRetry(connection);
    });
    this.connection = connection;
    await this.start(connection);
  }

  private async start(connection: signalR.HubConnection): Promise<void> {
    if (this.connection !== connection || !this.token) return;
    this.setState("connecting");
    try {
      await connection.start();
      if (this.connection !== connection) return;
      this.retryAttempt = 0;
      this.setState("connected");
    } catch (error) {
      if (this.connection !== connection) return;
      this.setState("disconnected");
      console.warn("No fue posible iniciar SignalR; se reintentará automáticamente.", error);
      this.scheduleRetry(connection);
    }
  }

  private scheduleRetry(connection: signalR.HubConnection): void {
    if (this.connection !== connection || !this.token || this.retryTimer !== null) return;
    const delays = [2_000, 5_000, 10_000, 30_000];
    const delay = delays[Math.min(this.retryAttempt, delays.length - 1)];
    this.retryAttempt += 1;
    this.retryTimer = window.setTimeout(() => {
      this.retryTimer = null;
      void this.start(connection);
    }, delay);
  }

  async disconnect(): Promise<void> {
    const connection = this.connection;
    this.connection = null;
    this.token = null;
    this.retryAttempt = 0;
    if (this.retryTimer !== null) {
      window.clearTimeout(this.retryTimer);
      this.retryTimer = null;
    }
    if (connection) {
      try {
        await connection.stop();
      } catch {
        // Closing an already interrupted connection is safe to ignore.
      }
    }
    this.setState("disconnected");
  }
}

export const realtimeService = new RealtimeService();
