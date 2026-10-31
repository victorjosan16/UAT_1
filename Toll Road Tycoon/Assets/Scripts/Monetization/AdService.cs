using System;
using System.Collections.Generic;

namespace TollRoadTycoon.Monetization
{
    public static class AdPlacements
    {
        public const string OfflineDouble = "offline_x2";
        public const string IncomeBoost = "income_boost";
        public const string InstantCash = "instant_cash";
    }

    public enum AdResult
    {
        Completed,
        Skipped,
        Failed,
        Unavailable,
    }

    /// <summary>
    /// Rewarded-ads abstraction. The game works fully without any ad SDK: the default service is
    /// unavailable and never rewards. No production ids are stored here.
    /// </summary>
    public interface IAdService
    {
        /// <summary>Short label for the UI, e.g. "TEST".</summary>
        string Mode { get; }
        bool IsAvailable(string placement);
        /// <summary>Shows a rewarded ad; the callback is invoked exactly once.</summary>
        void Show(string placement, Action<AdResult> onFinished);
    }

    public sealed class DisabledAdService : IAdService
    {
        public string Mode => "OFF";
        public bool IsAvailable(string placement) => false;
        public void Show(string placement, Action<AdResult> onFinished) => onFinished?.Invoke(AdResult.Unavailable);
    }

    /// <summary>
    /// Test-mode ads for development builds: a request stays pending until the host UI (a clearly
    /// labelled "test ad" screen) completes or skips it. Nothing is awarded unless completed.
    /// </summary>
    public sealed class TestAdService : IAdService
    {
        public sealed class Request
        {
            public string Placement;
            internal Action<AdResult> Callback;
        }

        private Request _pending;

        public string Mode => "TEST";
        public Request Pending => _pending;
        public event Action<Request> Started;

        public bool IsAvailable(string placement) => _pending == null;

        public void Show(string placement, Action<AdResult> onFinished)
        {
            if (_pending != null)
            {
                onFinished?.Invoke(AdResult.Failed);
                return;
            }
            _pending = new Request { Placement = placement, Callback = onFinished };
            Started?.Invoke(_pending);
        }

        public void Finish(AdResult result)
        {
            var request = _pending;
            if (request == null) return;
            _pending = null;
            request.Callback?.Invoke(result);
        }
    }

    /// <summary>Grants each ad reward at most once, and only for a completed ad.</summary>
    public sealed class AdRewards
    {
        private readonly IAdService _service;
        private readonly HashSet<long> _granted = new HashSet<long>();
        private long _nextId = 1;

        public AdRewards(IAdService service)
        {
            _service = service ?? new DisabledAdService();
        }

        public IAdService Service => _service;

        public bool IsAvailable(string placement) => _service.IsAvailable(placement);

        /// <summary>Shows an ad and runs grant only if it completes. Returns false if no ad could start.</summary>
        public bool Request(string placement, Action grant, Action<AdResult> onFinished = null)
        {
            if (!_service.IsAvailable(placement)) return false;
            long id = _nextId++;
            _service.Show(placement, result =>
            {
                if (result == AdResult.Completed && _granted.Add(id)) grant?.Invoke();
                onFinished?.Invoke(result);
            });
            return true;
        }
    }
}
