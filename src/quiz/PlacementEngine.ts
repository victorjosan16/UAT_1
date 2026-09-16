import { SeededRandom } from "@/utils/rng";
import { ALL_QUESTIONS } from "./QuestionBank";
import { buildQuestion } from "./questionGenerator";
import { ScoreEngine } from "./ScoreEngine";
import { advanceStreak, initialStreakState, type StreakState } from "./StreakSystem";
import { computeKnowledgeIQ } from "./QuizIQ";
import { initialRatingFromIQ } from "./RatingEngine";
import { GAME_VERSION, RULES_VERSION } from "@/branding";
import type { AnswerResult, Difficulty, Language, QuizQuestion } from "@/types";

export type PlacementEngineStatus = "PLAYING" | "REVEAL" | "COMPLETE";

const REVEAL_DURATION_MS = 1200;
export const PLACEMENT_QUESTION_COUNT = 10;
const PLACEMENT_TIME_LIMIT_MS = 15000;

/** EASY -> MEDIUM -> HARD target difficulties a placement "band" picks questions from. */
const BAND_TARGET_DIFFICULTY: readonly Difficulty[] = [1, 3, 5];

export interface PlacementSummary {
  gameVersion: string;
  rulesVersion: number;
  answers: AnswerResult[];
  correctCount: number;
  totalQuestions: number;
  bestStreak: number;
  averageResponseMs: number;
  knowledgeIQ: number;
  startingRating: number;
}

export interface PlacementEngineState {
  status: PlacementEngineStatus;
  questionIndex: number;
  totalQuestions: number;
  currentQuestion: QuizQuestion | null;
  remainingMs: number;
  timeLimitMs: number;
  lastAnswer: AnswerResult | null;
  correctCount: number;
  streak: StreakState;
}

export interface PlacementEngineCallbacks {
  onStateChange?: (state: PlacementEngineState) => void;
  onComplete?: (summary: PlacementSummary) => void;
}

/**
 * The first-ever quiz: NOT a standard pre-built question list. Difficulty
 * adapts one question at a time — two-in-a-row correct nudges the target
 * band up (easy -> medium -> hard), a wrong answer just holds the current
 * band rather than dropping it (see MASTER PROMPT §4: "stabilize", never
 * punish). This one-off, non-competitive, unshareable run is the one place
 * in the app where questions are picked live instead of pre-built from a
 * seed — Quick/Category/Daily/Challenge/Arena all need a shared,
 * reproducible set (see QuizEngine.buildStandardQuestionSet), which
 * adaptive-by-definition selection can never be.
 */
export class PlacementEngine {
  private readonly rng = new SeededRandom(`placement:${Date.now()}:${Math.random()}`);
  private readonly scoreEngine = new ScoreEngine();
  private readonly answers: AnswerResult[] = [];
  private readonly usedQuestionIds = new Set<string>();
  private streak: StreakState = initialStreakState;
  private status: PlacementEngineStatus = "PLAYING";
  private questionIndex = 0;
  private bandIndex = 0; // 0=easy, 1=medium, 2=hard
  private correctStreakForBand = 0;
  private currentQuestion: QuizQuestion | null = null;
  private remainingMs = PLACEMENT_TIME_LIMIT_MS;
  private revealElapsedMs = 0;
  private lastAnswer: AnswerResult | null = null;

  constructor(private readonly language: Language, private readonly callbacks: PlacementEngineCallbacks = {}) {
    this.currentQuestion = this.pickNextQuestion();
  }

  private pickNextQuestion(): QuizQuestion | null {
    const targetDifficulty = BAND_TARGET_DIFFICULTY[this.bandIndex]!;
    const candidates = ALL_QUESTIONS.filter((q) => !this.usedQuestionIds.has(q.id));
    if (candidates.length === 0) return null;

    const sorted = [...candidates].sort((a, b) => Math.abs(a.difficulty - targetDifficulty) - Math.abs(b.difficulty - targetDifficulty));
    const closestGap = Math.abs(sorted[0]!.difficulty - targetDifficulty);
    const pool = sorted.filter((q) => Math.abs(q.difficulty - targetDifficulty) === closestGap);
    const source = this.rng.pick(pool);
    this.usedQuestionIds.add(source.id);

    return buildQuestion(this.questionIndex, source, this.rng, "FULL", PLACEMENT_TIME_LIMIT_MS, this.language);
  }

  private emit(): void {
    this.callbacks.onStateChange?.(this.snapshot());
  }

  snapshot(): PlacementEngineState {
    return {
      status: this.status,
      questionIndex: this.questionIndex,
      totalQuestions: PLACEMENT_QUESTION_COUNT,
      currentQuestion: this.currentQuestion,
      remainingMs: this.remainingMs,
      timeLimitMs: PLACEMENT_TIME_LIMIT_MS,
      lastAnswer: this.lastAnswer,
      correctCount: this.scoreEngine.correctAnswerCount,
      streak: this.streak,
    };
  }

  start(): void {
    this.emit();
  }

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

  submitAnswer(answer: string | null): void {
    if (this.status !== "PLAYING" || !this.currentQuestion) return;
    const question = this.currentQuestion;

    const correct = answer !== null && answer === question.options[question.correctIndex];
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
      timedOut: answer === null,
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

    // Two in a row correct escalates the band; a wrong answer holds it (never drops it) — see class docstring.
    if (correct) {
      this.correctStreakForBand += 1;
      if (this.correctStreakForBand >= 2 && this.bandIndex < BAND_TARGET_DIFFICULTY.length - 1) {
        this.bandIndex += 1;
        this.correctStreakForBand = 0;
      }
    } else {
      this.correctStreakForBand = 0;
    }

    this.status = "REVEAL";
    this.revealElapsedMs = 0;
    this.emit();
  }

  private advance(): void {
    const nextIndex = this.questionIndex + 1;
    if (nextIndex >= PLACEMENT_QUESTION_COUNT) {
      this.status = "COMPLETE";
      this.currentQuestion = null;
      this.emit();
      this.callbacks.onComplete?.(this.buildSummary());
      return;
    }

    this.questionIndex = nextIndex;
    this.currentQuestion = this.pickNextQuestion();
    this.status = "PLAYING";
    this.remainingMs = PLACEMENT_TIME_LIMIT_MS;
    this.lastAnswer = null;
    this.emit();
  }

  private buildSummary(): PlacementSummary {
    const knowledgeIQ = computeKnowledgeIQ(this.answers);
    return {
      gameVersion: GAME_VERSION,
      rulesVersion: RULES_VERSION,
      answers: this.answers,
      correctCount: this.scoreEngine.correctAnswerCount,
      totalQuestions: PLACEMENT_QUESTION_COUNT,
      bestStreak: this.streak.best,
      averageResponseMs: this.scoreEngine.averageResponseMs,
      knowledgeIQ,
      startingRating: initialRatingFromIQ(knowledgeIQ),
    };
  }
}
