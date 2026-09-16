export type NavTab = "HOME" | "DISCOVER" | "PLAY" | "RANKING" | "PROFILE";

export interface BottomNavProps {
  active: NavTab;
  onNavigate: (tab: NavTab) => void;
}

const ITEMS: readonly { tab: NavTab; label: string; icon: string }[] = [
  { tab: "HOME", label: "Home", icon: "🏠" },
  { tab: "DISCOVER", label: "Discover", icon: "🧭" },
  { tab: "PLAY", label: "Play", icon: "▶" },
  { tab: "RANKING", label: "Ranking", icon: "🏆" },
  { tab: "PROFILE", label: "Profile", icon: "👤" },
];

/** PLAY is visually elevated as the primary action — see MASTER PROMPT §5. */
export function BottomNav({ active, onNavigate }: BottomNavProps) {
  return (
    <nav className="bottom-nav">
      {ITEMS.map((item) => {
        if (item.tab === "PLAY") {
          return (
            <button key={item.tab} className="bottom-nav__play" onClick={() => onNavigate(item.tab)} aria-label="Play">
              {item.icon}
            </button>
          );
        }
        return (
          <button key={item.tab} className={`bottom-nav__item${active === item.tab ? " bottom-nav__item--active" : ""}`} onClick={() => onNavigate(item.tab)}>
            <span className="bottom-nav__icon">{item.icon}</span>
            {item.label}
          </button>
        );
      })}
    </nav>
  );
}
