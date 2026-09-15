import { useEffect, useMemo, useRef, useState } from "react";
import { PlayerQuizSession, type PlayerQuizSessionState } from "@/quiz/PlayerQuizSession";
import type { PlayerQuizSummary, QuizMode } from "@/types";

export interface UsePlayerQuizSessionResult {
  state: PlayerQuizSessionState;
  submitAnswer: (playerId: string | null) => void;
}

/** Same rAF-driven React wrapper pattern as useQuizSession, for the "Guess the Player" mode. */
export function usePlayerQuizSession(mode: QuizMode, seed: string, level: number, onComplete: (summary: PlayerQuizSummary) => void): UsePlayerQuizSessionResult {
  const [state, setState] = useState<PlayerQuizSessionState | null>(null);
  const onCompleteRef = useRef(onComplete);
  onCompleteRef.current = onComplete;

  const session = useMemo(() => {
    return new PlayerQuizSession(mode, seed, level, {
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

  return { state: state ?? session.snapshot(), submitAnswer: (playerId) => session.submitAnswer(playerId) };
}
