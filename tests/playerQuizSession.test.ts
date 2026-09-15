import { describe, expect, it } from "vitest";
import { PlayerQuizSession } from "@/quiz/PlayerQuizSession";
import type { PlayerQuizSummary } from "@/types";

const REVEAL_DURATION_MS = 1500;

describe("PlayerQuizSession", () => {
  it("builds the level's configured number of questions", () => {
    const session = new PlayerQuizSession("QUICK", "player-session-1", 1);
    session.start();
    expect(session.snapshot().totalQuestions).toBe(10);
  });

  it("answering correctly scores points and moves to REVEAL", () => {
    const session = new PlayerQuizSession("QUICK", "player-session-2", 1);
    session.start();
    const question = session.snapshot().currentQuestion!;
    const correctId = question.options[question.correctIndex]!.id;

    session.submitAnswer(correctId);
    const state = session.snapshot();

    expect(state.status).toBe("REVEAL");
    expect(state.lastAnswer?.correct).toBe(true);
    expect(state.score).toBeGreaterThan(0);
  });

  it("ticking the timer to zero without an answer auto-submits a timeout", () => {
    const session = new PlayerQuizSession("QUICK", "player-session-3", 1);
    session.start();
    const timeLimitMs = session.snapshot().timeLimitMs;

    session.tick(timeLimitMs + 10);
    const state = session.snapshot();

    expect(state.status).toBe("REVEAL");
    expect(state.lastAnswer?.timedOut).toBe(true);
  });

  it("advances through every question and fires onComplete with a consistent summary", () => {
    let completedSummary: PlayerQuizSummary | null = null;
    const session = new PlayerQuizSession("QUICK", "player-session-4", 1, {
      onComplete: (summary) => {
        completedSummary = summary;
      },
    });
    session.start();

    for (let i = 0; i < 10; i++) {
      const question = session.snapshot().currentQuestion;
      if (!question) break;
      session.submitAnswer(question.options[question.correctIndex]!.id);
      session.tick(REVEAL_DURATION_MS);
    }

    expect(session.snapshot().status).toBe("COMPLETE");
    expect(completedSummary).not.toBeNull();
    const summary = completedSummary as unknown as PlayerQuizSummary;
    expect(summary.totalQuestions).toBe(10);
    expect(summary.correctCount).toBe(10);
    expect(summary.answers).toHaveLength(10);
  });

  it("is deterministic — the same seed and level produce the same question sequence", () => {
    const collectPlayerIds = (seed: string): string[] => {
      const ids: string[] = [];
      const session = new PlayerQuizSession("QUICK", seed, 1);
      session.start();
      for (let i = 0; i < 10; i++) {
        const question = session.snapshot().currentQuestion;
        if (!question) break;
        ids.push(question.player.id);
        session.submitAnswer(question.options[question.correctIndex]!.id);
        session.tick(REVEAL_DURATION_MS);
      }
      return ids;
    };

    expect(collectPlayerIds("player-determinism-seed")).toEqual(collectPlayerIds("player-determinism-seed"));
  });
});
