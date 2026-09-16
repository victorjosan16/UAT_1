import { describe, expect, it } from "vitest";
import { previousMonthKey, seasonMonthLabel, seasonNumberFor, utcMonthKey } from "@/utils/season";

describe("utcMonthKey", () => {
  it("formats a UTC year-month", () => {
    expect(utcMonthKey(new Date(Date.UTC(2026, 8, 16)))).toBe("2026-09");
  });

  it("pads single-digit months", () => {
    expect(utcMonthKey(new Date(Date.UTC(2026, 0, 1)))).toBe("2026-01");
  });
});

describe("previousMonthKey", () => {
  it("steps back one month within the same year", () => {
    expect(previousMonthKey("2026-09")).toBe("2026-08");
  });

  it("rolls back across a year boundary", () => {
    expect(previousMonthKey("2026-01")).toBe("2025-12");
  });
});

describe("seasonNumberFor", () => {
  it("Season 01 is January 2025", () => {
    expect(seasonNumberFor("2025-01")).toBe(1);
  });

  it("counts consecutive months within a year", () => {
    expect(seasonNumberFor("2025-09")).toBe(9);
  });

  it("carries the count across a year boundary", () => {
    expect(seasonNumberFor("2026-01")).toBe(13);
  });
});

describe("seasonMonthLabel", () => {
  it("renders the month name in English, uppercased", () => {
    expect(seasonMonthLabel("2026-09", "en")).toBe("SEPTEMBER");
  });

  it("renders the month name in Romanian", () => {
    expect(seasonMonthLabel("2026-09", "ro")).toBe("SEPTEMBRIE");
  });
});
