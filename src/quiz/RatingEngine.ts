/**
 * Knowledge Rating (KR) — a long-term, in-game competitive rating, never
 * described as a real-world intelligence measurement (see QuizIQ.ts's own
 * per-run "Q5 IQ", which is a *different* number: a 0-100 score for a
 * single run, not a persistent rating). KR persists across runs, moves
 * slowly, and maps onto a tier ladder players climb over weeks/months.
 *
 * Tier thresholds live here, in one place — never hardcoded into a screen.
 */

export type RatingTierName = "BRONZE" | "SILVER" | "GOLD" | "PLATINUM" | "DIAMOND" | "MASTER" | "GRANDMASTER";

export interface TierDefinition {
  name: RatingTierName;
  minRating: number;
  /** Master/Grandmaster are single-band — no I/II/III split. */
  hasDivisions: boolean;
}

/** Ascending by minRating — tierForRating relies on this order. */
export const TIER_CONFIG: readonly TierDefinition[] = [
  { name: "BRONZE", minRating: 0, hasDivisions: true },
  { name: "SILVER", minRating: 800, hasDivisions: true },
  { name: "GOLD", minRating: 1100, hasDivisions: true },
  { name: "PLATINUM", minRating: 1400, hasDivisions: true },
  { name: "DIAMOND", minRating: 1700, hasDivisions: true },
  { name: "MASTER", minRating: 2000, hasDivisions: false },
  { name: "GRANDMASTER", minRating: 2400, hasDivisions: false },
];

/** 3 = lowest division (III), 1 = highest (I) — matches the "GOLD III -> GOLD I" convention. */
export type Division = 1 | 2 | 3;

export interface RatingTierInfo {
  name: RatingTierName;
  division: Division | null;
  /** e.g. "GOLD III" or "GRANDMASTER". */
  label: string;
}

const DIVISION_ROMAN: Record<Division, string> = { 1: "I", 2: "II", 3: "III" };
/** Width of an undivided top tier's band, used only to size Grandmaster's (nonexistent) division math — never actually divided. */
const DEFAULT_BAND_WIDTH = 300;

export function tierForRating(rating: number): RatingTierInfo {
  let currentIndex = 0;
  for (let i = 0; i < TIER_CONFIG.length; i++) {
    const tier = TIER_CONFIG[i]!;
    if (rating >= tier.minRating) currentIndex = i;
    else break;
  }

  const current = TIER_CONFIG[currentIndex]!;
  if (!current.hasDivisions) {
    return { name: current.name, division: null, label: current.name };
  }

  const next = TIER_CONFIG[currentIndex + 1];
  const bandWidth = (next ? next.minRating - current.minRating : DEFAULT_BAND_WIDTH) / 3;
  const offset = Math.max(0, rating - current.minRating);
  const divisionIndex = Math.min(2, Math.floor(offset / bandWidth)); // 0, 1, 2
  const division = (3 - divisionIndex) as Division; // 0->III, 1->II, 2->I

  return { name: current.name, division, label: `${current.name} ${DIVISION_ROMAN[division]}` };
}

/**
 * Turns a completed Placement Quiz's 0-100 Q5 IQ (see QuizIQ.computeKnowledgeIQ,
 * already accuracy-weighted over speed) into a starting KR. Deliberately
 * linear and simple: IQ 0 -> 500 (deep Bronze), IQ 100 -> 1999 (top of
 * Diamond) — Master/Grandmaster are earned later through play, capped out
 * so a placement result alone can never hand them out on day one.
 */
export function initialRatingFromIQ(knowledgeIQ: number): number {
  const clamped = Math.min(100, Math.max(0, knowledgeIQ));
  return Math.min(1999, Math.round(500 + clamped * 15));
}

const RATING_NUDGE_FACTOR = 0.08;
const RATING_NUDGE_CAP = 40;

/**
 * The only rating movement wired up so far: after any solo/Daily 10 run,
 * KR drifts a small, capped step toward what that run's own performance
 * implies — consistency over many runs moves it, one great or terrible
 * run can't swing it. This is NOT the opponent-aware Elo-style model
 * ranked Arena results deserve (that needs match data — placements,
 * opponent ratings — this function doesn't have); Arena's own rating
 * adjustment is a deliberately separate, still-to-build follow-up so the
 * two never get tangled together.
 */
export function nudgeRating(currentRating: number, runKnowledgeIQ: number): number {
  const implied = initialRatingFromIQ(runKnowledgeIQ);
  const delta = Math.min(RATING_NUDGE_CAP, Math.max(-RATING_NUDGE_CAP, (implied - currentRating) * RATING_NUDGE_FACTOR));
  return Math.round(currentRating + delta);
}
