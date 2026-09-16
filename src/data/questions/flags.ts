import { COUNTRIES } from "@/data/countries";
import { confusableCountriesFor } from "./countryDistractors";
import type { QuizQuestionSource } from "@/types";

export const FLAG_QUESTIONS: readonly QuizQuestionSource[] = COUNTRIES.map((country) => ({
  id: `flag-${country.id}`,
  categoryId: "FLAGS",
  difficulty: country.difficulty,
  renderKind: "FLAG",
  prompt: "Which country does this flag belong to?",
  correctAnswer: country.name,
  distractors: confusableCountriesFor(country.id).map((c) => c.name),
  flagCountryId: country.id,
}));
