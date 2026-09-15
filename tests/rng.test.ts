import { describe, expect, it } from "vitest";
import { SeededRandom } from "@/utils/rng";

describe("SeededRandom", () => {
  it("is deterministic for a given seed", () => {
    const a = new SeededRandom("seed-1");
    const b = new SeededRandom("seed-1");
    const seqA = Array.from({ length: 10 }, () => a.float());
    const seqB = Array.from({ length: 10 }, () => b.float());
    expect(seqA).toEqual(seqB);
  });

  it("produces different sequences for different seeds", () => {
    const a = new SeededRandom("seed-1");
    const b = new SeededRandom("seed-2");
    const seqA = Array.from({ length: 10 }, () => a.float());
    const seqB = Array.from({ length: 10 }, () => b.float());
    expect(seqA).not.toEqual(seqB);
  });

  describe("shuffle", () => {
    it("is a permutation of the input (no duplicates, no drops)", () => {
      const rng = new SeededRandom("shuffle-seed");
      const items = [1, 2, 3, 4, 5, 6, 7, 8];
      const shuffled = rng.shuffle(items);
      expect(shuffled.slice().sort((x, y) => x - y)).toEqual(items);
    });

    it("does not mutate the input array", () => {
      const rng = new SeededRandom("shuffle-seed-2");
      const items = [1, 2, 3];
      const copy = [...items];
      rng.shuffle(items);
      expect(items).toEqual(copy);
    });

    it("is deterministic for the same seed", () => {
      const items = ["a", "b", "c", "d", "e"];
      const a = new SeededRandom("shuffle-det").shuffle(items);
      const b = new SeededRandom("shuffle-det").shuffle(items);
      expect(a).toEqual(b);
    });
  });
});
