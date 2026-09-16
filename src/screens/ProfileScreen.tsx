import { useState } from "react";
import { useLanguage } from "@/i18n/LanguageContext";
import type { StringKey } from "@/i18n/strings";
import type { LocalBests } from "@/storage/LocalStorage";
import type { Language } from "@/types";

export interface ProfileScreenProps {
  nickname: string;
  bests: LocalBests;
}

type Tab = "STATS" | "BADGES";

const BADGE_KEYS: StringKey[] = ["badge.firstWin", "badge.perfect10", "badge.onFire", "badge.sevenDayStreak", "badge.thousandQuestions", "badge.champion"];
const LANGUAGE_OPTIONS: readonly { code: Language; label: string }[] = [
  { code: "en", label: "EN" },
  { code: "ro", label: "RO" },
  { code: "es", label: "ES" },
  { code: "pt", label: "PT" },
  { code: "hi", label: "HI" },
  { code: "id", label: "ID" },
  { code: "ru", label: "RU" },
];

export function ProfileScreen({ nickname, bests }: ProfileScreenProps) {
  const { language, setLanguage, t } = useLanguage();
  const [tab, setTab] = useState<Tab>("STATS");

  return (
    <div id="profile-screen">
      <div className="profile-header">
        <div className="profile-avatar">{nickname.slice(0, 1).toUpperCase()}</div>
        <h1 className="page-title" style={{ marginBottom: 0 }}>{nickname}</h1>
        <p className="page-subtitle" style={{ marginBottom: 0 }}>{t("profile.guestPlayer")}</p>
      </div>

      <div className="section-heading">
        <h2>{t("profile.language")}</h2>
      </div>
      <div className="tab-row">
        {LANGUAGE_OPTIONS.map((option) => (
          <button key={option.code} className={language === option.code ? "tab--active" : ""} onClick={() => setLanguage(option.code)}>
            {option.label}
          </button>
        ))}
      </div>

      <div className="tab-row">
        <button className={tab === "STATS" ? "tab--active" : ""} onClick={() => setTab("STATS")}>{t("profile.stats")}</button>
        <button className={tab === "BADGES" ? "tab--active" : ""} onClick={() => setTab("BADGES")}>{t("profile.badges")}</button>
      </div>

      {tab === "STATS" && (
        <div className="stat-grid">
          <div className="stat-card">
            <div className="stat-card__label">{t("profile.runsPlayed")}</div>
            <div className="stat-card__value">{bests.totalQuizzesPlayed}</div>
          </div>
          <div className="stat-card">
            <div className="stat-card__label">{t("profile.bestScore")}</div>
            <div className="stat-card__value">{bests.bestScore.toLocaleString("en-US")}</div>
          </div>
          <div className="stat-card">
            <div className="stat-card__label">{t("profile.bestStreak")}</div>
            <div className="stat-card__value">🔥 ×{bests.bestStreak}</div>
          </div>
          <div className="stat-card">
            <div className="stat-card__label">{t("profile.bestIQ")}</div>
            <div className="stat-card__value">{bests.bestKnowledgeIQ}</div>
          </div>
          <div className="stat-card">
            <div className="stat-card__label">{t("profile.dailyStreak")}</div>
            <div className="stat-card__value">🔥 {bests.dailyStreak}</div>
          </div>
        </div>
      )}

      {tab === "BADGES" && (
        <div className="category-grid">
          {BADGE_KEYS.map((key) => (
            <div key={key} className="category-card category-card--locked">
              <span className="category-card__badge">{t("profile.locked")}</span>
              <span className="category-card__emoji">🔒</span>
              <span className="category-card__label">{t(key)}</span>
            </div>
          ))}
        </div>
      )}
    </div>
  );
}
