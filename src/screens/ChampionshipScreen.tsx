import { useEffect, useState } from "react";
import { useLanguage } from "@/i18n/LanguageContext";
import { useChampionshipRound } from "@/hooks/useChampionshipRound";
import { ChampionshipService, MAX_PLAYERS } from "@/services/ChampionshipService";
import { ChampionshipLobbyScreen } from "@/screens/ChampionshipLobbyScreen";
import { ChampionshipQuizScreen } from "@/screens/ChampionshipQuizScreen";
import { ChampionshipResultsScreen } from "@/screens/ChampionshipResultsScreen";
import { GAME_NAME } from "@/branding";
import type { CategoryId } from "@/types";

export interface ChampionshipScreenProps {
  playerId: string;
  nickname: string;
  categoryId?: CategoryId;
  onExit: () => void;
}

/** Top-level orchestrator: resolves a lobby via matchmaking, then hands off to the round hook and renders whichever phase it reports. */
export function ChampionshipScreen({ playerId, nickname, categoryId, onExit }: ChampionshipScreenProps) {
  const { t } = useLanguage();
  const [matchKey, setMatchKey] = useState(0);
  const [lobbyId, setLobbyId] = useState<string | null>(null);
  const [matchmakingFailed, setMatchmakingFailed] = useState(false);

  useEffect(() => {
    let cancelled = false;
    setLobbyId(null);
    setMatchmakingFailed(false);
    ChampionshipService.findOrCreateLobby(playerId, nickname, categoryId)
      .then((id) => {
        if (!cancelled) setLobbyId(id);
      })
      .catch(() => {
        if (!cancelled) setMatchmakingFailed(true);
      });
    return () => {
      cancelled = true;
    };
  }, [playerId, nickname, categoryId, matchKey]);

  if (matchmakingFailed) {
    return (
      <div className="screen screen--start" id="championship-matchmaking-failed">
        <h1 className="title">{GAME_NAME}</h1>
        <p className="page-subtitle">{t("championship.notFound")}</p>
        <button className="btn btn--ghost" onClick={onExit}>{t("results.backToHome")}</button>
      </div>
    );
  }

  if (!lobbyId) {
    return (
      <div className="screen screen--start" id="championship-matchmaking-screen">
        <h1 className="title">{GAME_NAME}</h1>
        <p className="page-subtitle">{t("championship.waiting")}</p>
        <button className="btn btn--ghost" onClick={onExit}>{t("championship.leaveQueue")}</button>
      </div>
    );
  }

  return <ChampionshipRound key={lobbyId} lobbyId={lobbyId} playerId={playerId} onExit={onExit} onRematch={() => setMatchKey((k) => k + 1)} />;
}

interface ChampionshipRoundProps {
  lobbyId: string;
  playerId: string;
  onExit: () => void;
  onRematch: () => void;
}

function ChampionshipRound({ lobbyId, playerId, onExit, onRematch }: ChampionshipRoundProps) {
  const { language } = useLanguage();
  const { state, submitAnswer, leave } = useChampionshipRound(lobbyId, playerId, language);

  function handleLeaveQueue(): void {
    void leave();
    onExit();
  }

  if (state.phase === "LOADING") return null;

  if (state.phase === "WAITING" || state.phase === "COUNTDOWN") {
    return (
      <ChampionshipLobbyScreen
        phase={state.phase}
        players={state.players}
        maxPlayers={MAX_PLAYERS}
        countdownRemainingMs={state.countdownRemainingMs}
        onLeave={handleLeaveQueue}
      />
    );
  }

  if (state.phase === "PLAYING") {
    return <ChampionshipQuizScreen state={state} playerId={playerId} onSubmit={submitAnswer} onQuit={onExit} />;
  }

  return <ChampionshipResultsScreen players={state.players} playerId={playerId} knowledgeIQ={state.knowledgeIQ} onPlayAgain={onRematch} onBackToHome={onExit} />;
}
