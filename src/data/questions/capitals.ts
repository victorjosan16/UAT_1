import { COUNTRIES } from "@/data/countries";
import { confusableCountriesFor } from "./countryDistractors";
import type { Language, QuizQuestionSource } from "@/types";

/**
 * Phrased as "{country} — what is its capital?" in every language instead
 * of "What is the capital of {country}?" — the latter needs a per-country,
 * per-language grammatical form (Romanian genitive "a României" vs "al
 * Ciadului", Russian genitive "России" vs "Германии", English/Spanish/
 * Portuguese definite articles for a handful of names like "the United
 * States"/"the Netherlands") that would need hand-checking for all 38
 * countries × 7 languages. Naming the country first and asking about "its"
 * capital keeps the name in its plain, invariant form everywhere.
 */
const CAPITAL_QUESTION_SUFFIX: Record<Language, string> = {
  en: "what is its capital?",
  ro: "care este capitala sa?",
  es: "¿cuál es su capital?",
  pt: "qual é a sua capital?",
  hi: "इसकी राजधानी क्या है?",
  id: "apa ibu kotanya?",
  ru: "какая у неё столица?",
};

const LANGUAGES: readonly Language[] = ["en", "ro", "es", "pt", "hi", "id", "ru"];

export const CAPITAL_QUESTIONS: readonly QuizQuestionSource[] = COUNTRIES.map((country) => {
  const distractors = confusableCountriesFor(country.id);

  return {
    id: `capital-${country.id}`,
    categoryId: "CAPITALS",
    difficulty: country.difficulty,
    renderKind: "TEXT",
    prompt: Object.fromEntries(LANGUAGES.map((lang) => [lang, `${country.name[lang]} — ${CAPITAL_QUESTION_SUFFIX[lang]}`])) as Record<Language, string>,
    correctAnswer: country.capital,
    distractors: Object.fromEntries(LANGUAGES.map((lang) => [lang, distractors.map((c) => c.capital[lang])])) as Record<Language, string[]>,
  };
});
