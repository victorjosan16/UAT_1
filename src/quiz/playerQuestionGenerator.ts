import type { SeededRandom } from "@/utils/rng";
import type { Player, PlayerQuestion } from "@/types";

export const PLAYER_ANSWER_COUNT = 4;

function sharesAClub(a: Player, b: Player): boolean {
  return a.careerClubIds.some((id) => b.careerClubIds.includes(id));
}

/**
 * Picks distractors for `target`, preferring players who shared at least
 * one club (a genuinely plausible mix-up), then same difficulty tier,
 * before falling back to the whole pool — never the target itself, never
 * a duplicate. Pure and deterministic given `rng`.
 */
export function pickPlayerDistractors(target: Player, pool: readonly Player[], rng: SeededRandom, count = PLAYER_ANSWER_COUNT - 1): Player[] {
  const others = pool.filter((p) => p.id !== target.id);
  const sharedClub = others.filter((p) => sharesAClub(target, p));
  const sameDifficulty = others.filter((p) => p.difficulty === target.difficulty);

  const distractors: Player[] = [];
  const seen = new Set<string>([target.id]);

  for (const candidatePool of [sharedClub, sameDifficulty, others]) {
    if (distractors.length >= count) break;
    for (const player of rng.shuffle(candidatePool)) {
      if (distractors.length >= count) break;
      if (seen.has(player.id)) continue;
      seen.add(player.id);
      distractors.push(player);
    }
  }

  return distractors;
}

export function buildPlayerQuestion(index: number, target: Player, pool: readonly Player[], rng: SeededRandom, timeLimitMs: number): PlayerQuestion {
  const distractors = pickPlayerDistractors(target, pool, rng);
  const options = rng.shuffle([target, ...distractors]);
  const correctIndex = options.findIndex((p) => p.id === target.id);

  return { index, player: target, options, correctIndex, timeLimitMs };
}
