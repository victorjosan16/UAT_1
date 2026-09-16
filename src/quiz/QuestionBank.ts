import { GENERAL_KNOWLEDGE_QUESTIONS } from "@/data/questions/generalKnowledge";
import { GEOGRAPHY_QUESTIONS } from "@/data/questions/geography";
import { FLAG_QUESTIONS } from "@/data/questions/flags";
import { CAPITAL_QUESTIONS } from "@/data/questions/capitals";
import { HISTORY_QUESTIONS } from "@/data/questions/history";
import { SCIENCE_QUESTIONS } from "@/data/questions/science";
import { SPACE_QUESTIONS } from "@/data/questions/space";
import { ANIMALS_QUESTIONS } from "@/data/questions/animals";
import { TECHNOLOGY_QUESTIONS } from "@/data/questions/technology";
import { ART_QUESTIONS } from "@/data/questions/art";
import { HUMAN_BODY_QUESTIONS } from "@/data/questions/humanBody";
import type { CategoryId, QuizQuestionSource } from "@/types";

/**
 * Every reviewed, published question in the game, aggregated by category.
 * A category with no bank here simply has no playable questions yet — see
 * data/categories.ts `available` flag, which Discover/Quick Play/Level
 * Journey all respect. Adding a new category's content later means adding
 * one file here, nothing in the quiz engine changes.
 */
const QUESTIONS_BY_CATEGORY: Partial<Record<CategoryId, readonly QuizQuestionSource[]>> = {
  GENERAL_KNOWLEDGE: GENERAL_KNOWLEDGE_QUESTIONS,
  GEOGRAPHY: GEOGRAPHY_QUESTIONS,
  FLAGS: FLAG_QUESTIONS,
  CAPITALS: CAPITAL_QUESTIONS,
  HISTORY: HISTORY_QUESTIONS,
  SCIENCE: SCIENCE_QUESTIONS,
  SPACE: SPACE_QUESTIONS,
  ANIMALS: ANIMALS_QUESTIONS,
  TECHNOLOGY: TECHNOLOGY_QUESTIONS,
  ART: ART_QUESTIONS,
  HUMAN_BODY: HUMAN_BODY_QUESTIONS,
};

export const ALL_QUESTIONS: readonly QuizQuestionSource[] = Object.values(QUESTIONS_BY_CATEGORY).flat();

export function questionsForCategory(categoryId: CategoryId): readonly QuizQuestionSource[] {
  return QUESTIONS_BY_CATEGORY[categoryId] ?? [];
}
