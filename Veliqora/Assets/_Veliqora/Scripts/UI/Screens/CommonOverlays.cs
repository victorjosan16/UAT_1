using System;
using UnityEngine;
using UnityEngine.UI;
using Veliqora.Infrastructure;

namespace Veliqora.UI.Screens
{
    /// <summary>Settings (overlay + full Settings scene share one builder) and the Pause overlay.</summary>
    public static class CommonOverlays
    {
        public const string PrivacyPolicyUrl = ""; // PLACEHOLDER: set your hosted privacy policy URL before release
        public const string TermsUrl = "";         // PLACEHOLDER

        public static OverlayHandle ShowSettings(AppServices s, Action onClosed = null)
        {
            var h = Overlays.Open("Settings", new Vector2(960, 1640), o => BuildSettings(o.Content, s, inOverlay: true), true, Loc.T("Settings"));
            h.Closed += () => { s.SaveNow(); onClosed?.Invoke(); };
            return h;
        }

        /// <summary>Builds every settings control into <paramref name="content"/> (top-anchored rows).</summary>
        public static void BuildSettings(RectTransform content, AppServices s, bool inOverlay)
        {
            var set = s.Settings;
            var d = set.Data;
            float y = -10;
            const float row = 112;
            Toggle(content, Loc.T("Music"), d.Music, set.SetMusic, ref y, row);
            Toggle(content, Loc.T("Sound effects"), d.Sound, set.SetSound, ref y, row);
            Toggle(content, Loc.T("Vibration"), d.Haptics, v => { set.SetHaptics(v); if (v) s.Haptics.Medium(); }, ref y, row);
            Toggle(content, Loc.T("Reduced motion"), d.ReducedMotion, set.SetReducedMotion, ref y, row);
            Toggle(content, Loc.T("Color symbols (color-blind assist)"), d.ColorblindSymbols, set.SetColorblind, ref y, row);
            Toggle(content, Loc.T("Light gameplay background"), d.LightGameplayBackground, set.SetLightGameplay, ref y, row);

            // Animation speed cycle
            var speedLabel = UIFactory.Text(content, Loc.T("Animation speed"), UIStyle.FontLabel, UIStyle.Ink, TextAnchor.MiddleLeft);
            UIFactory.Band(speedLabel.rectTransform, 1f, row, y, 0, 1f);
            ButtonHandle speed = null;
            speed = UIFactory.Button(content, Loc.T(set.AnimationSpeedLabel), UIStyle.ButtonKind.Accent, () =>
            {
                set.CycleAnimationSpeed();
                speed.SetLabel(Loc.T(set.AnimationSpeedLabel));
            }, new Vector2(260, 96), null, UIStyle.FontLabel);
            UIFactory.Place(speed.Rect, new Vector2(1, 1), speed.Rect.sizeDelta, new Vector2(0, y - 8), new Vector2(1, 1));
            y -= row;

            // Language (language-ready architecture: strings_<lang>.json tables)
            var langLabel = UIFactory.Text(content, Loc.T("Language"), UIStyle.FontLabel, UIStyle.Ink, TextAnchor.MiddleLeft);
            UIFactory.Band(langLabel.rectTransform, 1f, row, y, 0, 1f);
            var lang = UIFactory.Button(content, Loc.LanguageName(Loc.Current.Language), UIStyle.ButtonKind.Accent, () =>
            {
                int i = Array.IndexOf(Loc.Supported, Loc.Current.Language);
                string next = Loc.Supported[(i + 1) % Loc.Supported.Length];
                set.SetLanguage(next);
                Loc.Load(next);
                Overlays.CloseAll();
                s.Navigator.Go(s.Navigator.CurrentId == ScreenId.Gameplay ? ScreenId.Gameplay : s.Navigator.CurrentId, s.Navigator.CurrentArgs);
            }, new Vector2(260, 96), null, UIStyle.FontLabel);
            UIFactory.Place(lang.Rect, new Vector2(1, 1), lang.Rect.sizeDelta, new Vector2(0, y - 8), new Vector2(1, 1));
            y -= row;

            if (s.Ads.IsMock)
                Toggle(content, Loc.T("Debug: simulate ad failure (mock ads)"), d.DebugSimulateAdFailure, set.SetSimulateAdFailure, ref y, row);

            y -= 16;
            var privacy = UIFactory.Button(content, Loc.T("Privacy policy"), UIStyle.ButtonKind.Secondary, () => OpenOrExplain(PrivacyPolicyUrl, Loc.T("Privacy policy")), new Vector2(420, 104), null, UIStyle.FontLabel);
            UIFactory.Place(privacy.Rect, new Vector2(0, 1), privacy.Rect.sizeDelta, new Vector2(0, y), new Vector2(0, 1));
            var terms = UIFactory.Button(content, Loc.T("Terms of use"), UIStyle.ButtonKind.Secondary, () => OpenOrExplain(TermsUrl, Loc.T("Terms of use")), new Vector2(420, 104), null, UIStyle.FontLabel);
            UIFactory.Place(terms.Rect, new Vector2(1, 1), terms.Rect.sizeDelta, new Vector2(0, y), new Vector2(1, 1));
            y -= 122;
            var consent = UIFactory.Button(content, Loc.T("Ad privacy choices"), UIStyle.ButtonKind.Secondary, () =>
                s.Consent.ShowPrivacyOptions(() => Overlays.Message(Loc.T("Ad privacy choices"), s.Ads.IsMock
                    ? Loc.T("Mock ads are active: no ad network is contacted and no advertising data is collected.\n\nA consent flow must be integrated with your production ad SDK before release.")
                    : Loc.T("No ad provider is active in this build."))), new Vector2(860, 104), null, UIStyle.FontLabel);
            UIFactory.Place(consent.Rect, new Vector2(0.5f, 1), consent.Rect.sizeDelta, new Vector2(0, y), new Vector2(0.5f, 1));
            y -= 130;

            var reset = UIFactory.Button(content, Loc.T("Reset progress"), UIStyle.ButtonKind.Danger, () =>
            {
                Overlays.Confirm(Loc.T("Reset progress?"), Loc.T("This erases levels, stars, coins, boosters and themes on this device. It can't be undone."), Loc.T("Erase"), () =>
                    Overlays.Confirm(Loc.T("Are you sure?"), Loc.T("Last chance — all progress will be deleted."), Loc.T("Yes, erase"), () => ResetProgress(s), Loc.T("Keep"), UIStyle.ButtonKind.Danger),
                    Loc.T("Cancel"), UIStyle.ButtonKind.Danger);
            }, new Vector2(560, 112), null, UIStyle.FontLabel);
            UIFactory.Place(reset.Rect, new Vector2(0.5f, 1), reset.Rect.sizeDelta, new Vector2(0, y), new Vector2(0.5f, 1));

            var version = UIFactory.Text(content, "Veliqora " + Application.version + "  ·  " + Loc.T("Ads") + ": " + s.Ads.ProviderName, UIStyle.FontCaption, UIStyle.InkSoft, TextAnchor.LowerCenter, FontStyle.Normal);
            UIFactory.Band(version.rectTransform, 0f, 60, 0, 0, 0f);
        }

