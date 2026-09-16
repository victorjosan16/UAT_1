import { useLanguage } from "@/i18n/LanguageContext";
import type { ChampionshipPlayer } from "@/services/ChampionshipService";
import { GAME_NAME } from "@/branding";

export interface ChampionshipLobbyScreenProps {
  phase: "WAITING" | "COUNTDOWN";
  players: readonly ChampionshipPlayer[];
  maxPlayers: number;
  countdownRemainingMs: number;
  onLeave: () => void;
}

export function ChampionshipLobbyScreen({ phase, players, maxPlayers, countdownRemainingMs, onLeave }: ChampionshipLobbyScreenProps) {
  const { t } = useLanguage();

  if (phase === "COUNTDOWN") {
    const seconds = Math.max(1, Math.ceil(countdownRemainingMs / 1000));
    return (
      <div className="screen screen--start" id="championship-countdown-screen">
        <h1 className="title">{GAME_NAME}</h1>
        <p className="page-subtitle">{t("championship.ready")}</p>
        <div className="championship-countdown">{seconds}</div>
      </div>
    );
  }

  return (
    <div id="championship-lobby-screen">
      <h1 className="page-title">{t("championship.waitingRoom")}</h1>
      <p className="page-subtitle">{t("championship.waiting")}</p>

      <div className="championship-players">
        {players.map((player) => (
          <div key={player.playerId} className="championship-player-chip">
            <span className="championship-player-chip__avatar">{player.nickname.charAt(0).toUpperCase()}</span>
            <span>{player.nickname}</span>
          </div>
        ))}
        {Array.from({ length: Math.max(0, maxPlayers - players.length) }).map((_, i) => (
          <div key={`empty-${i}`} className="championship-player-chip championship-player-chip--empty">
            <span className="championship-player-chip__avatar">?</span>
          </div>
        ))}
      </div>

      <p className="page-subtitle" style={{ textAlign: "center" }}>{players.length} / {maxPlayers}</p>

      <button className="btn btn--ghost" onClick={onLeave}>{t("championship.leaveQueue")}</button>
    </div>
  );
}
