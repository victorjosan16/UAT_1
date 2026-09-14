import { BASE_BLOCK_HEIGHT, MIN_BLOCK_WIDTH, PLAY_AREA_WIDTH, type LevelDefinition } from "./LevelDefinition";
import type { DirectionPattern, SpecialModifier } from "@/types";

export { BASE_BLOCK_HEIGHT, MIN_BLOCK_WIDTH, PLAY_AREA_WIDTH };

interface LevelSpec {
  level: number;
  requiredBlocks: number;
  startingBlockWidth: number;
  movementSpeed: number;
  speedIncrease: number;
  perfectTolerance: number;
  directionPattern: DirectionPattern;
  specialModifier: SpecialModifier;
}

/**
 * 20 handcrafted stages. Data only — the same `Game`/`ClassicMode` code
 * plays every level. See docs/GAME_DESIGN.md §6 for the design intent
 * behind each stage.
 */
const LEVEL_SPECS: readonly LevelSpec[] = [
  { level: 1, requiredBlocks: 8, startingBlockWidth: 220, movementSpeed: 70, speedIncrease: 0, perfectTolerance: 26, directionPattern: "CONSTANT", specialModifier: "NONE" },
  { level: 2, requiredBlocks: 8, startingBlockWidth: 210, movementSpeed: 85, speedIncrease: 1, perfectTolerance: 24, directionPattern: "CONSTANT", specialModifier: "NONE" },
  { level: 3, requiredBlocks: 9, startingBlockWidth: 200, movementSpeed: 100, speedIncrease: 1.5, perfectTolerance: 22, directionPattern: "CONSTANT", specialModifier: "NONE" },
  { level: 4, requiredBlocks: 9, startingBlockWidth: 190, movementSpeed: 110, speedIncrease: 2, perfectTolerance: 21, directionPattern: "ALTERNATING", specialModifier: "NONE" },
  { level: 5, requiredBlocks: 9, startingBlockWidth: 180, movementSpeed: 130, speedIncrease: 2.5, perfectTolerance: 20, directionPattern: "ALTERNATING", specialModifier: "NONE" },
  { level: 6, requiredBlocks: 10, startingBlockWidth: 170, movementSpeed: 145, speedIncrease: 3, perfectTolerance: 19, directionPattern: "ALTERNATING", specialModifier: "WIND" },
  { level: 7, requiredBlocks: 10, startingBlockWidth: 165, movementSpeed: 155, speedIncrease: 3, perfectTolerance: 18, directionPattern: "ALTERNATING", specialModifier: "SPEED_SHIFT" },
  { level: 8, requiredBlocks: 10, startingBlockWidth: 160, movementSpeed: 165, speedIncrease: 3.5, perfectTolerance: 17, directionPattern: "ALTERNATING", specialModifier: "DOUBLE_SPEED" },
  { level: 9, requiredBlocks: 10, startingBlockWidth: 155, movementSpeed: 175, speedIncrease: 3.5, perfectTolerance: 14, directionPattern: "ALTERNATING", specialModifier: "PRECISION" },
  { level: 10, requiredBlocks: 12, startingBlockWidth: 150, movementSpeed: 190, speedIncrease: 4, perfectTolerance: 15, directionPattern: "ALTERNATING", specialModifier: "NONE" },
  { level: 11, requiredBlocks: 11, startingBlockWidth: 145, movementSpeed: 210, speedIncrease: 4, perfectTolerance: 14, directionPattern: "ALTERNATING", specialModifier: "NONE" },
  { level: 12, requiredBlocks: 11, startingBlockWidth: 138, movementSpeed: 220, speedIncrease: 4.5, perfectTolerance: 13, directionPattern: "VARIABLE", specialModifier: "SMALL_START" },
  { level: 13, requiredBlocks: 11, startingBlockWidth: 132, movementSpeed: 230, speedIncrease: 4.5, perfectTolerance: 13, directionPattern: "VARIABLE", specialModifier: "SPEED_SHIFT" },
  { level: 14, requiredBlocks: 12, startingBlockWidth: 126, movementSpeed: 240, speedIncrease: 5, perfectTolerance: 12, directionPattern: "VARIABLE", specialModifier: "FOG" },
  { level: 15, requiredBlocks: 12, startingBlockWidth: 118, movementSpeed: 250, speedIncrease: 5, perfectTolerance: 10, directionPattern: "VARIABLE", specialModifier: "PRECISION" },
  { level: 16, requiredBlocks: 12, startingBlockWidth: 110, movementSpeed: 265, speedIncrease: 5.5, perfectTolerance: 10, directionPattern: "VARIABLE", specialModifier: "DOUBLE_SPEED" },
  { level: 17, requiredBlocks: 13, startingBlockWidth: 102, movementSpeed: 280, speedIncrease: 6, perfectTolerance: 9, directionPattern: "VARIABLE", specialModifier: "REVERSE" },
  { level: 18, requiredBlocks: 13, startingBlockWidth: 95, movementSpeed: 295, speedIncrease: 6, perfectTolerance: 7, directionPattern: "VARIABLE", specialModifier: "PRECISION" },
  { level: 19, requiredBlocks: 13, startingBlockWidth: 88, movementSpeed: 315, speedIncrease: 6.5, perfectTolerance: 6, directionPattern: "VARIABLE", specialModifier: "DOUBLE_SPEED" },
  { level: 20, requiredBlocks: 15, startingBlockWidth: 80, movementSpeed: 335, speedIncrease: 7, perfectTolerance: 6, directionPattern: "VARIABLE", specialModifier: "MOVING_BASE" },
];

export const LEVELS: readonly LevelDefinition[] = LEVEL_SPECS.map((spec) => ({
  ...spec,
  startingBlockWidth: Math.max(spec.startingBlockWidth, MIN_BLOCK_WIDTH),
  cameraSpeed: 260,
  scoreMultiplier: 1 + (spec.level - 1) * 0.05,
}));

export const MAX_LEVEL = LEVELS.length;

export function getLevel(level: number): LevelDefinition {
  const def = LEVELS[clampLevelIndex(level)];
  if (!def) throw new Error(`No level definition for level ${level}`);
  return def;
}

function clampLevelIndex(level: number): number {
  return Math.min(Math.max(level, 1), MAX_LEVEL) - 1;
}
