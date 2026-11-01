using System;

namespace VoxelBloom.Core
{
    public enum AdPlacement { DoubleReward, ExtraBooster, ExtraLife }
    public enum AdOutcome { Completed, Skipped, Failed, Unavailable }

    /// <summary>Provider-independent rewarded ad contract. A real SDK adapter implements this.</summary>
    public interface IRewardedAdService
    {
        string ProviderName { get; }
        /// <summary>True for mock/test providers. Rewards are never granted from these.</summary>
        bool IsTestProvider { get; }
        bool IsReady(AdPlacement placement);
        void Show(AdPlacement placement, Action<AdOutcome> onFinished);
    }

    /// <summary>Default when no SDK is configured: always unavailable.</summary>
    public sealed class NoAdsService : IRewardedAdService
    {
        public string ProviderName => "None";
        public bool IsTestProvider => false;
        public bool IsReady(AdPlacement placement) => false;
        public void Show(AdPlacement placement, Action<AdOutcome> onFinished) => onFinished?.Invoke(AdOutcome.Unavailable);
    }

    /// <summary>Explicitly labelled test provider. Completes instantly but its callbacks never grant rewards.</summary>
    public sealed class MockTestAdService : IRewardedAdService
    {
        public string ProviderName => "MOCK (test only - no rewards)";
        public bool IsTestProvider => true;
        public bool IsReady(AdPlacement placement) => true;
        public void Show(AdPlacement placement, Action<AdOutcome> onFinished) => onFinished?.Invoke(AdOutcome.Completed);
    }

    public static class RewardGate
    {
        /// <summary>The only place deciding whether an ad callback may grant a reward.</summary>
        public static bool MayGrant(IRewardedAdService provider, AdOutcome outcome)
            => provider != null && !provider.IsTestProvider && outcome == AdOutcome.Completed;
    }
}
