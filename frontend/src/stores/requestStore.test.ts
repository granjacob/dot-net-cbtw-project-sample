import { beforeEach, describe, expect, it, vi } from "vitest";
import { requestStore } from "./requestStore";

describe("requestStore", () => {
  beforeEach(() => requestStore.reset());

  it("publica un snapshot nuevo cuando llega un evento", () => {
    const observer = vi.fn();
    requestStore.subscribe(observer);

    requestStore.apply("RequestStatusChanged", { requestId: 148, newStatus: "Resolved" });

    expect(requestStore.getSnapshot()).toMatchObject({
      version: 1,
      lastEventName: "RequestStatusChanged",
      lastEvent: { requestId: 148, newStatus: "Resolved" }
    });
    expect(observer).toHaveBeenCalledOnce();
  });
});
