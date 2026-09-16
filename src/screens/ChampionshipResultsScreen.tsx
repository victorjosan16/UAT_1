import { useLanguage } from "@/i18n/LanguageContext";
import { knowledgeRankLabel, rankForKnowledgeIQ } from "@/quiz/QuizIQ";
import type { ChampionshipPlayer } from "@/services/ChampionshipService";

export interface ChampionshipResultsScreenProps {
  players: readonly ChampionshipPlayer[];
  playerId: string;
  knowledgeIQ: number;
  onPlayAgain: () => void;
  onBackToHome: () => void;
}

const PODIUM_MEDALS = ["🥇", "🥈", "🥉"];

export function ChampionshipResultsScreen({ players, playerId, knowledgeIQ, onPlayAgain, onBackToHome }: ChampionshipResultsScreenProps) {
  const { language, t } = useLanguage();
  const rank = knowledgeRankLabel(rankForKnowledgeIQ(knowledgeIQ), language);

  const podium = players.slice(0, 3);
  const rest = players.slice(3);
  const podiumOrder = podium.length === 3 ? [podium[1]!, podium[0]!, podium[2]!] : podium;
  const won = players[0]?.playerId === playerId;

  return (
    <div className="results-screen" id="championship-results-screen">
      <span className="accent-chip accent-coral">{t("championship.matchComplete")}</span>
      {won && <span className="accent-chip accent-amber">{t("challenge.youWin")}</span>}

      <div className="results-iq-card">
        <p className="results-iq-card__label">Q5 IQ</p>
        <p className="results-iq-card__value">{knowledgeIQ}</p>
        <p className="results-iq-card__rank">{rank}</p>
      </div>

      {podium.length === 3 && (
        <div className="podium">
          {podiumOrder.map((player) => {
            const place = podium.indexOf(player) + 1;
            const isMe = player.playerId === playerId;
            return (
              <div key={player.playerId} className={`podium__slot podium__slot--${place}${isMe ? " podium__slot--me" : ""}`}>
                <span className="podium__medal">{PODIUM_MEDALS[place - 1]}</span>
                <span className="podium__name">{player.nickname}</span>
                <span className="podium__score">{player.score.toLocaleString("en-US")}</span>
              </div>
            );
          })}
        </div>
      )}

      <div className="leaderboard-list" style={{ width: "100%" }}>
        {(podium.length === 3 ? rest : players).map((player, index) => {
          const isMe = player.playerId === playerId;
          const rankNumber = podium.length === 3 ? index + 4 : index + 1;
          return (
            <div key={player.playerId} className={`leaderboard-row${isMe ? " leaderboard-row--me" : ""}`}>
              <span className="leaderboard-row__rank">{rankNumber}</span>
              <span className="leaderboard-row__name">
                {player.nickname}
                {isMe && <span className="accent-chip accent-emerald" style={{ marginLeft: 6 }}>{t("ranking.you")}</span>}
              </span>
              <span className="leaderboard-row__score">{player.score.toLocaleString("en-US")}</span>
            </div>
          );
        })}
      </div>

      <button className="btn btn--primary" onClick={onPlayAgain}>{t("championship.newMatch")}</button>
      <button className="btn btn--ghost" onClick={onBackToHome}>{t("results.backToHome")}</button>
    </div>
  );
}
