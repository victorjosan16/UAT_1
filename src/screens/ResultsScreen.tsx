import { knowledgeRankLabel, rankForKnowledgeIQ } from "@/quiz/QuizIQ";
import { resultMessage } from "@/quiz/resultMessages";
import { useLanguage } from "@/i18n/LanguageContext";

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
  const { language, t } = useLanguage();
  const rank = knowledgeRankLabel(rankForKnowledgeIQ(summary.knowledgeIQ), language);
  const isPerfect = summary.correctCount === summary.totalQuestions;
  const message = resultMessage(summary, language);

  return (
    <div className="results-screen" id="results-screen">
      {isPerfect && <span className="accent-chip accent-amber">{t("results.perfectGame")}</span>}
      {isNewRecord && <span className="accent-chip accent-coral">{t("results.newRecord")}</span>}

      <div className="results-iq-card">
        <p className="results-iq-card__label">Q5 IQ</p>
        <p className="results-iq-card__value">{summary.knowledgeIQ}</p>
        <p className="results-iq-card__rank">{rank}</p>
      </div>

      <p style={{ fontWeight: 800, letterSpacing: 0.3, margin: 0 }}>{message}</p>

      <div className="stat-grid">
        <div className="stat-card">
          <div className="stat-card__label">{t("results.correct")}</div>
          <div className="stat-card__value">{summary.correctCount} / {summary.totalQuestions}</div>
        </div>
        <div className="stat-card">
          <div className="stat-card__label">{t("results.points")}</div>
          <div className="stat-card__value">{summary.score.toLocaleString("en-US")}</div>
        </div>
        <div className="stat-card">
          <div className="stat-card__label">{t("results.bestStreak")}</div>
          <div className="stat-card__value">🔥 ×{summary.bestStreak}</div>
        </div>
        <div className="stat-card">
          <div className="stat-card__label">{t("results.avgResponse")}</div>
          <div className="stat-card__value">{(summary.averageResponseMs / 1000).toFixed(1)}s</div>
        </div>
      </div>

      <button className="btn btn--primary" onClick={onPlayAgain}>{t("results.playAgain")}</button>
      <button className="btn btn--ghost" onClick={onBackToStart}>{t("results.backToHome")}</button>
    </div>
  );
}
