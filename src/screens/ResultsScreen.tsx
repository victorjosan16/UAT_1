import { rankForKnowledgeIQ } from "@/quiz/QuizIQ";
import { resultMessage } from "@/quiz/resultMessages";

/** Shared shape every mode's summary reduces to — this screen never needs the mode-specific `answers` entries. */
export interface ResultsSummaryView {
  score: number;
  correctCount: number;
  totalQuestions: number;
  bestStreak: number;
  averageResponseMs: number;
  knowledgeIQ: number;
}

export interface ResultsScreenProps {
  summary: ResultsSummaryView;
  isNewRecord: boolean;
  onPlayAgain: () => void;
  onBackToStart: () => void;
}

export function ResultsScreen({ summary, isNewRecord, onPlayAgain, onBackToStart }: ResultsScreenProps) {
  const rank = rankForKnowledgeIQ(summary.knowledgeIQ);
  const isPerfect = summary.correctCount === summary.totalQuestions;
  const message = resultMessage(summary);

  return (
    <div className="results-screen" id="results-screen">
      {isPerfect && <span className="accent-chip accent-amber">PERFECT GAME</span>}
      {isNewRecord && <span className="accent-chip accent-coral">NEW RECORD!</span>}

      <div className="results-iq-card">
        <p className="results-iq-card__label">Q5 IQ</p>
        <p className="results-iq-card__value">{summary.knowledgeIQ}</p>
        <p className="results-iq-card__rank">{rank}</p>
      </div>

      <p style={{ fontWeight: 800, letterSpacing: 0.3, margin: 0 }}>{message}</p>

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
      <button className="btn btn--ghost" onClick={onBackToStart}>BACK TO HOME</button>
    </div>
  );
}
