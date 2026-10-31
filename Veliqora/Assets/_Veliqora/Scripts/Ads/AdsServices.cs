using System;
using UnityEngine;
using UnityEngine.UI;
using Veliqora.Core.Ads;
using Veliqora.Core.Json;
using Veliqora.Core.Save;
using Veliqora.UI;

namespace Veliqora.Ads
{
    /// <summary>Parsed Resources/Veliqora/Config/ads.json. IDs are PLACEHOLDERS until real credentials are provided.</summary>
    public sealed class AdsConfig
    {
        public string Mode = "mock";              // mock | none | levelplay
        public bool BannerEnabled;
        public bool AllowMockInReleaseBuilds;
        public bool ChildDirected;
        public float RewardedTimeoutSeconds = 30f;
        public string AndroidAppKey = "REPLACE_WITH_ANDROID_APP_KEY";
        public string RewardedAdUnitId = "REPLACE_WITH_REWARDED_AD_UNIT_ID";
        public string BannerAdUnitId = "REPLACE_WITH_BANNER_AD_UNIT_ID";

        public bool HasRealCredentials => !AndroidAppKey.StartsWith("REPLACE_") && !RewardedAdUnitId.StartsWith("REPLACE_");

        public static AdsConfig Load()
        {
            var cfg = new AdsConfig();
            var text = Resources.Load<TextAsset>("Veliqora/Config/ads");
            if (text == null) return cfg;
            try
            {
                var d = MiniJson.AsObject(MiniJson.Parse(text.text));
                cfg.Mode = MiniJson.GetString(d, "mode", cfg.Mode);
                cfg.BannerEnabled = MiniJson.GetBool(d, "bannerEnabled");
                cfg.AllowMockInReleaseBuilds = MiniJson.GetBool(d, "allowMockInReleaseBuilds");
                cfg.ChildDirected = MiniJson.GetBool(d, "childDirected");
                cfg.RewardedTimeoutSeconds = (float)MiniJson.GetDouble(d, "rewardedTimeoutSeconds", 30);
                var a = MiniJson.GetObject(d, "android");
                cfg.AndroidAppKey = MiniJson.GetString(a, "appKey", cfg.AndroidAppKey);
                cfg.RewardedAdUnitId = MiniJson.GetString(a, "rewardedAdUnitId", cfg.RewardedAdUnitId);
                cfg.BannerAdUnitId = MiniJson.GetString(a, "bannerAdUnitId", cfg.BannerAdUnitId);
            }
            catch (Exception e)
            {
                Debug.LogWarning("[Veliqora] ads.json invalid, ads disabled: " + e.Message);
                cfg.Mode = "none";
            }
            return cfg;
        }
    }

    public static class AdsFactory
    {
        /// <summary>
        /// Picks the ad implementation. Mock ads are refused in non-development player builds unless explicitly
        /// allowed, so a test double can never ship pretending to be real ads.
        /// </summary>
        public static IAdsService Create(AdsConfig cfg, SettingsData settings)
        {
            bool isReleasePlayer = !Application.isEditor && !Debug.isDebugBuild;
            switch (cfg.Mode)
            {
                case "mock":
                    if (isReleasePlayer && !cfg.AllowMockInReleaseBuilds) return new NoAdsService();
                    return new MockAdsService(settings, cfg.BannerEnabled);
#if VELIQORA_LEVELPLAY
                case "levelplay":
                    return new LevelPlayAdsAdapter(cfg);
#endif
                default:
                    return new NoAdsService();
            }
        }
    }

    /// <summary>
    /// Editor/testing ad double. Shows a full-screen panel clearly labelled "MOCK AD — TEST ONLY". The tester
    /// chooses the outcome (finish → Completed, close → Cancelled). Offline or "simulate failure" → NoFill.
    /// </summary>
    public sealed class MockAdsService : IAdsService
    {
        private readonly SettingsData _settings;
        private readonly bool _banner;
        private GameObject _bannerGo;

        public MockAdsService(SettingsData settings, bool bannerEnabled)
        {
            _settings = settings;
            _banner = bannerEnabled;
        }

        public string ProviderName => "Mock (test only)";
        public bool IsMock => true;
        public bool SupportsBanner => _banner;

        private static bool Offline => Application.internetReachability == NetworkReachability.NotReachable;

