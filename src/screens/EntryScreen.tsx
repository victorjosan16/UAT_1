import { useLanguage } from "@/i18n/LanguageContext";
import { GAME_NAME } from "@/branding";

export interface EntryScreenProps {
  onStart: () => void;
}

/**
 * First thing a brand-new player ever sees — no login, no registration, no
 * tutorial slides, nothing to configure. One question, one button; the
 * next tap lands them on Placement Quiz question 1 (see MASTER PROMPT §2).
 */
export function EntryScreen({ onStart }: EntryScreenProps) {
  const { t } = useLanguage();

  return (
    <div className="screen screen--start" id="entry-screen">
      <h1 className="title">{GAME_NAME}</h1>
      <p style={{ fontWeight: 900, fontSize: 26, margin: "20px 0 4px", letterSpacing: 0.2 }}>{t("entry.heading")}</p>
      <p className="page-subtitle" style={{ marginBottom: 4 }}>{t("entry.meta")}</p>
      <button className="btn btn--primary" style={{ marginTop: 18 }} onClick={onStart}>{t("entry.cta")}</button>
      <p className="page-subtitle" style={{ marginTop: 12, fontSize: 12 }}>{t("entry.noAccount")}</p>
    </div>
  );
}
