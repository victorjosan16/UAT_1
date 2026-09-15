import { describe, expect, it } from "vitest";
import { QuizSession } from "@/quiz/QuizSession";
import type { QuizSummary } from "@/types";

const REVEAL_DURATION_MS = 1500; // matches QuizSession's internal constant with margin

describe("QuizSession", () => {
  it("builds the level's configured number of questions", () => {
    const session = new QuizSession("QUICK", "session-seed-1", 1);
    session.start();
    expect(session.snapshot().totalQuestions).toBe(10);
  });

  it("answering correctly scores points and moves to REVEAL", () => {
    const session = new QuizSession("QUICK", "session-seed-2", 1);
    session.start();
    const question = session.snapshot().currentQuestion!;
    const correctClubId = question.options[question.correctIndex]!.id;

    session.submitAnswer(correctClubId);
    const state = session.snapshot();

    expect(state.status).toBe("REVEAL");
    expect(state.lastAnswer?.correct).toBe(true);
    expect(state.score).toBeGreaterThan(0);
  });

  it("answering wrong scores nothing for that question and resets the streak", () => {
    const session = new QuizSession("QUICK", "session-seed-3", 1);
    session.start();
    const question = session.snapshot().currentQuestion!;
    const wrongClubId = question.options.find((_, i) => i !== question.correctIndex)!.id;

    session.submitAnswer(wrongClubId);
    const state = session.snapshot();

    expect(state.lastAnswer?.correct).toBe(false);
    expect(state.streak.current).toBe(0);
  });

  it("a second submitAnswer call while already in REVEAL is ignored", () => {
    const session = new QuizSession("QUICK", "session-seed-4", 1);
    session.start();
    const question = session.snapshot().currentQuestion!;
    session.submitAnswer(question.options[0]!.id);
    const afterFirst = session.snapshot();

    session.submitAnswer(question.options[1]!.id);
    const afterSecond = session.snapshot();

    expect(afterSecond.lastAnswer).toEqual(afterFirst.lastAnswer);
  });

  it("ticking the timer to zero without an answer auto-submits a timeout", () => {
    const session = new QuizSession("QUICK", "session-seed-5", 1);
    session.start();
    const timeLimitMs = session.snapshot().timeLimitMs;

    session.tick(timeLimitMs + 10);
    const state = session.snapshot();

    expect(state.status).toBe("REVEAL");
    expect(state.lastAnswer?.timedOut).toBe(true);
    expect(state.lastAnswer?.correct).toBe(false);
  });

  it("advances through every question and fires onComplete with a consistent summary", () => {
    let completedSummary: QuizSummary | null = null;
    const session = new QuizSession("QUICK", "session-seed-6", 1, {
      onComplete: (summary) => {
        completedSummary = summary;
      },
    });
    session.start();

    for (let i = 0; i < 10; i++) {
      const question = session.snapshot().currentQuestion;
      if (!question) break;
      const correctId = question.options[question.correctIndex]!.id;
      session.submitAnswer(correctId);
      session.tick(REVEAL_DURATION_MS);
    }

    expect(session.snapshot().status).toBe("COMPLETE");
    expect(completedSummary).not.toBeNull();
    const summary = completedSummary as unknown as QuizSummary;
    expect(summary.totalQuestions).toBe(10);
    expect(summary.correctCount).toBe(10);
    expect(summary.answers).toHaveLength(10);
    expect(summary.footballIQ).toBeGreaterThan(0);
  });

  it("is deterministic — the same seed and level produce the same question sequence", () => {
    const collectClubIds = (seed: string): string[] => {
      const ids: string[] = [];
      const session = new QuizSession("QUICK", seed, 1);
      session.start();
      for (let i = 0; i < 10; i++) {
        const question = session.snapshot().currentQuestion;
        if (!question) break;
        ids.push(question.club.id);
        session.submitAnswer(question.options[question.correctIndex]!.id);
        session.tick(REVEAL_DURATION_MS);
      }
      return ids;
    };

    expect(collectClubIds("determinism-seed")).toEqual(collectClubIds("determinism-seed"));
  });
});
