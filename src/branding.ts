/**
 * Single source of truth for game identity strings/colors.
 * Change the game name here — nothing else should hardcode it.
 */
export const GAME_NAME = "TINY TOWER";
export const GAME_SUBTITLE = "BUILD HIGHER. BEAT EVERYONE.";
export const GAME_VERSION = "1.0.0";

/** Bump when scoring/level/difficulty rules change in a way that makes old
 * seeds/scores non-comparable. Stored with every submitted score. */
export const RULES_VERSION = 1;

export const THEME_COLOR = "#0a0e17";
export const BACKGROUND_COLOR = "#05070c";
export const ACCENT_COLOR = "#4fd1ff";

export const TAGLINES = {
  tap: "TAP TO PLACE",
  nice: "NICE!",
  soClose: "SO CLOSE!",
  perfect: "PERFECT!",
  newRecord: "NEW RECORD",
} as const;
