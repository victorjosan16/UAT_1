export interface TimerProps {
  remainingMs: number;
  timeLimitMs: number;
}

const CRITICAL_THRESHOLD = 0.25;

/** Visible countdown bar + seconds readout, escalating normal -> critical as time runs low (see docs/GAME_DESIGN.md §Timer). */
export function Timer({ remainingMs, timeLimitMs }: TimerProps) {
  const fraction = timeLimitMs > 0 ? Math.min(1, Math.max(0, remainingMs / timeLimitMs)) : 0;
  const seconds = (remainingMs / 1000).toFixed(1);

  const isCritical = fraction <= CRITICAL_THRESHOLD;
  const fillClass = isCritical ? "pitch-timer-fill pitch-timer-fill--critical" : "pitch-timer-fill";

  return (
    <div>
      <div className="pitch-timer-track" role="progressbar" aria-valuemin={0} aria-valuemax={timeLimitMs} aria-valuenow={remainingMs}>
        <div className={fillClass} style={{ width: `${fraction * 100}%` }} />
      </div>
      <div className="pitch-timer-label">{seconds}</div>
    </div>
  );
}
