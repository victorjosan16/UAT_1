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

export interface ChallengeComparison {
  creatorNickname: string;
  creatorScore: number;
}

export interface ResultsScreenProps {
  summary: ResultsSummaryView;
  isNewRecord: boolean;
  challengeComparison?: ChallengeComparison | null;
  onPlayAgain: () => void;
  onBackToStart: () => void;
  onChallengeFriend: () => void;
}

export function ResultsScreen({ summary, isNewRecord, challengeComparison, onPlayAgain, onBackToStart, onChallengeFriend }: ResultsScreenProps) {
  const { language, t } = useLanguage();
  const rank = knowledgeRankLabel(rankForKnowledgeIQ(summary.knowledgeIQ), language);
  const isPerfect = summary.correctCount === summary.totalQuestions;
  const message = resultMessage(summary, language);

  const wonChallenge = challengeComparison ? summary.score > challengeComparison.creatorScore : null;
  const pointsAway = challengeComparison ? Math.abs(summary.score - challengeComparison.creatorScore) : 0;

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

      {challengeComparison && (
        <div className="card" style={{ width: "100%", textAlign: "center" }}>
          <p className="accent-chip accent-emerald" style={{ marginBottom: 8 }}>
            {wonChallenge ? t("challenge.youWin") : t("challenge.youLose")}
          </p>
          <div style={{ display: "flex", justifyContent: "space-around" }}>
            <div>
              <div className="stat-card__label">{t("ranking.you")}</div>
              <div className="stat-card__value">{summary.score.toLocaleString("en-US")}</div>
            </div>
            <div>
              <div className="stat-card__label">{challengeComparison.creatorNickname}</div>
              <div className="stat-card__value">{challengeComparison.creatorScore.toLocaleString("en-US")}</div>
            </div>
          </div>
          {!wonChallenge && <p className="page-subtitle" style={{ marginTop: 8, marginBottom: 0 }}>{t("challenge.pointsAway", { points: pointsAway.toLocaleString("en-US") })}</p>}
        </div>
      )}

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

      <button className="btn btn--primary" onClick={onPlayAgain}>{t(challengeComparison ? "challenge.rematch" : "results.playAgain")}</button>
      <button className="btn btn--secondary" onClick={onChallengeFriend}>🔗 {t("results.challengeFriend")}</button>
      <button className="btn btn--ghost" onClick={onBackToStart}>{t("results.backToHome")}</button>
    </div>
  );
}
