import { useEffect, useRef, useState } from "react";
import { ChampionshipService, COUNTDOWN_MS, type ChampionshipLobby, type ChampionshipPlayer, type ChampionshipStatus } from "@/services/ChampionshipService";
import { buildStandardQuestionSet, STANDARD_QUESTION_COUNT } from "@/quiz/QuizEngine";
import { ScoreEngine } from "@/quiz/ScoreEngine";
import { advanceStreak, initialStreakState, streakTierReached, type StreakState, type StreakTierLabel } from "@/quiz/StreakSystem";
import { computeKnowledgeIQ } from "@/quiz/QuizIQ";
import type { AnswerResult, Language, QuizQuestion } from "@/types";

export type ChampionshipPhase = "LOADING" | ChampionshipStatus;

export interface ChampionshipRoundState {
  phase: ChampionshipPhase;
  players: ChampionshipPlayer[];
  currentQuestion: QuizQuestion | null;
  questionIndex: number;
  totalQuestions: number;
  remainingMs: number;
  timeLimitMs: number;
  countdownRemainingMs: number;
  waitElapsedMs: number;
  hasAnsweredCurrent: boolean;
  lastAnswer: AnswerResult | null;
  streakTierReached: StreakTierLabel | null;
  score: number;
  correctCount: number;
  streak: StreakState;
  knowledgeIQ: number;
}

const TICK_MS = 200;
const WAIT_RETRY_MS = 2000;

/**
 * Drives one Championship match end to end (matchmaking already resolved
 * the lobby id upstream). There is no server here — every subscribed
 * client independently builds the identical question set from the lobby's
 * shared seed (same trick as Challenge a Friend), and every phase
 * transition (countdown -> playing -> next question -> complete) is a
 * guarded Firestore transaction any client may attempt; the first one to
 * land wins, everyone else's attempt just no-ops on a stale read. Pacing
 * is wall-clock driven off `questionStartAt`, never "wait for everyone to
 * answer" — a slow or disconnected player simply times out locally without
 * blocking the other four.
 */
