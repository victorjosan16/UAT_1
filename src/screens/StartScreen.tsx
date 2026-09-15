import { GAME_NAME, GAME_SUBTITLE, TAGLINES } from "@/branding";
import { LocalStorageService } from "@/storage/LocalStorage";
import { difficultyGroupForLevel, MAX_LEVEL } from "@/quiz/LevelDefinition";

export interface StartScreenProps {
  onPlay: () => void;
}

export function StartScreen({ onPlay }: StartScreenProps) {
  const bests = LocalStorageService.getLocalBests();
  const currentLevel = LocalStorageService.getCurrentLevel();
  const isEndless = currentLevel > MAX_LEVEL;
  const levelLabel = isEndless ? `ENDLESS — ROUND ${currentLevel - MAX_LEVEL}` : `LEVEL ${currentLevel} · ${difficultyGroupForLevel(currentLevel)}`;

  return (
    <div className="screen" id="start-screen">
      <h1 className="title">{GAME_NAME}</h1>
      <p className="subtitle">{GAME_SUBTITLE}</p>

      {bests.totalQuizzesPlayed > 0 && (
        <div className="stat-grid">
          <div className="stat-card">
            <div className="stat-card__label">BEST SCORE</div>
            <div className="stat-card__value">{bests.bestScore.toLocaleString("en-US")}</div>
          </div>
          <div className="stat-card">
            <div className="stat-card__label">BEST FOOTBALL IQ</div>
            <div className="stat-card__value">{bests.bestFootballIQ}</div>
          </div>
        </div>
      )}

      <p className="subtitle" style={{ marginTop: 4 }}>
        {levelLabel}
      </p>

      <button className="btn btn--primary" onClick={onPlay}>
        {bests.totalQuizzesPlayed > 0 ? "PLAY" : TAGLINES.tap}
      </button>
    </div>
  );
}
