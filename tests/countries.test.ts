import { describe, expect, it } from "vitest";
import { COUNTRIES, getCountryById } from "@/data/countries";

describe("COUNTRIES dataset", () => {
  it("has no duplicate ids", () => {
    const ids = COUNTRIES.map((c) => c.id);
    expect(new Set(ids).size).toBe(ids.length);
  });

  it("has no duplicate names", () => {
    const names = COUNTRIES.map((c) => c.name);
    expect(new Set(names).size).toBe(names.length);
  });

  it("has enough countries to build 4-option questions", () => {
    expect(COUNTRIES.length).toBeGreaterThanOrEqual(20);
  });

  it("getCountryById finds a known country and returns undefined for an unknown one", () => {
    expect(getCountryById("romania")?.name).toBe("Romania");
    expect(getCountryById("does-not-exist")).toBeUndefined();
  });
});
