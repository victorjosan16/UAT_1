/** The subset of QuizSummary/PlayerQuizSummary this needs — works for either mode's result. */
export interface ResultSignal {
  totalQuestions: number;
  correctCount: number;
  averageResponseMs: number;
  bestStreak: number;
}

/**
 * Contextual result copy generated from this run's own real numbers —
 * never a fabricated or generic line (see docs/GAME_DESIGN.md). Checked
 * roughly most-impressive-first so a perfect fast streaky round still
 * leads with "perfect" rather than something less specific.
 */
export function resultMessage(summary: ResultSignal): string {
  if (summary.totalQuestions === 0) return "";
  if (summary.correctCount === summary.totalQuestions) return "PERFECT KNOWLEDGE.";
  if (summary.correctCount === summary.totalQuestions - 1) return "ONE AWAY FROM PERFECT.";

  const avgSeconds = summary.averageResponseMs / 1000;
  if (summary.correctCount > 0 && avgSeconds > 0 && avgSeconds < 2) return "LIGHTNING FAST.";
  if (summary.bestStreak >= 8) return `ON FIRE ×${summary.bestStreak}`;

  const accuracy = summary.correctCount / summary.totalQuestions;
  if (accuracy >= 0.7) return "SOLID PERFORMANCE.";
  if (accuracy >= 0.4) return "GETTING THERE.";
  return "KEEP PRACTICING.";
}
