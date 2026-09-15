import { PlayerSilhouette } from "@/components/PlayerSilhouette";
import { TransferChain } from "@/components/TransferChain";
import { Timer } from "@/components/Timer";
import { usePlayerQuizSession } from "@/hooks/usePlayerQuizSession";
import { getClubById } from "@/data/clubs";
import { difficultyGroupForLevel } from "@/quiz/LevelDefinition";
import { TAGLINES } from "@/branding";
import type { Player, PlayerQuizSummary, QuizMode } from "@/types";

export interface PlayerQuizScreenProps {
  mode: QuizMode;
  seed: string;
  level: number;
  onComplete: (summary: PlayerQuizSummary) => void;
}

function feedbackText(correct: boolean, timedOut: boolean, correctPlayer: Player): string {
  if (timedOut) return TAGLINES.timesUp;
  return correct ? TAGLINES.correct : correctPlayer.name.toUpperCase();
}

export function PlayerQuizScreen({ mode, seed, level, onComplete }: PlayerQuizScreenProps) {
  const { state, submitAnswer } = usePlayerQuizSession(mode, seed, level, onComplete);
  const { currentQuestion, status, score, streak, remainingMs, timeLimitMs, lastAnswer, streakTierReached, questionIndex, totalQuestions } = state;

  if (!currentQuestion) return null;

  const isReveal = status === "REVEAL";
  const revealProgress = isReveal ? 1 : 1 - Math.min(1, Math.max(0, remainingMs / timeLimitMs));
  const chainClubs = currentQuestion.player.careerClubIds.map((id) => getClubById(id)).filter((c): c is NonNullable<typeof c> => Boolean(c));

  return (
    <div className="quiz-screen" id="player-quiz-screen">
      <div className="quiz-hud">
        <span>
          LEVEL {level > 20 ? `E${level - 20}` : level} · {difficultyGroupForLevel(Math.min(level, 20))} · {questionIndex + 1}/{totalQuestions}
        </span>
        <span className={streakTierReached ? "streak-badge streak-badge--visible" : "streak-badge"}>🔥 ×{streak.current}</span>
        <span className="quiz-hud__score">{score.toLocaleString("en-US")}</span>
      </div>

      <Timer remainingMs={isReveal ? timeLimitMs : remainingMs} timeLimitMs={timeLimitMs} />

      <div className="crest-stage" style={{ position: "relative", flexDirection: "column", gap: 14 }}>
        <PlayerSilhouette size={100} />
        <TransferChain clubs={chainClubs} revealProgress={revealProgress} />

        {isReveal && lastAnswer && (
          <div key={questionIndex} className={`feedback-banner feedback-banner--show ${lastAnswer.correct ? "feedback-banner--correct" : "feedback-banner--wrong"}`}>
            {lastAnswer.correct ? "✓" : "✕"} {feedbackText(lastAnswer.correct, lastAnswer.timedOut, currentQuestion.player)}
            {lastAnswer.correct && <div className="score-float score-float--show">+{lastAnswer.scoreGained.toLocaleString("en-US")}</div>}
          </div>
        )}
      </div>

      <p className="question-prompt">Who is this player?</p>

      <div className="answers-grid">
        {currentQuestion.options.map((player) => {
          let extraClass = "";
          if (isReveal && lastAnswer) {
            if (player.id === lastAnswer.playerId) extraClass = " answer-btn--correct";
            else if (player.id === lastAnswer.selectedPlayerId) extraClass = " answer-btn--wrong";
            else extraClass = " answer-btn--disabled";
          }
          return (
            <button key={player.id} className={`answer-btn${extraClass}`} disabled={isReveal} onClick={() => submitAnswer(player.id)}>
              {player.name}
            </button>
          );
        })}
      </div>
    </div>
  );
}
