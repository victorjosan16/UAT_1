import { z } from "zod";

export const GameModeSchema = z.enum(["CLASSIC", "ENDLESS", "DAILY", "CHALLENGE"]);
export const GradeSchema = z.enum(["PERFECT", "GREAT", "GOOD", "RISKY"]);

export const PlacementResultSchema = z.object({
  index: z.number().int().min(0),
  floor: z.number().int().min(0),
  accuracy: z.number().min(0).max(100),
  grade: GradeSchema,
  isPerfect: z.boolean(),
  perfectStreak: z.number().int().min(0),
  comboMultiplier: z.number().min(1),
  overlapWidth: z.number().min(0),
  blockWidthBefore: z.number().min(0),
  blockWidthAfter: z.number().min(0),
  scoreGained: z.number().int(),
  totalScore: z.number().int().min(0),
  timestampMs: z.number().min(0),
});

export const RunSummarySchema = z.object({
  mode: GameModeSchema,
  seed: z.string().min(1).max(200),
  gameVersion: z.string().min(1).max(40),
  rulesVersion: z.number().int(),
  score: z.number().int().min(0),
  height: z.number().int().min(0),
  perfectCount: z.number().int().min(0),
  bestCombo: z.number().min(1),
  bestPerfectStreak: z.number().int().min(0),
  averageAccuracy: z.number().min(0).max(100),
  // Cap the trace length generously above what's actually reachable in one
  // session — a submission with more placements than this is rejected
  // outright as implausible, before we even bother re-scoring it.
  placements: z.array(PlacementResultSchema).max(5000),
  durationMs: z.number().min(0),
});

export const NicknameBodySchema = z.object({
  nickname: z.string().min(1).max(64),
});

export const SessionStartBodySchema = z.object({
  mode: GameModeSchema,
  seed: z.string().min(1).max(200).optional(),
});

export const SessionCompleteBodySchema = z.object({
  sessionId: z.string().min(1).max(128),
  summary: RunSummarySchema,
});

export const SessionIdBodySchema = z.object({
  sessionId: z.string().min(1).max(128),
});

export const LeaderboardWindowSchema = z.enum(["daily", "weekly", "all-time"]);
