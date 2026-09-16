import { useState } from "react";
import { useLanguage } from "@/i18n/LanguageContext";
import { usePwaInstall } from "@/hooks/usePwaInstall";
import type { StringKey } from "@/i18n/strings";
import { LocalStorageService, type LocalBests } from "@/storage/LocalStorage";
import type { Language } from "@/types";

export interface ProfileScreenProps {
  nickname: string;
  bests: LocalBests;
  onChangeNickname: (nickname: string) => void;
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

export function ProfileScreen({ nickname, bests, onChangeNickname }: ProfileScreenProps) {
  const { language, setLanguage, t } = useLanguage();
  const { canInstall, promptInstall } = usePwaInstall();
  const [tab, setTab] = useState<Tab>("STATS");
  const [editing, setEditing] = useState(false);
  const [draft, setDraft] = useState(nickname);
  const [preferences, setPreferences] = useState(() => LocalStorageService.getPreferences());

  function toggleSound(): void {
    setPreferences(LocalStorageService.updatePreferences({ soundEnabled: !preferences.soundEnabled }));
  }

  function toggleHaptics(): void {
    setPreferences(LocalStorageService.updatePreferences({ hapticsEnabled: !preferences.hapticsEnabled }));
  }

  function startEditing(): void {
    setDraft(nickname);
    setEditing(true);
  }

  function handleSave(e: React.FormEvent): void {
    e.preventDefault();
    onChangeNickname(draft);
    setEditing(false);
  }

  return (
    <div id="profile-screen">
      <div className="profile-header">
        <div className="profile-avatar">{nickname.slice(0, 1).toUpperCase()}</div>
        {editing ? (
          <form onSubmit={handleSave} style={{ display: "flex", flexDirection: "column", alignItems: "center", gap: 10, width: "100%" }}>
            <input className="input" value={draft} onChange={(e) => setDraft(e.target.value)} maxLength={16} autoFocus />
            <div style={{ display: "flex", gap: 8 }}>
              <button type="submit" className="btn btn--primary" style={{ width: "auto", padding: "10px 20px" }}>{t("profile.save")}</button>
              <button type="button" className="btn btn--ghost" style={{ width: "auto" }} onClick={() => setEditing(false)}>{t("profile.cancel")}</button>
            </div>
          </form>
        ) : (
          <>
            <h1 className="page-title" style={{ marginBottom: 0 }}>{nickname}</h1>
            <p className="page-subtitle" style={{ marginBottom: 0 }}>{t("profile.guestPlayer")}</p>
            <button className="btn--ghost" style={{ background: "none", border: "none", fontWeight: 800, fontSize: 12, color: "var(--emerald)", cursor: "pointer", padding: 0 }} onClick={startEditing}>
              {t("profile.editNickname")}
            </button>
          </>
        )}
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

      <div className="settings-row">
        <span className="settings-row__label">{t("profile.sound")}</span>
        <button className={`toggle-switch${preferences.soundEnabled ? " toggle-switch--on" : ""}`} onClick={toggleSound}>
          {preferences.soundEnabled ? t("common.on") : t("common.off")}
        </button>
      </div>
      <div className="settings-row">
        <span className="settings-row__label">{t("profile.haptics")}</span>
        <button className={`toggle-switch${preferences.hapticsEnabled ? " toggle-switch--on" : ""}`} onClick={toggleHaptics}>
          {preferences.hapticsEnabled ? t("common.on") : t("common.off")}
        </button>
      </div>

      {canInstall && (
        <button className="btn btn--secondary" style={{ marginBottom: 16 }} onClick={() => void promptInstall()}>
          📲 {t("profile.installApp")}
        </button>
      )}

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
