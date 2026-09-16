import { useState } from "react";
import type { LocalBests } from "@/storage/LocalStorage";

export interface ProfileScreenProps {
  nickname: string;
  bests: LocalBests;
}

type Tab = "STATS" | "BADGES";

const BADGE_SLOTS = ["FIRST WIN", "PERFECT 10", "ON FIRE", "7 DAY STREAK", "1000 QUESTIONS", "CHAMPION"];

export function ProfileScreen({ nickname, bests }: ProfileScreenProps) {
  const [tab, setTab] = useState<Tab>("STATS");

  return (
    <div id="profile-screen">
      <div className="profile-header">
        <div className="profile-avatar">{nickname.slice(0, 1).toUpperCase()}</div>
        <h1 className="page-title" style={{ marginBottom: 0 }}>{nickname}</h1>
        <p className="page-subtitle" style={{ marginBottom: 0 }}>Guest player</p>
      </div>

      <div className="tab-row">
        <button className={tab === "STATS" ? "tab--active" : ""} onClick={() => setTab("STATS")}>Stats</button>
        <button className={tab === "BADGES" ? "tab--active" : ""} onClick={() => setTab("BADGES")}>Badges</button>
      </div>

      {tab === "STATS" && (
        <div className="stat-grid">
          <div className="stat-card">
            <div className="stat-card__label">Runs Played</div>
            <div className="stat-card__value">{bests.totalQuizzesPlayed}</div>
          </div>
          <div className="stat-card">
            <div className="stat-card__label">Best Score</div>
            <div className="stat-card__value">{bests.bestScore.toLocaleString("en-US")}</div>
          </div>
          <div className="stat-card">
            <div className="stat-card__label">Best Streak</div>
            <div className="stat-card__value">🔥 ×{bests.bestStreak}</div>
          </div>
          <div className="stat-card">
            <div className="stat-card__label">Best Q5 IQ</div>
            <div className="stat-card__value">{bests.bestKnowledgeIQ}</div>
          </div>
          <div className="stat-card">
            <div className="stat-card__label">Daily Streak</div>
            <div className="stat-card__value">🔥 {bests.dailyStreak}</div>
          </div>
        </div>
      )}

      {tab === "BADGES" && (
        <div className="category-grid">
          {BADGE_SLOTS.map((label) => (
            <div key={label} className="category-card category-card--locked">
              <span className="category-card__badge">Locked</span>
              <span className="category-card__emoji">🔒</span>
              <span className="category-card__label">{label}</span>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
