import { useEffect, useState } from "react";
import { BottomNav, type NavTab } from "@/components/BottomNav";
import { WelcomeScreen } from "@/screens/WelcomeScreen";
import { EntryScreen } from "@/screens/EntryScreen";
import { PlacementQuizScreen } from "@/screens/PlacementQuizScreen";
import { PlacementResultScreen } from "@/screens/PlacementResultScreen";
import { ChallengeScreen } from "@/screens/ChallengeScreen";
import { ChampionshipScreen } from "@/screens/ChampionshipScreen";
import { HomeScreen } from "@/screens/HomeScreen";
import { DiscoverScreen } from "@/screens/DiscoverScreen";
import { PlayScreen } from "@/screens/PlayScreen";
import { RankingScreen } from "@/screens/RankingScreen";
import { ProfileScreen } from "@/screens/ProfileScreen";
import { QuizScreen } from "@/screens/QuizScreen";
import { ResultsScreen, type ChallengeComparison, type ResultsSummaryView } from "@/screens/ResultsScreen";
import { LocalStorageService } from "@/storage/LocalStorage";
import { playerService } from "@/services/PlayerService";
import { LeaderboardService } from "@/services/LeaderboardService";
import { ChallengeService, type ChallengeDoc } from "@/services/ChallengeService";
import { soundManager } from "@/services/SoundManager";
import { hapticsManager } from "@/services/HapticsManager";
import { shareResult } from "@/utils/share";
import { randomId } from "@/utils/rng";
import { dailySeed, utcDateKey } from "@/utils/dailySeed";
import { nudgeRating } from "@/quiz/RatingEngine";
import { MilestoneService, type MilestoneRank } from "@/services/MilestoneService";
import type { PlacementSummary } from "@/quiz/PlacementEngine";
import { GAME_VERSION } from "@/branding";
import type { CategoryId, QuizMode, QuizSummary } from "@/types";

type OverlayScreen = "QUIZ" | "RESULTS" | "CHAMPIONSHIP" | null;

/** Brand-new players only — see MASTER PROMPT §2-7. A returning player who already confirmed a nickname skips straight past this (see PlayerService's legacy-rating default). */
type OnboardingStage = "ENTRY" | "PLACEMENT" | "PLACEMENT_RESULT" | "NICKNAME";

interface RunConfig {
  mode: QuizMode;
  seed: string;
  level?: number;
  categoryId?: CategoryId;
}

const ONE_DAY_MS = 86_400_000;

