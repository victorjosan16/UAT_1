import { describe, expect, it } from "vitest";
import { SeededRandom } from "@/utils/rng";
import { CLUBS } from "@/data/clubs";
import { ANSWER_COUNT, buildQuestion, pickDistractors } from "@/quiz/questionGenerator";

const arsenal = CLUBS.find((c) => c.id === "arsenal")!;

describe("pickDistractors", () => {
  it("never includes the target club itself", () => {
    const rng = new SeededRandom("distractor-1");
    const distractors = pickDistractors(arsenal, CLUBS, rng);
    expect(distractors.every((c) => c.id !== arsenal.id)).toBe(true);
  });

  it("never produces duplicate distractors", () => {
    const rng = new SeededRandom("distractor-2");
    const distractors = pickDistractors(arsenal, CLUBS, rng);
    expect(new Set(distractors.map((c) => c.id)).size).toBe(distractors.length);
  });

  it("returns the requested count when the pool is large enough", () => {
    const rng = new SeededRandom("distractor-3");
    const distractors = pickDistractors(arsenal, CLUBS, rng, 3);
    expect(distractors).toHaveLength(3);
  });

  it("prefers same-league clubs when enough are available", () => {
    const rng = new SeededRandom("distractor-4");
    const distractors = pickDistractors(arsenal, CLUBS, rng, 3);
    expect(distractors.every((c) => c.league === arsenal.league)).toBe(true);
  });
});

describe("buildQuestion", () => {
  it("produces ANSWER_COUNT options including the correct club exactly once", () => {
    const rng = new SeededRandom("question-1");
    const question = buildQuestion(0, arsenal, CLUBS, rng, "FULL", 8000);
    expect(question.options).toHaveLength(ANSWER_COUNT);
    const matches = question.options.filter((c) => c.id === arsenal.id);
    expect(matches).toHaveLength(1);
  });

  it("correctIndex points at the target club in the final option order", () => {
    const rng = new SeededRandom("question-2");
    const question = buildQuestion(0, arsenal, CLUBS, rng, "FULL", 8000);
    expect(question.options[question.correctIndex]?.id).toBe(arsenal.id);
  });

  it("is deterministic for the same seed", () => {
    const q1 = buildQuestion(0, arsenal, CLUBS, new SeededRandom("same-seed"), "FULL", 8000);
    const q2 = buildQuestion(0, arsenal, CLUBS, new SeededRandom("same-seed"), "FULL", 8000);
    expect(q1.options.map((c) => c.id)).toEqual(q2.options.map((c) => c.id));
    expect(q1.correctIndex).toBe(q2.correctIndex);
  });
});
