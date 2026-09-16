import { LocalStorageService } from "@/storage/LocalStorage";

/**
 * One-time leaderboard milestones (see MASTER PROMPT §17/18) — Top 100,
 * Top 50, Top 10, #1. Each fires its celebration at most once per scope,
 * ever, via LocalStorageService's markMilestoneReached; a player who later
 * drops out of a tier they already claimed never gets it dangled again.
 */
export type MilestoneRank = 100 | 50 | 10 | 1;

/** Ascending exclusivity — evaluateMilestones relies on this order to pick the single best (most exclusive) newly-crossed one. */
const THRESHOLDS: readonly MilestoneRank[] = [100, 50, 10, 1];

export function milestoneKey(scope: string, threshold: MilestoneRank): string {
  return `milestone:${scope}:top${threshold}`;
}

export interface MilestoneEvaluation {
  /** Every threshold key newly reached at this rank — normally just one, but a huge single-run jump (e.g. straight to #1) can cross several at once. */
  toMark: string[];
  /** The single most exclusive newly-crossed threshold to actually celebrate, or null if nothing new. */
  celebrate: MilestoneRank | null;
}

/** Pure — never touches storage. See MilestoneService.checkAndMark for the side-effecting wrapper this app actually calls. */
export function evaluateMilestones(scope: string, rank: number, alreadyReached: ReadonlySet<string>): MilestoneEvaluation {
  const toMark: string[] = [];
  let celebrate: MilestoneRank | null = null;

  for (const threshold of THRESHOLDS) {
    if (rank > threshold) continue;
    const key = milestoneKey(scope, threshold);
    if (alreadyReached.has(key)) continue;
    toMark.push(key);
    celebrate = threshold; // THRESHOLDS is ascending exclusivity, so the last one set is the most exclusive.
  }

  return { toMark, celebrate };
}

export const MilestoneService = {
  /** Call once per leaderboard-scope rank update (e.g. right after a score submission resolves a fresh rank). Returns the milestone to celebrate, if any. */
  checkAndMark(scope: string, rank: number): MilestoneRank | null {
    const alreadyReached = new Set(LocalStorageService.getMilestonesReached());
    const { toMark, celebrate } = evaluateMilestones(scope, rank, alreadyReached);
    for (const key of toMark) LocalStorageService.markMilestoneReached(key);
    return celebrate;
  },
};
