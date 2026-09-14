import type { GameMode } from "@/types";

export interface ChallengeSummary {
  challengeId: string;
  creatorNickname: string;
  creatorScore: number;
  creatorHeight: number;
  seed: string;
  gameVersion: string;
}

export interface ChallengeRunConfig {
  mode: GameMode;
  seed: string;
  targetScoreToBeat: number;
}

/** Replays a friend's exact seed so both runs face identical conditions. */
export function createChallengeRun(challenge: ChallengeSummary): ChallengeRunConfig {
  return { mode: "CHALLENGE", seed: challenge.seed, targetScoreToBeat: challenge.creatorScore };
}

export type ChallengeOutcome = "WIN" | "LOSS" | "SO_CLOSE";

export function evaluateChallengeOutcome(yourScore: number, opponentScore: number): ChallengeOutcome {
  if (yourScore > opponentScore) return "WIN";
  if (opponentScore - yourScore <= Math.max(50, opponentScore * 0.02)) return "SO_CLOSE";
  return "LOSS";
}
