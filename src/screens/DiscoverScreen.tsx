import { CATEGORIES } from "@/data/categories";
import type { CategoryId } from "@/types";

export interface DiscoverScreenProps {
  onOpenCategory: (id: CategoryId) => void;
}

export function DiscoverScreen({ onOpenCategory }: DiscoverScreenProps) {
  return (
    <div id="discover-screen">
      <h1 className="page-title">Discover</h1>
      <p className="page-subtitle">Pick a category and start a 10-question run.</p>

      <div className="category-grid">
        {CATEGORIES.map((category) => (
          <button
            key={category.id}
            className={`category-card${category.available ? "" : " category-card--locked"}`}
            onClick={() => category.available && onOpenCategory(category.id)}
            disabled={!category.available}
          >
            {!category.available && <span className="category-card__badge">Coming soon</span>}
            <span className="category-card__emoji">{category.emoji}</span>
            <span className="category-card__label">{category.label}</span>
            <span className={`accent-chip accent-${category.accent}`} style={{ alignSelf: "flex-start" }}>
              {category.available ? "PLAY" : "SOON"}
            </span>
          </button>
        ))}
      </div>
    </div>
  );
}
