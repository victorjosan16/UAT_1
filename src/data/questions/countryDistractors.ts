import { SeededRandom } from "@/utils/rng";
import { COUNTRIES, type Country } from "@/data/countries";

const rng = new SeededRandom("q5-country-distractors-v1");

/** Famous look-alike flag families — surfacing these as distractors is the whole point of a flags quiz. */
const CONFUSABLE_GROUPS: readonly string[][] = [
  ["romania", "chad", "andorra", "moldova"],
  ["indonesia", "monaco", "poland"],
];

/**
 * Picks `count` plausible wrong-answer countries for `countryId`: known
 * look-alikes first, then same-continent countries, then anywhere else.
 * Deterministic (seeded), never the target itself, never a duplicate.
 */
export function confusableCountriesFor(countryId: string, count = 3): Country[] {
  const country = COUNTRIES.find((c) => c.id === countryId);
  if (!country) return [];

  const confusableGroup = CONFUSABLE_GROUPS.find((g) => g.includes(countryId));
  const confusableIds = confusableGroup ? confusableGroup.filter((id) => id !== countryId) : [];
  const confusables = confusableIds.map((id) => COUNTRIES.find((c) => c.id === id)).filter((c): c is Country => Boolean(c));

  const remaining = COUNTRIES.filter((c) => c.id !== countryId && !confusableIds.includes(c.id));
  const sameContinent = remaining.filter((c) => c.continent === country.continent);
  const others = remaining.filter((c) => c.continent !== country.continent);

  const pool = [...confusables, ...rng.shuffle(sameContinent), ...rng.shuffle(others)];
  return pool.slice(0, count);
}
