import { AVAILABLE_CATEGORIES } from "@/data/categories";
import { useLanguage } from "@/i18n/LanguageContext";
import { difficultyGroupForLevel, MAX_LEVEL } from "@/quiz/LevelDefinition";
import { tierForRating } from "@/quiz/RatingEngine";
import type { LocalBests } from "@/storage/LocalStorage";
import type { CategoryId } from "@/types";

export interface HomeScreenProps {
  nickname: string;
  bests: LocalBests;
  rating: number | null;
  currentLevel: number;
  onPlayDaily: () => void;
  onPlayQuick: () => void;
  onPlayLevel: () => void;
  onOpenCategory: (id: CategoryId) => void;
  onOpenDiscover: () => void;
  onOpenQotd: () => void;
}

export function HomeScreen({ nickname, bests, rating, currentLevel, onPlayDaily, onPlayQuick, onPlayLevel, onOpenCategory, onOpenDiscover, onOpenQotd }: HomeScreenProps) {
  const { language, t } = useLanguage();

  function levelLabel(level: number): string {
    return level > MAX_LEVEL ? `Endless · Round ${level - MAX_LEVEL}` : `${t("quiz.level")} ${level} · ${difficultyGroupForLevel(level)}`;
  }

  return (
    <div id="home-screen">
      <div className="home-header">
        <h1 className="home-header__greeting">{t("home.greeting", { name: nickname })}</h1>
        {bests.dailyStreak > 0 && (
          <span className="home-header__streak">🔥 {t(bests.dailyStreak === 1 ? "home.dailyStreakDay" : "home.dailyStreakDays", { count: bests.dailyStreak })}</span>
        )}
      </div>

      {rating !== null && (
        <div className="rank-card">
          <p className="rank-card__tier">{tierForRating(rating).label}</p>
          <p className="rank-card__kr">{t("placement.krLabel", { rating: rating.toLocaleString("en-US") })}</p>
        </div>
      )}

      <div className="hero-card">
        <p className="hero-card__eyebrow">{t("home.hero.eyebrow")}</p>
        <h2 className="hero-card__title">{t("home.hero.title")}</h2>
        <p className="hero-card__meta">{t("home.hero.meta")}</p>
        <button className="btn btn--primary" onClick={onPlayDaily}>{t("home.hero.cta")}</button>
      </div>

      <div className="quick-actions">
        <button className="quick-action" onClick={onPlayQuick}>
          <span className="quick-action__icon">⚡</span>
          {t("home.quickPlay")}
        </button>
        <button className="quick-action" onClick={onPlayLevel}>
          <span className="quick-action__icon">🗺️</span>
          {t("home.levelJourney")}
        </button>
        <button className="quick-action" onClick={onOpenDiscover}>
          <span className="quick-action__icon">🧭</span>
          {t("home.categories")}
        </button>
      </div>

      <button className="card" style={{ width: "100%", textAlign: "left", cursor: "pointer" }} onClick={onOpenQotd}>
        <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between" }}>
          <div>
            <p style={{ margin: 0, fontWeight: 800, fontSize: 14 }}>❓ {t("qotd.homeTitle")}</p>
            <p style={{ margin: "2px 0 0", fontSize: 12, color: "var(--ink-dim)" }}>{t("qotd.homeDesc")}</p>
          </div>
          <span className="accent-chip accent-amber">{t("home.hero.cta")}</span>
        </div>
      </button>

      <div className="section-heading">
        <h2>{t("home.categories")}</h2>
        <button className="link" onClick={onOpenDiscover}>{t("home.seeAll")}</button>
      </div>
      <div className="category-row">
        {AVAILABLE_CATEGORIES.map((category) => (
          <button key={category.id} className="category-pill" onClick={() => onOpenCategory(category.id)}>
            <span>{category.emoji}</span>
            {category.label[language]}
          </button>
        ))}
      </div>

      <div className="section-heading">
        <h2>{t("home.continue")}</h2>
      </div>
      <button className="card" style={{ width: "100%", textAlign: "left", cursor: "pointer" }} onClick={onPlayLevel}>
        <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between" }}>
          <div>
            <p style={{ margin: 0, fontWeight: 800, fontSize: 14 }}>{t("home.levelJourney")}</p>
            <p style={{ margin: "2px 0 0", fontSize: 12, color: "var(--ink-dim)" }}>{levelLabel(currentLevel)}</p>
          </div>
          <span className="accent-chip accent-emerald">{t("home.continueCta")}</span>
        </div>
      </button>

      {bests.totalQuizzesPlayed > 0 && (
        <>
          <div className="section-heading">
            <h2>{t("home.yourBest")}</h2>
          </div>
          <div className="stat-grid">
            <div className="stat-card">
              <div className="stat-card__label">{t("home.bestScore")}</div>
              <div className="stat-card__value">{bests.bestScore.toLocaleString("en-US")}</div>
            </div>
            <div className="stat-card">
              <div className="stat-card__label">{t("home.bestIQ")}</div>
              <div className="stat-card__value">{bests.bestKnowledgeIQ}</div>
            </div>
          </div>
        </>
      )}
    </div>
  );
}
