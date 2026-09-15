import type { SeededRandom } from "@/utils/rng";
import type { Club, QuizQuestion, RevealMode } from "@/types";

export const ANSWER_COUNT = 4;

/**
 * Picks `count` distractors for `target`, preferring clubs that make a
 * plausible wrong answer (same league, then same difficulty tier) before
 * falling back to the whole pool — never the target itself, never a
 * duplicate. Pure and deterministic given `rng`.
 */
export function pickDistractors(target: Club, pool: readonly Club[], rng: SeededRandom, count = ANSWER_COUNT - 1): Club[] {
  const others = pool.filter((c) => c.id !== target.id);
  const sameLeague = others.filter((c) => c.league === target.league);
  const sameDifficulty = others.filter((c) => c.difficulty === target.difficulty);

  const distractors: Club[] = [];
  const seen = new Set<string>([target.id]);

  for (const candidatePool of [sameLeague, sameDifficulty, others]) {
    if (distractors.length >= count) break;
    for (const club of rng.shuffle(candidatePool)) {
      if (distractors.length >= count) break;
      if (seen.has(club.id)) continue;
      seen.add(club.id);
      distractors.push(club);
    }
  }

  return distractors;
}

/** Builds one full question: correct club + shuffled distractors, with the correct answer's final position tracked. */
export function buildQuestion(
  index: number,
  target: Club,
  pool: readonly Club[],
  rng: SeededRandom,
  revealMode: RevealMode,
  timeLimitMs: number,
): QuizQuestion {
  const distractors = pickDistractors(target, pool, rng);
  const options = rng.shuffle([target, ...distractors]);
  const correctIndex = options.findIndex((c) => c.id === target.id);

  return { index, club: target, options, correctIndex, revealMode, timeLimitMs };
}
