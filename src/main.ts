import { Game } from "@/game/Game";
import { HUD } from "@/ui/HUD";
import { StartScreen } from "@/ui/StartScreen";
import { GameOverScreen, type GameOverStats } from "@/ui/GameOverScreen";
import { Leaderboard } from "@/ui/Leaderboard";
import { ChallengeScreen } from "@/ui/ChallengeScreen";
import { NicknameScreen } from "@/ui/NicknameScreen";
import { playerService } from "@/services/PlayerService";
import { challengeService, type ChallengeDetails } from "@/services/ChallengeService";
import { runSubmitter } from "@/services/RunSubmitter";
import { leaderboardService } from "@/services/LeaderboardService";
import { analyticsService } from "@/services/AnalyticsService";
import { LocalStorageService } from "@/storage/LocalStorage";
import { createClassicRun } from "@/modes/ClassicMode";
import { createDailyRun } from "@/modes/DailyMode";
import { createChallengeRun, evaluateChallengeOutcome } from "@/modes/ChallengeMode";
import { shareResult } from "@/utils/share";
import type { GameMode, RunSummary } from "@/types";

const canvas = document.getElementById("game-canvas") as HTMLCanvasElement;
const uiRoot = document.getElementById("ui-root");
if (!uiRoot) throw new Error("#ui-root missing");

let identity: { playerId: string; nickname: string } = { playerId: "", nickname: "" };
let activeGame: Game | null = null;
let pendingChallenge: ChallengeDetails | null = null;
let pendingDailyDateKey: string | undefined;
let isFirstBoot = true;

const hud = new HUD({
  onToggleSound: (enabled) => activeGame?.setSoundEnabled(enabled),
  onToggleHaptics: (enabled) => activeGame?.setHapticsEnabled(enabled),
});
hud.setVisible(false);

const gameOverScreen = new GameOverScreen({
  onPlayAgain: () => startRun(lastModeConfig()),
  onChallengeFriend: () => void handleCreateChallenge(),
  onOpenLeaderboard: () => openLeaderboard(() => gameOverScreen.setVisible(true)),
  onShare: () => void handleShare(),
});

/** Whichever screen the leaderboard should return to on close — since it can be opened from more than one place, `showStart()` alone would drop the player back at the wrong screen. */
let leaderboardReturnTo: () => void = () => showStart();

const leaderboard = new Leaderboard(() => {
  leaderboard.setVisible(false);
  leaderboardReturnTo();
});

function openLeaderboard(returnTo: () => void): void {
  leaderboardReturnTo = returnTo;
  startScreen.setVisible(false);
  gameOverScreen.setVisible(false);
  challengeScreen.setVisible(false);
  nicknameScreen.setVisible(false);
  void leaderboard.open(identity.playerId);
}

const challengeScreen = new ChallengeScreen({
  onAccept: (challenge) => {
    pendingChallenge = challenge;
    analyticsService.track("challenge_started", { challengeId: challenge.challengeId });
    const config = createChallengeRun(challenge);
    startRun({ mode: config.mode, seed: config.seed, targetScoreToBeat: config.targetScoreToBeat });
  },
  onTryAgain: (challenge) => {
    const config = createChallengeRun(challenge);
    startRun({ mode: config.mode, seed: config.seed, targetScoreToBeat: config.targetScoreToBeat });
  },
  onChallengeSomeoneElse: () => {
    pendingChallenge = null;
    history.pushState({}, "", "/");
    showStart();
  },
  onClose: () => showStart(),
});

