import { describe, expect, it } from "vitest";
import { BADGE_FLAG, computeEarnedBadges } from "@/services/BadgeService";
import { milestoneKey } from "@/services/MilestoneService";

function input(overrides: Partial<Parameters<typeof computeEarnedBadges>[0]> = {}) {
  return { totalQuizzesPlayed: 0, dailyStreak: 0, arenaWins: 0, reachedFlags: new Set<string>(), ...overrides };
}

describe("computeEarnedBadges", () => {
  it("earns nothing for a completely fresh player", () => {
    expect(computeEarnedBadges(input())).toEqual(new Set());
  });

  it("FIRST_QUIZ unlocks after one completed run", () => {
    expect(computeEarnedBadges(input({ totalQuizzesPlayed: 1 })).has("FIRST_QUIZ")).toBe(true);
  });

  it("PERFECT_10 and ON_FIRE_10 unlock from their one-time flags", () => {
    const earned = computeEarnedBadges(input({ reachedFlags: new Set([BADGE_FLAG.perfect10, BADGE_FLAG.onFire10]) }));
    expect(earned.has("PERFECT_10")).toBe(true);
    expect(earned.has("ON_FIRE_10")).toBe(true);
  });

  it("STREAK_7 and STREAK_30 unlock at their exact daily-streak thresholds", () => {
    expect(computeEarnedBadges(input({ dailyStreak: 6 })).has("STREAK_7")).toBe(false);
    expect(computeEarnedBadges(input({ dailyStreak: 7 })).has("STREAK_7")).toBe(true);
    expect(computeEarnedBadges(input({ dailyStreak: 29 })).has("STREAK_30")).toBe(false);
    expect(computeEarnedBadges(input({ dailyStreak: 30 })).has("STREAK_30")).toBe(true);
  });

  it("FIRST_ARENA_WIN and ARENA_WINS_10 unlock at their win-count thresholds", () => {
    expect(computeEarnedBadges(input({ arenaWins: 1 })).has("FIRST_ARENA_WIN")).toBe(true);
    expect(computeEarnedBadges(input({ arenaWins: 9 })).has("ARENA_WINS_10")).toBe(false);
    expect(computeEarnedBadges(input({ arenaWins: 10 })).has("ARENA_WINS_10")).toBe(true);
  });

  it("leaderboard rank badges reuse the same flags MilestoneService already tracks", () => {
    const earned = computeEarnedBadges(input({ reachedFlags: new Set([milestoneKey("allTime", 100), milestoneKey("allTime", 1)]) }));
    expect(earned.has("TOP_100")).toBe(true);
    expect(earned.has("NUMBER_ONE")).toBe(true);
    expect(earned.has("TOP_50")).toBe(false);
    expect(earned.has("TOP_10")).toBe(false);
  });

  it("season rank badges use the monthly scope's milestone flags, independent of allTime", () => {
    const earned = computeEarnedBadges(input({ reachedFlags: new Set([milestoneKey("monthly", 10)]) }));
    expect(earned.has("SEASON_TOP_10")).toBe(true);
    expect(earned.has("TOP_10")).toBe(false);
  });

  it("category mastery badges only unlock for their own category", () => {
    const earned = computeEarnedBadges(input({ reachedFlags: new Set([BADGE_FLAG.categoryMastered("GEOGRAPHY")]) }));
    expect(earned.has("GEOGRAPHY_MASTER")).toBe(true);
    expect(earned.has("FLAG_MASTER")).toBe(false);
  });
});
