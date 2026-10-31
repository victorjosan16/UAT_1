namespace TollRoadTycoon.Monetization
{
    /// <summary>Timed toll income multiplier (x2 by default). Extending adds time instead of stacking.</summary>
    public sealed class BoostManager
    {
        public double SecondsLeft { get; private set; }
        public long BasisPoints { get; private set; } = 10000;

        public bool IsActive => SecondsLeft > 0;

        /// <summary>Multiplier to apply to tolls right now (10000 = x1).</summary>
        public long CurrentBasisPoints => IsActive ? BasisPoints : 10000;

        public void Activate(double seconds, long basisPoints)
        {
            if (seconds <= 0) return;
            BasisPoints = basisPoints;
            SecondsLeft += seconds;
        }

        public void Tick(double dt)
        {
            if (SecondsLeft <= 0) return;
            SecondsLeft -= dt;
            if (SecondsLeft < 0) SecondsLeft = 0;
        }

        internal void Restore(double seconds, long basisPoints)
        {
            SecondsLeft = seconds > 0 && seconds < 24 * 3600 ? seconds : 0;
            BasisPoints = basisPoints >= 10000 && basisPoints <= 100000 ? basisPoints : 10000;
        }
    }
}