export function App() {
  const [tab, setTab] = useState<NavTab>("HOME");
  const [overlay, setOverlay] = useState<OverlayScreen>(null);
  const [runConfig, setRunConfig] = useState<RunConfig | null>(null);
  const [lastSummary, setLastSummary] = useState<ResultsSummaryView | null>(null);
  const [isNewRecord, setIsNewRecord] = useState(false);
  const [playerId, setPlayerId] = useState(() => LocalStorageService.getPlayerId() ?? "");
  const [nickname, setNickname] = useState(() => LocalStorageService.getNickname() ?? "Player");
  const [nicknameConfirmed, setNicknameConfirmed] = useState(() => LocalStorageService.getPreferences().nicknameConfirmed);
  const [bests, setBests] = useState(() => LocalStorageService.getLocalBests());
  const [currentLevel, setCurrentLevelState] = useState(() => LocalStorageService.getCurrentLevel());
  const [rating, setRating] = useState(() => LocalStorageService.getRating());
  const [onboardingStage, setOnboardingStage] = useState<OnboardingStage>("ENTRY");
  const [placementSummary, setPlacementSummary] = useState<PlacementSummary | null>(null);
  const [rankMovement, setRankMovement] = useState<{ from: number; to: number } | null>(null);
  const [milestone, setMilestone] = useState<{ threshold: MilestoneRank; rank: number } | null>(null);

  const [incomingChallengeId] = useState(() => ChallengeService.parseIdFromLocation());
  const [challengeScreenVisible, setChallengeScreenVisible] = useState(() => incomingChallengeId !== null);
  const [incomingChallenge, setIncomingChallenge] = useState<ChallengeDoc | null | undefined>(incomingChallengeId ? undefined : null);
  const [activeChallenge, setActiveChallenge] = useState<ChallengeComparison | null>(null);
  const [challengeComparison, setChallengeComparison] = useState<ChallengeComparison | null>(null);

  useEffect(() => {
    // Never blocks Home (or the nickname prompt) from rendering immediately — identity resolves in the background (see PlayerService).
    void playerService.ensureIdentity().then((identity) => {
      setPlayerId(identity.playerId);
      setNickname(identity.nickname);
      // A legacy player's rating may have just been defaulted for the first time inside ensureIdentity — pick it up.
      setRating(LocalStorageService.getRating());
    });
  }, []);

  useEffect(() => {
    if (!incomingChallengeId) return;
    void ChallengeService.fetch(incomingChallengeId).then(setIncomingChallenge);
  }, [incomingChallengeId]);

  function handlePlacementComplete(summary: PlacementSummary): void {
    setPlacementSummary(summary);
    setOnboardingStage("PLACEMENT_RESULT");
  }

  function handleClaimPlacement(): void {
    setOnboardingStage("NICKNAME");
  }

  function handleConfirmNickname(raw: string): void {
    const trimmed = playerService.setNickname(playerId, raw);
    setNickname(trimmed);
    setNicknameConfirmed(true);
    LocalStorageService.updatePreferences({ nicknameConfirmed: true, placementCompleted: placementSummary !== null });

    if (placementSummary) {
      LocalStorageService.setRating(placementSummary.startingRating);
      setRating(placementSummary.startingRating);
      LocalStorageService.setLocalBests({ ...LocalStorageService.getLocalBests(), bestKnowledgeIQ: Math.max(LocalStorageService.getLocalBests().bestKnowledgeIQ, placementSummary.knowledgeIQ) });
      setBests(LocalStorageService.getLocalBests());
      if (playerId) void playerService.syncRating(playerId, placementSummary.startingRating);
    }
  }

  function handleChangeNickname(raw: string): void {
    const trimmed = playerService.setNickname(playerId, raw);
    setNickname(trimmed);
  }

  function startRun(mode: QuizMode, options: { seed?: string; level?: number; categoryId?: CategoryId } = {}): void {
    setRunConfig({ mode, seed: options.seed ?? `${mode}:${randomId(10)}`, level: options.level, categoryId: options.categoryId });
    setOverlay("QUIZ");
  }

  function handlePlayQuick(): void {
    startRun("QUICK");
  }

  function handlePlayDaily(): void {
    startRun("DAILY", { seed: dailySeed(utcDateKey(), GAME_VERSION) });
  }

  function handlePlayLevel(): void {
    startRun("LEVEL", { level: currentLevel });
  }

  function handleOpenCategory(categoryId: CategoryId): void {
    startRun("CATEGORY", { categoryId });
  }

  function handlePlayChampionship(): void {
    setOverlay("CHAMPIONSHIP");
  }

  function handlePlayIncomingChallenge(): void {
    if (!incomingChallenge) return;
    setActiveChallenge({ creatorNickname: incomingChallenge.creatorNickname, creatorScore: incomingChallenge.creatorScore });
    setChallengeScreenVisible(false);
    window.history.replaceState(null, "", "/");
    startRun(incomingChallenge.mode, { seed: incomingChallenge.seed, level: incomingChallenge.level, categoryId: incomingChallenge.categoryId });
  }

  function handleDismissChallenge(): void {
    setChallengeScreenVisible(false);
    window.history.replaceState(null, "", "/");
  }

  async function handleChallengeFriend(): Promise<void> {
    if (!runConfig || !lastSummary || !playerId) return;
    try {
      const id = await ChallengeService.create({
        creatorId: playerId,
        creatorNickname: nickname,
        mode: runConfig.mode,
        seed: runConfig.seed,
        categoryId: runConfig.categoryId,
        level: runConfig.level,
        creatorScore: lastSummary.score,
        creatorKnowledgeIQ: lastSummary.knowledgeIQ,
      });
      await shareResult({
        knowledgeIQ: lastSummary.knowledgeIQ,
        correctCount: lastSummary.correctCount,
        totalQuestions: lastSummary.totalQuestions,
        score: lastSummary.score,
        bestStreak: lastSummary.bestStreak,
        topPercent: null,
        url: ChallengeService.buildUrl(id),
      });
    } catch {
      // Offline or Firestore-blocked — the button simply doesn't produce a link; never throw into the results screen.
    }
  }

  function handleComplete(summary: QuizSummary): void {
    const previous = LocalStorageService.getLocalBests();
    const newRecord = summary.score > previous.bestScore && previous.totalQuizzesPlayed > 0;

    const updated = {
      ...previous,
      bestScore: Math.max(previous.bestScore, summary.score),
      bestKnowledgeIQ: Math.max(previous.bestKnowledgeIQ, summary.knowledgeIQ),
      bestStreak: Math.max(previous.bestStreak, summary.bestStreak),
      totalQuizzesPlayed: previous.totalQuizzesPlayed + 1,
    };

    if (summary.mode === "DAILY") {
      const today = utcDateKey();
      if (updated.lastDailyDateKey !== today) {
        const yesterday = utcDateKey(new Date(Date.now() - ONE_DAY_MS));
        updated.dailyStreak = updated.lastDailyDateKey === yesterday ? updated.dailyStreak + 1 : 1;
        updated.lastDailyDateKey = today;
      }
    }

    LocalStorageService.setLocalBests(updated);
    setBests(updated);

    if ((summary.mode === "LEVEL" || summary.mode === "ENDLESS") && runConfig?.level === currentLevel) {
      const nextLevel = currentLevel + 1;
      LocalStorageService.setCurrentLevel(nextLevel);
      setCurrentLevelState(nextLevel);
    }

    // Fire-and-forget: the leaderboard is a nice-to-have, never a gate on seeing your results.
    // Rank movement and milestone celebration only ever arrive after this resolves — Results
    // renders immediately either way and picks them up via a re-render once they land.
    setRankMovement(null);
    setMilestone(null);
    if (playerId) {
      void (async () => {
        await LeaderboardService.submitScore({ playerId, nickname, score: summary.score, knowledgeIQ: summary.knowledgeIQ });
        const newRank = await LeaderboardService.getMyRank("allTime", playerId);
        if (!newRank) return;

        const previousRank = LocalStorageService.getLastKnownRank("allTime");
        LocalStorageService.setLastKnownRank("allTime", newRank.rank);
        if (previousRank !== null && previousRank !== newRank.rank) {
          setRankMovement({ from: previousRank, to: newRank.rank });
        }

        const celebrated = MilestoneService.checkAndMark("allTime", newRank.rank);
        if (celebrated !== null) {
          setMilestone({ threshold: celebrated, rank: newRank.rank });
          soundManager.playMilestone();
          hapticsManager.milestone();
        }
      })();
    }

    // KR drifts a small, capped step toward this run's own implied rating — see RatingEngine.nudgeRating.
    const currentRating = rating ?? LocalStorageService.getRating();
    if (currentRating !== null) {
      const nextRating = nudgeRating(currentRating, summary.knowledgeIQ);
      LocalStorageService.setRating(nextRating);
      setRating(nextRating);
      if (playerId) void playerService.syncRating(playerId, nextRating);
    }

    soundManager.playComplete(summary.correctCount === summary.totalQuestions);
    hapticsManager.complete();

    setChallengeComparison(activeChallenge);
    setActiveChallenge(null);
    setIsNewRecord(newRecord);
    setLastSummary(summary);
    setOverlay("RESULTS");
  }

  function handlePlayAgain(): void {
    if (!runConfig) return;
    if (challengeComparison) {
      // A rematch replays the same mode/category with a fresh seed — not the old challenge's exact set, which is already spoiled.
      setChallengeComparison(null);
      startRun(runConfig.mode, { level: runConfig.level, categoryId: runConfig.categoryId });
      return;
    }
    if (runConfig.mode === "LEVEL" || runConfig.mode === "ENDLESS") {
      startRun("LEVEL", { level: currentLevel });
      return;
    }
    if (runConfig.mode === "DAILY") {
      startRun("DAILY", { seed: runConfig.seed });
      return;
    }
    startRun(runConfig.mode, { categoryId: runConfig.categoryId });
  }

  function handleBackToHome(): void {
    setOverlay(null);
    setTab("HOME");
  }

  if (!nicknameConfirmed) {
    if (onboardingStage === "ENTRY") return <EntryScreen onStart={() => setOnboardingStage("PLACEMENT")} />;
    if (onboardingStage === "PLACEMENT") return <PlacementQuizScreen onComplete={handlePlacementComplete} />;
    if (onboardingStage === "PLACEMENT_RESULT" && placementSummary) {
      return <PlacementResultScreen summary={placementSummary} onContinue={handleClaimPlacement} />;
    }
    return <WelcomeScreen initialNickname={nickname} onConfirm={handleConfirmNickname} />;
  }

  if (challengeScreenVisible) {
    if (incomingChallenge === undefined) return null;
    return <ChallengeScreen challenge={incomingChallenge} onPlay={handlePlayIncomingChallenge} onDismiss={handleDismissChallenge} />;
  }

  if (overlay === "QUIZ" && runConfig) {
    return <QuizScreen mode={runConfig.mode} seed={runConfig.seed} level={runConfig.level} categoryId={runConfig.categoryId} onComplete={handleComplete} onQuit={handleBackToHome} />;
  }

  if (overlay === "CHAMPIONSHIP" && playerId) {
    return <ChampionshipScreen playerId={playerId} nickname={nickname} rating={rating} onExit={handleBackToHome} />;
  }

  if (overlay === "RESULTS" && lastSummary) {
    return (
      <ResultsScreen
        summary={lastSummary}
        isNewRecord={isNewRecord}
        challengeComparison={challengeComparison}
        rankMovement={rankMovement}
        milestone={milestone}
        onPlayAgain={handlePlayAgain}
        onBackToStart={handleBackToHome}
        onChallengeFriend={() => void handleChallengeFriend()}
      />
    );
  }

  return (
    <div className="app-shell">
      <div className="app-shell__content">
        {tab === "HOME" && (
          <HomeScreen
            nickname={nickname}
            bests={bests}
            rating={rating}
            currentLevel={currentLevel}
            onPlayDaily={handlePlayDaily}
            onPlayQuick={handlePlayQuick}
            onPlayLevel={handlePlayLevel}
            onOpenCategory={handleOpenCategory}
            onOpenDiscover={() => setTab("DISCOVER")}
          />
        )}
        {tab === "DISCOVER" && <DiscoverScreen onOpenCategory={handleOpenCategory} />}
        {tab === "PLAY" && (
          <PlayScreen
            onPlayQuick={handlePlayQuick}
            onPlayDaily={handlePlayDaily}
            onPlayLevel={handlePlayLevel}
            onOpenDiscover={() => setTab("DISCOVER")}
            onChallengeFriend={handlePlayQuick}
            onPlayChampionship={handlePlayChampionship}
          />
        )}
        {tab === "RANKING" && <RankingScreen playerId={playerId} bests={bests} />}
        {tab === "PROFILE" && <ProfileScreen nickname={nickname} bests={bests} rating={rating} onChangeNickname={handleChangeNickname} />}
      </div>
      <BottomNav active={tab} onNavigate={setTab} />
    </div>
  );
}
