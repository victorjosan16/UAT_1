import { describe, expect, it } from "vitest";
import { PLACEMENT_QUESTION_COUNT, PlacementEngine, type PlacementSummary } from "@/quiz/PlacementEngine";

const REVEAL_DURATION_MS = 1500;

function playToCompletion(engine: PlacementEngine, answerCorrectly: (correctOption: string, index: number) => boolean = () => true): void {
  for (let i = 0; i < 50; i++) {
    const snapshot = engine.snapshot();
    if (snapshot.status === "COMPLETE") return;
    const question = snapshot.currentQuestion;
    if (!question) return;
    const correctOption = question.options[question.correctIndex]!;
    const shouldAnswerCorrectly = answerCorrectly(correctOption, snapshot.questionIndex);
    engine.submitAnswer(shouldAnswerCorrectly ? correctOption : null);
    engine.tick(REVEAL_DURATION_MS);
  }
}

describe("PlacementEngine", () => {
  it("always runs exactly 10 questions", () => {
    const engine = new PlacementEngine("en");
    engine.start();
    expect(engine.snapshot().totalQuestions).toBe(PLACEMENT_QUESTION_COUNT);
  });

  it("starts on an easy question", () => {
    const engine = new PlacementEngine("en");
    engine.start();
    expect(engine.snapshot().currentQuestion?.source.difficulty).toBeLessThanOrEqual(2);
  });

  it("never repeats a question id within one run", () => {
    const engine = new PlacementEngine("en");
    engine.start();
    const seenIds = new Set<string>();
    for (let i = 0; i < PLACEMENT_QUESTION_COUNT; i++) {
      const question = engine.snapshot().currentQuestion;
      if (!question) break;
      expect(seenIds.has(question.source.id)).toBe(false);
      seenIds.add(question.source.id);
      engine.submitAnswer(question.options[question.correctIndex]!);
      engine.tick(REVEAL_DURATION_MS);
    }
  });

  it("escalates toward harder questions when the player answers well", () => {
    const engine = new PlacementEngine("en");
    engine.start();
    const difficulties: number[] = [];
    for (let i = 0; i < PLACEMENT_QUESTION_COUNT; i++) {
      const question = engine.snapshot().currentQuestion;
      if (!question) break;
      difficulties.push(question.source.difficulty);
      engine.submitAnswer(question.options[question.correctIndex]!);
      engine.tick(REVEAL_DURATION_MS);
    }
    expect(difficulties[difficulties.length - 1]!).toBeGreaterThan(difficulties[0]!);
  });

  it("holds difficulty steady (never escalates) when every answer is wrong", () => {
    const engine = new PlacementEngine("en");
    engine.start();
    const difficulties: number[] = [];
    for (let i = 0; i < PLACEMENT_QUESTION_COUNT; i++) {
      const question = engine.snapshot().currentQuestion;
      if (!question) break;
      difficulties.push(question.source.difficulty);
      engine.submitAnswer(null); // always "wrong" (timeout)
      engine.tick(REVEAL_DURATION_MS);
    }
    expect(Math.max(...difficulties)).toBeLessThanOrEqual(2);
  });

  it("fires onComplete with a full summary including a starting KR", () => {
    let completed: PlacementSummary | null = null;
    const engine = new PlacementEngine("en", { onComplete: (summary) => (completed = summary) });
    engine.start();
    playToCompletion(engine);
    expect(engine.snapshot().status).toBe("COMPLETE");
    expect(completed).not.toBeNull();
    const summary = completed as unknown as PlacementSummary;
    expect(summary.totalQuestions).toBe(PLACEMENT_QUESTION_COUNT);
    expect(summary.answers).toHaveLength(PLACEMENT_QUESTION_COUNT);
    expect(summary.correctCount).toBe(PLACEMENT_QUESTION_COUNT);
    expect(summary.startingRating).toBeGreaterThan(500);
    expect(summary.startingRating).toBeLessThanOrEqual(1999);
  });

  it("a perfect all-wrong run still completes with a valid (low) starting rating", () => {
    let completed: PlacementSummary | null = null;
    const engine = new PlacementEngine("en", { onComplete: (summary) => (completed = summary) });
    engine.start();
    playToCompletion(engine, () => false);
    const summary = completed as unknown as PlacementSummary;
    expect(summary.correctCount).toBe(0);
    expect(summary.startingRating).toBe(500);
  });
});
