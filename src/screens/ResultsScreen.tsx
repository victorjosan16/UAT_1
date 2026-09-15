import { rankForFootballIQ } from "@/quiz/FootballIQ";
import { resultMessage } from "@/quiz/resultMessages";

/** Shared shape between QuizSummary and PlayerQuizSummary — this screen never needs the mode-specific `answers` entries. */
export interface ResultsSummaryView {
  score: number;
  correctCount: number;
  totalQuestions: number;
  bestStreak: number;
  averageResponseMs: number;
  footballIQ: number;
}

export interface ResultsScreenProps {
  summary: ResultsSummaryView;
  isNewRecord: boolean;
  onPlayAgain: () => void;
  onBackToStart: () => void;
}

export function ResultsScreen({ summary, isNewRecord, onPlayAgain, onBackToStart }: ResultsScreenProps) {
  const rank = rankForFootballIQ(summary.footballIQ);
  const isPerfect = summary.correctCount === summary.totalQuestions;
  const message = resultMessage(summary);

  return (
    <div className="screen" id="results-screen">
      {isPerfect && <p className="subtitle" style={{ color: "var(--accent-2)", fontSize: 20, letterSpacing: 3 }}>PERFECT GAME</p>}
      {isNewRecord && <p className="subtitle" style={{ color: "var(--accent-2)" }}>NEW RECORD!</p>}

      <p className="stat-card__label" style={{ marginTop: 8 }}>FOOTBALL IQ</p>
      <h1 className="title" style={{ fontSize: 64 }}>{summary.footballIQ}</h1>
      <p className="subtitle">{rank}</p>

      <p className="subtitle" style={{ color: "var(--text)", fontWeight: 700, letterSpacing: 0.5, textTransform: "none" }}>{message}</p>

      <div className="stat-grid">
        <div className="stat-card">
          <div className="stat-card__label">CORRECT</div>
          <div className="stat-card__value">{summary.correctCount} / {summary.totalQuestions}</div>
        </div>
        <div className="stat-card">
          <div className="stat-card__label">POINTS</div>
          <div className="stat-card__value">{summary.score.toLocaleString("en-US")}</div>
        </div>
        <div className="stat-card">
          <div className="stat-card__label">BEST STREAK</div>
          <div className="stat-card__value">🔥 ×{summary.bestStreak}</div>
        </div>
        <div className="stat-card">
          <div className="stat-card__label">AVG RESPONSE</div>
          <div className="stat-card__value">{(summary.averageResponseMs / 1000).toFixed(1)}s</div>
        </div>
      </div>

      <button className="btn btn--primary" onClick={onPlayAgain}>PLAY AGAIN</button>
      <button className="btn btn--ghost" onClick={onBackToStart}>BACK TO START</button>
    </div>
  );
}
