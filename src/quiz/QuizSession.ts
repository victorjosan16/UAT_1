import { SeededRandom } from "@/utils/rng";
import { CLUBS } from "@/data/clubs";
import { buildQuestion } from "./questionGenerator";
import { ScoreEngine } from "./ScoreEngine";
import { advanceStreak, initialStreakState, streakTierReached, type StreakState, type StreakTierLabel } from "./StreakSystem";
import { computeFootballIQ } from "./FootballIQ";
import { getLevel, MAX_LEVEL } from "./levels";
import { DifficultyEngine } from "./DifficultyEngine";
import { GAME_VERSION, RULES_VERSION } from "@/branding";
import type { AnswerResult, Club, QuizMode, QuizQuestion, QuizSummary } from "@/types";

export type QuizSessionStatus = "PLAYING" | "REVEAL" | "COMPLETE";

const REVEAL_DURATION_MS = 1400;

export interface QuizSessionState {
  status: QuizSessionStatus;
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

export interface QuizSessionCallbacks {
  /** Fired after every state transition (answer submitted, reveal ends, timer ticks) — React wraps this in a hook. */
  onStateChange?: (state: QuizSessionState) => void;
  onComplete?: (summary: QuizSummary) => void;
}

/**
 * Orchestrates one full quiz run: question sequence, timer, scoring,
 * streak, and the PLAYING -> REVEAL -> (next | COMPLETE) state machine.
 * Framework-agnostic (no React) so it's directly unit-testable — see
 * tests/quizSession.test.ts. A thin hook (useQuizSession) wraps this for
 * re-rendering; nothing here reads or touches the DOM.
 */
export class QuizSession {
  private readonly rng: SeededRandom;
  private readonly scoreEngine = new ScoreEngine();
  private readonly difficultyEngine = new DifficultyEngine();
  private readonly questions: QuizQuestion[] = [];
  private readonly answers: AnswerResult[] = [];
  private streak: StreakState = initialStreakState;
  private status: QuizSessionStatus = "PLAYING";
  private questionIndex = 0;
  private remainingMs = 0;
  private revealElapsedMs = 0;
  private lastAnswer: AnswerResult | null = null;
  private lastStreakTier: StreakTierLabel | null = null;
  private readonly startedAtMs: number;

  constructor(
    private readonly mode: QuizMode,
    private readonly seed: string,
    private readonly level: number,
    private readonly callbacks: QuizSessionCallbacks = {},
  ) {
    this.rng = new SeededRandom(seed);
    this.startedAtMs = Date.now();
    this.buildQuestions();
    this.remainingMs = this.questions[0]?.timeLimitMs ?? 0;
  }

  private levelDefinition() {
    return this.level <= MAX_LEVEL ? getLevel(this.level) : this.difficultyEngine.definitionForRound(this.level - MAX_LEVEL);
  }

  private buildQuestions(): void {
    const def = this.levelDefinition();
    const pool = CLUBS.filter((c) => c.difficulty >= def.minDifficulty && c.difficulty <= def.maxDifficulty);
    const effectivePool = pool.length >= def.questionCount ? pool : CLUBS;
    const targets = this.rng.shuffle(effectivePool).slice(0, def.questionCount);

    targets.forEach((club: Club, i: number) => {
      this.questions.push(buildQuestion(i, club, CLUBS, this.rng, def.revealMode, def.timeLimitMs));
    });
  }

  private emit(): void {
    this.callbacks.onStateChange?.(this.snapshot());
  }

  snapshot(): QuizSessionState {
    const currentQuestion = this.questions[this.questionIndex] ?? null;
    return {
      status: this.status,
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

  /** `clubId` null means the player didn't answer in time. */
  submitAnswer(clubId: string | null): void {
    if (this.status !== "PLAYING") return;
    const question = this.questions[this.questionIndex];
    if (!question) return;

    const correct = clubId !== null && clubId === question.club.id;
    const timedOut = clubId === null;
    const responseTimeMs = question.timeLimitMs - this.remainingMs;
    const nextStreak = advanceStreak(this.streak, correct);

    const result = this.scoreEngine.score({
      correct,
      responseTimeMs,
      timeLimitMs: question.timeLimitMs,
      difficulty: question.club.difficulty,
      streakAfter: nextStreak.current,
    });

    const answer: AnswerResult = {
      index: this.questionIndex,
      clubId: question.club.id,
      selectedClubId: clubId,
      correct,
      timedOut,
      responseTimeMs,
      timeLimitMs: question.timeLimitMs,
      difficulty: question.club.difficulty,
      scoreGained: result.gained,
      streakAfter: nextStreak.current,
      totalScore: this.scoreEngine.totalScore,
    };

    this.answers.push(answer);
    this.streak = nextStreak;
    this.lastAnswer = answer;
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
      footballIQ: computeFootballIQ(this.answers),
      answers: this.answers,
      durationMs: Date.now() - this.startedAtMs,
    };
  }
}
