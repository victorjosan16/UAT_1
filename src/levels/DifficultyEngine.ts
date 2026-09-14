import { clamp } from "@/utils/math";
import { LEVELS, MIN_BLOCK_WIDTH } from "./levels";
import type { LevelDefinition } from "./LevelDefinition";
import type { SpecialModifier } from "@/types";
import { SeededRandom } from "@/utils/rng";

const MODIFIER_POOL: readonly SpecialModifier[] = [
  "WIND",
  "SPEED_SHIFT",
  "PRECISION",
  "REVERSE",
  "DOUBLE_SPEED",
  "FOG",
  "MOVING_BASE",
];

const lastClassicLevel: LevelDefinition = (() => {
  const level = LEVELS[LEVELS.length - 1];
  if (!level) throw new Error("LEVELS must not be empty");
  return level;
})();

/**
 * Procedural difficulty for Endless Mode (floors beyond the 20 handcrafted
 * levels). Every parameter is a smooth, capped function of floor number —
 * never a random unfair spike — so the game stays hard but always fair.
 */
export class DifficultyEngine {
  private readonly rng: SeededRandom;

  constructor(seed: string) {
    this.rng = new SeededRandom(`${seed}:endless`);
  }

  definitionForFloor(floorBeyondClassic: number): LevelDefinition {
    const t = floorBeyondClassic; // 1, 2, 3, ...

    const movementSpeed = clamp(lastClassicLevel.movementSpeed + t * 3.2, lastClassicLevel.movementSpeed, 620);
    const perfectTolerance = clamp(lastClassicLevel.perfectTolerance - t * 0.05, 4, lastClassicLevel.perfectTolerance);
    const startingBlockWidth = clamp(lastClassicLevel.startingBlockWidth - t * 0.35, MIN_BLOCK_WIDTH, lastClassicLevel.startingBlockWidth);
    const speedIncrease = clamp(lastClassicLevel.speedIncrease + t * 0.05, lastClassicLevel.speedIncrease, 14);

    const modifierEvery = 3; // one modifier-flavored floor every N floors, deterministic by seed
    const specialModifier: SpecialModifier = t % modifierEvery === 0 ? this.rng.pick(MODIFIER_POOL) : "NONE";

    return {
      level: 20 + t,
      requiredBlocks: 1,
      startingBlockWidth,
      movementSpeed,
      speedIncrease,
      perfectTolerance,
      directionPattern: "VARIABLE",
      cameraSpeed: 260,
      specialModifier,
      scoreMultiplier: clamp(lastClassicLevel.scoreMultiplier + t * 0.02, lastClassicLevel.scoreMultiplier, 6),
    };
  }
}
