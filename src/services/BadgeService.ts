import { milestoneKey } from "./MilestoneService";
import type { StringKey } from "@/i18n/strings";

/**
 * Cosmetic/status achievements only (see MASTER PROMPT §52) — never a
 * currency, never purchasable. Tracked with the same client-trusted model
 * as everything else in this app (leaderboard scores, KR): a determined
 * player could fake local flags, but there is no server to re-derive them
 * against, same accepted tradeoff documented in firestore.rules.
 */
export type BadgeId =
  | "FIRST_QUIZ"
  | "PERFECT_10"
  | "ON_FIRE_10"
  | "STREAK_7"
  | "STREAK_30"
  | "FIRST_ARENA_WIN"
  | "ARENA_WINS_10"
  | "TOP_100"
  | "TOP_50"
  | "TOP_10"
  | "NUMBER_ONE"
  | "GEOGRAPHY_MASTER"
  | "FLAG_MASTER"
  | "SEASON_TOP_100"
  | "SEASON_TOP_10";

export interface BadgeDefinition {
  id: BadgeId;
  emoji: string;
  labelKey: StringKey;
}

export const BADGE_DEFINITIONS: readonly BadgeDefinition[] = [
  { id: "FIRST_QUIZ", emoji: "🎯", labelKey: "badge.firstQuiz" },
  { id: "PERFECT_10", emoji: "💯", labelKey: "badge.perfect10" },
  { id: "ON_FIRE_10", emoji: "🔥", labelKey: "badge.onFire10" },
  { id: "STREAK_7", emoji: "📅", labelKey: "badge.streak7" },
  { id: "STREAK_30", emoji: "🗓️", labelKey: "badge.streak30" },
  { id: "FIRST_ARENA_WIN", emoji: "🏆", labelKey: "badge.firstArenaWin" },
  { id: "ARENA_WINS_10", emoji: "👑", labelKey: "badge.arenaWins10" },
  { id: "TOP_100", emoji: "🥉", labelKey: "badge.top100" },
  { id: "TOP_50", emoji: "🥈", labelKey: "badge.top50" },
  { id: "TOP_10", emoji: "🥇", labelKey: "badge.top10" },
  { id: "NUMBER_ONE", emoji: "🌟", labelKey: "badge.numberOne" },
  { id: "GEOGRAPHY_MASTER", emoji: "🌍", labelKey: "badge.geographyMaster" },
  { id: "FLAG_MASTER", emoji: "🚩", labelKey: "badge.flagMaster" },
  { id: "SEASON_TOP_100", emoji: "🎖️", labelKey: "badge.seasonTop100" },
  { id: "SEASON_TOP_10", emoji: "🏅", labelKey: "badge.seasonTop10" },
];

/** One-time event flags this module marks directly (parallel to, and stored alongside, leaderboard milestone flags — see MilestoneService). */
export const BADGE_FLAG = {
  perfect10: "badge:perfect10",
  onFire10: "badge:onfire10",
  categoryMastered: (categoryId: string) => `badge:category:${categoryId}`,
} as const;

export interface BadgeCheckInput {
  totalQuizzesPlayed: number;
  dailyStreak: number;
  arenaWins: number;
  reachedFlags: ReadonlySet<string>;
}

/** Pure — derives every earned badge from already-tracked counters/flags. Never mutates storage itself. */
export function computeEarnedBadges(input: BadgeCheckInput): Set<BadgeId> {
  const earned = new Set<BadgeId>();

  if (input.totalQuizzesPlayed >= 1) earned.add("FIRST_QUIZ");
  if (input.reachedFlags.has(BADGE_FLAG.perfect10)) earned.add("PERFECT_10");
  if (input.reachedFlags.has(BADGE_FLAG.onFire10)) earned.add("ON_FIRE_10");
  if (input.dailyStreak >= 7) earned.add("STREAK_7");
  if (input.dailyStreak >= 30) earned.add("STREAK_30");
  if (input.arenaWins >= 1) earned.add("FIRST_ARENA_WIN");
  if (input.arenaWins >= 10) earned.add("ARENA_WINS_10");
  if (input.reachedFlags.has(milestoneKey("allTime", 100))) earned.add("TOP_100");
  if (input.reachedFlags.has(milestoneKey("allTime", 50))) earned.add("TOP_50");
  if (input.reachedFlags.has(milestoneKey("allTime", 10))) earned.add("TOP_10");
  if (input.reachedFlags.has(milestoneKey("allTime", 1))) earned.add("NUMBER_ONE");
  if (input.reachedFlags.has(BADGE_FLAG.categoryMastered("GEOGRAPHY"))) earned.add("GEOGRAPHY_MASTER");
  if (input.reachedFlags.has(BADGE_FLAG.categoryMastered("FLAGS"))) earned.add("FLAG_MASTER");
  if (input.reachedFlags.has(milestoneKey("monthly", 100))) earned.add("SEASON_TOP_100");
  if (input.reachedFlags.has(milestoneKey("monthly", 10))) earned.add("SEASON_TOP_10");

  return earned;
}
