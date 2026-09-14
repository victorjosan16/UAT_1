import { describe, expect, it } from "vitest";
import { hexToHsl, hslToHex, rotateHueTo, lerpHueShortest } from "@/utils/color";

describe("hexToHsl / hslToHex round-trip", () => {
  it("recovers (close to) the original hex for a mid-tone color", () => {
    const original = "#4fd1ff";
    const { h, s, l } = hexToHsl(original);
    const roundTripped = hslToHex(h, s, l);
    // Allow a tiny rounding tolerance (hex quantization).
    for (let i = 1; i < original.length; i += 2) {
      const a = parseInt(original.slice(i, i + 2), 16);
      const b = parseInt(roundTripped.slice(i, i + 2), 16);
      expect(Math.abs(a - b)).toBeLessThanOrEqual(2);
    }
  });

  it("treats pure gray as having zero saturation", () => {
    const { s } = hexToHsl("#808080");
    expect(s).toBeCloseTo(0, 2);
  });
});

describe("rotateHueTo", () => {
  it("keeps saturation/lightness but changes the hue", () => {
    const original = "#4fd1ff";
    const rotated = rotateHueTo(original, 300);
    expect(rotated).not.toBe(original);
    const before = hexToHsl(original);
    const after = hexToHsl(rotated);
    expect(after.h).toBeCloseTo(300, 0);
    expect(after.s).toBeCloseTo(before.s, 1);
    expect(after.l).toBeCloseTo(before.l, 1);
  });

  it("is idempotent-ish: rotating to the same hue twice gives the same result", () => {
    const a = rotateHueTo("#4fd1ff", 120);
    const b = rotateHueTo(a, 120);
    expect(a).toBe(b);
  });
});

describe("lerpHueShortest", () => {
  it("interpolates linearly when there's no wraparound", () => {
    expect(lerpHueShortest(10, 50, 0.5)).toBeCloseTo(30, 5);
  });

  it("takes the short way around the 0/360 seam instead of the long way", () => {
    // 350 -> 10 the "short way" goes through 0/360 (20° apart), not through 180 (340° apart).
    const mid = lerpHueShortest(350, 10, 0.5);
    const distanceFromSeam = Math.min(Math.abs(mid - 0), Math.abs(mid - 360));
    expect(distanceFromSeam).toBeLessThan(15);
  });

  it("returns the start hue at t=0 and the end hue at t=1", () => {
    expect(lerpHueShortest(200, 260, 0)).toBeCloseTo(200, 5);
    expect(lerpHueShortest(200, 260, 1)).toBeCloseTo(260, 5);
  });
});
