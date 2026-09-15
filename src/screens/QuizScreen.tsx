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
  onQuit: () => void;
}

function feedbackText(correct: boolean, timedOut: boolean, correctClub: Club, scoreGained: number): string {
  if (timedOut) return TAGLINES.timesUp;
  if (correct) return `${TAGLINES.correct} +${scoreGained.toLocaleString("en-US")}`;
  return correctClub.name.toUpperCase();
}

export function QuizScreen({ mode, seed, level, onComplete, onQuit }: QuizScreenProps) {
  const { state, submitAnswer } = useQuizSession(mode, seed, level, onComplete);
  const { currentQuestion, status, score, streak, remainingMs, timeLimitMs, lastAnswer, questionIndex, totalQuestions, correctCount } = state;

  if (!currentQuestion) return null;

  const isReveal = status === "REVEAL";
  const revealProgress = 1 - Math.min(1, Math.max(0, remainingMs / timeLimitMs));

  const crestClass = isReveal ? (lastAnswer?.correct ? "crest-stage__inner crest-stage__inner--correct" : "crest-stage__inner crest-stage__inner--wrong") : "crest-stage__inner";

  return (
    <div className="pitch-quiz" id="quiz-screen">
      <div className="pitch-stage">
        <div className="pitch-topbar">
          <button className="pitch-close" onClick={onQuit} aria-label="Quit">×</button>
          <span className="pitch-level-chip">LEVEL {level > 20 ? `E${level - 20}` : level} · {difficultyGroupForLevel(Math.min(level, 20))}</span>
        </div>

        <p className="pitch-eyebrow">Question {questionIndex + 1} of {totalQuestions}</p>

        <div style={{ position: "relative" }}>
          <div className={crestClass}>
            <ClubCrest club={currentQuestion.club} size={110} revealMode={isReveal ? "FULL" : currentQuestion.revealMode} revealProgress={isReveal ? 1 : revealProgress} />
          </div>
        </div>

        <p className="pitch-question">Which club is this?</p>

        <Timer remainingMs={isReveal ? timeLimitMs : remainingMs} timeLimitMs={timeLimitMs} />

        {isReveal && lastAnswer && (
          <div key={questionIndex} className={`feedback-banner feedback-banner--show ${lastAnswer.correct ? "feedback-banner--correct" : "feedback-banner--wrong"}`}>
            {lastAnswer.correct ? "✓" : "✕"} {feedbackText(lastAnswer.correct, lastAnswer.timedOut, currentQuestion.club, lastAnswer.scoreGained)}
          </div>
        )}
      </div>

      <div className="answers-sheet">
        <div className="answer-pill-list">
          {currentQuestion.options.map((club) => {
            let extraClass = "";
            if (isReveal && lastAnswer) {
              if (club.id === lastAnswer.clubId) extraClass = " answer-pill--correct";
              else if (club.id === lastAnswer.selectedClubId) extraClass = " answer-pill--wrong";
              else extraClass = " answer-pill--disabled";
            }
            return (
              <button key={club.id} className={`answer-pill${extraClass}`} disabled={isReveal} onClick={() => submitAnswer(club.id)}>
                {club.name}
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
