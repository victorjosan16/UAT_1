import { useState } from "react";
import { StartScreen } from "@/screens/StartScreen";
import { QuizScreen } from "@/screens/QuizScreen";
import { ResultsScreen } from "@/screens/ResultsScreen";
import { LocalStorageService } from "@/storage/LocalStorage";
import { randomId } from "@/utils/rng";
import type { QuizMode, QuizSummary } from "@/types";

type Screen = "START" | "QUIZ" | "RESULTS";

interface RunConfig {
  mode: QuizMode;
  seed: string;
  level: number;
}

export function App() {
  const [screen, setScreen] = useState<Screen>("START");
  const [runConfig, setRunConfig] = useState<RunConfig | null>(null);
  const [lastSummary, setLastSummary] = useState<QuizSummary | null>(null);
  const [isNewRecord, setIsNewRecord] = useState(false);

  function startRun(level: number): void {
    setRunConfig({ mode: "QUICK", seed: `quick:${randomId(10)}`, level });
    setScreen("QUIZ");
  }

  function handlePlay(): void {
    startRun(LocalStorageService.getCurrentLevel());
  }

  function handleComplete(summary: QuizSummary): void {
    const bests = LocalStorageService.getLocalBests();
    const newRecord = summary.score > bests.bestScore && bests.totalQuizzesPlayed > 0;

    LocalStorageService.setLocalBests({
      ...bests,
      bestScore: Math.max(bests.bestScore, summary.score),
      bestFootballIQ: Math.max(bests.bestFootballIQ, summary.footballIQ),
      bestStreak: Math.max(bests.bestStreak, summary.bestStreak),
      totalQuizzesPlayed: bests.totalQuizzesPlayed + 1,
    });

    const currentLevel = LocalStorageService.getCurrentLevel();
    if (runConfig && runConfig.level === currentLevel) {
      LocalStorageService.setCurrentLevel(currentLevel + 1);
    }

    setIsNewRecord(newRecord);
    setLastSummary(summary);
    setScreen("RESULTS");
  }

  function handlePlayAgain(): void {
    if (!runConfig) return;
    // Replays the level the player just finished, not whatever they've since progressed to.
    startRun(runConfig.level);
  }

  function handleBackToStart(): void {
    setScreen("START");
  }

  switch (screen) {
    case "START":
      return <StartScreen onPlay={handlePlay} />;
    case "QUIZ":
      if (!runConfig) return null;
      return <QuizScreen mode={runConfig.mode} seed={runConfig.seed} level={runConfig.level} onComplete={handleComplete} />;
    case "RESULTS":
      if (!lastSummary) return null;
      return <ResultsScreen summary={lastSummary} isNewRecord={isNewRecord} onPlayAgain={handlePlayAgain} onBackToStart={handleBackToStart} />;
    default:
      return null;
  }
}
