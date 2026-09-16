import { useEffect } from "react";
import { Timer } from "@/components/Timer";
import { useLanguage } from "@/i18n/LanguageContext";
import { soundManager } from "@/services/SoundManager";
import { hapticsManager } from "@/services/HapticsManager";
import type { ChampionshipRoundState } from "@/hooks/useChampionshipRound";

export interface ChampionshipQuizScreenProps {
  state: ChampionshipRoundState;
  playerId: string;
  onSubmit: (answer: string | null) => void;
  onQuit: () => void;
}

export function ChampionshipQuizScreen({ state, playerId, onSubmit, onQuit }: ChampionshipQuizScreenProps) {
  const { t } = useLanguage();
  const { currentQuestion, questionIndex, totalQuestions, remainingMs, timeLimitMs, hasAnsweredCurrent, lastAnswer, streakTierReached, score, streak, correctCount, players } = state;

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

  const correctOption = currentQuestion.options[currentQuestion.correctIndex]!;
  const isLocked = hasAnsweredCurrent;

  function feedbackText(): string {
    if (!lastAnswer) return t("championship.waitingForNext");
    if (lastAnswer.timedOut) return t("quiz.timesUp");
    if (lastAnswer.correct) return `${t("quiz.correct")} +${lastAnswer.scoreGained.toLocaleString("en-US")}`;
    return correctOption.toUpperCase();
  }

  return (
    <div className="pitch-quiz" id="championship-quiz-screen">
      <div className="pitch-stage">
        <div className="pitch-topbar">
          <button className="pitch-close" onClick={onQuit} aria-label={t("quiz.quit")}>×</button>
          <span className="pitch-level-chip">{questionIndex + 1} / {totalQuestions}</span>
        </div>

        <div className="championship-players-strip">
          {players.map((player) => (
            <span key={player.playerId} className={`championship-player-pill${player.playerId === playerId ? " championship-player-pill--me" : ""}${player.lastAnsweredIndex >= questionIndex ? " championship-player-pill--answered" : ""}`}>
              {player.nickname}
            </span>
          ))}
        </div>

        <p className="pitch-question">{currentQuestion.prompt}</p>

        <Timer remainingMs={isLocked ? timeLimitMs : remainingMs} timeLimitMs={timeLimitMs} />

        {isLocked && (
          <div key={questionIndex} className={`feedback-banner feedback-banner--show ${lastAnswer?.correct ? "feedback-banner--correct" : lastAnswer ? "feedback-banner--wrong" : ""}`}>
            {lastAnswer ? `${lastAnswer.correct ? "✓" : "✕"} ${feedbackText()}` : t("championship.answerLocked")}
          </div>
        )}
      </div>

      <div className="answers-sheet">
        <div className="answer-pill-list">
          {currentQuestion.options.map((option) => {
            let extraClass = "";
            if (isLocked) {
              if (option === correctOption) extraClass = " answer-pill--correct";
              else if (lastAnswer && option === lastAnswer.selectedAnswer) extraClass = " answer-pill--wrong";
              else extraClass = " answer-pill--disabled";
            }
            return (
              <button key={option} className={`answer-pill${extraClass}`} disabled={isLocked} onClick={() => onSubmit(option)}>
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
