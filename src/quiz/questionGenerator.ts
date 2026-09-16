import type { SeededRandom } from "@/utils/rng";
import type { QuizQuestion, QuizQuestionSource, RevealMode } from "@/types";

export const ANSWER_COUNT = 4;

/** Builds one full question: correct answer + its distractors shuffled together, with the correct answer's final position tracked. */
export function buildQuestion(index: number, source: QuizQuestionSource, rng: SeededRandom, revealMode: RevealMode, timeLimitMs: number): QuizQuestion {
  const options = rng.shuffle([source.correctAnswer, ...source.distractors]);
  const correctIndex = options.findIndex((option) => option === source.correctAnswer);

  return { index, source, options, correctIndex, revealMode, timeLimitMs };
}
