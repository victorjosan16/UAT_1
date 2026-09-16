import { SeededRandom } from "./rng";
import { ALL_QUESTIONS } from "@/quiz/QuestionBank";
import { buildQuestion } from "@/quiz/questionGenerator";
import type { Language, QuizQuestion } from "@/types";

const QOTD_TIME_LIMIT_MS = 20_000;

/**
 * Exactly one featured question, identical for every player on a given
 * UTC day (see MASTER PROMPT §11) — same deterministic-seed trick as
 * Daily 10, just picking a single item instead of ten. Reusing the exact
 * same seed for the pool shuffle AND the option shuffle means the
 * featured question and its answer order are both the same for everyone,
 * everywhere, all day — essential for the "X% answered correctly" stat
 * to mean the same question for every contributor.
 */
export function pickQotdQuestion(dateKey: string, language: Language): QuizQuestion {
  const rng = new SeededRandom(`qotd:${dateKey}`);
  const source = rng.pick(ALL_QUESTIONS);
  return buildQuestion(0, source, rng, "FULL", QOTD_TIME_LIMIT_MS, language);
}
