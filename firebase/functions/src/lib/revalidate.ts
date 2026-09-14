import { ScoreEngine, buildLevelSequence, MAX_LEVEL, type RunSummary } from "./gameRules";

export interface RevalidationResult {
  valid: boolean;
  reason?: string;
}

const MIN_AVG_MS_PER_PLACEMENT = 25;

/**
 * Re-derives a submitted run's score/combo/perfect-count from its raw
 * placement trace using the exact same `ScoreEngine`/level logic the
 * client runs, and rejects anything that doesn't match. This is the
 * whole anti-cheat model in one function — see docs/SECURITY.md.
 */
export function revalidateRun(summary: RunSummary): RevalidationResult {
  const { placements } = summary;

  if (placements.length !== summary.height) return { valid: false, reason: "HEIGHT_MISMATCH" };
  if (placements.length === 0) return { valid: false, reason: "EMPTY_RUN" };

  const startLevel = summary.mode === "ENDLESS" ? MAX_LEVEL + 1 : 1;
  const levelSequence = buildLevelSequence(summary.seed, placements.length, startLevel);

  const engine = new ScoreEngine();
  let perfectCount = 0;

  for (let i = 0; i < placements.length; i++) {
    const p = placements[i];
    const levelDef = levelSequence[i];
    if (!p || !levelDef) return { valid: false, reason: "TRACE_CORRUPT" };
    if (p.index !== i) return { valid: false, reason: `INDEX_MISMATCH_AT_${i}` };
    if (p.floor !== i + 1) return { valid: false, reason: `FLOOR_MISMATCH_AT_${i}` };
    if (p.overlapWidth <= 0 || p.overlapWidth > p.blockWidthBefore + 0.01) return { valid: false, reason: `IMPLAUSIBLE_OVERLAP_AT_${i}` };

    const result = engine.place(p.overlapWidth, p.blockWidthBefore, p.floor, p.isPerfect, levelDef.scoreMultiplier);
    if (p.isPerfect) perfectCount += 1;

    // Deterministic integer math end-to-end — a tiny epsilon only absorbs float noise, not real drift.
    if (Math.abs(result.gained - p.scoreGained) > 1) return { valid: false, reason: `SCORE_MISMATCH_AT_${i}` };
    if (Math.abs(engine.totalScore - p.totalScore) > 1) return { valid: false, reason: `RUNNING_TOTAL_MISMATCH_AT_${i}` };
  }

  if (Math.abs(engine.totalScore - summary.score) > 1) return { valid: false, reason: "FINAL_SCORE_MISMATCH" };
  if (perfectCount !== summary.perfectCount) return { valid: false, reason: "PERFECT_COUNT_MISMATCH" };
  if (engine.comboState.bestCombo !== summary.bestCombo) return { valid: false, reason: "BEST_COMBO_MISMATCH" };
  if (engine.comboState.bestPerfectStreak !== summary.bestPerfectStreak) return { valid: false, reason: "BEST_PERFECT_STREAK_MISMATCH" };

  const lastTimestamp = placements[placements.length - 1]?.timestampMs ?? 0;
  const avgMsPerPlacement = lastTimestamp / placements.length;
  if (placements.length > 5 && avgMsPerPlacement < MIN_AVG_MS_PER_PLACEMENT) {
    return { valid: false, reason: "TIMING_IMPLAUSIBLE" };
  }

  return { valid: true };
}
