import { describe, expect, it } from "vitest";
import { resultMessage } from "@/quiz/resultMessages";
import type { QuizSummary } from "@/types";

function makeSummary(overrides: Partial<QuizSummary>): QuizSummary {
  return {
    mode: "QUICK",
    seed: "s",
    gameVersion: "1.0.0",
    rulesVersion: 1,
    score: 5000,
    correctCount: 7,
    totalQuestions: 10,
    bestStreak: 3,
    averageResponseMs: 3000,
    knowledgeIQ: 60,
    answers: [],
    durationMs: 30000,
    ...overrides,
  };
}

describe("resultMessage", () => {
  it("celebrates a perfect round distinctly", () => {
    expect(resultMessage(makeSummary({ correctCount: 10, totalQuestions: 10 }))).toBe("PERFECT KNOWLEDGE.");
  });

  it("calls out being one away from perfect", () => {
    expect(resultMessage(makeSummary({ correctCount: 9, totalQuestions: 10 }))).toBe("ONE AWAY FROM PERFECT.");
  });

  it("never claims a stat the run didn't actually have", () => {
    // A slow, low-accuracy run should never get "LIGHTNING FAST" or a streak callout it didn't earn.
    const summary = makeSummary({ correctCount: 2, totalQuestions: 10, averageResponseMs: 7500, bestStreak: 1 });
    const message = resultMessage(summary);
    expect(message).not.toContain("LIGHTNING FAST");
    expect(message).not.toContain("ON FIRE");
  });

  it("is a pure function of the summary", () => {
    const summary = makeSummary({});
    expect(resultMessage(summary)).toBe(resultMessage(summary));
  });
});
