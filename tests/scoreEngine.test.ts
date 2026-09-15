import { describe, expect, it } from "vitest";
import { BASE_SCORE, ScoreEngine, difficultyBonusFor, scoreAnswer, speedFractionFor, streakBonusFor } from "@/quiz/ScoreEngine";

describe("speedFractionFor", () => {
  it("is 1 for an instant answer", () => {
    expect(speedFractionFor(0, 8000)).toBe(1);
  });

  it("is 0 for an answer that used the whole time limit", () => {
    expect(speedFractionFor(8000, 8000)).toBe(0);
  });

  it("clamps to 0 rather than going negative when responseTime exceeds the limit", () => {
    expect(speedFractionFor(9000, 8000)).toBe(0);
  });

  it("scales linearly in between", () => {
    expect(speedFractionFor(4000, 8000)).toBeCloseTo(0.5, 5);
  });
});

describe("difficultyBonusFor", () => {
  it("is 0 for the easiest tier", () => {
    expect(difficultyBonusFor(1)).toBe(0);
  });

  it("is the max bonus for the hardest tier", () => {
    expect(difficultyBonusFor(5)).toBe(300);
  });
});

describe("streakBonusFor", () => {
  it("grows with streak count", () => {
    expect(streakBonusFor(1)).toBeLessThan(streakBonusFor(5));
  });

  it("caps out beyond the cap count", () => {
    expect(streakBonusFor(10)).toBe(streakBonusFor(20));
  });
});

describe("scoreAnswer", () => {
  it("awards nothing for a wrong or timed-out answer", () => {
    const result = scoreAnswer({ correct: false, responseTimeMs: 1000, timeLimitMs: 8000, difficulty: 3, streakAfter: 0 });
    expect(result.gained).toBe(0);
  });

  it("awards at least the base score for any correct answer", () => {
    const result = scoreAnswer({ correct: true, responseTimeMs: 8000, timeLimitMs: 8000, difficulty: 1, streakAfter: 1 });
    expect(result.gained).toBeGreaterThanOrEqual(BASE_SCORE);
  });

  it("rewards a faster correct answer with a higher score, all else equal", () => {
    const slow = scoreAnswer({ correct: true, responseTimeMs: 7000, timeLimitMs: 8000, difficulty: 2, streakAfter: 1 });
    const fast = scoreAnswer({ correct: true, responseTimeMs: 500, timeLimitMs: 8000, difficulty: 2, streakAfter: 1 });
    expect(fast.gained).toBeGreaterThan(slow.gained);
  });

  it("rewards a harder question with a higher score, all else equal", () => {
    const easy = scoreAnswer({ correct: true, responseTimeMs: 2000, timeLimitMs: 8000, difficulty: 1, streakAfter: 1 });
    const hard = scoreAnswer({ correct: true, responseTimeMs: 2000, timeLimitMs: 8000, difficulty: 5, streakAfter: 1 });
    expect(hard.gained).toBeGreaterThan(easy.gained);
  });

  it("rewards a longer streak with a higher score, all else equal", () => {
    const lowStreak = scoreAnswer({ correct: true, responseTimeMs: 2000, timeLimitMs: 8000, difficulty: 2, streakAfter: 1 });
    const highStreak = scoreAnswer({ correct: true, responseTimeMs: 2000, timeLimitMs: 8000, difficulty: 2, streakAfter: 8 });
    expect(highStreak.gained).toBeGreaterThan(lowStreak.gained);
  });

  it("is deterministic — same input always yields the same output", () => {
    const input = { correct: true, responseTimeMs: 1234, timeLimitMs: 8000, difficulty: 3 as const, streakAfter: 4 };
    expect(scoreAnswer(input)).toEqual(scoreAnswer(input));
  });
});

describe("ScoreEngine", () => {
  it("accumulates total score across answers", () => {
    const engine = new ScoreEngine();
    const r1 = engine.score({ correct: true, responseTimeMs: 1000, timeLimitMs: 8000, difficulty: 2, streakAfter: 1 });
    const r2 = engine.score({ correct: true, responseTimeMs: 2000, timeLimitMs: 8000, difficulty: 2, streakAfter: 2 });
    expect(engine.totalScore).toBe(r1.gained + r2.gained);
  });

  it("tracks correct count separately from answered count", () => {
    const engine = new ScoreEngine();
    engine.score({ correct: true, responseTimeMs: 1000, timeLimitMs: 8000, difficulty: 1, streakAfter: 1 });
    engine.score({ correct: false, responseTimeMs: 8000, timeLimitMs: 8000, difficulty: 1, streakAfter: 0 });
    expect(engine.answeredCount).toBe(2);
    expect(engine.correctAnswerCount).toBe(1);
  });

  it("computes average response time over correct answers only", () => {
    const engine = new ScoreEngine();
    engine.score({ correct: true, responseTimeMs: 1000, timeLimitMs: 8000, difficulty: 1, streakAfter: 1 });
    engine.score({ correct: true, responseTimeMs: 3000, timeLimitMs: 8000, difficulty: 1, streakAfter: 2 });
    engine.score({ correct: false, responseTimeMs: 8000, timeLimitMs: 8000, difficulty: 1, streakAfter: 0 });
    expect(engine.averageResponseMs).toBe(2000);
  });
});
