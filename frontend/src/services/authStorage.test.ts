import { beforeEach, describe, expect, it, vi } from "vitest";
import { clearSession, getAccessToken, readSession, saveSession } from "./authStorage";
import type { AuthSession } from "../types";

const session: AuthSession = {
  token: "signed-token",
  expiresAt: "2030-01-01T00:00:00.000Z",
  user: { email: "employee@serviceflow.local", name: "Employee", role: "Employee" }
};

describe("authStorage", () => {
  beforeEach(() => {
    localStorage.clear();
    vi.useRealTimers();
  });

  it("persists and restores a valid session", () => {
    vi.setSystemTime(new Date("2026-07-27T12:00:00.000Z"));

    saveSession(session);

    expect(readSession()).toEqual(session);
    expect(getAccessToken()).toBe("signed-token");
  });

  it("removes expired sessions", () => {
    vi.setSystemTime(new Date("2031-01-01T00:00:00.000Z"));
    localStorage.setItem("serviceflow.session", JSON.stringify(session));

    expect(readSession()).toBeNull();
    expect(localStorage.getItem("serviceflow.session")).toBeNull();
  });

  it.each([
    ["invalid JSON", "{"],
    ["missing token", JSON.stringify({ ...session, token: "" })],
    ["missing user", JSON.stringify({ ...session, user: null })]
  ])("rejects and removes a session with %s", (_, storedValue) => {
    localStorage.setItem("serviceflow.session", storedValue);

    expect(readSession()).toBeNull();
    expect(localStorage.getItem("serviceflow.session")).toBeNull();
  });

  it("clears the stored session", () => {
    saveSession(session);

    clearSession();

    expect(readSession()).toBeNull();
  });
});
