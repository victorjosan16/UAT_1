import { SeededRandom } from "@/utils/rng";
import { ALL_QUESTIONS, questionsForCategory } from "./QuestionBank";
import { buildQuestion } from "./questionGenerator";
import { ScoreEngine } from "./ScoreEngine";
import { advanceStreak, initialStreakState, streakTierReached, type StreakState, type StreakTierLabel } from "./StreakSystem";
import { computeKnowledgeIQ } from "./QuizIQ";
import { getLevel, MAX_LEVEL, QUESTIONS_PER_LEVEL } from "./levels";
import { DifficultyEngine } from "./DifficultyEngine";
import { GAME_VERSION, RULES_VERSION } from "@/branding";
import type { AnswerResult, CategoryId, Difficulty, QuizMode, QuizQuestion, QuizQuestionSource, QuizSummary } from "@/types";

export type QuizEngineStatus = "PLAYING" | "REVEAL" | "COMPLETE";

const REVEAL_DURATION_MS = 1400;
const STANDARD_QUESTION_COUNT = QUESTIONS_PER_LEVEL;

/** Default per-question timer: 10s at difficulty 1, down to 6s at difficulty 5 — see MASTER PROMPT §18/19. */
function standardTimeLimitFor(difficulty: Difficulty): number {
  return 10000 - (difficulty - 1) * 1000;
}

export interface QuizEngineOptions {
  /** Required for LEVEL/ENDLESS — which of the 20 curriculum levels (or beyond, for Endless). */
  level?: number;
  /** Required for CATEGORY mode. */
  categoryId?: CategoryId;
}

export interface QuizEngineState {
  status: QuizEngineStatus;
  mode: QuizMode;
  level: number;
  questionIndex: number;
  totalQuestions: number;
  currentQuestion: QuizQuestion | null;
  score: number;
  correctCount: number;
  streak: StreakState;
  remainingMs: number;
  timeLimitMs: number;
  lastAnswer: AnswerResult | null;
  streakTierReached: StreakTierLabel | null;
  isPerfectSoFar: boolean;
}

export interface QuizEngineCallbacks {
  /** Fired after every state transition (answer submitted, reveal ends, timer ticks) — React wraps this in a hook. */
  onStateChange?: (state: QuizEngineState) => void;
  onComplete?: (summary: QuizSummary) => void;
}

/**
 * Orchestrates one full quiz run for EVERY mode (Quick Play, Category,
 * Daily Challenge, Level Journey, Endless) and every question category —
 * question sequence, timer, scoring, streak, and the PLAYING -> REVEAL ->
 * (next | COMPLETE) state machine. A new category or mode never needs a
 * new engine class, only a new question bank or pool-selection rule below.
 * Framework-agnostic (no React) so it's directly unit-testable — see
 * tests/quizEngine.test.ts. A thin hook (useQuizEngine) wraps this for
 * re-rendering; nothing here reads or touches the DOM.
 */
export class QuizEngine {
  private readonly rng: SeededRandom;
  private readonly scoreEngine = new ScoreEngine();
  private readonly difficultyEngine = new DifficultyEngine();
  private readonly questions: QuizQuestion[] = [];
  private readonly answers: AnswerResult[] = [];
  private readonly level: number;
  private streak: StreakState = initialStreakState;
  private status: QuizEngineStatus = "PLAYING";
  private questionIndex = 0;
  private remainingMs = 0;
  private revealElapsedMs = 0;
  private lastAnswer: AnswerResult | null = null;
  private lastStreakTier: StreakTierLabel | null = null;
  private readonly startedAtMs: number;

  constructor(
    private readonly mode: QuizMode,
    private readonly seed: string,
    private readonly options: QuizEngineOptions = {},
    private readonly callbacks: QuizEngineCallbacks = {},
  ) {
    this.rng = new SeededRandom(seed);
    this.level = options.level ?? 1;
    this.startedAtMs = Date.now();
    this.buildQuestions();
    this.remainingMs = this.questions[0]?.timeLimitMs ?? 0;
  }

  private isLeveled(): boolean {
    return this.mode === "LEVEL" || this.mode === "ENDLESS";
  }

  private levelDefinition() {
    return this.level <= MAX_LEVEL ? getLevel(this.level) : this.difficultyEngine.definitionForRound(this.level - MAX_LEVEL);
  }

  private pool(): readonly QuizQuestionSource[] {
    if (this.mode === "CATEGORY") {
      if (!this.options.categoryId) throw new Error("QuizEngine: CATEGORY mode requires options.categoryId");
      return questionsForCategory(this.options.categoryId);
    }
    return ALL_QUESTIONS;
  }

