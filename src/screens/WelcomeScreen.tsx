import { useState } from "react";
import { useLanguage } from "@/i18n/LanguageContext";
import { GAME_NAME, GAME_SUBTITLE } from "@/branding";

export interface WelcomeScreenProps {
  initialNickname: string;
  onConfirm: (nickname: string) => void;
}

/** First-launch gate: pick a nickname (saved to Firestore — see PlayerService) before Home appears. */
export function WelcomeScreen({ initialNickname, onConfirm }: WelcomeScreenProps) {
  const { t } = useLanguage();
  const [value, setValue] = useState(initialNickname);

  function handleSubmit(e: React.FormEvent): void {
    e.preventDefault();
    onConfirm(value);
  }

  return (
    <div className="screen screen--start" id="welcome-screen">
      <h1 className="title">{GAME_NAME}</h1>
      <p className="subtitle">{GAME_SUBTITLE}</p>

      <p style={{ fontWeight: 800, fontSize: 16, margin: "18px 0 2px" }}>{t("welcome.heading")}</p>
      <p className="page-subtitle" style={{ marginBottom: 4 }}>{t("welcome.subtitle")}</p>

      <form onSubmit={handleSubmit} style={{ width: "100%", display: "flex", flexDirection: "column", alignItems: "center", gap: 14 }}>
        <input
          className="input"
          value={value}
          onChange={(e) => setValue(e.target.value)}
          placeholder={t("welcome.placeholder")}
          maxLength={16}
          autoFocus
        />
        <button type="submit" className="btn btn--primary">{t("welcome.cta")}</button>
      </form>
    </div>
  );
}
