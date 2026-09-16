import { FlagIcon } from "@/components/FlagIcon";
import { Timer } from "@/components/Timer";
import { useQuizEngine } from "@/hooks/useQuizEngine";
import { getCountryById } from "@/data/countries";
import { difficultyGroupForLevel } from "@/quiz/LevelDefinition";
import { TAGLINES } from "@/branding";
import type { CategoryId, QuizMode, QuizQuestionSource, QuizSummary } from "@/types";

export interface QuizScreenProps {
  mode: QuizMode;
  seed: string;
  level?: number;
  categoryId?: CategoryId;
  onComplete: (summary: QuizSummary) => void;
  onQuit: () => void;
}

function feedbackText(correct: boolean, timedOut: boolean, source: QuizQuestionSource, scoreGained: number): string {
  if (timedOut) return TAGLINES.timesUp;
  if (correct) return `${TAGLINES.correct} +${scoreGained.toLocaleString("en-US")}`;
  return source.correctAnswer.toUpperCase();
}

export function QuizScreen({ mode, seed, level, categoryId, onComplete, onQuit }: QuizScreenProps) {
  const { state, submitAnswer } = useQuizEngine(mode, seed, { level, categoryId }, onComplete);
  const { currentQuestion, status, score, streak, remainingMs, timeLimitMs, lastAnswer, questionIndex, totalQuestions, correctCount, level: engineLevel } = state;

  if (!currentQuestion) return null;

  const isReveal = status === "REVEAL";
  const revealProgress = 1 - Math.min(1, Math.max(0, remainingMs / timeLimitMs));
  const source = currentQuestion.source;
  const country = source.flagCountryId ? getCountryById(source.flagCountryId) : undefined;

  return (
    <div className="pitch-quiz" id="quiz-screen">
      <div className="pitch-stage">
        <div className="pitch-topbar">
          <button className="pitch-close" onClick={onQuit} aria-label="Quit">×</button>
          {mode === "LEVEL" || mode === "ENDLESS" ? (
            <span className="pitch-level-chip">LEVEL {engineLevel > 20 ? `E${engineLevel - 20}` : engineLevel} · {difficultyGroupForLevel(Math.min(engineLevel, 20))}</span>
          ) : (
            <span className="pitch-level-chip">{questionIndex + 1} / {totalQuestions}</span>
          )}
        </div>

        <p className="pitch-eyebrow">Question {questionIndex + 1} of {totalQuestions}</p>

        {country && (
          <FlagIcon pattern={country.flag} countryName={country.name} width={150} revealMode={isReveal ? "FULL" : currentQuestion.revealMode} revealProgress={isReveal ? 1 : revealProgress} />
        )}

        <p className="pitch-question">{source.prompt}</p>

        <Timer remainingMs={isReveal ? timeLimitMs : remainingMs} timeLimitMs={timeLimitMs} />

        {isReveal && lastAnswer && (
          <div key={questionIndex} className={`feedback-banner feedback-banner--show ${lastAnswer.correct ? "feedback-banner--correct" : "feedback-banner--wrong"}`}>
            {lastAnswer.correct ? "✓" : "✕"} {feedbackText(lastAnswer.correct, lastAnswer.timedOut, source, lastAnswer.scoreGained)}
          </div>
        )}
      </div>

      <div className="answers-sheet">
        <div className="answer-pill-list">
          {currentQuestion.options.map((option) => {
            let extraClass = "";
            if (isReveal && lastAnswer) {
              if (option === source.correctAnswer) extraClass = " answer-pill--correct";
              else if (option === lastAnswer.selectedAnswer) extraClass = " answer-pill--wrong";
              else extraClass = " answer-pill--disabled";
            }
            return (
              <button key={option} className={`answer-pill${extraClass}`} disabled={isReveal} onClick={() => submitAnswer(option)}>
                {option}
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
