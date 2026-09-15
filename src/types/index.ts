export type League = "PREMIER_LEAGUE" | "LALIGA" | "SERIE_A" | "BUNDESLIGA" | "LIGUE_1" | "OTHER_EUROPE" | "INTERNATIONAL";

/** 1 = extremely famous, 5 = obscure / easily confused with a similar crest. */
export type ClubDifficulty = 1 | 2 | 3 | 4 | 5;

export interface Club {
  id: string;
  name: string;
  country: string;
  league: League;
  difficulty: ClubDifficulty;
  /** Alternate accepted names (e.g. "Man United" for "Manchester United") — reserved for future free-text modes; the multiple-choice mode only needs `name`. */
  aliases: string[];
}

/** How the crest is presented for a given question — same underlying club data either way. */
export type RevealMode = "FULL" | "ZOOM" | "BLUR" | "SILHOUETTE" | "PIECE";

export interface QuizQuestion {
  index: number;
  club: Club;
  /** 4 clubs including the correct one, in final display order. */
  options: Club[];
  correctIndex: number;
  revealMode: RevealMode;
  timeLimitMs: number;
}

export type QuizMode = "QUICK" | "DAILY" | "CHALLENGE" | "ENDLESS";

export type QuizStatus = "IDLE" | "PLAYING" | "REVEAL" | "COMPLETE";

/** One question's outcome — the unit the scoring engine and the server-validation trace both use. */
export interface AnswerResult {
  index: number;
  clubId: string;
  selectedClubId: string | null;
  correct: boolean;
  timedOut: boolean;
  responseTimeMs: number;
  timeLimitMs: number;
  difficulty: ClubDifficulty;
  scoreGained: number;
  streakAfter: number;
  totalScore: number;
}

export interface QuizSummary {
  mode: QuizMode;
  seed: string;
  gameVersion: string;
  rulesVersion: number;
  score: number;
  correctCount: number;
  totalQuestions: number;
  bestStreak: number;
  averageResponseMs: number;
  footballIQ: number;
  answers: AnswerResult[];
  durationMs: number;
}
