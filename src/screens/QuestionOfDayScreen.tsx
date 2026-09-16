import { useMemo, useState } from "react";
import { useLanguage } from "@/i18n/LanguageContext";
import { soundManager } from "@/services/SoundManager";
import { hapticsManager } from "@/services/HapticsManager";
import { QotdService } from "@/services/QotdService";
import { pickQotdQuestion } from "@/utils/qotd";
import { shareLink } from "@/utils/share";
import { GAME_NAME } from "@/branding";

export interface QuestionOfDayScreenProps {
  dateKey: string;
  shareUrl: string;
  onBack: () => void;
}

/** One featured question, the same for every player today — see MASTER PROMPT §11. Real aggregated "X% answered correctly" only; never a fabricated number. */
export function QuestionOfDayScreen({ dateKey, shareUrl, onBack }: QuestionOfDayScreenProps) {
  const { language, t } = useLanguage();
  const question = useMemo(() => pickQotdQuestion(dateKey, language), [dateKey, language]);
  const [selected, setSelected] = useState<string | null>(null);
  const [correctPercent, setCorrectPercent] = useState<number | null>(null);

  const correctOption = question.options[question.correctIndex]!;
  const answered = selected !== null;
  const correct = selected === correctOption;

  function handleAnswer(option: string): void {
    if (answered) return;
    setSelected(option);
    const isCorrect = option === correctOption;
    if (isCorrect) {
      soundManager.playCorrect();
      hapticsManager.correct();
    } else {
      soundManager.playWrong();
      hapticsManager.wrong();
    }
    void QotdService.submitAnswer(dateKey, isCorrect).then(() => QotdService.getCorrectPercent(dateKey)).then(setCorrectPercent);
  }

  async function handleShare(): Promise<void> {
    const text = `${GAME_NAME}\n\n${t("qotd.title")}\n\n${question.prompt}\n\n${t("qotd.shareCta")}\n${shareUrl}`;
    await shareLink(GAME_NAME, text, shareUrl);
  }

  return (
    <div className="pitch-quiz" id="qotd-screen">
      <div className="pitch-stage">
        <div className="pitch-topbar">
          <button className="pitch-close" onClick={onBack} aria-label={t("quiz.quit")}>×</button>
          <span className="pitch-level-chip">{t("qotd.title")}</span>
        </div>

        <p className="pitch-question">{question.prompt}</p>

        {answered && (
          <div className={`feedback-banner feedback-banner--show ${correct ? "feedback-banner--correct" : "feedback-banner--wrong"}`}>
            {correct ? `✓ ${t("quiz.correct")}` : `✕ ${correctOption.toUpperCase()}`}
          </div>
        )}

        {answered && correctPercent !== null && (
          <p className="page-subtitle" style={{ marginTop: 8 }}>{t("qotd.percentCorrect", { percent: correctPercent })}</p>
        )}
      </div>

      <div className="answers-sheet">
        <div className="answer-pill-list">
          {question.options.map((option) => {
            let extraClass = "";
            if (answered) {
              if (option === correctOption) extraClass = " answer-pill--correct";
              else if (option === selected) extraClass = " answer-pill--wrong";
              else extraClass = " answer-pill--disabled";
            }
            return (
              <button key={option} className={`answer-pill${extraClass}`} disabled={answered} onClick={() => handleAnswer(option)}>
                {option}
              </button>
            );
          })}
        </div>

        {answered && (
          <button className="btn btn--secondary" onClick={() => void handleShare()}>
            🔗 {t("qotd.share")}
          </button>
        )}
        <button className="btn btn--ghost" onClick={onBack}>{t("results.backToHome")}</button>
      </div>
    </div>
  );
}