const startScreen = new StartScreen({
  onPlayClassic: () => {
    const config = createClassicRun();
    startRun({ mode: config.mode, seed: config.seed, targetScoreToBeat: config.targetScoreToBeat });
  },
  onPlayDaily: () => {
    const config = createDailyRun();
    pendingDailyDateKey = config.dateKey;
    analyticsService.track("daily_started", { dateKey: config.dateKey });
    startRun({ mode: config.mode, seed: config.seed, targetScoreToBeat: config.targetScoreToBeat });
  },
  onOpenLeaderboard: () => {
    analyticsService.track("leaderboard_opened", {});
    openLeaderboard(showStart);
  },
  onEditNickname: () => {
    nicknameScreen.setValue(identity.nickname);
    startScreen.setVisible(false);
    nicknameScreen.setVisible(true);
  },
});

const nicknameScreen = new NicknameScreen({
  onConfirm: (nickname) => {
    playerService.setNickname(nickname);
    LocalStorageService.updatePreferences({ nicknameConfirmed: true });
    identity = { ...identity, nickname };
    startScreen.setNickname(nickname);
    nicknameScreen.setVisible(false);
    if (isFirstBoot) {
      isFirstBoot = false;
      void enterApp();
    } else {
      showStart();
    }
  },
});

uiRoot.append(startScreen.root, hud.root, gameOverScreen.root, leaderboard.root, challengeScreen.root, nicknameScreen.root);

interface RunConfig {
  mode: GameMode;
  seed: string;
  targetScoreToBeat: number | null;
  startLevel?: number;
}

let lastConfig: RunConfig = { mode: "CLASSIC", seed: "classic:seed", targetScoreToBeat: null };
function lastModeConfig(): RunConfig {
  return lastConfig;
}

function showStart(): void {
  startScreen.setVisible(true);
  hud.setVisible(false);
  gameOverScreen.setVisible(false);
  challengeScreen.setVisible(false);
  leaderboard.setVisible(false);
  nicknameScreen.setVisible(false);
}

function startRun(config: RunConfig): void {
  lastConfig = config;
  startScreen.setVisible(false);
  gameOverScreen.setVisible(false);
  challengeScreen.setVisible(false);
  leaderboard.setVisible(false);
  hud.setVisible(true);

  const prefs = LocalStorageService.getPreferences();
  activeGame?.destroy();
  hud.resetScore();

  const personalBestScore = LocalStorageService.getLocalBests().highScore;

  activeGame = new Game(
    canvas,
    config.mode,
    config.seed,
    {
      onScoreChange: (score) => hud.setScore(score),
      onFeedback: (text, kind) => hud.showFeedback(text, kind),
      onPlacement: () => hud.setFloor(activeGame?.state.floor ?? 0),
      onLevelComplete: (level) => {
        analyticsService.track("level_complete", { level });
        hud.showFeedback(`LEVEL ${level + 1}`, "close");
      },
      onEndlessUnlocked: () => {
        analyticsService.track("endless_unlocked", {});
        hud.showFeedback("ENDLESS MODE UNLOCKED", "perfect");
      },
      onMilestone: (floor) => hud.showFeedback(`${floor} FLOORS!`, "perfect"),
      onNewRecordCrossed: () => hud.showRecordBanner(),
      onGameOver: (summary) => onGameOver(summary),
    },
    config.targetScoreToBeat,
    config.startLevel ?? 1,
    personalBestScore,
  );
  activeGame.setSoundEnabled(prefs.soundEnabled);
  activeGame.setHapticsEnabled(prefs.hapticsEnabled);

  runSubmitter.beginRun(config.mode, config.seed);
  analyticsService.track("game_start", { mode: config.mode });
  activeGame.start();
  LocalStorageService.updatePreferences({ tutorialCompleted: true });
}

