import { roundTo } from "@/utils/math";
import type { Difficulty, Language } from "@/types";

/** The subset of AnswerResult this needs — works for every quiz mode's answer log. */
export interface IQSignal {
  correct: boolean;
  responseTimeMs: number;
  timeLimitMs: number;
  difficulty: Difficulty;
}

/**
 * Q5 IQ (0-100) is an in-game performance rating, not a real intelligence
 * measurement — see docs/GAME_DESIGN.md. It's derived purely from this
 * run's own answers: accuracy, the difficulty of what was answered
 * correctly, response speed, and timing consistency. Never a random or
 * fabricated number.
 */
export type KnowledgeRank = "ROOKIE" | "EXPLORER" | "PRO" | "EXPERT" | "MASTER" | "GENIUS";

const RANK_THRESHOLDS: readonly { min: number; rank: KnowledgeRank }[] = [
  { min: 95, rank: "GENIUS" },
  { min: 85, rank: "MASTER" },
  { min: 70, rank: "EXPERT" },
  { min: 50, rank: "PRO" },
  { min: 30, rank: "EXPLORER" },
  { min: 0, rank: "ROOKIE" },
];

export function rankForKnowledgeIQ(iq: number): KnowledgeRank {
  return RANK_THRESHOLDS.find((t) => iq >= t.min)?.rank ?? "ROOKIE";
}

const RANK_LABELS: Record<KnowledgeRank, Record<Language, string>> = {
  ROOKIE: { en: "ROOKIE", ro: "ÎNCEPĂTOR" },
  EXPLORER: { en: "EXPLORER", ro: "EXPLORATOR" },
  PRO: { en: "PRO", ro: "PRO" },
  EXPERT: { en: "EXPERT", ro: "EXPERT" },
  MASTER: { en: "MASTER", ro: "MAESTRU" },
  GENIUS: { en: "GENIUS", ro: "GENIU" },
};

export function knowledgeRankLabel(rank: KnowledgeRank, language: Language): string {
  return RANK_LABELS[rank][language];
}

function mean(values: readonly number[]): number {
  if (values.length === 0) return 0;
  return values.reduce((sum, v) => sum + v, 0) / values.length;
}

function stdDev(values: readonly number[]): number {
  if (values.length < 2) return 0;
  const avg = mean(values);
  const variance = mean(values.map((v) => (v - avg) ** 2));
  return Math.sqrt(variance);
}

/** Pure and deterministic given the run's own answers — no Date/Math.random. */
export function computeKnowledgeIQ(answers: readonly IQSignal[]): number {
  if (answers.length === 0) return 0;

  const correct = answers.filter((a) => a.correct);
  const accuracyScore = (correct.length / answers.length) * 100;

  const avgDifficulty = correct.length === 0 ? 0 : mean(correct.map((a) => a.difficulty));
  const difficultyScore = correct.length === 0 ? 0 : ((avgDifficulty - 1) / 4) * 100;

  const correctResponseTimes = correct.map((a) => a.responseTimeMs);
  const avgResponseMs = mean(correctResponseTimes);
  const avgTimeLimitMs = mean(correct.map((a) => a.timeLimitMs)) || 1;
  const speedScore = correct.length === 0 ? 0 : Math.min(100, Math.max(0, 100 - (avgResponseMs / avgTimeLimitMs) * 100));

  // Consistency: how steady the response rhythm was, independent of accuracy/speed themselves —
  // a high coefficient-of-variation in response time (rushed some, hesitated on others) scores
  // lower. With zero correct answers there's no rhythm to measure, so it's 0, not a neutral
  // default — an all-wrong run must score 0 overall, never a "consolation" IQ.
  const responseStdDev = stdDev(correctResponseTimes);
  const consistencyScore =
    correctResponseTimes.length === 0 ? 0 : correctResponseTimes.length === 1 ? 70 : Math.min(100, Math.max(0, 100 - (responseStdDev / Math.max(1, avgResponseMs)) * 80));

  const iq = accuracyScore * 0.45 + difficultyScore * 0.25 + speedScore * 0.2 + consistencyScore * 0.1;
  return Math.round(roundTo(Math.min(100, Math.max(0, iq)), 0));
}
