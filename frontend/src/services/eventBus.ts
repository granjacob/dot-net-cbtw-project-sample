import type { RealtimeEvent } from "../types";

export type EventListener<T> = (event: T) => void;

export class EventBus {
  private readonly listeners = new Map<string, Set<EventListener<unknown>>>();

  subscribe<T>(eventName: string, listener: EventListener<T>): () => void {
    const subscribers = this.listeners.get(eventName) ?? new Set<EventListener<unknown>>();
    subscribers.add(listener as EventListener<unknown>);
    this.listeners.set(eventName, subscribers);

    return () => {
      subscribers.delete(listener as EventListener<unknown>);
      if (subscribers.size === 0) this.listeners.delete(eventName);
    };
  }

  publish<T>(eventName: string, event: T): void {
    this.listeners.get(eventName)?.forEach((listener) => listener(event));
    this.listeners.get("*")?.forEach((listener) => listener({ eventName, event }));
  }

  clear(): void {
    this.listeners.clear();
  }
}

export const eventBus = new EventBus();

export const realtimeEventNames = [
  "RequestCreated",
  "RequestUpdated",
  "RequestAssigned",
  "RequestStatusChanged",
  "CommentAdded",
  "NotificationCreated"
] as const;

export type RealtimeEventName = (typeof realtimeEventNames)[number];
export type RealtimeListener = EventListener<RealtimeEvent>;
