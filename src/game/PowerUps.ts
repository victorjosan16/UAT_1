/**
 * Occasional bonus carried by a moving block instead of its normal skin
 * hue — collected automatically by placing that block successfully (same
 * single-tap input as always, no new interaction to learn). Selection is
 * drawn from the level's own SeededRandom, so Daily/Challenge runs stay
 * fully reproducible.
 */
export type PowerUpType = "FREEZE" | "WIDE" | "MULTIPLIER";

export const POWERUP_TYPES: readonly PowerUpType[] = ["FREEZE", "WIDE", "MULTIPLIER"];

/** Chance per spawn that a block carries a power-up. */
export const POWERUP_SPAWN_CHANCE = 0.15;
/** No power-ups before this floor — let the player get a feel for the base game first. */
export const POWERUP_MIN_FLOOR = 3;

/** FREEZE: this block slides at this fraction of normal speed for its whole pass. */
export const POWERUP_FREEZE_SPEED_FACTOR = 0.4;
/** WIDE: this block spawns this much wider than it normally would. */
export const POWERUP_WIDE_FACTOR = 1.6;
/** WIDE: absolute cap as a fraction of the play area, so it can never span almost the whole width. */
export const POWERUP_WIDE_MAX_FRACTION = 0.55;
/** MULTIPLIER: this placement's score is multiplied by this on top of the level's own multiplier. */
export const POWERUP_SCORE_MULTIPLIER = 2;

export const POWERUP_LABEL: Record<PowerUpType, string> = {
  FREEZE: "SLOW-MO!",
  WIDE: "WIDE BLOCK!",
  MULTIPLIER: "×2 SCORE!",
};

/** Short glyph drawn centered on the carrying block — plain text, never emoji, so it renders identically on every canvas/font stack. */
export const POWERUP_GLYPH: Record<PowerUpType, string> = {
  FREEZE: "SLOW",
  WIDE: "WIDE",
  MULTIPLIER: "×2",
};

export const POWERUP_GLOW_COLOR: Record<PowerUpType, string> = {
  FREEZE: "#4fd1ff",
  WIDE: "#5fff8f",
  MULTIPLIER: "#ffd54f",
};