  private buildQuestions(): void {
    const pool = this.pool();

    if (this.isLeveled()) {
      const def = this.levelDefinition();
      const filtered = pool.filter((q) => q.difficulty >= def.minDifficulty && q.difficulty <= def.maxDifficulty);
      const effectivePool = filtered.length >= def.questionCount ? filtered : pool;
      const targets = this.rng.shuffle(effectivePool).slice(0, def.questionCount);
      targets.forEach((source, i) => {
        this.questions.push(buildQuestion(i, source, this.rng, def.revealMode, def.timeLimitMs));
      });
      return;
    }

    const targets = this.rng.shuffle(pool).slice(0, Math.min(STANDARD_QUESTION_COUNT, pool.length));
    targets.forEach((source, i) => {
      this.questions.push(buildQuestion(i, source, this.rng, "FULL", standardTimeLimitFor(source.difficulty)));
    });
  }

  private emit(): void {
    this.callbacks.onStateChange?.(this.snapshot());
  }

  snapshot(): QuizEngineState {
    const currentQuestion = this.questions[this.questionIndex] ?? null;
    return {
      status: this.status,
      mode: this.mode,
      level: this.level,
      questionIndex: this.questionIndex,
      totalQuestions: this.questions.length,
      currentQuestion,
      score: this.scoreEngine.totalScore,
      correctCount: this.scoreEngine.correctAnswerCount,
      streak: this.streak,
      remainingMs: this.remainingMs,
      timeLimitMs: currentQuestion?.timeLimitMs ?? 0,
      lastAnswer: this.lastAnswer,
      streakTierReached: this.lastStreakTier,
      isPerfectSoFar: this.answers.every((a) => a.correct),
    };
  }

  start(): void {
    this.emit();
  }

  /** Advances the countdown; auto-submits a timeout once it reaches zero. Ignored outside PLAYING. */
  tick(dtMs: number): void {
    if (this.status === "PLAYING") {
      this.remainingMs = Math.max(0, this.remainingMs - dtMs);
      if (this.remainingMs <= 0) {
        this.submitAnswer(null);
        return;
      }
      this.emit();
      return;
    }

    if (this.status === "REVEAL") {
      this.revealElapsedMs += dtMs;
      if (this.revealElapsedMs >= REVEAL_DURATION_MS) {
        this.advance();
        return;
      }
      this.emit();
    }
  }

  /** `answer` null means the player didn't answer in time. */
  submitAnswer(answer: string | null): void {
    if (this.status !== "PLAYING") return;
    const question = this.questions[this.questionIndex];
    if (!question) return;

    const correct = answer !== null && answer === question.source.correctAnswer;
    const timedOut = answer === null;
    const responseTimeMs = question.timeLimitMs - this.remainingMs;
    const nextStreak = advanceStreak(this.streak, correct);

    const result = this.scoreEngine.score({
      correct,
      responseTimeMs,
      timeLimitMs: question.timeLimitMs,
      difficulty: question.source.difficulty,
      streakAfter: nextStreak.current,
    });

    const answerResult: AnswerResult = {
      index: this.questionIndex,
      questionId: question.source.id,
      selectedAnswer: answer,
      correct,
      timedOut,
      responseTimeMs,
      timeLimitMs: question.timeLimitMs,
      difficulty: question.source.difficulty,
      scoreGained: result.gained,
      streakAfter: nextStreak.current,
      totalScore: this.scoreEngine.totalScore,
    };

    this.answers.push(answerResult);
    this.streak = nextStreak;
    this.lastAnswer = answerResult;
    this.lastStreakTier = streakTierReached(nextStreak.current);
    this.status = "REVEAL";
    this.revealElapsedMs = 0;
    this.emit();
  }

  private advance(): void {
    const nextIndex = this.questionIndex + 1;
    if (nextIndex >= this.questions.length) {
      this.status = "COMPLETE";
      this.emit();
      this.callbacks.onComplete?.(this.buildSummary());
      return;
    }

    this.questionIndex = nextIndex;
    this.status = "PLAYING";
    this.remainingMs = this.questions[nextIndex]?.timeLimitMs ?? 0;
    this.lastAnswer = null;
    this.lastStreakTier = null;
    this.emit();
  }

  private buildSummary(): QuizSummary {
    return {
      mode: this.mode,
      seed: this.seed,
      gameVersion: GAME_VERSION,
      rulesVersion: RULES_VERSION,
      score: this.scoreEngine.totalScore,
      correctCount: this.scoreEngine.correctAnswerCount,
      totalQuestions: this.questions.length,
      bestStreak: this.streak.best,
      averageResponseMs: this.scoreEngine.averageResponseMs,
      knowledgeIQ: computeKnowledgeIQ(this.answers),
      answers: this.answers,
      durationMs: Date.now() - this.startedAtMs,
    };
  }
}
