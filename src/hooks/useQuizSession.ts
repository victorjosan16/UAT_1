import { useEffect, useMemo, useRef, useState } from "react";
import { QuizSession, type QuizSessionState } from "@/quiz/QuizSession";
import type { QuizMode, QuizSummary } from "@/types";

export interface UseQuizSessionResult {
  state: QuizSessionState;
  submitAnswer: (clubId: string | null) => void;
}

/**
 * Thin React wrapper around the framework-agnostic QuizSession: owns the
 * requestAnimationFrame timer loop and re-renders on every state change.
 * All actual game logic lives in QuizSession (unit-tested on its own,
 * with no React dependency at all).
 */
export function useQuizSession(mode: QuizMode, seed: string, level: number, onComplete: (summary: QuizSummary) => void): UseQuizSessionResult {
  const [state, setState] = useState<QuizSessionState | null>(null);
  const onCompleteRef = useRef(onComplete);
  onCompleteRef.current = onComplete;

  const session = useMemo(() => {
    return new QuizSession(mode, seed, level, {
      onStateChange: setState,
      onComplete: (summary) => onCompleteRef.current(summary),
    });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [mode, seed, level]);

  useEffect(() => {
    session.start();
    let raf = 0;
    let last = performance.now();
    const loop = (now: number): void => {
      const dt = now - last;
      last = now;
      session.tick(dt);
      raf = requestAnimationFrame(loop);
    };
    raf = requestAnimationFrame(loop);
    return () => cancelAnimationFrame(raf);
  }, [session]);

  return { state: state ?? session.snapshot(), submitAnswer: (clubId) => session.submitAnswer(clubId) };
}
