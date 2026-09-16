import type { SeededRandom } from "@/utils/rng";
import type { Language, QuizQuestion, QuizQuestionSource, RevealMode } from "@/types";

export const ANSWER_COUNT = 4;

/** Builds one full question: correct answer + its distractors shuffled together, resolved into the given language, with the correct answer's final position tracked. */
export function buildQuestion(index: number, source: QuizQuestionSource, rng: SeededRandom, revealMode: RevealMode, timeLimitMs: number, language: Language): QuizQuestion {
  const correctAnswer = source.correctAnswer[language];
  const options = rng.shuffle([correctAnswer, ...source.distractors[language]]);
  const correctIndex = options.findIndex((option) => option === correctAnswer);

  return { index, source, prompt: source.prompt[language], options, correctIndex, revealMode, timeLimitMs };
}
