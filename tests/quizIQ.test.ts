import { describe, expect, it } from "vitest";
import { computeKnowledgeIQ, rankForKnowledgeIQ, type IQSignal } from "@/quiz/QuizIQ";

function signal(overrides: Partial<IQSignal> = {}): IQSignal {
  return { correct: true, responseTimeMs: 2000, timeLimitMs: 8000, difficulty: 3, ...overrides };
}

describe("computeKnowledgeIQ", () => {
  it("scores an empty run at 0", () => {
    expect(computeKnowledgeIQ([])).toBe(0);
  });

  it("scores an all-wrong run at 0", () => {
    const answers = [signal({ correct: false }), signal({ correct: false }), signal({ correct: false })];
    expect(computeKnowledgeIQ(answers)).toBe(0);
  });

  it("scores a perfect, instant, hardest-difficulty run near 100", () => {
    const answers = Array.from({ length: 10 }, () => signal({ correct: true, responseTimeMs: 0, difficulty: 5 }));
    expect(computeKnowledgeIQ(answers)).toBeGreaterThanOrEqual(95);
  });

  it("rewards accuracy — more correct answers scores higher, all else equal", () => {
    const lowAccuracy = [signal({ correct: true }), signal({ correct: false }), signal({ correct: false }), signal({ correct: false })];
    const highAccuracy = [signal({ correct: true }), signal({ correct: true }), signal({ correct: true }), signal({ correct: false })];
    expect(computeKnowledgeIQ(highAccuracy)).toBeGreaterThan(computeKnowledgeIQ(lowAccuracy));
  });

  it("rewards speed — a faster correct run scores higher, all else equal", () => {
    const slow = [signal({ responseTimeMs: 7500 }), signal({ responseTimeMs: 7500 })];
    const fast = [signal({ responseTimeMs: 200 }), signal({ responseTimeMs: 200 })];
    expect(computeKnowledgeIQ(fast)).toBeGreaterThan(computeKnowledgeIQ(slow));
  });

  it("stays within 0-100", () => {
    const answers = Array.from({ length: 5 }, () => signal({ correct: true, responseTimeMs: 0, difficulty: 5 }));
    const iq = computeKnowledgeIQ(answers);
    expect(iq).toBeGreaterThanOrEqual(0);
    expect(iq).toBeLessThanOrEqual(100);
  });

  it("is deterministic given the same answers", () => {
    const answers = [signal({ correct: true }), signal({ correct: false })];
    expect(computeKnowledgeIQ(answers)).toBe(computeKnowledgeIQ(answers));
  });
});

describe("rankForKnowledgeIQ", () => {
  it("maps score ranges to the expected rank", () => {
    expect(rankForKnowledgeIQ(0)).toBe("ROOKIE");
    expect(rankForKnowledgeIQ(40)).toBe("EXPLORER");
    expect(rankForKnowledgeIQ(60)).toBe("PRO");
    expect(rankForKnowledgeIQ(75)).toBe("EXPERT");
    expect(rankForKnowledgeIQ(90)).toBe("MASTER");
    expect(rankForKnowledgeIQ(100)).toBe("GENIUS");
  });
});
