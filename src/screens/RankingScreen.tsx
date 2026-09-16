import { useEffect, useState } from "react";
import { useLanguage } from "@/i18n/LanguageContext";
import { useLeaderboard } from "@/hooks/useLeaderboard";
import { LeaderboardService, type LeaderboardScope, type MyRank } from "@/services/LeaderboardService";
import type { LocalBests } from "@/storage/LocalStorage";

export interface RankingScreenProps {
  playerId: string;
  bests: LocalBests;
}

const SCOPES: readonly { value: LeaderboardScope; key: "ranking.allTime" | "ranking.daily" | "ranking.weekly" }[] = [
  { value: "allTime", key: "ranking.allTime" },
  { value: "daily", key: "ranking.daily" },
  { value: "weekly", key: "ranking.weekly" },
];

const PODIUM_MEDALS = ["🥇", "🥈", "🥉"];

/**
 * Live leaderboard — subscribed via Firestore onSnapshot (see
 * useLeaderboard/LeaderboardService), so every entry updates instantly for
 * everyone watching, with no refresh or polling. Real players, real scores
 * only; an empty list is shown honestly rather than with placeholder rows.
 */
export function RankingScreen({ playerId, bests }: RankingScreenProps) {
  const { t } = useLanguage();
  const [scope, setScope] = useState<LeaderboardScope>("allTime");
  const { entries, loading } = useLeaderboard(scope);
  const [myRank, setMyRank] = useState<MyRank | null>(null);

  const isInVisibleList = entries.some((e) => e.playerId === playerId);

  useEffect(() => {
    setMyRank(null);
    if (isInVisibleList || !playerId) return;
    void LeaderboardService.getMyRank(scope, playerId).then(setMyRank);
    // Only re-check when the scope changes or the visible list stops/starts containing us.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [scope, isInVisibleList, playerId]);

  const podium = entries.slice(0, 3);
  const rest = entries.slice(3);
  // Visual order: #2, #1, #3 — the classic podium layout.
  const podiumOrder = podium.length === 3 ? [podium[1]!, podium[0]!, podium[2]!] : podium;

  return (
    <div id="ranking-screen">
      <h1 className="page-title">{t("ranking.title")}</h1>

      <div className="tab-row">
        {SCOPES.map((s) => (
          <button key={s.value} className={scope === s.value ? "tab--active" : ""} onClick={() => setScope(s.value)}>
            {t(s.key)}
          </button>
        ))}
      </div>

      {!loading && entries.length === 0 && (
        <div className="empty-state">
          <div className="empty-state__icon">🏆</div>
          <p>{t("ranking.emptyLive")}</p>
        </div>
      )}

      {podium.length === 3 && (
        <div className="podium">
          {podiumOrder.map((entry) => {
            const rank = podium.indexOf(entry) + 1;
            const isMe = entry.playerId === playerId;
            return (
              <div key={entry.playerId} className={`podium__slot podium__slot--${rank}${isMe ? " podium__slot--me" : ""}`}>
                <span className="podium__medal">{PODIUM_MEDALS[rank - 1]}</span>
                <span className="podium__name">{entry.nickname}</span>
                <span className="podium__score">{entry.score.toLocaleString("en-US")}</span>
              </div>
            );
          })}
        </div>
      )}

      {(podium.length === 3 ? rest : entries).length > 0 && (
        <div className="leaderboard-list">
          {(podium.length === 3 ? rest : entries).map((entry, index) => {
            const isMe = entry.playerId === playerId;
            const rank = podium.length === 3 ? index + 4 : index + 1;
            return (
              <div key={entry.playerId} className={`leaderboard-row${isMe ? " leaderboard-row--me" : ""}`}>
                <span className="leaderboard-row__rank">{rank}</span>
                <span className="leaderboard-row__name">
                  {entry.nickname}
                  {isMe && <span className="accent-chip accent-emerald" style={{ marginLeft: 6 }}>{t("ranking.you")}</span>}
                </span>
                <span className="leaderboard-row__score">{entry.score.toLocaleString("en-US")}</span>
              </div>
            );
          })}
        </div>
      )}

      {myRank && (
        <div className="leaderboard-row leaderboard-row--me" style={{ marginBottom: 18 }}>
          <span className="leaderboard-row__rank">{myRank.rank}</span>
          <span className="leaderboard-row__name">{t("ranking.you")}</span>
          <span className="leaderboard-row__score">{myRank.score.toLocaleString("en-US")}</span>
        </div>
      )}

      {bests.totalQuizzesPlayed > 0 && (
        <>
          <div className="section-heading">
            <h2>{t("home.yourBest")}</h2>
          </div>
          <div className="stat-grid">
            <div className="stat-card">
              <div className="stat-card__label">{t("ranking.bestScore")}</div>
              <div className="stat-card__value">{bests.bestScore.toLocaleString("en-US")}</div>
            </div>
            <div className="stat-card">
              <div className="stat-card__label">{t("ranking.bestIQ")}</div>
              <div className="stat-card__value">{bests.bestKnowledgeIQ}</div>
            </div>
          </div>
        </>
      )}
    </div>
  );
}
