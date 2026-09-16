import { useLanguage } from "@/i18n/LanguageContext";
import type { StringKey } from "@/i18n/strings";

export type NavTab = "HOME" | "DISCOVER" | "PLAY" | "RANKING" | "PROFILE";

export interface BottomNavProps {
  active: NavTab;
  onNavigate: (tab: NavTab) => void;
}

const ITEMS: readonly { tab: NavTab; labelKey: StringKey; icon: string }[] = [
  { tab: "HOME", labelKey: "nav.home", icon: "🏠" },
  { tab: "DISCOVER", labelKey: "nav.discover", icon: "🧭" },
  { tab: "PLAY", labelKey: "nav.play", icon: "▶" },
  { tab: "RANKING", labelKey: "nav.ranking", icon: "🏆" },
  { tab: "PROFILE", labelKey: "nav.profile", icon: "👤" },
];

/** PLAY is visually elevated as the primary action — see MASTER PROMPT §5. */
export function BottomNav({ active, onNavigate }: BottomNavProps) {
  const { t } = useLanguage();

  return (
    <nav className="bottom-nav">
      {ITEMS.map((item) => {
        const label = t(item.labelKey);
        if (item.tab === "PLAY") {
          return (
            <button key={item.tab} className="bottom-nav__play" onClick={() => onNavigate(item.tab)} aria-label={label}>
              {item.icon}
            </button>
          );
        }
        return (
          <button key={item.tab} className={`bottom-nav__item${active === item.tab ? " bottom-nav__item--active" : ""}`} onClick={() => onNavigate(item.tab)}>
            <span className="bottom-nav__icon">{item.icon}</span>
            {label}
          </button>
        );
      })}
    </nav>
  );
}