        public bool IsRewardedReady(string placement) => !Offline && !(_settings != null && _settings.DebugSimulateAdFailure);

        public void ShowRewarded(string placement, Action<AdOutcome> callback)
        {
            if (Offline) { callback?.Invoke(AdOutcome.Unavailable); return; }
            if (_settings != null && _settings.DebugSimulateAdFailure) { callback?.Invoke(AdOutcome.NoFill); return; }

            Overlays.Open("MockAd", Vector2.zero, h =>
            {
                var bg = UIFactory.Image(h.Content, "AdBg", SpriteLibrary.Pixel, UIStyle.Hex("#2A1E5C"), true);
                var title = UIFactory.Text(h.Content, "MOCK AD — TEST ONLY", UIStyle.FontTitle, UIStyle.Sunshine);
                UIFactory.Band(title.rectTransform, 0.72f, 160, 0);
                var body = UIFactory.Text(h.Content,
                    "This is a simulated rewarded ad for development.\nNo real advertisement is shown and no ad network is contacted.\n\nPlacement: " + placement,
                    UIStyle.FontBody, Color.white, TextAnchor.MiddleCenter, FontStyle.Normal);
                UIFactory.Band(body.rectTransform, 0.5f, 520, 0, 80);
                var finish = UIFactory.Button(h.Content, "Finish (simulate reward)", UIStyle.ButtonKind.Primary, () =>
                {
                    h.Close();
                    callback?.Invoke(AdOutcome.Completed);
                }, new Vector2(760, UIStyle.ButtonHeight));
                UIFactory.Place(finish.Rect, new Vector2(0.5f, 0.28f), finish.Rect.sizeDelta, Vector2.zero);
                var close = UIFactory.Button(h.Content, "Close early (simulate cancel)", UIStyle.ButtonKind.Secondary, () =>
                {
                    h.Close();
                    callback?.Invoke(AdOutcome.Cancelled);
                }, new Vector2(760, UIStyle.ButtonHeight));
                UIFactory.Place(close.Rect, new Vector2(0.5f, 0.17f), close.Rect.sizeDelta, Vector2.zero);
            }, dismissable: false, title: null, panel: false);
        }

        public void ShowBanner(string placement)
        {
            if (!_banner || _bannerGo != null) return;
            var layer = Overlays.Layer;
            var safe = UIFactory.SafeArea(layer);
            safe.name = "MockBanner";
            _bannerGo = safe.gameObject;
            var bar = UIFactory.Image(safe, "Bar", SpriteLibrary.Pixel, new Color(0.15f, 0.15f, 0.2f, 0.92f), true);
            UIFactory.Band(bar.rectTransform, 0f, 110, 0, 0, 0f);
            var t = UIFactory.Text(bar.transform, "MOCK BANNER AD (test only — not a real ad)", UIStyle.FontCaption, Color.white);
            UIFactory.Stretch(t.rectTransform);
        }

        public void HideBanner()
        {
            if (_bannerGo != null) UnityEngine.Object.Destroy(_bannerGo);
            _bannerGo = null;
        }
    }

#if VELIQORA_LEVELPLAY
    /// <summary>
    /// Production adapter PLACEHOLDER. Intentionally not implemented: SDK APIs change between versions and we
    /// do not invent method names. To integrate: install the official Unity LevelPlay package compatible with
    /// your Unity version, follow its current integration guide, and implement these members using the SDK's
    /// rewarded/banner callbacks — granting rewards ONLY from the SDK's "rewarded" (completed) callback.
    /// Until then this adapter reports ads as unavailable.
    /// </summary>
    public sealed class LevelPlayAdsAdapter : IAdsService
    {
        private readonly AdsConfig _cfg;
        public LevelPlayAdsAdapter(AdsConfig cfg)
        {
            _cfg = cfg;
            Debug.LogWarning("[Veliqora] LevelPlayAdsAdapter is a placeholder; ads are disabled until it is implemented.");
        }
        public string ProviderName => "LevelPlay (not implemented)";
        public bool IsMock => false;
        public bool IsRewardedReady(string placement) => false;
        public void ShowRewarded(string placement, Action<AdOutcome> callback) => callback?.Invoke(AdOutcome.Unavailable);
        public bool SupportsBanner => false;
        public void ShowBanner(string placement) { }
        public void HideBanner() { }
    }
#endif
}
