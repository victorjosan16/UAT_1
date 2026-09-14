/**
 * Re-exports the exact same scoring/levels/RNG code the client bundles,
 * so anti-cheat re-validation in session.ts runs the identical formulas
 * instead of a hand-copied duplicate that could silently drift out of
 * sync. See docs/ARCHITECTURE.md — "Anti-cheat model".
 */
export { ScoreEngine, computeAccuracy, gradeForPlacement } from "../../../../src/scoring/ScoreEngine";
export { initialComboState, advanceCombo } from "../../../../src/scoring/ComboSystem";
export { LEVELS, MAX_LEVEL, getLevel } from "../../../../src/levels/levels";
export { DifficultyEngine } from "../../../../src/levels/DifficultyEngine";
export { buildLevelSequence } from "../../../../src/levels/levelSequence";
export { evaluateChallengeOutcome } from "../../../../src/modes/ChallengeMode";
export { dailySeed, utcDateKey } from "../../../../src/utils/dailySeed";
export { randomId } from "../../../../src/utils/rng";
export { GAME_VERSION, RULES_VERSION } from "../../../../src/branding";
export type { RunSummary, PlacementResult, GameMode } from "../../../../src/types";
