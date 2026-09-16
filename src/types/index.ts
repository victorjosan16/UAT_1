/** 1 = very easy/well-known, 5 = obscure / easily confused with a similar answer. */
export type Difficulty = 1 | 2 | 3 | 4 | 5;

/** Supported content/UI languages — see docs/GAME_DESIGN.md §Localization. More can be added without touching the quiz engine. */
export type Language = "en" | "ro";

/** A string translated into every supported language, keyed by canonical content id elsewhere (question id, country id, category id). */
export type LocalizedText = Record<Language, string>;

export type CategoryId =
  | "GENERAL_KNOWLEDGE"
  | "GEOGRAPHY"
  | "FLAGS"
  | "CAPITALS"
  | "HISTORY"
  | "SCIENCE"
  | "SPACE"
  | "NATURE"
  | "ANIMALS"
  | "TECHNOLOGY"
  | "INVENTIONS"
  | "ART"
  | "ARCHITECTURE"
  | "HUMAN_BODY"
  | "MATHEMATICS"
  | "LOGIC"
  | "FOOD"
  | "LANDMARKS"
  | "CULTURES"
  | "MYSTERY_IMAGE";

export type CategoryAccent = "emerald" | "violet" | "amber" | "cyan" | "coral";

export interface Category {
  id: CategoryId;
  label: LocalizedText;
  emoji: string;
  accent: CategoryAccent;
  /** Has real, reviewed question content wired up yet — false renders as "Coming soon" in Discover. */
  available: boolean;
}

/** How a question is presented — most are plain text; FLAG additionally renders a FlagIcon built from real, structured flag data (see data/countries.ts), never an AI-generated or trademarked image. */
export type QuestionRenderKind = "TEXT" | "FLAG";

/** How the visual is progressively revealed as the timer runs — ignored for TEXT questions. */
export type RevealMode = "FULL" | "ZOOM" | "BLUR" | "SILHOUETTE" | "PIECE";

/**
 * Authored question content — the single source of truth for both the
 * correct answer and the presentation. An attached image (FLAG today,
 * more render kinds later) is a presentation layer only; it never decides
 * the correct answer, which always comes from this record.
 */
export interface QuizQuestionSource {
  id: string;
  categoryId: CategoryId;
  difficulty: Difficulty;
  renderKind: QuestionRenderKind;
  prompt: LocalizedText;
  correctAnswer: LocalizedText;
  /** At least 3 plausible wrong answers, in every supported language, same order across languages. */
  distractors: Record<Language, string[]>;
  /** Set when renderKind is "FLAG" — id into COUNTRIES (see data/countries.ts). */
  flagCountryId?: string;
}

export interface QuizQuestion {
  index: number;
  /** Raw, still-localized authored content — id/category/difficulty/flagCountryId. Display text is already resolved below. */
  source: QuizQuestionSource;
  /** Resolved in the language the run was built with. */
  prompt: string;
  /** 4 options including the correct one, in final display order, resolved in the run's language. */
  options: string[];
  correctIndex: number;
  revealMode: RevealMode;
  timeLimitMs: number;
}

export type QuizMode = "QUICK" | "CATEGORY" | "DAILY" | "LEVEL" | "ENDLESS";

export type QuizStatus = "IDLE" | "PLAYING" | "REVEAL" | "COMPLETE";

/** One question's outcome — the unit the scoring engine and the (future) server-validation trace both use. */
export interface AnswerResult {
  index: number;
  questionId: string;
  selectedAnswer: string | null;
  correct: boolean;
  timedOut: boolean;
  responseTimeMs: number;
  timeLimitMs: number;
  difficulty: Difficulty;
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
  knowledgeIQ: number;
  answers: AnswerResult[];
  durationMs: number;
}
