using System;
using UnityEngine;
using UnityEngine.UI;
using Veliqora.Infrastructure;

namespace Veliqora.UI.Screens
{
    /// <summary>Settings and Pause overlays (shared by Home, Map and Gameplay).</summary>
    public static class CommonOverlays
    {
        public const string PrivacyPolicyUrl = ""; // PLACEHOLDER: set your hosted privacy policy URL before release
        public const string TermsUrl = "";         // PLACEHOLDER

        public static OverlayHandle ShowSettings(AppServices s, Action onClosed = null)
        {
            var h = Overlays.Open("Settings", new Vector2(960, 1560), o =>
            {
                var set = s.Settings;
                var d = set.Data;
                float y = -10;
                const float row = 118;
                Toggle(o.Content, "Music", d.Music, set.SetMusic, ref y, row);
                Toggle(o.Content, "Sound effects", d.Sound, set.SetSound, ref y, row);
                Toggle(o.Content, "Vibration", d.Haptics, v => { set.SetHaptics(v); if (v) s.Haptics.Medium(); }, ref y, row);
                Toggle(o.Content, "Reduced motion", d.ReducedMotion, set.SetReducedMotion, ref y, row);
                Toggle(o.Content, "Color symbols (color-blind assist)", d.ColorblindSymbols, set.SetColorblind, ref y, row);
                Toggle(o.Content, "Light gameplay background", d.LightGameplayBackground, set.SetLightGameplay, ref y, row);

                // Animation speed cycle
                var speedLabel = UIFactory.Text(o.Content, "Animation speed", UIStyle.FontLabel, UIStyle.Ink, TextAnchor.MiddleLeft);
                UIFactory.Band(speedLabel.rectTransform, 1f, row, y, 0, 1f);
                ButtonHandle speed = null;
                speed = UIFactory.Button(o.Content, set.AnimationSpeedLabel, UIStyle.ButtonKind.Accent, () =>
                {
                    set.CycleAnimationSpeed();
                    speed.SetLabel(set.AnimationSpeedLabel);
                }, new Vector2(260, 100), null, UIStyle.FontLabel);
                UIFactory.Place(speed.Rect, new Vector2(1, 1), speed.Rect.sizeDelta, new Vector2(0, y - 9), new Vector2(1, 1));
                y -= row;

                if (s.Ads.IsMock)
                {
                    Toggle(o.Content, "Debug: simulate ad failure (mock ads)", d.DebugSimulateAdFailure, set.SetSimulateAdFailure, ref y, row);
                }

                y -= 20;
                var privacy = UIFactory.Button(o.Content, "Privacy policy", UIStyle.ButtonKind.Secondary, () => OpenOrExplain(PrivacyPolicyUrl, "Privacy policy"), new Vector2(420, 110), null, UIStyle.FontLabel);
                UIFactory.Place(privacy.Rect, new Vector2(0, 1), privacy.Rect.sizeDelta, new Vector2(0, y), new Vector2(0, 1));
                var terms = UIFactory.Button(o.Content, "Terms of use", UIStyle.ButtonKind.Secondary, () => OpenOrExplain(TermsUrl, "Terms of use"), new Vector2(420, 110), null, UIStyle.FontLabel);
                UIFactory.Place(terms.Rect, new Vector2(1, 1), terms.Rect.sizeDelta, new Vector2(0, y), new Vector2(1, 1));
                y -= 130;
                var consent = UIFactory.Button(o.Content, "Ad privacy choices", UIStyle.ButtonKind.Secondary, () =>
                    Overlays.Message("Ad privacy choices", s.Ads.IsMock
                        ? "Mock ads are active: no ad network is contacted and no advertising data is collected.\n\nA consent flow must be integrated with your production ad SDK before release."
                        : "No ad provider is active in this build."), new Vector2(860, 110), null, UIStyle.FontLabel);
                UIFactory.Place(consent.Rect, new Vector2(0.5f, 1), consent.Rect.sizeDelta, new Vector2(0, y), new Vector2(0.5f, 1));
                y -= 140;

                var reset = UIFactory.Button(o.Content, "Reset progress", UIStyle.ButtonKind.Danger, () =>
                {
                    Overlays.Confirm("Reset progress?", "This erases levels, stars, coins, boosters and themes on this device. It can't be undone.", "Erase", () =>
                        Overlays.Confirm("Are you sure?", "Last chance — all progress will be deleted.", "Yes, erase", () => ResetProgress(s), "Keep", UIStyle.ButtonKind.Danger),
                        "Cancel", UIStyle.ButtonKind.Danger);
                }, new Vector2(560, 120), null, UIStyle.FontLabel);
                UIFactory.Place(reset.Rect, new Vector2(0.5f, 1), reset.Rect.sizeDelta, new Vector2(0, y), new Vector2(0.5f, 1));

                var version = UIFactory.Text(o.Content, "Veliqora " + Application.version + "  ·  Ads: " + s.Ads.ProviderName, UIStyle.FontCaption, UIStyle.InkSoft, TextAnchor.LowerCenter, FontStyle.Normal);
                UIFactory.Band(version.rectTransform, 0f, 60, 0, 0, 0f);
            }, true, "Settings");
            h.Closed += () => { s.SaveNow(); onClosed?.Invoke(); };
            return h;
        }

