import { describe, expect, it } from "vitest";
import { distanceToNextMilestone, type MilestoneCutoffs } from "@/services/LeaderboardService";

const FULL_CUTOFFS: MilestoneCutoffs = { top10: 9000, top50: 5000, top100: 2000 };
const NO_CUTOFFS: MilestoneCutoffs = { top10: null, top50: null, top100: null };

describe("distanceToNextMilestone", () => {
  it("targets Top 100 when that's the nearest unreached milestone", () => {
    expect(distanceToNextMilestone(142, FULL_CUTOFFS)).toEqual({ milestoneRank: 100, placesAway: 42 });
  });

  it("targets Top 50 once already inside Top 100", () => {
    expect(distanceToNextMilestone(62, FULL_CUTOFFS)).toEqual({ milestoneRank: 50, placesAway: 12 });
  });

  it("targets Top 10 once already inside Top 50", () => {
    expect(distanceToNextMilestone(13, FULL_CUTOFFS)).toEqual({ milestoneRank: 10, placesAway: 3 });
  });

  it("returns null once already inside Top 10 — nothing further to chase", () => {
    expect(distanceToNextMilestone(5, FULL_CUTOFFS)).toBeNull();
  });

  it("never fabricates a milestone the leaderboard doesn't have enough real players to back", () => {
    expect(distanceToNextMilestone(50000, NO_CUTOFFS)).toBeNull();
  });

  it("skips a milestone whose cutoff isn't backed by enough real players yet, even if rank qualifies", () => {
    const partial: MilestoneCutoffs = { top10: 9000, top50: null, top100: null };
    // Rank 500 would normally target Top 100, but there aren't 100 real entries yet — only Top 10 is real.
    expect(distanceToNextMilestone(500, partial)).toEqual({ milestoneRank: 10, placesAway: 490 });
  });
});
