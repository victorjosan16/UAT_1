import { describe, expect, it } from "vitest";
import { computeOverlap } from "@/entities/Tower";

describe("computeOverlap", () => {
  it("returns a PERFECT snap when offset is within tolerance", () => {
    const previous = { left: -50, right: 50 };
    const moving = { left: -48, right: 52 }; // center offset = 2
    const result = computeOverlap(previous, moving, 5);

    expect(result.isPerfect).toBe(true);
    expect(result.overlap).toEqual(previous);
    expect(result.fallingFragments).toHaveLength(0);
  });

  it("clips to the intersection and produces one fragment on a single-side overhang", () => {
    const previous = { left: -50, right: 50 };
    const moving = { left: -20, right: 80 }; // shifted right by 30
    const result = computeOverlap(previous, moving, 5);

    expect(result.isPerfect).toBe(false);
    expect(result.overlap).toEqual({ left: -20, right: 50 });
    expect(result.fallingFragments).toEqual([{ left: 50, right: 80 }]);
  });

  it("produces two fragments when the moving block fully contains the previous block (and isn't center-aligned enough to be PERFECT)", () => {
    const previous = { left: -20, right: 20 };
    const moving = { left: -70, right: 50 }; // wider, shifted left by 10
    const result = computeOverlap(previous, moving, 2);

    expect(result.isPerfect).toBe(false);
    expect(result.overlap).toEqual({ left: -20, right: 20 });
    expect(result.fallingFragments).toEqual([
      { left: -70, right: -20 },
      { left: 20, right: 50 },
    ]);
  });

  it("returns null overlap (miss) when there is zero geometric overlap", () => {
    const previous = { left: -50, right: -10 };
    const moving = { left: 10, right: 50 };
    const result = computeOverlap(previous, moving, 5);

    expect(result.overlap).toBeNull();
    expect(result.fallingFragments).toEqual([{ left: 10, right: 50 }]);
  });

  it("treats exactly-touching edges as a miss (zero-width overlap)", () => {
    const previous = { left: -50, right: 0 };
    const moving = { left: 0, right: 50 };
    const result = computeOverlap(previous, moving, 1);

    expect(result.overlap).toBeNull();
  });

  it("is symmetric: tolerance boundary is inclusive", () => {
    const previous = { left: -50, right: 50 };
    const moving = { left: -45, right: 55 }; // offset exactly 5
    const result = computeOverlap(previous, moving, 5);
    expect(result.isPerfect).toBe(true);
  });
});
