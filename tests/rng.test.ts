import { describe, expect, it } from "vitest";
import { SeededRandom, randomId } from "@/utils/rng";
import { dailySeed, utcDateKey } from "@/utils/dailySeed";

describe("SeededRandom", () => {
  it("produces an identical sequence for the same seed", () => {
    const a = new SeededRandom("challenge-123");
    const b = new SeededRandom("challenge-123");
    const seqA = Array.from({ length: 20 }, () => a.float());
    const seqB = Array.from({ length: 20 }, () => b.float());
    expect(seqA).toEqual(seqB);
  });

  it("produces different sequences for different seeds", () => {
    const a = new SeededRandom("seed-one");
    const b = new SeededRandom("seed-two");
    const seqA = Array.from({ length: 10 }, () => a.float());
    const seqB = Array.from({ length: 10 }, () => b.float());
    expect(seqA).not.toEqual(seqB);
  });

  it("range()/intRange() stay within bounds", () => {
    const rng = new SeededRandom("bounds-test");
    for (let i = 0; i < 200; i++) {
      const v = rng.range(5, 10);
      expect(v).toBeGreaterThanOrEqual(5);
      expect(v).toBeLessThan(10);
      const iv = rng.intRange(1, 3);
      expect([1, 2, 3]).toContain(iv);
    }
  });
});

describe("randomId", () => {
  it("generates ids of the requested length from an unambiguous alphabet", () => {
    const id = randomId(10);
    expect(id).toHaveLength(10);
    expect(id).toMatch(/^[A-Z0-9]+$/);
    expect(id).not.toMatch(/[01OI]/); // ambiguous characters excluded
  });
});

describe("dailySeed / utcDateKey", () => {
  it("is stable for the same UTC calendar day regardless of local time", () => {
    const morning = new Date(Date.UTC(2026, 0, 15, 1, 0, 0));
    const night = new Date(Date.UTC(2026, 0, 15, 23, 59, 0));
    expect(utcDateKey(morning)).toBe(utcDateKey(night));
    expect(utcDateKey(morning)).toBe("2026-01-15");
  });

  it("changes across a UTC day boundary", () => {
    const beforeMidnight = new Date(Date.UTC(2026, 0, 15, 23, 59, 59));
    const afterMidnight = new Date(Date.UTC(2026, 0, 16, 0, 0, 1));
    expect(utcDateKey(beforeMidnight)).not.toBe(utcDateKey(afterMidnight));
  });

  it("produces a seed that is the same for every player on a given day+version", () => {
    const a = dailySeed("2026-01-15", "1.0.0");
    const b = dailySeed("2026-01-15", "1.0.0");
    expect(a).toBe(b);
  });

  it("changes the seed when the game version changes (old scores stay interpretable, new ones aren't mixed in)", () => {
    const v1 = dailySeed("2026-01-15", "1.0.0");
    const v2 = dailySeed("2026-01-15", "1.1.0");
    expect(v1).not.toBe(v2);
  });
});
