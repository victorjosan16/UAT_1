/**
 * Deterministic PRNG (mulberry32) + string seed hashing (xmur3).
 * Used everywhere gameplay must be reproducible: Daily Tower, Challenges,
 * and Endless Mode modifier timing.
 */

export function xmur3(str: string): () => number {
  let h = 1779033703 ^ str.length;
  for (let i = 0; i < str.length; i++) {
    h = Math.imul(h ^ str.charCodeAt(i), 3432918353);
    h = (h << 13) | (h >>> 19);
  }
  return () => {
    h = Math.imul(h ^ (h >>> 16), 2246822507);
    h = Math.imul(h ^ (h >>> 13), 3266489909);
    h ^= h >>> 16;
    return h >>> 0;
  };
}

export function mulberry32(seedInt: number): () => number {
  let a = seedInt >>> 0;
  return () => {
    a |= 0;
    a = (a + 0x6d2b79f5) | 0;
    let t = Math.imul(a ^ (a >>> 15), 1 | a);
    t = (t + Math.imul(t ^ (t >>> 7), 61 | t)) ^ t;
    return ((t ^ (t >>> 14)) >>> 0) / 4294967296;
  };
}

/** A named RNG bound to a string seed, producing floats in [0, 1). */
export class SeededRandom {
  private readonly next: () => number;

  constructor(seed: string) {
    const hash = xmur3(seed);
    this.next = mulberry32(hash());
  }

  float(): number {
    return this.next();
  }

  range(min: number, max: number): number {
    return min + this.next() * (max - min);
  }

  intRange(min: number, maxInclusive: number): number {
    return Math.floor(this.range(min, maxInclusive + 1));
  }

  pick<T>(items: readonly T[]): T {
    const value = items[this.intRange(0, items.length - 1)];
    if (value === undefined) {
      throw new Error("SeededRandom.pick called on an empty array");
    }
    return value;
  }

  sign(): 1 | -1 {
    return this.next() < 0.5 ? -1 : 1;
  }

  /** Fisher-Yates shuffle — deterministic for this RNG's seed/position, never mutates the input. */
  shuffle<T>(items: readonly T[]): T[] {
    const result = [...items];
    for (let i = result.length - 1; i > 0; i--) {
      const j = this.intRange(0, i);
      const a = result[i];
      const b = result[j];
      if (a === undefined || b === undefined) continue;
      result[i] = b;
      result[j] = a;
    }
    return result;
  }
}

/** Random-looking but unguessable id for guest players / challenges. */
export function randomId(length = 8): string {
  const alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // no ambiguous chars
  const bytes = new Uint32Array(length);
  if (typeof crypto !== "undefined" && crypto.getRandomValues) {
    crypto.getRandomValues(bytes);
  } else {
    for (let i = 0; i < length; i++) bytes[i] = Math.floor(Math.random() * 2 ** 32);
  }
  let out = "";
  for (let i = 0; i < length; i++) {
    out += alphabet[(bytes[i] ?? 0) % alphabet.length];
  }
  return out;
}
