import { COUNTRIES } from "@/data/countries";
import { confusableCountriesFor } from "./countryDistractors";
import type { QuizQuestionSource } from "@/types";

/** "Statele Unite" is the only plural country name in the dataset — needs plural verb agreement in Romanian ("au" not "are"). */
const PLURAL_NAME_IDS = new Set(["usa"]);

export const CAPITAL_QUESTIONS: readonly QuizQuestionSource[] = COUNTRIES.map((country) => {
  const distractors = confusableCountriesFor(country.id);
  const roVerb = PLURAL_NAME_IDS.has(country.id) ? "au" : "are";

  return {
    id: `capital-${country.id}`,
    categoryId: "CAPITALS",
    difficulty: country.difficulty,
    renderKind: "TEXT",
    prompt: {
      en: `What is the capital of ${country.name.en}?`,
      // Phrased as "What capital does X have?" — sidesteps Romanian genitive
      // inflection (which would otherwise need a hand-checked form per
      // country, e.g. "a României" vs "al Ciadului" vs "lui Monaco") while
      // staying completely natural and grammatically correct.
      ro: `Ce capitală ${roVerb} ${country.name.ro}?`,
    },
    correctAnswer: country.capital,
    distractors: {
      en: distractors.map((c) => c.capital.en),
      ro: distractors.map((c) => c.capital.ro),
    },
  };
});