        private static void OpenOrExplain(string url, string title)
        {
            if (string.IsNullOrEmpty(url))
                Overlays.Message(title, title + " link isn't configured yet.\n(Set it in CommonOverlays before release.)");
            else Application.OpenURL(url);
        }

        private static void ResetProgress(AppServices s)
        {
            Overlays.CloseAll();
            s.Root.ResetAllProgress();
        }

        private static void Toggle(RectTransform parent, string label, bool value, Action<bool> onChange, ref float y, float row)
        {
            var t = UIFactory.Text(parent, label, UIStyle.FontLabel, UIStyle.Ink, TextAnchor.MiddleLeft);
            UIFactory.Band(t.rectTransform, 1f, row, y, 0, 1f);
            t.rectTransform.offsetMax = new Vector2(-230, t.rectTransform.offsetMax.y);
            var sw = ToggleSwitch.Create(parent, value, onChange);
            UIFactory.Place(sw, new Vector2(1, 1), new Vector2(180, 96), new Vector2(0, y - 11), new Vector2(1, 1));
            y -= row;
        }

        public static OverlayHandle ShowPause(AppServices s, Action onResume, Action onRestart, Action onMap)
        {
            var h = Overlays.Open("Pause", new Vector2(900, 1080), o =>
            {
                var resume = UIFactory.Button(o.Content, "Resume", UIStyle.ButtonKind.Primary, () => o.Close(), new Vector2(640, UIStyle.ButtonHeight), Icon.Play);
                UIFactory.Place(resume.Rect, new Vector2(0.5f, 1), resume.Rect.sizeDelta, new Vector2(0, -20), new Vector2(0.5f, 1));
                var restart = UIFactory.Button(o.Content, "Restart level", UIStyle.ButtonKind.Gold, () =>
                    Overlays.Confirm("Restart level?", "Your moves on this level will be reset.", "Restart", () => { o.Close(); onRestart?.Invoke(); }), new Vector2(640, UIStyle.ButtonHeight), Icon.Restart);
                UIFactory.Place(restart.Rect, new Vector2(0.5f, 1), restart.Rect.sizeDelta, new Vector2(0, -200), new Vector2(0.5f, 1));
                var settings = UIFactory.Button(o.Content, "Settings", UIStyle.ButtonKind.Accent, () => ShowSettings(s), new Vector2(640, UIStyle.ButtonHeight), Icon.Gear);
                UIFactory.Place(settings.Rect, new Vector2(0.5f, 1), settings.Rect.sizeDelta, new Vector2(0, -380), new Vector2(0.5f, 1));
                var map = UIFactory.Button(o.Content, "Level map", UIStyle.ButtonKind.Secondary, () => { o.Close(); onMap?.Invoke(); }, new Vector2(640, UIStyle.ButtonHeight), Icon.Map);
                UIFactory.Place(map.Rect, new Vector2(0.5f, 1), map.Rect.sizeDelta, new Vector2(0, -560), new Vector2(0.5f, 1));
                var hint = UIFactory.Text(o.Content, "Your progress on this level is saved.", UIStyle.FontCaption, UIStyle.InkSoft, TextAnchor.LowerCenter, FontStyle.Normal);
                UIFactory.Band(hint.rectTransform, 0f, 60, 0, 0, 0f);
            }, true, "Paused");
            h.Closed += () => onResume?.Invoke();
            return h;
        }
    }

    /// <summary>Accessible on/off switch (large hit area, color + knob position + text label).</summary>
    public static class ToggleSwitch
    {
        public static RectTransform Create(RectTransform parent, bool value, Action<bool> onChange)
        {
            var root = UIFactory.Rect("Switch", parent);
            var track = UIFactory.Image(root, "Track", SpriteLibrary.Rounded(48), value ? UIStyle.Mint : UIStyle.Disabled, true);
            var knob = UIFactory.Image(root, "Knob", SpriteLibrary.Circle, Color.white);
            var label = UIFactory.Text(track.transform, value ? "ON" : "OFF", UIStyle.FontCaption, Color.white);
            UIFactory.AddShadow(knob, UIStyle.Shadow, new Vector2(0, -4));
            bool state = value;
            Action layout = () =>
            {
                track.color = state ? UIStyle.Mint : UIStyle.Disabled;
                label.text = state ? "ON" : "OFF";
                label.alignment = state ? TextAnchor.MiddleLeft : TextAnchor.MiddleRight;
                label.rectTransform.offsetMin = new Vector2(22, 0);
                label.rectTransform.offsetMax = new Vector2(-22, 0);
                UIFactory.Place(knob.rectTransform, new Vector2(state ? 1 : 0, 0.5f), new Vector2(80, 80), new Vector2(state ? -8 : 8, 0), new Vector2(state ? 1 : 0, 0.5f));
            };
            layout();
            var btn = track.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            track.gameObject.AddComponent<ButtonFx>().Target = root;
            btn.onClick.AddListener(() =>
            {
                state = !state;
                layout();
                onChange?.Invoke(state);
            });
            return root;
        }
    }
}
