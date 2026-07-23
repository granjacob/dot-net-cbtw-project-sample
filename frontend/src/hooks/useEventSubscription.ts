import { useEffect, useRef } from "react";
import { eventBus } from "../services/eventBus";

export function useEventSubscription<T>(eventName: string, callback: (event: T) => void): void {
  const callbackRef = useRef(callback);
  callbackRef.current = callback;

  useEffect(
    () => eventBus.subscribe<T>(eventName, (event) => callbackRef.current(event)),
    [eventName]
  );
}
