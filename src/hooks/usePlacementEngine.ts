import { useEffect, useMemo, useRef, useState } from "react";
import { PlacementEngine, type PlacementEngineState, type PlacementSummary } from "@/quiz/PlacementEngine";
import type { Language } from "@/types";

export interface UsePlacementEngineResult {
  state: PlacementEngineState;
  submitAnswer: (answer: string | null) => void;
}

/** Mirrors useQuizEngine's shape exactly — same rAF-driven tick loop, different engine underneath. */
export function usePlacementEngine(language: Language, onComplete: (summary: PlacementSummary) => void): UsePlacementEngineResult {
  const [state, setState] = useState<PlacementEngineState | null>(null);
  const onCompleteRef = useRef(onComplete);
  onCompleteRef.current = onComplete;

  const engine = useMemo(() => {
    return new PlacementEngine(language, {
      onStateChange: setState,
      onComplete: (summary) => onCompleteRef.current(summary),
    });
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [language]);

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
