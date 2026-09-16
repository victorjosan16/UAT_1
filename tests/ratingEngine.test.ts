import { describe, expect, it } from "vitest";
import { initialRatingFromIQ, nudgeRating, tierForRating, TIER_CONFIG } from "@/quiz/RatingEngine";

describe("tierForRating", () => {
  it("places rating 0 at BRONZE III", () => {
    expect(tierForRating(0)).toEqual({ name: "BRONZE", division: 3, label: "BRONZE III" });
  });

  it("is monotonic — TIER_CONFIG is sorted ascending by minRating", () => {
    for (let i = 1; i < TIER_CONFIG.length; i++) {
      expect(TIER_CONFIG[i]!.minRating).toBeGreaterThan(TIER_CONFIG[i - 1]!.minRating);
    }
  });

  it("crosses into the next tier's bottom division exactly at its minRating", () => {
    expect(tierForRating(799)).toEqual({ name: "BRONZE", division: 1, label: "BRONZE I" });
    expect(tierForRating(800)).toEqual({ name: "SILVER", division: 3, label: "SILVER III" });
  });

  it("climbs divisions within a tier as rating rises", () => {
    // SILVER spans 800-1099 (GOLD starts at 1100), so each division is 100 wide.
    expect(tierForRating(800).label).toBe("SILVER III");
    expect(tierForRating(900).label).toBe("SILVER II");
    expect(tierForRating(1000).label).toBe("SILVER I");
  });

  it("never splits MASTER or GRANDMASTER into divisions", () => {
    expect(tierForRating(2000)).toEqual({ name: "MASTER", division: null, label: "MASTER" });
    expect(tierForRating(2400)).toEqual({ name: "GRANDMASTER", division: null, label: "GRANDMASTER" });
    expect(tierForRating(5000)).toEqual({ name: "GRANDMASTER", division: null, label: "GRANDMASTER" });
  });

  it("never goes negative or throws for an out-of-range rating", () => {
    expect(() => tierForRating(-500)).not.toThrow();
    expect(tierForRating(-500).name).toBe("BRONZE");
  });
});

describe("initialRatingFromIQ", () => {
  it("maps IQ 0 to the Bronze floor and IQ 100 to the top of Diamond", () => {
    expect(initialRatingFromIQ(0)).toBe(500);
    expect(initialRatingFromIQ(100)).toBe(1999);
  });

  it("clamps out-of-range IQ inputs", () => {
    expect(initialRatingFromIQ(-10)).toBe(500);
    expect(initialRatingFromIQ(150)).toBe(1999);
  });

  it("never places a fresh placement result above Diamond", () => {
    expect(tierForRating(initialRatingFromIQ(100)).name).not.toBe("MASTER");
  });
});

describe("nudgeRating", () => {
  it("moves rating toward a strong run's implied rating, not onto it", () => {
    const next = nudgeRating(1000, 100); // implied = 2000
    expect(next).toBeGreaterThan(1000);
    expect(next).toBeLessThan(2000);
  });

  it("moves rating toward a weak run's implied rating", () => {
    const next = nudgeRating(1000, 0); // implied = 500
    expect(next).toBeLessThan(1000);
  });

  it("caps a single run's swing so one result can't dominate the rating", () => {
    const next = nudgeRating(500, 100); // implied = 2000, a huge gap
    expect(next - 500).toBeLessThanOrEqual(40);
  });

  it("leaves rating unchanged when the run's implied rating matches it exactly", () => {
    expect(nudgeRating(1250, (1250 - 500) / 15)).toBe(1250);
  });
});