function onGameOver(summary: RunSummary): void {
  analyticsService.track("game_over", { score: summary.score, height: summary.height, mode: summary.mode });

  const bests = LocalStorageService.getLocalBests();
  const isNewRecord = summary.score > bests.highScore;
  const nextBests = {
    highScore: Math.max(bests.highScore, summary.score),
    highestFloor: Math.max(bests.highestFloor, summary.height),
    bestPerfectStreak: Math.max(bests.bestPerfectStreak, summary.bestPerfectStreak),
    bestAverageAccuracy: Math.max(bests.bestAverageAccuracy, summary.averageAccuracy),
    totalTowersBuilt: bests.totalTowersBuilt + 1,
    dailyStreak: bests.dailyStreak,
    lastDailyDateKey: bests.lastDailyDateKey,
  };

  if (summary.mode === "DAILY" && pendingDailyDateKey) {
    if (bests.lastDailyDateKey !== pendingDailyDateKey) {
      nextBests.dailyStreak = bests.lastDailyDateKey ? bests.dailyStreak + 1 : 1;
      nextBests.lastDailyDateKey = pendingDailyDateKey;
    }
  }
  LocalStorageService.setLocalBests(nextBests);

  void runSubmitter.completeRun(summary, {
    dailyDateKey: summary.mode === "DAILY" ? pendingDailyDateKey : undefined,
    challengeId: summary.mode === "CHALLENGE" ? pendingChallenge?.challengeId : undefined,
  });
  void leaderboardService.submitScore(identity.playerId, identity.nickname, summary.score, summary.height);

  if (summary.mode === "CHALLENGE" && pendingChallenge) {
    const outcome = evaluateChallengeOutcome(summary.score, pendingChallenge.creatorScore);
    challengeScreen.showResult(pendingChallenge, {
      accepted: true,
      yourScore: summary.score,
      yourHeight: summary.height,
      opponentScore: pendingChallenge.creatorScore,
      opponentHeight: pendingChallenge.creatorHeight,
      outcome,
    });
    analyticsService.track("challenge_completed", { challengeId: pendingChallenge.challengeId, outcome });
    hud.setVisible(false);
    return;
  }

  const stats: GameOverStats = {
    ...summary,
    best: nextBests.highScore,
    worldRank: null,
    topPercent: null,
    isNewRecord,
  };
  gameOverScreen.show(stats);
  hud.setVisible(false);
}

async function handleCreateChallenge(): Promise<void> {
  try {
    const sessionId = await runSubmitter.getSessionId();
    const created = await challengeService.createChallenge(sessionId);
    analyticsService.track("challenge_created", { challengeId: created.challengeId });
    await shareResult({ height: LocalStorageService.getLocalBests().highestFloor, score: LocalStorageService.getLocalBests().highScore, topPercent: null, url: created.url });
  } catch {
    // Offline or backend unavailable — challenge creation requires the network; silently no-op.
  }
}

async function handleShare(): Promise<void> {
  const bests = LocalStorageService.getLocalBests();
  analyticsService.track("share_clicked", {});
  await shareResult({ height: bests.highestFloor, score: bests.highScore, topPercent: null, url: window.location.origin });
}

/** Deep-link/start-screen routing, resumed once a nickname is confirmed. */
async function enterApp(): Promise<void> {
  const match = /^\/challenge\/([A-Za-z0-9]+)/.exec(window.location.pathname);
  if (match) {
    const challengeId = match[1] ?? "";
    try {
      const challenge = await challengeService.fetchChallenge(challengeId);
      pendingChallenge = challenge;
      analyticsService.track("challenge_opened", { challengeId });
      challengeScreen.showInvite(challenge);
      return;
    } catch {
      // Falls through to normal start screen if the challenge can't be loaded (expired/offline).
    }
  }
  showStart();
}

async function boot(): Promise<void> {
  identity = await playerService.ensureIdentity();
  analyticsService.track("app_open", { playerId: identity.playerId });
  startScreen.setNickname(identity.nickname);

  if (!LocalStorageService.getPreferences().nicknameConfirmed) {
    // First launch — ask the player to confirm/pick a nickname before
    // anything else, since it's what identifies them on the leaderboard.
    startScreen.setVisible(false);
    nicknameScreen.setValue(identity.nickname);
    nicknameScreen.setVisible(true);
    return;
  }

  isFirstBoot = false;
  await enterApp();
}

void boot();
