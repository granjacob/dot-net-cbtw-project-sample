import type { RealtimeEvent } from "../types";

export interface RequestStoreSnapshot {
  version: number;
  lastEventName: string | null;
  lastEvent: RealtimeEvent | null;
  receivedAt: number | null;
}

let snapshot: RequestStoreSnapshot = {
  version: 0,
  lastEventName: null,
  lastEvent: null,
  receivedAt: null
};

const listeners = new Set<() => void>();

export const requestStore = {
  subscribe(listener: () => void): () => void {
    listeners.add(listener);
    return () => listeners.delete(listener);
  },

  getSnapshot(): RequestStoreSnapshot {
    return snapshot;
  },

  apply(eventName: string, event: RealtimeEvent): void {
    snapshot = {
      version: snapshot.version + 1,
      lastEventName: eventName,
      lastEvent: event,
      receivedAt: Date.now()
    };
    listeners.forEach((listener) => listener());
  },

  reset(): void {
    snapshot = { version: 0, lastEventName: null, lastEvent: null, receivedAt: null };
    listeners.forEach((listener) => listener());
  }
};
