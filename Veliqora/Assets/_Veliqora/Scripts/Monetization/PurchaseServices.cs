using System;

namespace Veliqora.Monetization
{
    public enum PurchaseOutcome { Purchased, Cancelled, Failed, Unavailable }

    /// <summary>
    /// Optional real-money purchases (e.g. Unity IAP / Google Play Billing). Products are only offered when a real
    /// store implementation is configured; there is no simulated checkout in the game.
    /// </summary>
    public interface IPurchaseService
    {
        bool IsAvailable { get; }
        string ProviderName { get; }
        void Purchase(string productId, Action<PurchaseOutcome> callback);
        void RestorePurchases(Action<bool> callback);
    }

    /// <summary>Default: store disabled. The shop hides real-money products; gameplay is unaffected.</summary>
    public sealed class NoPurchaseService : IPurchaseService
    {
        public bool IsAvailable => false;
        public string ProviderName => "None";
        public void Purchase(string productId, Action<PurchaseOutcome> callback) => callback?.Invoke(PurchaseOutcome.Unavailable);
        public void RestorePurchases(Action<bool> callback) => callback?.Invoke(false);
    }

    public enum ConsentStatus { Unknown, NotRequired, Required, Obtained, Denied }

    /// <summary>
    /// Advertising consent / privacy choices (to be backed by a certified CMP such as Google UMP before release).
    /// Until then ads stay in mock (development) or disabled (release) mode, so no personal data is processed.
    /// </summary>
    public interface IConsentService
    {
        ConsentStatus Status { get; }
        bool CanRequestAds { get; }
        void ShowPrivacyOptions(Action done);
    }

    public sealed class PlaceholderConsentService : IConsentService
    {
        public ConsentStatus Status => ConsentStatus.Unknown;
        public bool CanRequestAds => true; // only mock/no ads exist until a real SDK + CMP are integrated
        public void ShowPrivacyOptions(Action done) => done?.Invoke();
    }
}
