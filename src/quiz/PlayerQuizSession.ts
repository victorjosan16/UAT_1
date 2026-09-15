import { SeededRandom } from "@/utils/rng";
import { PLAYERS } from "@/data/players";
import { buildPlayerQuestion } from "./playerQuestionGenerator";
import { ScoreEngine } from "./ScoreEngine";
import { advanceStreak, initialStreakState, streakTierReached, type StreakState, type StreakTierLabel } from "./StreakSystem";
import { computeFootballIQ } from "./FootballIQ";
import { getLevel, MAX_LEVEL } from "./levels";
import { DifficultyEngine } from "./DifficultyEngine";
import { GAME_VERSION, RULES_VERSION } from "@/branding";
import type { Player, PlayerAnswerResult, PlayerQuestion, PlayerQuizSummary, QuizMode } from "@/types";

export type QuizSessionStatus = "PLAYING" | "REVEAL" | "COMPLETE";

const REVEAL_DURATION_MS = 1400;

export interface PlayerQuizSessionState {
  status: QuizSessionStatus;
  level: number;
  questionIndex: number;
  totalQuestions: number;
  currentQuestion: PlayerQuestion | null;
  score: number;
  correctCount: number;
  streak: StreakState;
  remainingMs: number;
  timeLimitMs: number;
  lastAnswer: PlayerAnswerResult | null;
  streakTierReached: StreakTierLabel | null;
}

export interface PlayerQuizSessionCallbacks {
  onStateChange?: (state: PlayerQuizSessionState) => void;
  onComplete?: (summary: PlayerQuizSummary) => void;
}

/**
 * "Guess the Player" run: same PLAYING -> REVEAL -> (next | COMPLETE)
 * state machine as QuizSession, reusing the same level curve/scoring/
 * streak/Football IQ engines — only the question content (player career
 * chain vs. club crest) differs. Framework-agnostic, directly unit-tested.
 */
export class PlayerQuizSession {
  private readonly rng: SeededRandom;
  private readonly scoreEngine = new ScoreEngine();
  private readonly difficultyEngine = new DifficultyEngine();
  private readonly questions: PlayerQuestion[] = [];
  private readonly answers: PlayerAnswerResult[] = [];
  private streak: StreakState = initialStreakState;
  private status: QuizSessionStatus = "PLAYING";
  private questionIndex = 0;
  private remainingMs = 0;
  private revealElapsedMs = 0;
  private lastAnswer: PlayerAnswerResult | null = null;
  private lastStreakTier: StreakTierLabel | null = null;
  private readonly startedAtMs: number;

  constructor(
    private readonly mode: QuizMode,
    private readonly seed: string,
    private readonly level: number,
    private readonly callbacks: PlayerQuizSessionCallbacks = {},
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
    const pool = PLAYERS.filter((p) => p.difficulty >= def.minDifficulty && p.difficulty <= def.maxDifficulty);
    const effectivePool = pool.length >= def.questionCount ? pool : PLAYERS;
    const targets = this.rng.shuffle(effectivePool).slice(0, def.questionCount);

    targets.forEach((player: Player, i: number) => {
      this.questions.push(buildPlayerQuestion(i, player, PLAYERS, this.rng, def.timeLimitMs));
    });
  }

  private emit(): void {
    this.callbacks.onStateChange?.(this.snapshot());
  }

  snapshot(): PlayerQuizSessionState {
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

  /** `playerId` null means the player didn't answer in time. */
  submitAnswer(playerId: string | null): void {
    if (this.status !== "PLAYING") return;
    const question = this.questions[this.questionIndex];
    if (!question) return;

    const correct = playerId !== null && playerId === question.player.id;
    const timedOut = playerId === null;
    const responseTimeMs = question.timeLimitMs - this.remainingMs;
    const nextStreak = advanceStreak(this.streak, correct);

    const result = this.scoreEngine.score({
      correct,
      responseTimeMs,
      timeLimitMs: question.timeLimitMs,
      difficulty: question.player.difficulty,
      streakAfter: nextStreak.current,
    });

    const answer: PlayerAnswerResult = {
      index: this.questionIndex,
      playerId: question.player.id,
      selectedPlayerId: playerId,
      correct,
      timedOut,
      responseTimeMs,
      timeLimitMs: question.timeLimitMs,
      difficulty: question.player.difficulty,
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

  private buildSummary(): PlayerQuizSummary {
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
