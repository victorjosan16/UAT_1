import { GAME_NAME, GAME_SUBTITLE, TAGLINES } from "@/branding";
import { LocalStorageService } from "@/storage/LocalStorage";
import { difficultyGroupForLevel, MAX_LEVEL } from "@/quiz/LevelDefinition";

export interface StartScreenProps {
  onPlayLogoQuiz: () => void;
  onPlayGuessThePlayer: () => void;
}

function levelLabelFor(level: number): string {
  return level > MAX_LEVEL ? `ENDLESS — ROUND ${level - MAX_LEVEL}` : `LEVEL ${level} · ${difficultyGroupForLevel(level)}`;
}

export function StartScreen({ onPlayLogoQuiz, onPlayGuessThePlayer }: StartScreenProps) {
  const bests = LocalStorageService.getLocalBests();
  const logoLevel = LocalStorageService.getCurrentLevel();
  const playerLevel = LocalStorageService.getCurrentPlayerLevel();

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
        {levelLabelFor(logoLevel)}
      </p>
      <button className="btn btn--primary" onClick={onPlayLogoQuiz}>
        {bests.totalQuizzesPlayed > 0 ? "LOGO QUIZ" : TAGLINES.tap}
      </button>

      <p className="subtitle" style={{ marginTop: 12 }}>
        GUESS THE PLAYER · {levelLabelFor(playerLevel)}
      </p>
      <button className="btn btn--secondary" onClick={onPlayGuessThePlayer}>
        GUESS THE PLAYER
      </button>
    </div>
  );
}
