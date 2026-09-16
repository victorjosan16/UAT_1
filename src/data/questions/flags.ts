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
      es: "¿A qué país pertenece esta bandera?",
      pt: "A que país pertence esta bandeira?",
      hi: "यह झंडा किस देश का है?",
      id: "Bendera ini milik negara mana?",
      ru: "Какой стране принадлежит этот флаг?",
    },
    correctAnswer: country.name,
    distractors: {
      en: distractors.map((c) => c.name.en),
      ro: distractors.map((c) => c.name.ro),
      es: distractors.map((c) => c.name.es),
      pt: distractors.map((c) => c.name.pt),
      hi: distractors.map((c) => c.name.hi),
      id: distractors.map((c) => c.name.id),
      ru: distractors.map((c) => c.name.ru),
    },
    flagCountryId: country.id,
  };
});
