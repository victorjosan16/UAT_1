export interface PlayScreenProps {
  onPlayQuick: () => void;
  onPlayDaily: () => void;
  onPlayLevel: () => void;
  onOpenDiscover: () => void;
}

export function PlayScreen({ onPlayQuick, onPlayDaily, onPlayLevel, onOpenDiscover }: PlayScreenProps) {
  return (
    <div id="play-screen">
      <h1 className="page-title">Play</h1>
      <p className="page-subtitle">Choose how you want to play.</p>

      <button className="mode-card" onClick={onPlayQuick}>
        <div className="mode-card__icon accent-emerald">⚡</div>
        <div>
          <p className="mode-card__title">Quick Play</p>
          <p className="mode-card__desc">10 mixed questions, straight in.</p>
        </div>
      </button>

      <button className="mode-card" onClick={onOpenDiscover}>
        <div className="mode-card__icon accent-cyan">🧭</div>
        <div>
          <p className="mode-card__title">Category Quiz</p>
          <p className="mode-card__desc">Pick a topic and test it in depth.</p>
        </div>
      </button>

      <button className="mode-card" onClick={onPlayDaily}>
        <div className="mode-card__icon accent-amber">📅</div>
        <div>
          <p className="mode-card__title">Daily Challenge</p>
          <p className="mode-card__desc">Same 10 questions for everyone today.</p>
        </div>
      </button>

      <button className="mode-card" onClick={onPlayLevel}>
        <div className="mode-card__icon accent-violet">🗺️</div>
        <div>
          <p className="mode-card__title">Level Journey</p>
          <p className="mode-card__desc">20 levels, Rookie to Genius, then Endless.</p>
        </div>
      </button>

      <button className="mode-card mode-card--locked" disabled>
        <div className="mode-card__icon accent-coral">🏆</div>
        <div>
          <p className="mode-card__title">5-Player Championship</p>
          <p className="mode-card__desc">Live multiplayer — coming soon.</p>
        </div>
      </button>

      <button className="mode-card mode-card--locked" disabled>
        <div className="mode-card__icon accent-emerald">🤝</div>
        <div>
          <p className="mode-card__title">Challenge a Friend</p>
          <p className="mode-card__desc">Async 1v1 — coming soon.</p>
        </div>
      </button>
    </div>
  );
}
