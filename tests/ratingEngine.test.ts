import { describe, expect, it } from "vitest";
import { computeArenaRatingChanges, initialRatingFromIQ, nudgeRating, placementsFromScores, tierForRating, TIER_CONFIG, type ArenaMatchResult } from "@/quiz/RatingEngine";

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

describe("placementsFromScores", () => {
  it("ranks distinct scores in descending order", () => {
    const placements = placementsFromScores([
      { playerId: "a", score: 100 },
      { playerId: "b", score: 300 },
      { playerId: "c", score: 200 },
    ]);
    expect(placements.get("b")).toBe(1);
    expect(placements.get("c")).toBe(2);
    expect(placements.get("a")).toBe(3);
  });

  it("gives tied scores the same placement and skips the next rank (competition ranking)", () => {
    const placements = placementsFromScores([
      { playerId: "a", score: 100 },
      { playerId: "b", score: 300 },
      { playerId: "c", score: 300 },
      { playerId: "d", score: 50 },
    ]);
    expect(placements.get("b")).toBe(1);
    expect(placements.get("c")).toBe(1);
    expect(placements.get("a")).toBe(3); // not 2 — two players share 1st
    expect(placements.get("d")).toBe(4);
  });
});

describe("computeArenaRatingChanges", () => {
  function result(playerId: string, ratingBefore: number, placement: number): ArenaMatchResult {
    return { playerId, ratingBefore, placement };
  }

  it("a single player (no opponents) never changes rating", () => {
    const changes = computeArenaRatingChanges([result("solo", 1200, 1)]);
    expect(changes).toEqual([{ playerId: "solo", ratingBefore: 1200, ratingAfter: 1200, delta: 0 }]);
  });

  it("equal-rated 1v1: the winner gains exactly what the loser loses", () => {
    const changes = computeArenaRatingChanges([result("winner", 1000, 1), result("loser", 1000, 2)]);
    const winner = changes.find((c) => c.playerId === "winner")!;
    const loser = changes.find((c) => c.playerId === "loser")!;
    expect(winner.delta).toBeGreaterThan(0);
    expect(loser.delta).toBe(-winner.delta);
  });

  it("beating a higher-rated field (an upset) gains more than beating an equally-rated field", () => {
    const upsetChanges = computeArenaRatingChanges([result("underdog", 900, 1), result("favorite", 1300, 2)]);
    const evenChanges = computeArenaRatingChanges([result("a", 1000, 1), result("b", 1000, 2)]);
    const upsetGain = upsetChanges.find((c) => c.playerId === "underdog")!.delta;
    const evenGain = evenChanges.find((c) => c.playerId === "a")!.delta;
    expect(upsetGain).toBeGreaterThan(evenGain);
  });

  it("in a 5-player equal-rated match, rating change strictly improves with better placement", () => {
    const changes = computeArenaRatingChanges([1, 2, 3, 4, 5].map((placement) => result(`p${placement}`, 1200, placement)));
    for (let i = 1; i < changes.length; i++) {
      expect(changes[i - 1]!.delta).toBeGreaterThan(changes[i]!.delta);
    }
    // 1st place gains, last place loses.
    expect(changes[0]!.delta).toBeGreaterThan(0);
    expect(changes[changes.length - 1]!.delta).toBeLessThan(0);
  });

  it("tied placements produce equal rating changes among the tied players", () => {
    const changes = computeArenaRatingChanges([result("a", 1200, 1), result("b", 1200, 1), result("c", 1200, 3)]);
    const a = changes.find((c) => c.playerId === "a")!.delta;
    const b = changes.find((c) => c.playerId === "b")!.delta;
    expect(a).toBe(b);
  });

  it("caps a single match's swing so it can never dominate a rating", () => {
    // An extreme mismatch: a very low-rated player somehow wins against a much higher field.
    const changes = computeArenaRatingChanges([
      result("longshot", 500, 1),
      result("a", 2400, 2),
      result("b", 2400, 3),
      result("c", 2400, 4),
      result("d", 2400, 5),
    ]);
    for (const change of changes) {
      expect(Math.abs(change.delta)).toBeLessThanOrEqual(60);
    }
  });
});
