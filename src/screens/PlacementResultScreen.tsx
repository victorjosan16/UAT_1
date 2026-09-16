import { useLanguage } from "@/i18n/LanguageContext";
import { tierForRating } from "@/quiz/RatingEngine";
import { GAME_NAME } from "@/branding";
import type { PlacementSummary } from "@/quiz/PlacementEngine";

export interface PlacementResultScreenProps {
  summary: PlacementSummary;
  onContinue: () => void;
}

/** The high-impact reveal after Placement Quiz — see MASTER PROMPT §6. No percentile: this app has no validated sample to back one yet. */
export function PlacementResultScreen({ summary, onContinue }: PlacementResultScreenProps) {
  const { t } = useLanguage();
  const tier = tierForRating(summary.startingRating);

  return (
    <div className="results-screen" id="placement-result-screen">
      <h1 className="title" style={{ fontSize: 22 }}>{GAME_NAME}</h1>
      <p className="page-subtitle">{t("placement.resultHeading")}</p>

      <div className="results-iq-card">
        <p className="results-iq-card__label">{tier.label}</p>
        <p className="results-iq-card__value">{t("placement.krLabel", { rating: summary.startingRating.toLocaleString("en-US") })}</p>
      </div>

      <div className="stat-grid">
        <div className="stat-card">
          <div className="stat-card__label">{t("results.correct")}</div>
          <div className="stat-card__value">{summary.correctCount} / {summary.totalQuestions}</div>
        </div>
        <div className="stat-card">
          <div className="stat-card__label">{t("results.avgResponse")}</div>
          <div className="stat-card__value">{(summary.averageResponseMs / 1000).toFixed(1)}s</div>
        </div>
        <div className="stat-card">
          <div className="stat-card__label">{t("results.bestStreak")}</div>
          <div className="stat-card__value">🔥 ×{summary.bestStreak}</div>
        </div>
      </div>

      <button className="btn btn--primary" onClick={onContinue}>{t("placement.claimCta")}</button>
    </div>
  );
}
