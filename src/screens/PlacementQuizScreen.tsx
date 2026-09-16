import { useEffect } from "react";
import { Timer } from "@/components/Timer";
import { usePlacementEngine } from "@/hooks/usePlacementEngine";
import { useLanguage } from "@/i18n/LanguageContext";
import { soundManager } from "@/services/SoundManager";
import { hapticsManager } from "@/services/HapticsManager";
import type { PlacementSummary } from "@/quiz/PlacementEngine";

export interface PlacementQuizScreenProps {
  onComplete: (summary: PlacementSummary) => void;
}

export function PlacementQuizScreen({ onComplete }: PlacementQuizScreenProps) {
  const { language, t } = useLanguage();
  const { state, submitAnswer } = usePlacementEngine(language, onComplete);
  const { currentQuestion, status, remainingMs, timeLimitMs, lastAnswer, questionIndex, totalQuestions, correctCount } = state;

  useEffect(() => {
    if (!lastAnswer) return;
    if (lastAnswer.correct) {
      soundManager.playCorrect();
      hapticsManager.correct();
    } else {
      soundManager.playWrong();
      hapticsManager.wrong();
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [lastAnswer]);

  if (!currentQuestion) return null;

  const isReveal = status === "REVEAL";
  const correctOption = currentQuestion.options[currentQuestion.correctIndex]!;

  return (
    <div className="pitch-quiz" id="placement-quiz-screen">
      <div className="pitch-stage">
        <div className="pitch-topbar">
          <span className="pitch-level-chip">{t("placement.title")}</span>
          <span className="pitch-level-chip">{questionIndex + 1} / {totalQuestions}</span>
        </div>

        <p className="pitch-question">{currentQuestion.prompt}</p>

        <Timer remainingMs={isReveal ? timeLimitMs : remainingMs} timeLimitMs={timeLimitMs} />

        {isReveal && lastAnswer && (
          <div key={questionIndex} className={`feedback-banner feedback-banner--show ${lastAnswer.correct ? "feedback-banner--correct" : "feedback-banner--wrong"}`}>
            {lastAnswer.correct ? `✓ ${t("quiz.correct")}` : `✕ ${correctOption.toUpperCase()}`}
          </div>
        )}
      </div>

      <div className="answers-sheet">
        <div className="answer-pill-list">
          {currentQuestion.options.map((option) => {
            let extraClass = "";
            if (isReveal && lastAnswer) {
              if (option === correctOption) extraClass = " answer-pill--correct";
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
            <div className="pitch-footer__label">{t("quiz.correctAnswers")}</div>
          </div>
        </div>
      </div>
    </div>
  );
}
