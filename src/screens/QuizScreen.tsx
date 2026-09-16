import { useEffect } from "react";
import { FlagIcon } from "@/components/FlagIcon";
import { Timer } from "@/components/Timer";
import { useQuizEngine } from "@/hooks/useQuizEngine";
import { useLanguage } from "@/i18n/LanguageContext";
import { getCountryById } from "@/data/countries";
import { difficultyGroupForLevel } from "@/quiz/LevelDefinition";
import { soundManager } from "@/services/SoundManager";
import { hapticsManager } from "@/services/HapticsManager";
import type { AnswerResult, CategoryId, QuizMode, QuizSummary } from "@/types";

export interface QuizScreenProps {
  mode: QuizMode;
  seed: string;
  level?: number;
  categoryId?: CategoryId;
  onComplete: (summary: QuizSummary) => void;
  onQuit: () => void;
}

export function QuizScreen({ mode, seed, level, categoryId, onComplete, onQuit }: QuizScreenProps) {
  const { language, t } = useLanguage();
  const { state, submitAnswer } = useQuizEngine(mode, seed, { level, categoryId, language }, onComplete);
  const { currentQuestion, status, score, streak, remainingMs, timeLimitMs, lastAnswer, streakTierReached, questionIndex, totalQuestions, correctCount, level: engineLevel } = state;

  useEffect(() => {
    if (!lastAnswer) return;
    if (streakTierReached) {
      soundManager.playStreak();
      hapticsManager.streak();
    } else if (lastAnswer.correct) {
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
  const revealProgress = 1 - Math.min(1, Math.max(0, remainingMs / timeLimitMs));
  const source = currentQuestion.source;
  const country = source.flagCountryId ? getCountryById(source.flagCountryId) : undefined;
  const correctOption = currentQuestion.options[currentQuestion.correctIndex]!;

  function feedbackText(answer: AnswerResult): string {
    if (answer.timedOut) return t("quiz.timesUp");
    if (answer.correct) return `${t("quiz.correct")} +${answer.scoreGained.toLocaleString("en-US")}`;
    return correctOption.toUpperCase();
  }

  return (
    <div className="pitch-quiz" id="quiz-screen">
      <div className="pitch-stage">
        <div className="pitch-topbar">
          <button className="pitch-close" onClick={onQuit} aria-label={t("quiz.quit")}>×</button>
          {mode === "LEVEL" || mode === "ENDLESS" ? (
            <span className="pitch-level-chip">{t("quiz.level")} {engineLevel > 20 ? `E${engineLevel - 20}` : engineLevel} · {difficultyGroupForLevel(Math.min(engineLevel, 20))}</span>
          ) : (
            <span className="pitch-level-chip">{questionIndex + 1} / {totalQuestions}</span>
          )}
        </div>

        <p className="pitch-eyebrow">{t("quiz.questionOf", { index: questionIndex + 1, total: totalQuestions })}</p>

        {country && (
          <FlagIcon pattern={country.flag} countryName={country.name[language]} width={150} revealMode={isReveal ? "FULL" : currentQuestion.revealMode} revealProgress={isReveal ? 1 : revealProgress} />
        )}

        <p className="pitch-question">{currentQuestion.prompt}</p>

        <Timer remainingMs={isReveal ? timeLimitMs : remainingMs} timeLimitMs={timeLimitMs} />

        {isReveal && lastAnswer && (
          <div key={questionIndex} className={`feedback-banner feedback-banner--show ${lastAnswer.correct ? "feedback-banner--correct" : "feedback-banner--wrong"}`}>
            {lastAnswer.correct ? "✓" : "✕"} {feedbackText(lastAnswer)}
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
          <div className="pitch-footer__stat">
            <div className="pitch-footer__value">{streak.current}</div>
            <div className="pitch-footer__label">{t("quiz.streak")}</div>
          </div>
          <div className="pitch-footer__stat">
            <div className="pitch-footer__value">{score.toLocaleString("en-US")}</div>
            <div className="pitch-footer__label">{t("quiz.score")}</div>
          </div>
        </div>
      </div>
    </div>
  );
}
