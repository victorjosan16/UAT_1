import { describe, expect, it } from "vitest";
import { SeededRandom } from "@/utils/rng";
import { buildQuestion } from "@/quiz/questionGenerator";
import type { QuizQuestionSource } from "@/types";

const SOURCE: QuizQuestionSource = {
  id: "q-1",
  categoryId: "GENERAL_KNOWLEDGE",
  difficulty: 2,
  renderKind: "TEXT",
  prompt: "What is 2 + 2?",
  correctAnswer: "4",
  distractors: ["3", "5", "22"],
};

describe("buildQuestion", () => {
  it("includes the correct answer exactly once among 4 options", () => {
    const question = buildQuestion(0, SOURCE, new SeededRandom("q-seed-1"), "FULL", 8000);
    expect(question.options).toHaveLength(4);
    expect(question.options.filter((o) => o === SOURCE.correctAnswer)).toHaveLength(1);
  });

  it("correctIndex points at the correct answer in the final option order", () => {
    const question = buildQuestion(0, SOURCE, new SeededRandom("q-seed-2"), "FULL", 8000);
    expect(question.options[question.correctIndex]).toBe(SOURCE.correctAnswer);
  });

  it("is deterministic for the same seed", () => {
    const q1 = buildQuestion(0, SOURCE, new SeededRandom("same-seed"), "FULL", 8000);
    const q2 = buildQuestion(0, SOURCE, new SeededRandom("same-seed"), "FULL", 8000);
    expect(q1.options).toEqual(q2.options);
    expect(q1.correctIndex).toBe(q2.correctIndex);
  });

  it("carries through the given reveal mode and time limit", () => {
    const question = buildQuestion(2, SOURCE, new SeededRandom("q-seed-3"), "BLUR", 5000);
    expect(question.index).toBe(2);
    expect(question.revealMode).toBe("BLUR");
    expect(question.timeLimitMs).toBe(5000);
    expect(question.source).toBe(SOURCE);
  });
});
