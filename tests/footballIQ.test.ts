import { describe, expect, it } from "vitest";
import { computeFootballIQ, rankForFootballIQ } from "@/quiz/FootballIQ";
import type { AnswerResult } from "@/types";

function makeAnswer(overrides: Partial<AnswerResult>): AnswerResult {
  return {
    index: 0,
    clubId: "arsenal",
    selectedClubId: "arsenal",
    correct: true,
    timedOut: false,
    responseTimeMs: 2000,
    timeLimitMs: 8000,
    difficulty: 2,
    scoreGained: 500,
    streakAfter: 1,
    totalScore: 500,
    ...overrides,
  };
}

describe("computeFootballIQ", () => {
  it("is 0 for an empty run", () => {
    expect(computeFootballIQ([])).toBe(0);
  });

  it("scores a perfect, fast, hard, consistent run very highly", () => {
    const answers = Array.from({ length: 10 }, (_, i) =>
      makeAnswer({ index: i, correct: true, responseTimeMs: 500, timeLimitMs: 5000, difficulty: 5 }),
    );
    expect(computeFootballIQ(answers)).toBeGreaterThanOrEqual(85);
  });

  it("scores an all-wrong run at 0", () => {
    const answers = Array.from({ length: 10 }, (_, i) =>
      makeAnswer({ index: i, correct: false, timedOut: true, selectedClubId: null, responseTimeMs: 8000 }),
    );
    expect(computeFootballIQ(answers)).toBe(0);
  });

  it("never exceeds 100 or drops below 0", () => {
    const great = Array.from({ length: 10 }, (_, i) => makeAnswer({ index: i, responseTimeMs: 1, difficulty: 5 }));
    const terrible = Array.from({ length: 10 }, (_, i) => makeAnswer({ index: i, correct: false, timedOut: true, selectedClubId: null }));
    expect(computeFootballIQ(great)).toBeLessThanOrEqual(100);
    expect(computeFootballIQ(terrible)).toBeGreaterThanOrEqual(0);
  });

  it("rewards more correct answers, all else equal", () => {
    const fewCorrect = [
      makeAnswer({ index: 0, correct: true }),
      makeAnswer({ index: 1, correct: false, timedOut: true, selectedClubId: null }),
      makeAnswer({ index: 2, correct: false, timedOut: true, selectedClubId: null }),
    ];
    const moreCorrect = [
      makeAnswer({ index: 0, correct: true }),
      makeAnswer({ index: 1, correct: true }),
      makeAnswer({ index: 2, correct: false, timedOut: true, selectedClubId: null }),
    ];
    expect(computeFootballIQ(moreCorrect)).toBeGreaterThan(computeFootballIQ(fewCorrect));
  });

  it("is deterministic for the same answers", () => {
    const answers = [makeAnswer({ index: 0 }), makeAnswer({ index: 1, correct: false, timedOut: true, selectedClubId: null })];
    expect(computeFootballIQ(answers)).toBe(computeFootballIQ(answers));
  });
});

describe("rankForFootballIQ", () => {
  it("maps IQ ranges to the documented ranks", () => {
    expect(rankForFootballIQ(0)).toBe("ROOKIE");
    expect(rankForFootballIQ(29)).toBe("ROOKIE");
    expect(rankForFootballIQ(30)).toBe("FAN");
    expect(rankForFootballIQ(49)).toBe("FAN");
    expect(rankForFootballIQ(50)).toBe("PRO");
    expect(rankForFootballIQ(69)).toBe("PRO");
    expect(rankForFootballIQ(70)).toBe("EXPERT");
    expect(rankForFootballIQ(84)).toBe("EXPERT");
    expect(rankForFootballIQ(85)).toBe("MASTER");
    expect(rankForFootballIQ(94)).toBe("MASTER");
    expect(rankForFootballIQ(95)).toBe("LEGEND");
    expect(rankForFootballIQ(100)).toBe("LEGEND");
  });
});
