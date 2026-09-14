import type { TowerSkin } from "@/ui/skins/TowerSkin";
import { hueForFloor } from "./levelHue";
import { rotateHueTo } from "@/utils/color";

export interface ResolvedBlockColor {
  fillColor: string;
  gradientTopColor: string;
}

/**
 * Per-floor hue-shifted colors for the DEFAULT skin only — a purchased
 * skin (Neon, Ice, Gold, ...) keeps its own fixed palette and this
 * returns null, so callers should fall back to the skin's flat colors.
 * Gameplay never reads this; it's purely cosmetic (docs/ARCHITECTURE.md §Skins).
 */
export function resolveBlockColors(skin: TowerSkin, floor: number): ResolvedBlockColor | null {
  if (skin.id !== "default") return null;
  const hue = hueForFloor(floor);
  return {
    fillColor: rotateHueTo(skin.blockFill, hue),
    gradientTopColor: rotateHueTo(skin.blockGradientTop ?? skin.blockFill, hue),
  };
}
