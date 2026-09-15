import { describe, expect, it } from "vitest";
import { CLUBS, getClubById } from "@/data/clubs";

describe("CLUBS dataset", () => {
  it("has no duplicate ids", () => {
    const ids = CLUBS.map((c) => c.id);
    expect(new Set(ids).size).toBe(ids.length);
  });

  it("has no duplicate names", () => {
    const names = CLUBS.map((c) => c.name);
    expect(new Set(names).size).toBe(names.length);
  });

  it("every club has a valid difficulty tier (1-5)", () => {
    for (const club of CLUBS) {
      expect(club.difficulty).toBeGreaterThanOrEqual(1);
      expect(club.difficulty).toBeLessThanOrEqual(5);
    }
  });

  it("has enough clubs to build 4-option questions at every difficulty tier", () => {
    for (let d = 1; d <= 5; d++) {
      const atTier = CLUBS.filter((c) => c.difficulty === d);
      expect(atTier.length).toBeGreaterThanOrEqual(1);
    }
    // Overall pool must comfortably support 4-option questions.
    expect(CLUBS.length).toBeGreaterThanOrEqual(20);
  });

  it("getClubById finds a known club and returns undefined for an unknown one", () => {
    expect(getClubById("arsenal")?.name).toBe("Arsenal");
    expect(getClubById("does-not-exist")).toBeUndefined();
  });
});