export function useChampionshipRound(lobbyId: string, playerId: string, language: Language) {
  const [lobby, setLobby] = useState<ChampionshipLobby | null>(null);
  const [players, setPlayers] = useState<ChampionshipPlayer[]>([]);
  const [, setTick] = useState(0);

  const questionsRef = useRef<QuizQuestion[] | null>(null);
  const scoreEngineRef = useRef(new ScoreEngine());
  const streakRef = useRef<StreakState>(initialStreakState);
  const answersRef = useRef<AnswerResult[]>([]);
  const answeredIndexRef = useRef(-1);
  const lastAnswerRef = useRef<AnswerResult | null>(null);
  const lastStreakTierRef = useRef<StreakTierLabel | null>(null);
  const advanceAttemptedForRef = useRef(-1);
  const playingAttemptedRef = useRef(false);
  const lastWaitAttemptRef = useRef(0);

  useEffect(() => {
    const unsubLobby = ChampionshipService.subscribeLobby(lobbyId, setLobby);
    const unsubPlayers = ChampionshipService.subscribePlayers(lobbyId, setPlayers);
    return () => {
      unsubLobby();
      unsubPlayers();
    };
  }, [lobbyId]);

  if (lobby && !questionsRef.current) {
    questionsRef.current = buildStandardQuestionSet(lobby.seed, lobby.categoryId, language);
  }

  function submitAnswer(answer: string | null): void {
    const questions = questionsRef.current;
    const currentLobby = lobby;
    if (!questions || !currentLobby || currentLobby.status !== "PLAYING") return;
    const idx = currentLobby.currentQuestionIndex;
    if (answeredIndexRef.current >= idx) return;
    const question = questions[idx];
    if (!question) return;

    const now = Date.now();
    const startedAt = currentLobby.questionStartAt ?? now;
    const responseTimeMs = Math.min(question.timeLimitMs, Math.max(0, now - startedAt));
    const correct = answer !== null && answer === question.options[question.correctIndex];
    const nextStreak = advanceStreak(streakRef.current, correct);

    const result = scoreEngineRef.current.score({
      correct,
      responseTimeMs,
      timeLimitMs: question.timeLimitMs,
      difficulty: question.source.difficulty,
      streakAfter: nextStreak.current,
    });

    const answerResult: AnswerResult = {
      index: idx,
      questionId: question.source.id,
      selectedAnswer: answer,
      correct,
      timedOut: answer === null,
      responseTimeMs,
      timeLimitMs: question.timeLimitMs,
      difficulty: question.source.difficulty,
      scoreGained: result.gained,
      streakAfter: nextStreak.current,
      totalScore: scoreEngineRef.current.totalScore,
    };

    answersRef.current.push(answerResult);
    streakRef.current = nextStreak;
    lastAnswerRef.current = answerResult;
    lastStreakTierRef.current = streakTierReached(nextStreak.current);
    answeredIndexRef.current = idx;

    void ChampionshipService.reportProgress(lobbyId, playerId, {
      score: scoreEngineRef.current.totalScore,
      correctCount: scoreEngineRef.current.correctAnswerCount,
      lastAnsweredIndex: idx,
    });

    setTick((t) => t + 1);
  }

  useEffect(() => {
    const id = setInterval(() => {
      if (!lobby) return;
      const now = Date.now();

      if (lobby.status === "WAITING") {
        if (now - lastWaitAttemptRef.current > WAIT_RETRY_MS) {
          lastWaitAttemptRef.current = now;
          void ChampionshipService.tryStartCountdown(lobbyId);
        }
      } else if (lobby.status === "COUNTDOWN") {
        const remaining = COUNTDOWN_MS - (now - (lobby.countdownStartAt ?? now));
        if (remaining <= 0 && !playingAttemptedRef.current) {
          playingAttemptedRef.current = true;
          void ChampionshipService.tryStartPlaying(lobbyId);
        }
      } else if (lobby.status === "PLAYING" && questionsRef.current) {
        const idx = lobby.currentQuestionIndex;
        const question = questionsRef.current[idx];
        if (question) {
          const remaining = question.timeLimitMs - (now - (lobby.questionStartAt ?? now));
          if (remaining <= 0) {
            if (answeredIndexRef.current < idx) submitAnswer(null);
            if (advanceAttemptedForRef.current !== idx) {
              advanceAttemptedForRef.current = idx;
              void ChampionshipService.tryAdvanceQuestion(lobbyId, idx, questionsRef.current.length);
            }
          }
        }
      }

      setTick((t) => t + 1);
      // eslint-disable-next-line react-hooks/exhaustive-deps
    }, TICK_MS);
    return () => clearInterval(id);
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [lobby, lobbyId]);

  const now = Date.now();
  const totalQuestions = questionsRef.current?.length ?? STANDARD_QUESTION_COUNT;
  const currentQuestion = lobby?.status === "PLAYING" ? (questionsRef.current?.[lobby.currentQuestionIndex] ?? null) : null;
  const timeLimitMs = currentQuestion?.timeLimitMs ?? 0;
  const remainingMs = currentQuestion ? Math.max(0, timeLimitMs - (now - (lobby?.questionStartAt ?? now))) : timeLimitMs;
  const countdownRemainingMs = lobby?.status === "COUNTDOWN" ? Math.max(0, COUNTDOWN_MS - (now - (lobby.countdownStartAt ?? now))) : 0;
  const waitElapsedMs = lobby?.status === "WAITING" ? now - lobby.createdAt : 0;
  const currentIndex = lobby?.currentQuestionIndex ?? -1;
  const hasAnsweredCurrent = answeredIndexRef.current >= currentIndex;
  const lastAnswer = lastAnswerRef.current?.index === currentIndex ? lastAnswerRef.current : null;

  const state: ChampionshipRoundState = {
    phase: lobby?.status ?? "LOADING",
    players,
    currentQuestion,
    questionIndex: Math.max(0, currentIndex),
    totalQuestions,
    remainingMs,
    timeLimitMs,
    countdownRemainingMs,
    waitElapsedMs,
    hasAnsweredCurrent,
    lastAnswer,
    streakTierReached: hasAnsweredCurrent ? lastStreakTierRef.current : null,
    score: scoreEngineRef.current.totalScore,
    correctCount: scoreEngineRef.current.correctAnswerCount,
    streak: streakRef.current,
    knowledgeIQ: computeKnowledgeIQ(answersRef.current),
  };

  return {
    state,
    submitAnswer,
    leave: () => void ChampionshipService.leaveLobby(lobbyId, playerId),
  };
}
