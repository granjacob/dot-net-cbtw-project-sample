import { afterEach, describe, expect, it, vi } from "vitest";
import { formatDateTime, formatRelative, getApiMessage, getInitials } from "./format";

describe("format utilities", () => {
  afterEach(() => vi.useRealTimers());

  it.each([null, undefined, "", "not-a-date"])("uses a dash for invalid date value %s", (value) => {
    expect(formatDateTime(value)).toBe("—");
  });

  it("formats relative dates using the largest meaningful unit", () => {
    vi.useFakeTimers();
    vi.setSystemTime(new Date("2026-07-27T12:00:00.000Z"));

    expect(formatRelative("2026-07-27T12:00:20.000Z")).toBe("ahora");
    expect(formatRelative("2026-07-27T10:00:00.000Z")).toMatch(/2.*horas?/i);
    expect(formatRelative("2026-07-29T12:00:00.000Z")).toMatch(/pasado mañana|2.*días?/i);
  });

  it.each([
    ["Ana Pérez", "AP"],
    ["  juan   carlos restrepo ", "JC"],
    ["Marta", "M"]
  ])("gets at most two initials from %s", (name, expected) => {
    expect(getInitials(name)).toBe(expected);
  });

  it("extracts Error messages and provides a fallback", () => {
    expect(getApiMessage(new Error("API unavailable"))).toBe("API unavailable");
    expect(getApiMessage({ detail: "unknown" })).toBe("Ocurrió un error inesperado.");
  });
});
