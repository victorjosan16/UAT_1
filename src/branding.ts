/**
 * Single source of truth for game identity strings/colors.
 * Change the game name here — nothing else should hardcode it.
 */
export const GAME_NAME = "FOOTBALL LOGO CHALLENGE";
export const GAME_SUBTITLE = "HOW WELL DO YOU KNOW FOOTBALL?";
export const GAME_VERSION = "1.0.0";

/** Bump when scoring/level/difficulty rules change in a way that makes old
 * seeds/scores non-comparable. Stored with every submitted score. */
export const RULES_VERSION = 1;

export const THEME_COLOR = "#0a0e17";
export const BACKGROUND_COLOR = "#05070c";
export const ACCENT_COLOR = "#4fd1ff";

export const TAGLINES = {
  tap: "TAP TO PLAY",
  correct: "CORRECT!",
  wrong: "WRONG",
  timesUp: "TIME'S UP!",
  perfect: "PERFECT GAME",
  newRecord: "NEW RECORD",
} as const;
