/**
 * Single source of truth for game identity strings/colors.
 * Change the game name here — nothing else should hardcode it.
 */
export const GAME_NAME = "Q5 ARENA";
export const GAME_SUBTITLE = "HOW SHARP IS YOUR KNOWLEDGE?";
export const GAME_VERSION = "1.0.0";

/** Bump when scoring/level/difficulty rules change in a way that makes old
 * seeds/scores non-comparable. Stored with every submitted score. */
export const RULES_VERSION = 1;

export const THEME_COLOR = "#0f6e5c";
export const BACKGROUND_COLOR = "#faf6ef";
export const ACCENT_COLOR = "#0f6e5c";

export const TAGLINES = {
  tap: "PLAY NOW",
  correct: "CORRECT!",
  wrong: "WRONG",
  timesUp: "TIME'S UP!",
  perfect: "PERFECT GAME",
  newRecord: "NEW RECORD",
} as const;
