using System;

namespace Veliqora.Core.Ads
{
    public enum AdOutcome
    {
        /// <summary>The provider reported the rewarded ad was watched to completion. The ONLY outcome that grants.</summary>
        Completed,
        Cancelled,
        Failed,
        NoFill,
        Timeout,
        Unavailable,
    }

    public static class AdPlacements
    {
        public const string DoubleCoins = "rewarded_double_coins";
        public const string BonusBooster = "rewarded_bonus_booster";
        public const string Banner = "banner_menus";
    }

    /// <summary>Provider-agnostic ad interface. Implementations: MockAdsService (editor/testing), NoAdsService, real adapter.</summary>
    public interface IAdsService
    {
        string ProviderName { get; }
        /// <summary>True for test doubles; the UI must label these clearly and they must never pretend to be real ads.</summary>
        bool IsMock { get; }
        bool IsRewardedReady(string placement);
        /// <summary>Shows a rewarded ad. The callback may be invoked more than once by buggy SDKs; callers use RewardedAdGate.</summary>
        void ShowRewarded(string placement, Action<AdOutcome> callback);
        bool SupportsBanner { get; }
        void ShowBanner(string placement);
        void HideBanner();
    }

    /// <summary>Used when ads are disabled or offline: everything reports Unavailable, nothing blocks progression.</summary>
    public sealed class NoAdsService : IAdsService
    {
        public string ProviderName => "None";
        public bool IsMock => false;
        public bool IsRewardedReady(string placement) => false;
        public void ShowRewarded(string placement, Action<AdOutcome> callback) => callback?.Invoke(AdOutcome.Unavailable);
        public bool SupportsBanner => false;
        public void ShowBanner(string placement) { }
        public void HideBanner() { }
    }

    /// <summary>
    /// Guards a single rewarded flow: one request in flight, reward granted at most once and only on Completed,
    /// late/duplicate callbacks ignored, timeout handled via Tick(). Pure logic driven by an injected clock.
    /// </summary>
    public sealed class RewardedAdGate
    {
        private readonly IAdsService _ads;
        private readonly Func<double> _now;
        private readonly double _timeoutSeconds;

        private int _requestSerial;
        private int _activeSerial = -1;
        private double _startedAt;
        private Action _onReward;
        private Action<AdOutcome> _onFinished;

        public bool InFlight => _activeSerial >= 0;
        public double CooldownUntil { get; private set; }

        public RewardedAdGate(IAdsService ads, Func<double> nowSeconds, double timeoutSeconds = 30)
        {
            _ads = ads ?? throw new ArgumentNullException(nameof(ads));
            _now = nowSeconds ?? throw new ArgumentNullException(nameof(nowSeconds));
            _timeoutSeconds = timeoutSeconds;
        }

        public bool CanRequest(string placement) => !InFlight && _now() >= CooldownUntil && _ads.IsRewardedReady(placement);

        /// <summary>
        /// Starts a rewarded request. onReward runs at most once, only after a Completed callback.
        /// onFinished always runs exactly once with the final outcome.
        /// </summary>
        public bool Request(string placement, Action onReward, Action<AdOutcome> onFinished, double cooldownSeconds = 0)
        {
            if (InFlight) return false;
            if (!_ads.IsRewardedReady(placement))
            {
                onFinished?.Invoke(AdOutcome.Unavailable);
                return false;
            }
            int serial = ++_requestSerial;
            _activeSerial = serial;
            _startedAt = _now();
            _onReward = onReward;
            _onFinished = onFinished;
            double cooldown = cooldownSeconds;
            _ads.ShowRewarded(placement, outcome => Resolve(serial, outcome, cooldown));
            return true;
        }

        /// <summary>Call periodically (e.g. each frame) to enforce the timeout.</summary>
        public void Tick()
        {
            if (InFlight && _now() - _startedAt > _timeoutSeconds) Resolve(_activeSerial, AdOutcome.Timeout, 0);
        }

        /// <summary>App backgrounding/scene unload: abandon without granting.</summary>
        public void Abort()
        {
            if (InFlight) Resolve(_activeSerial, AdOutcome.Cancelled, 0);
        }

        private void Resolve(int serial, AdOutcome outcome, double cooldown)
        {
            if (serial != _activeSerial) return; // late or duplicate callback
            _activeSerial = -1;
            var reward = _onReward;
            var finished = _onFinished;
            _onReward = null;
            _onFinished = null;
            if (outcome == AdOutcome.Completed)
            {
                CooldownUntil = _now() + cooldown;
                reward?.Invoke();
            }
            finished?.Invoke(outcome);
        }
    }
}
