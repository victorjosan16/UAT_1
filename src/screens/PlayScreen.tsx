import { useLanguage } from "@/i18n/LanguageContext";

export interface PlayScreenProps {
  onPlayQuick: () => void;
  onPlayDaily: () => void;
  onPlayLevel: () => void;
  onOpenDiscover: () => void;
  onChallengeFriend: () => void;
}

export function PlayScreen({ onPlayQuick, onPlayDaily, onPlayLevel, onOpenDiscover, onChallengeFriend }: PlayScreenProps) {
  const { t } = useLanguage();

  return (
    <div id="play-screen">
      <h1 className="page-title">{t("play.title")}</h1>
      <p className="page-subtitle">{t("play.subtitle")}</p>

      <button className="mode-card" onClick={onPlayQuick}>
        <div className="mode-card__icon accent-emerald">⚡</div>
        <div>
          <p className="mode-card__title">{t("play.quickPlay.title")}</p>
          <p className="mode-card__desc">{t("play.quickPlay.desc")}</p>
        </div>
      </button>

      <button className="mode-card" onClick={onOpenDiscover}>
        <div className="mode-card__icon accent-cyan">🧭</div>
        <div>
          <p className="mode-card__title">{t("play.category.title")}</p>
          <p className="mode-card__desc">{t("play.category.desc")}</p>
        </div>
      </button>

      <button className="mode-card" onClick={onPlayDaily}>
        <div className="mode-card__icon accent-amber">📅</div>
        <div>
          <p className="mode-card__title">{t("play.daily.title")}</p>
          <p className="mode-card__desc">{t("play.daily.desc")}</p>
        </div>
      </button>

      <button className="mode-card" onClick={onPlayLevel}>
        <div className="mode-card__icon accent-violet">🗺️</div>
        <div>
          <p className="mode-card__title">{t("play.level.title")}</p>
          <p className="mode-card__desc">{t("play.level.desc")}</p>
        </div>
      </button>

      <button className="mode-card mode-card--locked" disabled>
        <div className="mode-card__icon accent-coral">🏆</div>
        <div>
          <p className="mode-card__title">{t("play.championship.title")}</p>
          <p className="mode-card__desc">{t("play.championship.desc")}</p>
        </div>
      </button>

      <button className="mode-card" onClick={onChallengeFriend}>
        <div className="mode-card__icon accent-emerald">🤝</div>
        <div>
          <p className="mode-card__title">{t("play.friend.title")}</p>
          <p className="mode-card__desc">{t("play.friend.desc")}</p>
        </div>
      </button>
    </div>
  );
}
