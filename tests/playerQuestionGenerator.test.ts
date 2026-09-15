import { describe, expect, it } from "vitest";
import { SeededRandom } from "@/utils/rng";
import { PLAYERS } from "@/data/players";
import { PLAYER_ANSWER_COUNT, buildPlayerQuestion, pickPlayerDistractors } from "@/quiz/playerQuestionGenerator";

const messi = PLAYERS.find((p) => p.id === "lionel-messi")!;

describe("pickPlayerDistractors", () => {
  it("never includes the target player itself", () => {
    const rng = new SeededRandom("player-distractor-1");
    const distractors = pickPlayerDistractors(messi, PLAYERS, rng);
    expect(distractors.every((p) => p.id !== messi.id)).toBe(true);
  });

  it("never produces duplicate distractors", () => {
    const rng = new SeededRandom("player-distractor-2");
    const distractors = pickPlayerDistractors(messi, PLAYERS, rng);
    expect(new Set(distractors.map((p) => p.id)).size).toBe(distractors.length);
  });

  it("returns the requested count when the pool is large enough", () => {
    const rng = new SeededRandom("player-distractor-3");
    const distractors = pickPlayerDistractors(messi, PLAYERS, rng, 3);
    expect(distractors).toHaveLength(3);
  });
});

describe("buildPlayerQuestion", () => {
  it("produces PLAYER_ANSWER_COUNT options including the correct player exactly once", () => {
    const rng = new SeededRandom("player-question-1");
    const question = buildPlayerQuestion(0, messi, PLAYERS, rng, 8000);
    expect(question.options).toHaveLength(PLAYER_ANSWER_COUNT);
    expect(question.options.filter((p) => p.id === messi.id)).toHaveLength(1);
  });

  it("correctIndex points at the target player in the final option order", () => {
    const rng = new SeededRandom("player-question-2");
    const question = buildPlayerQuestion(0, messi, PLAYERS, rng, 8000);
    expect(question.options[question.correctIndex]?.id).toBe(messi.id);
  });

  it("is deterministic for the same seed", () => {
    const q1 = buildPlayerQuestion(0, messi, PLAYERS, new SeededRandom("same-player-seed"), 8000);
    const q2 = buildPlayerQuestion(0, messi, PLAYERS, new SeededRandom("same-player-seed"), 8000);
    expect(q1.options.map((p) => p.id)).toEqual(q2.options.map((p) => p.id));
    expect(q1.correctIndex).toBe(q2.correctIndex);
  });
});
