import { useLanguage } from "@/i18n/LanguageContext";
import { GAME_NAME } from "@/branding";
import type { ChallengeDoc } from "@/services/ChallengeService";

export interface ChallengeScreenProps {
  challenge: ChallengeDoc | null;
  onPlay: () => void;
  onDismiss: () => void;
}

/** Landing screen for a shared `/c/{id}` link — see ChallengeService. No account needed to view or play it. */
export function ChallengeScreen({ challenge, onPlay, onDismiss }: ChallengeScreenProps) {
  const { t } = useLanguage();

  return (
    <div className="screen screen--start" id="challenge-screen">
      <h1 className="title">{GAME_NAME}</h1>

      {challenge ? (
        <>
          <p style={{ fontWeight: 800, fontSize: 18, margin: "10px 0 0" }}>{t("challenge.heading", { name: challenge.creatorNickname })}</p>
          <div className="results-iq-card" style={{ marginTop: 14 }}>
            <p className="results-iq-card__label">{t("challenge.theirScore")}</p>
            <p className="results-iq-card__value">{challenge.creatorScore.toLocaleString("en-US")}</p>
          </div>
          <button className="btn btn--primary" style={{ marginTop: 14 }} onClick={onPlay}>{t("challenge.playCta")}</button>
        </>
      ) : (
        <p className="page-subtitle" style={{ marginTop: 10 }}>{t("challenge.notFound")}</p>
      )}

      <button className="btn btn--ghost" onClick={onDismiss}>{t("results.backToHome")}</button>
    </div>
  );
}
