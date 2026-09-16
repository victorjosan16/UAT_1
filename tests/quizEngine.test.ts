import { describe, expect, it } from "vitest";
import { QuizEngine } from "@/quiz/QuizEngine";
import type { QuizSummary } from "@/types";

const REVEAL_DURATION_MS = 1500;

function playToCompletion(engine: QuizEngine): void {
  for (let i = 0; i < 50; i++) {
    const snapshot = engine.snapshot();
    if (snapshot.status === "COMPLETE") return;
    const question = snapshot.currentQuestion;
    if (!question) return;
    engine.submitAnswer(question.options[question.correctIndex]!);
    engine.tick(REVEAL_DURATION_MS);
  }
}

describe("QuizEngine — QUICK mode", () => {
  it("builds a standard 10-question run", () => {
    const engine = new QuizEngine("QUICK", "quick-seed-1", {});
    engine.start();
    expect(engine.snapshot().totalQuestions).toBe(10);
  });

  it("answering correctly scores points and moves to REVEAL", () => {
    const engine = new QuizEngine("QUICK", "quick-seed-2", {});
    engine.start();
    const question = engine.snapshot().currentQuestion!;
    engine.submitAnswer(question.options[question.correctIndex]!);
    const state = engine.snapshot();
    expect(state.status).toBe("REVEAL");
    expect(state.lastAnswer?.correct).toBe(true);
    expect(state.score).toBeGreaterThan(0);
  });

  it("ticking the timer to zero without an answer auto-submits a timeout", () => {
    const engine = new QuizEngine("QUICK", "quick-seed-3", {});
    engine.start();
    const timeLimitMs = engine.snapshot().timeLimitMs;
    engine.tick(timeLimitMs + 10);
    const state = engine.snapshot();
    expect(state.status).toBe("REVEAL");
    expect(state.lastAnswer?.timedOut).toBe(true);
  });

  it("fires onComplete with a consistent summary after every question", () => {
    let completed: QuizSummary | null = null;
    const engine = new QuizEngine("QUICK", "quick-seed-4", {}, { onComplete: (summary) => (completed = summary) });
    engine.start();
    playToCompletion(engine);
    expect(engine.snapshot().status).toBe("COMPLETE");
    expect(completed).not.toBeNull();
    const summary = completed as unknown as QuizSummary;
    expect(summary.totalQuestions).toBe(10);
    expect(summary.correctCount).toBe(10);
    expect(summary.answers).toHaveLength(10);
  });

  it("is deterministic — the same seed produces the same question sequence", () => {
    const collectIds = (seed: string): string[] => {
      const engine = new QuizEngine("QUICK", seed, {});
      engine.start();
      const ids: string[] = [];
      for (let i = 0; i < 10; i++) {
        const question = engine.snapshot().currentQuestion;
        if (!question) break;
        ids.push(question.source.id);
        engine.submitAnswer(question.options[question.correctIndex]!);
        engine.tick(REVEAL_DURATION_MS);
      }
      return ids;
    };
    expect(collectIds("determinism-seed")).toEqual(collectIds("determinism-seed"));
  });
});

describe("QuizEngine — CATEGORY mode", () => {
  it("only draws questions from the requested category", () => {
    const engine = new QuizEngine("CATEGORY", "category-seed-1", { categoryId: "SPACE" });
    engine.start();
    for (let i = 0; i < 10; i++) {
      const question = engine.snapshot().currentQuestion;
      if (!question) break;
      expect(question.source.categoryId).toBe("SPACE");
      engine.submitAnswer(question.options[question.correctIndex]!);
      engine.tick(REVEAL_DURATION_MS);
    }
  });

  it("throws if no categoryId is given", () => {
    expect(() => new QuizEngine("CATEGORY", "category-seed-2", {})).toThrow();
  });
});

describe("QuizEngine — LEVEL mode", () => {
  it("uses the level curve's question count and time limit", () => {
    const engine = new QuizEngine("LEVEL", "level-seed-1", { level: 1 });
    engine.start();
    const state = engine.snapshot();
    expect(state.totalQuestions).toBe(10);
    expect(state.timeLimitMs).toBeGreaterThan(0);
  });

  it("continues past level 20 via the Endless difficulty curve", () => {
    const engine = new QuizEngine("ENDLESS", "endless-seed-1", { level: 25 });
    engine.start();
    expect(engine.snapshot().level).toBe(25);
    expect(engine.snapshot().totalQuestions).toBe(10);
  });
});

describe("QuizEngine — DAILY mode", () => {
  it("is fully deterministic given the same seed, so every player gets the same run", () => {
    const collectIds = (seed: string): string[] => {
      const engine = new QuizEngine("DAILY", seed, {});
      engine.start();
      const ids: string[] = [];
      for (let i = 0; i < 10; i++) {
        const question = engine.snapshot().currentQuestion;
        if (!question) break;
        ids.push(question.source.id);
        engine.submitAnswer(question.options[question.correctIndex]!);
        engine.tick(REVEAL_DURATION_MS);
      }
      return ids;
    };
    expect(collectIds("daily:2026-01-01:1.0.0")).toEqual(collectIds("daily:2026-01-01:1.0.0"));
  });
});
