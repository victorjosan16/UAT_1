import type { Language } from "@/types";

/** The subset of QuizSummary this needs — works for every mode's result. */
export interface ResultSignal {
  totalQuestions: number;
  correctCount: number;
  averageResponseMs: number;
  bestStreak: number;
}

type MessageKey = "perfect" | "oneAway" | "lightningFast" | "onFire" | "solid" | "gettingThere" | "keepPracticing";

const MESSAGES: Record<MessageKey, Record<Language, string>> = {
  perfect: {
    en: "PERFECT KNOWLEDGE.",
    ro: "CUNOȘTINȚE PERFECTE.",
    es: "CONOCIMIENTO PERFECTO.",
    pt: "CONHECIMENTO PERFEITO.",
    hi: "परफेक्ट नॉलेज।",
    id: "PENGETAHUAN SEMPURNA.",
    ru: "ИДЕАЛЬНЫЕ ЗНАНИЯ.",
  },
  oneAway: {
    en: "ONE AWAY FROM PERFECT.",
    ro: "LA UN PAS DE PERFECȚIUNE.",
    es: "A UNO DE LA PERFECCIÓN.",
    pt: "A UM DA PERFEIÇÃO.",
    hi: "परफेक्शन से बस एक कदम दूर।",
    id: "SATU LANGKAH LAGI MENUJU SEMPURNA.",
    ru: "ОДИН ШАГ ДО ИДЕАЛА.",
  },
  lightningFast: {
    en: "LIGHTNING FAST.",
    ro: "RAPID CA FULGERUL.",
    es: "RÁPIDO COMO EL RAYO.",
    pt: "RÁPIDO COMO UM RAIO.",
    hi: "बिजली जैसी तेज़ी।",
    id: "SECEPAT KILAT.",
    ru: "МОЛНИЕНОСНО.",
  },
  onFire: {
    en: "ON FIRE ×{streak}",
    ro: "ÎN FLĂCĂRI ×{streak}",
    es: "EN RACHA ×{streak}",
    pt: "PEGANDO FOGO ×{streak}",
    hi: "ऑन फायर ×{streak}",
    id: "SEDANG PANAS ×{streak}",
    ru: "В УДАРЕ ×{streak}",
  },
  solid: {
    en: "SOLID PERFORMANCE.",
    ro: "PERFORMANȚĂ SOLIDĂ.",
    es: "BUEN RENDIMIENTO.",
    pt: "DESEMPENHO SÓLIDO.",
    hi: "ठोस प्रदर्शन।",
    id: "PERFORMA SOLID.",
    ru: "СОЛИДНЫЙ РЕЗУЛЬТАТ.",
  },
  gettingThere: {
    en: "GETTING THERE.",
    ro: "EȘTI PE DRUMUL CEL BUN.",
    es: "VAS POR BUEN CAMINO.",
    pt: "VOCÊ ESTÁ CHEGANDO LÁ.",
    hi: "आप सही रास्ते पर हैं।",
    id: "SEMAKIN DEKAT.",
    ru: "ТЫ НА ПУТИ К УСПЕХУ.",
  },
  keepPracticing: {
    en: "KEEP PRACTICING.",
    ro: "CONTINUĂ SĂ EXERSEZI.",
    es: "SIGUE PRACTICANDO.",
    pt: "CONTINUE PRATICANDO.",
    hi: "अभ्यास जारी रखें।",
    id: "TERUS BERLATIH.",
    ru: "ПРОДОЛЖАЙ ТРЕНИРОВАТЬСЯ.",
  },
};

function render(key: MessageKey, language: Language, vars?: Record<string, string | number>): string {
  const template = MESSAGES[key][language];
  if (!vars) return template;
  return template.replace(/\{(\w+)\}/g, (match, name: string) => (name in vars ? String(vars[name]) : match));
}

/**
 * Contextual result copy generated from this run's own real numbers —
 * never a fabricated or generic line (see docs/GAME_DESIGN.md). Checked
 * roughly most-impressive-first so a perfect fast streaky round still
 * leads with "perfect" rather than something less specific.
 */
export function resultMessage(summary: ResultSignal, language: Language): string {
  if (summary.totalQuestions === 0) return "";
  if (summary.correctCount === summary.totalQuestions) return render("perfect", language);
  if (summary.correctCount === summary.totalQuestions - 1) return render("oneAway", language);

  const avgSeconds = summary.averageResponseMs / 1000;
  if (summary.correctCount > 0 && avgSeconds > 0 && avgSeconds < 2) return render("lightningFast", language);
  if (summary.bestStreak >= 8) return render("onFire", language, { streak: summary.bestStreak });

  const accuracy = summary.correctCount / summary.totalQuestions;
  if (accuracy >= 0.7) return render("solid", language);
  if (accuracy >= 0.4) return render("gettingThere", language);
  return render("keepPracticing", language);
}
