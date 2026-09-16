import { COUNTRIES } from "@/data/countries";
import { confusableCountriesFor } from "./countryDistractors";
import type { QuizQuestionSource } from "@/types";

export const CAPITAL_QUESTIONS: readonly QuizQuestionSource[] = COUNTRIES.map((country) => ({
  id: `capital-${country.id}`,
  categoryId: "CAPITALS",
  difficulty: country.difficulty,
  renderKind: "TEXT",
  prompt: `What is the capital of ${country.name}?`,
  correctAnswer: country.capital,
  distractors: confusableCountriesFor(country.id).map((c) => c.capital),
}));
