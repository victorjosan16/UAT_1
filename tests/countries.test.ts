import { describe, expect, it } from "vitest";
import { COUNTRIES, getCountryById } from "@/data/countries";

describe("COUNTRIES dataset", () => {
  it("has no duplicate ids", () => {
    const ids = COUNTRIES.map((c) => c.id);
    expect(new Set(ids).size).toBe(ids.length);
  });

  it("has no duplicate English names", () => {
    const names = COUNTRIES.map((c) => c.name.en);
    expect(new Set(names).size).toBe(names.length);
  });

  it("has both languages filled in for name and capital", () => {
    for (const country of COUNTRIES) {
      expect(country.name.en.length).toBeGreaterThan(0);
      expect(country.name.ro.length).toBeGreaterThan(0);
      expect(country.capital.en.length).toBeGreaterThan(0);
      expect(country.capital.ro.length).toBeGreaterThan(0);
    }
  });

  it("has enough countries to build 4-option questions", () => {
    expect(COUNTRIES.length).toBeGreaterThanOrEqual(20);
  });

  it("getCountryById finds a known country and returns undefined for an unknown one", () => {
    expect(getCountryById("romania")?.name.en).toBe("Romania");
    expect(getCountryById("does-not-exist")).toBeUndefined();
  });
});
