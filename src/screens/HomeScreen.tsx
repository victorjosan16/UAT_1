import { AVAILABLE_CATEGORIES } from "@/data/categories";
import { difficultyGroupForLevel, MAX_LEVEL } from "@/quiz/LevelDefinition";
import type { LocalBests } from "@/storage/LocalStorage";
import type { CategoryId } from "@/types";

export interface HomeScreenProps {
  nickname: string;
  bests: LocalBests;
  currentLevel: number;
  onPlayDaily: () => void;
  onPlayQuick: () => void;
  onPlayLevel: () => void;
  onOpenCategory: (id: CategoryId) => void;
  onOpenDiscover: () => void;
}

function levelLabel(level: number): string {
  return level > MAX_LEVEL ? `Endless · Round ${level - MAX_LEVEL}` : `Level ${level} · ${difficultyGroupForLevel(level)}`;
}

export function HomeScreen({ nickname, bests, currentLevel, onPlayDaily, onPlayQuick, onPlayLevel, onOpenCategory, onOpenDiscover }: HomeScreenProps) {
  return (
    <div id="home-screen">
      <div className="home-header">
        <h1 className="home-header__greeting">Hi, {nickname}</h1>
        {bests.dailyStreak > 0 && <span className="home-header__streak">🔥 {bests.dailyStreak} day{bests.dailyStreak === 1 ? "" : "s"}</span>}
      </div>

      <div className="hero-card">
        <p className="hero-card__eyebrow">DAILY CHALLENGE</p>
        <h2 className="hero-card__title">10 Questions, Once a Day</h2>
        <p className="hero-card__meta">Same set for everyone today — see how you stack up.</p>
        <button className="btn btn--primary" onClick={onPlayDaily}>PLAY NOW</button>
      </div>

      <div className="quick-actions">
        <button className="quick-action" onClick={onPlayQuick}>
          <span className="quick-action__icon">⚡</span>
          Quick Play
        </button>
        <button className="quick-action" onClick={onPlayLevel}>
          <span className="quick-action__icon">🗺️</span>
          Level Journey
        </button>
        <button className="quick-action" onClick={onOpenDiscover}>
          <span className="quick-action__icon">🧭</span>
          Categories
        </button>
      </div>

      <div className="section-heading">
        <h2>Categories</h2>
        <button className="link" onClick={onOpenDiscover}>See all</button>
      </div>
      <div className="category-row">
        {AVAILABLE_CATEGORIES.map((category) => (
          <button key={category.id} className="category-pill" onClick={() => onOpenCategory(category.id)}>
            <span>{category.emoji}</span>
            {category.label}
          </button>
        ))}
      </div>

      <div className="section-heading">
        <h2>Continue</h2>
      </div>
      <button className="card" style={{ width: "100%", textAlign: "left", cursor: "pointer" }} onClick={onPlayLevel}>
        <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between" }}>
          <div>
            <p style={{ margin: 0, fontWeight: 800, fontSize: 14 }}>Level Journey</p>
            <p style={{ margin: "2px 0 0", fontSize: 12, color: "var(--ink-dim)" }}>{levelLabel(currentLevel)}</p>
          </div>
          <span className="accent-chip accent-emerald">CONTINUE</span>
        </div>
      </button>

      {bests.totalQuizzesPlayed > 0 && (
        <>
          <div className="section-heading">
            <h2>Your Best</h2>
          </div>
          <div className="stat-grid">
            <div className="stat-card">
              <div className="stat-card__label">Best Score</div>
              <div className="stat-card__value">{bests.bestScore.toLocaleString("en-US")}</div>
            </div>
            <div className="stat-card">
              <div className="stat-card__label">Best Q5 IQ</div>
              <div className="stat-card__value">{bests.bestKnowledgeIQ}</div>
            </div>
          </div>
        </>
      )}
    </div>
  );
}
