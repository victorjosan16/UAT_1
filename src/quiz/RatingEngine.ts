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

/** A legacy player (from before Knowledge Rating existed) never played a Placement Quiz — this is a fair, non-punishing mid-ladder default, not a guess at their real skill. */
export const LEGACY_DEFAULT_RATING = 1000;

const ARENA_K_FACTOR = 24;
const ARENA_MAX_SWING = 60;

export interface ArenaMatchResult {
  playerId: string;
  ratingBefore: number;
  /** 1 = winner. Ties share the same (better) placement — see placementsFromScores. */
  placement: number;
}

export interface ArenaRatingChange {
  playerId: string;
  ratingBefore: number;
  ratingAfter: number;
  delta: number;
}

/**
 * Opponent-aware, Elo-style rating for a 2-5 player Arena match (see
 * MASTER PROMPT §32). Each player is scored against every opponent
 * pairwise: 1 point for beating them, 0.5 for a tie, 0 for losing,
 * against the standard Elo expected score for that rating gap — an
 * upset (beating a much higher-rated field) swings rating more than
 * beating equally- or lower-rated opponents. Averaged over the
 * opponent count so the swing's *scale* stays the same regardless of
 * how many players were in the match, then capped so no single match
 * can dominate a rating built over many games.
 */
export function computeArenaRatingChanges(results: readonly ArenaMatchResult[]): ArenaRatingChange[] {
  const opponentCount = results.length - 1;
  if (opponentCount < 1) {
    return results.map((r) => ({ playerId: r.playerId, ratingBefore: r.ratingBefore, ratingAfter: r.ratingBefore, delta: 0 }));
  }

  return results.map((player) => {
    let actualSum = 0;
    let expectedSum = 0;

    for (const opponent of results) {
      if (opponent.playerId === player.playerId) continue;
      const actual = player.placement < opponent.placement ? 1 : player.placement > opponent.placement ? 0 : 0.5;
      const expected = 1 / (1 + 10 ** ((opponent.ratingBefore - player.ratingBefore) / 400));
      actualSum += actual;
      expectedSum += expected;
    }

    const rawDelta = (ARENA_K_FACTOR * (actualSum - expectedSum)) / opponentCount;
    const delta = Math.max(-ARENA_MAX_SWING, Math.min(ARENA_MAX_SWING, Math.round(rawDelta)));
    return { playerId: player.playerId, ratingBefore: player.ratingBefore, ratingAfter: player.ratingBefore + delta, delta };
  });
}

/**
 * Competition ranking ("1224"): tied scores share the same, better
 * placement, and the next distinct score skips ahead accordingly (two
 * players tied for 2nd means the next player is 4th, not 3rd) — standard
 * for any ranked competition and what computeArenaRatingChanges' tie
 * handling assumes.
 */
export function placementsFromScores(players: readonly { playerId: string; score: number }[]): Map<string, number> {
  const sorted = [...players].sort((a, b) => b.score - a.score);
  const placements = new Map<string, number>();
  sorted.forEach((player, i) => {
    const previous = sorted[i - 1];
    const placement = previous && previous.score === player.score ? placements.get(previous.playerId)! : i + 1;
    placements.set(player.playerId, placement);
  });
  return placements;
}
