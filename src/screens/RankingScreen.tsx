import { useLanguage } from "@/i18n/LanguageContext";
import type { LocalBests } from "@/storage/LocalStorage";

export interface RankingScreenProps {
  bests: LocalBests;
}

/**
 * Online leaderboards (Daily/Weekly/Monthly/All-time, per MASTER PROMPT
 * §26) need a server-authoritative score store, which this iteration
 * intentionally deferred (solo modes first — see task #36). Showing only
 * the player's own real local stats here, with an honest "coming soon"
 * note, is safer than any placeholder ranking: no fake players or scores
 * ever appear, per §28.
 */
export function RankingScreen({ bests }: RankingScreenProps) {
  const { t } = useLanguage();

  return (
    <div id="ranking-screen">
      <h1 className="page-title">{t("ranking.title")}</h1>
      <p className="page-subtitle">{t("ranking.subtitle")}</p>

      {bests.totalQuizzesPlayed === 0 ? (
        <div className="empty-state">
          <div className="empty-state__icon">🏆</div>
          <p>{t("ranking.emptyText")}</p>
        </div>
      ) : (
        <div className="stat-grid">
          <div className="stat-card">
            <div className="stat-card__label">{t("ranking.bestScore")}</div>
            <div className="stat-card__value">{bests.bestScore.toLocaleString("en-US")}</div>
          </div>
          <div className="stat-card">
            <div className="stat-card__label">{t("ranking.bestIQ")}</div>
            <div className="stat-card__value">{bests.bestKnowledgeIQ}</div>
          </div>
          <div className="stat-card">
            <div className="stat-card__label">{t("ranking.bestStreak")}</div>
            <div className="stat-card__value">🔥 ×{bests.bestStreak}</div>
          </div>
          <div className="stat-card">
            <div className="stat-card__label">{t("ranking.runsPlayed")}</div>
            <div className="stat-card__value">{bests.totalQuizzesPlayed}</div>
          </div>
        </div>
      )}
    </div>
  );
}
