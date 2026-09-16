import { CATEGORIES } from "@/data/categories";
import { useLanguage } from "@/i18n/LanguageContext";
import type { CategoryId } from "@/types";

export interface DiscoverScreenProps {
  onOpenCategory: (id: CategoryId) => void;
}

export function DiscoverScreen({ onOpenCategory }: DiscoverScreenProps) {
  const { language, t } = useLanguage();

  return (
    <div id="discover-screen">
      <h1 className="page-title">{t("discover.title")}</h1>
      <p className="page-subtitle">{t("discover.subtitle")}</p>

      <div className="category-grid">
        {CATEGORIES.map((category) => (
          <button
            key={category.id}
            className={`category-card${category.available ? "" : " category-card--locked"}`}
            onClick={() => category.available && onOpenCategory(category.id)}
            disabled={!category.available}
          >
            {!category.available && <span className="category-card__badge">{t("discover.comingSoonBadge")}</span>}
            <span className="category-card__emoji">{category.emoji}</span>
            <span className="category-card__label">{category.label[language]}</span>
            <span className={`accent-chip accent-${category.accent}`} style={{ alignSelf: "flex-start" }}>
              {category.available ? t("discover.play") : t("discover.comingSoon")}
            </span>
          </button>
        ))}
      </div>
    </div>
  );
}
