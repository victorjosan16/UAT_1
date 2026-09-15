import { ClubCrest } from "@/components/ClubCrest";
import { Timer } from "@/components/Timer";
import { useQuizSession } from "@/hooks/useQuizSession";
import { difficultyGroupForLevel } from "@/quiz/LevelDefinition";
import { TAGLINES } from "@/branding";
import type { Club, QuizMode, QuizSummary } from "@/types";

export interface QuizScreenProps {
  mode: QuizMode;
  seed: string;
  level: number;
  onComplete: (summary: QuizSummary) => void;
}

function feedbackText(correct: boolean, timedOut: boolean, correctClub: Club): string {
  if (timedOut) return TAGLINES.timesUp;
  return correct ? TAGLINES.correct : correctClub.name.toUpperCase();
}

export function QuizScreen({ mode, seed, level, onComplete }: QuizScreenProps) {
  const { state, submitAnswer } = useQuizSession(mode, seed, level, onComplete);
  const { currentQuestion, status, score, streak, remainingMs, timeLimitMs, lastAnswer, streakTierReached, questionIndex, totalQuestions } = state;

  if (!currentQuestion) return null;

  const isReveal = status === "REVEAL";
  const revealProgress = 1 - Math.min(1, Math.max(0, remainingMs / timeLimitMs));

  const crestClass = isReveal ? (lastAnswer?.correct ? "crest-stage__inner crest-stage__inner--correct" : "crest-stage__inner crest-stage__inner--wrong") : "crest-stage__inner";

  return (
    <div className="quiz-screen" id="quiz-screen">
      <div className="quiz-hud">
        <span>
          LEVEL {level > 20 ? `E${level - 20}` : level} · {difficultyGroupForLevel(Math.min(level, 20))} · {questionIndex + 1}/{totalQuestions}
        </span>
        <span className={streakTierReached ? "streak-badge streak-badge--visible" : "streak-badge"}>🔥 ×{streak.current}</span>
        <span className="quiz-hud__score">{score.toLocaleString("en-US")}</span>
      </div>

      <Timer remainingMs={isReveal ? timeLimitMs : remainingMs} timeLimitMs={timeLimitMs} />

      <div className="crest-stage" style={{ position: "relative" }}>
        <div className={crestClass}>
          <ClubCrest club={currentQuestion.club} size={150} revealMode={isReveal ? "FULL" : currentQuestion.revealMode} revealProgress={isReveal ? 1 : revealProgress} />
        </div>

        {isReveal && lastAnswer && (
          <div key={questionIndex} className={`feedback-banner feedback-banner--show ${lastAnswer.correct ? "feedback-banner--correct" : "feedback-banner--wrong"}`}>
            {lastAnswer.correct ? "✓" : "✕"} {feedbackText(lastAnswer.correct, lastAnswer.timedOut, currentQuestion.club)}
            {lastAnswer.correct && (
              <div className="score-float score-float--show">+{lastAnswer.scoreGained.toLocaleString("en-US")}</div>
            )}
          </div>
        )}
      </div>

      <p className="question-prompt">Which club is this?</p>

      <div className="answers-grid">
        {currentQuestion.options.map((club) => {
          let extraClass = "";
          if (isReveal && lastAnswer) {
            if (club.id === lastAnswer.clubId) extraClass = " answer-btn--correct";
            else if (club.id === lastAnswer.selectedClubId) extraClass = " answer-btn--wrong";
            else extraClass = " answer-btn--disabled";
          }
          return (
            <button key={club.id} className={`answer-btn${extraClass}`} disabled={isReveal} onClick={() => submitAnswer(club.id)}>
              {club.name}
            </button>
          );
        })}
      </div>
    </div>
  );
}
