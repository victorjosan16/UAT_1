import { useState } from "react";
import { StartScreen } from "@/screens/StartScreen";
import { QuizScreen } from "@/screens/QuizScreen";
import { PlayerQuizScreen } from "@/screens/PlayerQuizScreen";
import { ResultsScreen, type ResultsSummaryView } from "@/screens/ResultsScreen";
import { LocalStorageService } from "@/storage/LocalStorage";
import { randomId } from "@/utils/rng";
import type { PlayerQuizSummary, QuizGameType, QuizMode, QuizSummary } from "@/types";

type Screen = "START" | "QUIZ" | "RESULTS";

interface RunConfig {
  gameType: QuizGameType;
  mode: QuizMode;
  seed: string;
  level: number;
}

export function App() {
  const [screen, setScreen] = useState<Screen>("START");
  const [runConfig, setRunConfig] = useState<RunConfig | null>(null);
  const [lastSummary, setLastSummary] = useState<ResultsSummaryView | null>(null);
  const [isNewRecord, setIsNewRecord] = useState(false);

  function startRun(gameType: QuizGameType, level: number): void {
    setRunConfig({ gameType, mode: "QUICK", seed: `${gameType}:${randomId(10)}`, level });
    setScreen("QUIZ");
  }

  function handlePlayLogoQuiz(): void {
    startRun("LOGO", LocalStorageService.getCurrentLevel());
  }

  function handlePlayGuessThePlayer(): void {
    startRun("PLAYER_CHAIN", LocalStorageService.getCurrentPlayerLevel());
  }

  function recordCompletion(summary: QuizSummary | PlayerQuizSummary): boolean {
    const bests = LocalStorageService.getLocalBests();
    const newRecord = summary.score > bests.bestScore && bests.totalQuizzesPlayed > 0;

    LocalStorageService.setLocalBests({
      ...bests,
      bestScore: Math.max(bests.bestScore, summary.score),
      bestFootballIQ: Math.max(bests.bestFootballIQ, summary.footballIQ),
      bestStreak: Math.max(bests.bestStreak, summary.bestStreak),
      totalQuizzesPlayed: bests.totalQuizzesPlayed + 1,
    });

    return newRecord;
  }

  function handleLogoComplete(summary: QuizSummary): void {
    const newRecord = recordCompletion(summary);
    const currentLevel = LocalStorageService.getCurrentLevel();
    if (runConfig && runConfig.level === currentLevel) {
      LocalStorageService.setCurrentLevel(currentLevel + 1);
    }
    setIsNewRecord(newRecord);
    setLastSummary(summary);
    setScreen("RESULTS");
  }

  function handlePlayerComplete(summary: PlayerQuizSummary): void {
    const newRecord = recordCompletion(summary);
    const currentLevel = LocalStorageService.getCurrentPlayerLevel();
    if (runConfig && runConfig.level === currentLevel) {
      LocalStorageService.setCurrentPlayerLevel(currentLevel + 1);
    }
    setIsNewRecord(newRecord);
    setLastSummary(summary);
    setScreen("RESULTS");
  }

  function handlePlayAgain(): void {
    if (!runConfig) return;
    // Replays the level/mode the player just finished, not whatever they've since progressed to.
    startRun(runConfig.gameType, runConfig.level);
  }

  function handleBackToStart(): void {
    setScreen("START");
  }

  switch (screen) {
    case "START":
      return <StartScreen onPlayLogoQuiz={handlePlayLogoQuiz} onPlayGuessThePlayer={handlePlayGuessThePlayer} />;
    case "QUIZ":
      if (!runConfig) return null;
      if (runConfig.gameType === "PLAYER_CHAIN") {
        return <PlayerQuizScreen mode={runConfig.mode} seed={runConfig.seed} level={runConfig.level} onComplete={handlePlayerComplete} />;
      }
      return <QuizScreen mode={runConfig.mode} seed={runConfig.seed} level={runConfig.level} onComplete={handleLogoComplete} />;
    case "RESULTS":
      if (!lastSummary) return null;
      return <ResultsScreen summary={lastSummary} isNewRecord={isNewRecord} onPlayAgain={handlePlayAgain} onBackToStart={handleBackToStart} />;
    default:
      return null;
  }
}