        private static void OpenOrExplain(string url, string title)
        {
            if (string.IsNullOrEmpty(url))
                Overlays.Message(title, Loc.T("This link isn't configured yet."));
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
            UIFactory.Place(sw, new Vector2(1, 1), new Vector2(180, 92), new Vector2(0, y - 10), new Vector2(1, 1));
            y -= row;
        }

        public static OverlayHandle ShowPause(AppServices s, Action onResume, Action onRestart, Action onMap)
        {
            var h = Overlays.Open("Pause", new Vector2(900, 1080), o =>
            {
                var resume = UIFactory.Button(o.Content, Loc.T("Resume"), UIStyle.ButtonKind.Primary, () => o.Close(), new Vector2(640, UIStyle.ButtonHeight), Icon.Play);
                UIFactory.Place(resume.Rect, new Vector2(0.5f, 1), resume.Rect.sizeDelta, new Vector2(0, -20), new Vector2(0.5f, 1));
                var restart = UIFactory.Button(o.Content, Loc.T("Restart level"), UIStyle.ButtonKind.Gold, () =>
                    Overlays.Confirm(Loc.T("Restart level?"), Loc.T("Your moves on this level will be reset."), Loc.T("Restart"), () => { o.Close(); onRestart?.Invoke(); }), new Vector2(640, UIStyle.ButtonHeight), Icon.Restart);
                UIFactory.Place(restart.Rect, new Vector2(0.5f, 1), restart.Rect.sizeDelta, new Vector2(0, -200), new Vector2(0.5f, 1));
                var settings = UIFactory.Button(o.Content, Loc.T("Settings"), UIStyle.ButtonKind.Accent, () => ShowSettings(s), new Vector2(640, UIStyle.ButtonHeight), Icon.Gear);
                UIFactory.Place(settings.Rect, new Vector2(0.5f, 1), settings.Rect.sizeDelta, new Vector2(0, -380), new Vector2(0.5f, 1));
                var map = UIFactory.Button(o.Content, Loc.T("Level map"), UIStyle.ButtonKind.Secondary, () => { o.Close(); onMap?.Invoke(); }, new Vector2(640, UIStyle.ButtonHeight), Icon.Map);
                UIFactory.Place(map.Rect, new Vector2(0.5f, 1), map.Rect.sizeDelta, new Vector2(0, -560), new Vector2(0.5f, 1));
                var hint = UIFactory.Text(o.Content, Loc.T("Your progress on this level is saved."), UIStyle.FontCaption, UIStyle.InkSoft, TextAnchor.LowerCenter, FontStyle.Normal);
                UIFactory.Band(hint.rectTransform, 0f, 60, 0, 0, 0f);
            }, true, Loc.T("Paused"));
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
            var label = UIFactory.Text(track.transform, value ? Loc.T("ON") : Loc.T("OFF"), UIStyle.FontCaption, Color.white);
            UIFactory.AddShadow(knob, UIStyle.Shadow, new Vector2(0, -4));
            bool state = value;
            Action layout = () =>
            {
                track.color = state ? UIStyle.Mint : UIStyle.Disabled;
                label.text = state ? Loc.T("ON") : Loc.T("OFF");
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
