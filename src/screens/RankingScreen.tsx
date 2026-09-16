import { useState } from "react";
import { useLanguage } from "@/i18n/LanguageContext";
import { useLeaderboard } from "@/hooks/useLeaderboard";
import type { LeaderboardScope } from "@/services/LeaderboardService";
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

      {entries.length > 0 && (
        <div className="leaderboard-list">
          {entries.map((entry, index) => {
            const isMe = entry.playerId === playerId;
            return (
              <div key={entry.playerId} className={`leaderboard-row${isMe ? " leaderboard-row--me" : ""}`}>
                <span className="leaderboard-row__rank">{index + 1}</span>
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
