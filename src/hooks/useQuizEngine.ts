import { useEffect, useMemo, useRef, useState } from "react";
import { QuizEngine, type QuizEngineOptions, type QuizEngineState } from "@/quiz/QuizEngine";
import type { QuizMode, QuizSummary } from "@/types";

export interface UseQuizEngineResult {
  state: QuizEngineState;
  submitAnswer: (answer: string | null) => void;
}

/**
 * Thin React wrapper around the framework-agnostic QuizEngine: owns the
 * requestAnimationFrame timer loop and re-renders on every state change.
 * All actual game logic lives in QuizEngine (unit-tested on its own, with
 * no React dependency at all) — this hook is identical for every mode and
 * category; only the `mode`/`seed`/`options` identity changes what runs.
 */
export function useQuizEngine(mode: QuizMode, seed: string, options: QuizEngineOptions, onComplete: (summary: QuizSummary) => void): UseQuizEngineResult {
  const [state, setState] = useState<QuizEngineState | null>(null);
  const onCompleteRef = useRef(onComplete);
  onCompleteRef.current = onComplete;

  const engine = useMemo(() => {
    return new QuizEngine(mode, seed, options, {
      onStateChange: setState,
      onComplete: (summary) => onCompleteRef.current(summary),
    });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [mode, seed, options.level, options.categoryId]);

  useEffect(() => {
    engine.start();
    let raf = 0;
    let last = performance.now();
    const loop = (now: number): void => {
      const dt = now - last;
      last = now;
      engine.tick(dt);
      raf = requestAnimationFrame(loop);
    };
    raf = requestAnimationFrame(loop);
    return () => cancelAnimationFrame(raf);
  }, [engine]);

  return { state: state ?? engine.snapshot(), submitAnswer: (answer) => engine.submitAnswer(answer) };
}
