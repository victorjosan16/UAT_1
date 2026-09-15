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
  onQuit: () => void;
}

function feedbackText(correct: boolean, timedOut: boolean, correctPlayer: Player, scoreGained: number): string {
  if (timedOut) return TAGLINES.timesUp;
  if (correct) return `${TAGLINES.correct} +${scoreGained.toLocaleString("en-US")}`;
  return correctPlayer.name.toUpperCase();
}

export function PlayerQuizScreen({ mode, seed, level, onComplete, onQuit }: PlayerQuizScreenProps) {
  const { state, submitAnswer } = usePlayerQuizSession(mode, seed, level, onComplete);
  const { currentQuestion, status, score, streak, remainingMs, timeLimitMs, lastAnswer, questionIndex, totalQuestions, correctCount } = state;

  if (!currentQuestion) return null;

  const isReveal = status === "REVEAL";
  const revealProgress = isReveal ? 1 : 1 - Math.min(1, Math.max(0, remainingMs / timeLimitMs));
  const chainClubs = currentQuestion.player.careerClubIds.map((id) => getClubById(id)).filter((c): c is NonNullable<typeof c> => Boolean(c));

  return (
    <div className="pitch-quiz" id="player-quiz-screen">
      <div className="pitch-stage pitch-stage--player">
        <div className="pitch-topbar">
          <button className="pitch-close" onClick={onQuit} aria-label="Quit">×</button>
          <span className="pitch-level-chip">LEVEL {level > 20 ? `E${level - 20}` : level} · {difficultyGroupForLevel(Math.min(level, 20))}</span>
        </div>

        <div className="player-header">
          <PlayerSilhouette size={72} />
          <h2 className="player-header__title">GUESS THE PLAYER</h2>
        </div>

        <TransferChain clubs={chainClubs} revealProgress={revealProgress} />

        <Timer remainingMs={isReveal ? timeLimitMs : remainingMs} timeLimitMs={timeLimitMs} />

        {isReveal && lastAnswer && (
          <div key={questionIndex} className={`feedback-banner feedback-banner--show ${lastAnswer.correct ? "feedback-banner--correct" : "feedback-banner--wrong"}`}>
            {lastAnswer.correct ? "✓" : "✕"} {feedbackText(lastAnswer.correct, lastAnswer.timedOut, currentQuestion.player, lastAnswer.scoreGained)}
          </div>
        )}
      </div>

      <div className="answers-sheet">
        <div className="answer-pill-list">
          {currentQuestion.options.map((player) => {
            let extraClass = "";
            if (isReveal && lastAnswer) {
              if (player.id === lastAnswer.playerId) extraClass = " answer-pill--correct";
              else if (player.id === lastAnswer.selectedPlayerId) extraClass = " answer-pill--wrong";
              else extraClass = " answer-pill--disabled";
            }
            return (
              <button key={player.id} className={`answer-pill${extraClass}`} disabled={isReveal} onClick={() => submitAnswer(player.id)}>
                {player.name}
              </button>
            );
          })}
        </div>

        <div className="pitch-footer">
          <div className="pitch-footer__stat">
            <div className="pitch-footer__value">{correctCount}/{totalQuestions}</div>
            <div className="pitch-footer__label">Correct answers</div>
          </div>
          <div className="pitch-footer__stat">
            <div className="pitch-footer__value">{streak.current}</div>
            <div className="pitch-footer__label">Streak</div>
          </div>
          <div className="pitch-footer__stat">
            <div className="pitch-footer__value">{score.toLocaleString("en-US")}</div>
            <div className="pitch-footer__label">Score</div>
          </div>
        </div>
      </div>
    </div>
  );
}
