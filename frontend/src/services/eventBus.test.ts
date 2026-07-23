import { describe, expect, it, vi } from "vitest";
import { EventBus } from "./eventBus";

describe("EventBus", () => {
  it("notifica a todos los observadores suscritos", () => {
    const bus = new EventBus();
    const first = vi.fn();
    const second = vi.fn();
    bus.subscribe("RequestUpdated", first);
    bus.subscribe("RequestUpdated", second);

    bus.publish("RequestUpdated", { requestId: 42 });

    expect(first).toHaveBeenCalledWith({ requestId: 42 });
    expect(second).toHaveBeenCalledWith({ requestId: 42 });
  });

  it("deja de notificar después de cancelar la suscripción", () => {
    const bus = new EventBus();
    const listener = vi.fn();
    const unsubscribe = bus.subscribe("CommentAdded", listener);
    unsubscribe();

    bus.publish("CommentAdded", { requestId: 7 });

    expect(listener).not.toHaveBeenCalled();
  });
});
