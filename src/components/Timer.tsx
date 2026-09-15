export interface TimerProps {
  remainingMs: number;
  timeLimitMs: number;
}

const WARNING_THRESHOLD = 0.5;
const CRITICAL_THRESHOLD = 0.25;

/** Visible countdown bar + seconds readout, escalating normal -> warning -> critical as time runs low (see docs/GAME_DESIGN.md §Timer). */
export function Timer({ remainingMs, timeLimitMs }: TimerProps) {
  const fraction = timeLimitMs > 0 ? Math.min(1, Math.max(0, remainingMs / timeLimitMs)) : 0;
  const seconds = (remainingMs / 1000).toFixed(1);

  const tier = fraction <= CRITICAL_THRESHOLD ? "critical" : fraction <= WARNING_THRESHOLD ? "warning" : "normal";
  const fillClass = tier === "critical" ? "timer-fill timer-fill--critical" : tier === "warning" ? "timer-fill timer-fill--warning" : "timer-fill";

  return (
    <div>
      <div className="timer-track" role="progressbar" aria-valuemin={0} aria-valuemax={timeLimitMs} aria-valuenow={remainingMs}>
        <div className={fillClass} style={{ width: `${fraction * 100}%` }} />
      </div>
      <div style={{ textAlign: "center", fontSize: 12, marginTop: 4, color: tier === "critical" ? "var(--danger)" : "var(--text-dim)", fontVariantNumeric: "tabular-nums" }}>{seconds}</div>
    </div>
  );
}
