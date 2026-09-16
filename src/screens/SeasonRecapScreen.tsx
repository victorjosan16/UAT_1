import { useLanguage } from "@/i18n/LanguageContext";
import { tierForRating } from "@/quiz/RatingEngine";
import { seasonMonthLabel, seasonNumberFor } from "@/utils/season";
import { GAME_NAME } from "@/branding";

export interface SeasonRecapScreenProps {
  monthKey: string;
  rank: number;
  rating: number | null;
  onContinue: () => void;
}

/** Shown once, right when a new Season starts, recapping the player's final standing in the one that just ended — see MASTER PROMPT §20. Never blocks play beyond one tap. */
export function SeasonRecapScreen({ monthKey, rank, rating, onContinue }: SeasonRecapScreenProps) {
  const { language, t } = useLanguage();
  const tier = rating !== null ? tierForRating(rating) : null;

  return (
    <div className="results-screen" id="season-recap-screen">
      <h1 className="title" style={{ fontSize: 22 }}>{GAME_NAME}</h1>
      <span className="accent-chip accent-coral">{t("season.complete")}</span>
      <p className="page-subtitle">
        {t("ranking.season", { number: String(seasonNumberFor(monthKey)).padStart(2, "0") })} · {seasonMonthLabel(monthKey, language)}
      </p>

      <div className="results-iq-card">
        <p className="results-iq-card__label">{t("season.globalPosition")}</p>
        <p className="results-iq-card__value">#{rank.toLocaleString("en-US")}</p>
        {tier && <p className="results-iq-card__rank">{tier.label}</p>}
      </div>

      <button className="btn btn--primary" onClick={onContinue}>{t("season.continue")}</button>
    </div>
  );
}
