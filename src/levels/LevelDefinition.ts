import type { DirectionPattern, SpecialModifier } from "@/types";

export interface LevelDefinition {
  level: number;
  /** Number of successful placements needed to clear this level. */
  requiredBlocks: number;
  startingBlockWidth: number;
  /** World units / second. */
  movementSpeed: number;
  /** Additive speed increase applied per placement within the level. */
  speedIncrease: number;
  /** Max horizontal offset (world units) from perfect center still counted PERFECT. */
  perfectTolerance: number;
  directionPattern: DirectionPattern;
  cameraSpeed: number;
  specialModifier: SpecialModifier;
  scoreMultiplier: number;
}

export const BASE_BLOCK_HEIGHT = 34;
export const PLAY_AREA_WIDTH = 340;

/** A cap so width never degenerates into an unplayable sliver. */
export const MIN_BLOCK_WIDTH = 26;
