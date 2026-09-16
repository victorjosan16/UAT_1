import { COUNTRIES } from "@/data/countries";
import { confusableCountriesFor } from "./countryDistractors";
import type { QuizQuestionSource } from "@/types";

export const FLAG_QUESTIONS: readonly QuizQuestionSource[] = COUNTRIES.map((country) => {
  const distractors = confusableCountriesFor(country.id);

  return {
    id: `flag-${country.id}`,
    categoryId: "FLAGS",
    difficulty: country.difficulty,
    renderKind: "FLAG",
    prompt: {
      en: "Which country does this flag belong to?",
      ro: "Cărei țări îi aparține acest steag?",
    },
    correctAnswer: country.name,
    distractors: {
      en: distractors.map((c) => c.name.en),
      ro: distractors.map((c) => c.name.ro),
    },
    flagCountryId: country.id,
  };
});
