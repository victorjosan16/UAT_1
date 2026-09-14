import { describe, expect, it } from "vitest";
import { hueForFloor } from "@/ui/theme/levelHue";
import { resolveBlockColors } from "@/ui/theme/blockColor";
import { DEFAULT_TOWER_SKIN, TOWER_SKINS } from "@/ui/skins/TowerSkin";

describe("hueForFloor", () => {
  it("is deterministic for the same floor", () => {
    expect(hueForFloor(37)).toBe(hueForFloor(37));
  });

  it("changes gradually rather than jumping abruptly between adjacent floors", () => {
    for (let floor = 1; floor < 200; floor++) {
      const a = hueForFloor(floor);
      const b = hueForFloor(floor + 1);
      const diff = Math.min(Math.abs(a - b), 360 - Math.abs(a - b));
      expect(diff).toBeLessThan(10); // no hard cuts anywhere, including level boundaries
    }
  });

  it("stays within [0, 360)", () => {
    for (const floor of [0, 1, 50, 500, 5000]) {
      const hue = hueForFloor(floor);
      expect(hue).toBeGreaterThanOrEqual(0);
      expect(hue).toBeLessThan(360);
    }
  });

  it("keeps producing values indefinitely into Endless (no crash, stays bounded)", () => {
    expect(() => hueForFloor(1_000_000)).not.toThrow();
  });
});

describe("resolveBlockColors", () => {
  it("returns null for a non-default skin (purchased skins keep their fixed palette)", () => {
    const neon = TOWER_SKINS.find((s) => s.id === "neon");
    expect(neon).toBeDefined();
    if (neon) expect(resolveBlockColors(neon, 10)).toBeNull();
  });

  it("returns hue-shifted colors for the default skin, different across distant floors", () => {
    const early = resolveBlockColors(DEFAULT_TOWER_SKIN, 1);
    const later = resolveBlockColors(DEFAULT_TOWER_SKIN, 60);
    expect(early).not.toBeNull();
    expect(later).not.toBeNull();
    expect(early?.fillColor).not.toBe(later?.fillColor);
  });
});
