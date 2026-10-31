using System;
using UnityEngine;
using UnityEngine.UI;
using Veliqora.Infrastructure;
using Veliqora.UI.Widgets;

namespace Veliqora.UI.Screens
{
    /// <summary>Base for every screen controller: owns a canvas, a safe-area root and common header widgets.</summary>
    public abstract class ScreenBase : MonoBehaviour
    {
        protected AppServices S;
        protected Canvas Canvas;
        protected RectTransform Full;
        protected RectTransform Safe;

        /// <summary>Banners are only ever allowed on menu screens (never gameplay).</summary>
        public virtual bool AllowsBanner => false;

        public void Setup(AppServices services, object args)
        {
            S = services;
            Canvas = UIFactory.CreateCanvas(GetType().Name + "Canvas", 0, transform);
            Full = (RectTransform)Canvas.transform;
            BuildBackground(Full);
            Safe = UIFactory.SafeArea(Full);
            Build(args);
        }

        protected virtual void BuildBackground(RectTransform full) { }
        protected abstract void Build(object args);

        /// <summary>Return true if the screen consumed the Back action.</summary>
        public virtual bool HandleBack() => false;
        public virtual void OnAppPause(bool paused) { }

        // ---------------- shared header ----------------

        protected RectTransform TopBar()
        {
            var bar = UIFactory.Rect("TopBar", Safe);
            UIFactory.Band(bar, 1f, UIStyle.TopBarHeight, -UIStyle.Space2, UIStyle.Space3, 1f);
            return bar;
        }

        protected CoinBadge AddCoins(RectTransform bar, bool right = true)
        {
            var go = new GameObject("Coins", typeof(RectTransform));
            go.transform.SetParent(bar, false);
            var badge = go.AddComponent<CoinBadge>();
            badge.Init(S.Economy, () => { if (S.Navigator.CurrentId != ScreenId.Shop) S.Navigator.Go(ScreenId.Shop); });
            var rt = (RectTransform)go.transform;
            UIFactory.Place(rt, new Vector2(right ? 1 : 0, 0.5f), new Vector2(330, 112), Vector2.zero, new Vector2(right ? 1 : 0, 0.5f));
            return badge;
        }

        protected ButtonHandle AddBack(RectTransform bar, Action onBack)
        {
            var b = UIFactory.IconButton(bar, Icon.Back, UIStyle.Violet, 116, onBack);
            UIFactory.Place(b.Rect, new Vector2(0, 0.5f), b.Rect.sizeDelta, Vector2.zero, new Vector2(0, 0.5f));
            return b;
        }

        protected Text Ribbon(RectTransform parent, string text, float y, float width = 640f, Color? color = null)
        {
            var t = UIFactory.TitleBanner(parent, text, new Vector2(width, 130), color ?? UIStyle.Coral);
            var rt = (RectTransform)t.transform.parent;
            UIFactory.Place(rt, new Vector2(0.5f, 1f), new Vector2(width, 130), new Vector2(0, y), new Vector2(0.5f, 1f));
            return t;
        }

        /// <summary>Soft dark gradients at the top and bottom so HUD text stays readable over busy illustrations.</summary>
        protected void ReadabilityScrim(RectTransform full, float top, float bottom, float overall = 0f)
        {
            if (overall > 0f)
            {
                var all = UIFactory.Image(full, "Scrim", SpriteLibrary.Pixel, new Color(0.06f, 0.08f, 0.2f, overall));
                UIFactory.Stretch(all.rectTransform);
            }
            var g = UIFactory.Raw(full, "ScrimGradient", Presentation.BackgroundArt.ScrimGradient(top, bottom), new Color(0.04f, 0.05f, 0.15f, 1f));
            UIFactory.Stretch(g.rectTransform);
        }

        /// <summary>Slow glowing motes drifting up behind the UI (dark themes only; off in reduced motion).</summary>
        protected UiParticles AmbientMotes(float perSecond = 4f)
        {
            var theme = S.Themes.Theme;
            if (!theme.IsDark) return null;
            var fx = UiParticles.Create(Full, 48);
            fx.transform.SetSiblingIndex(Safe.GetSiblingIndex()); // behind the safe-area UI
            fx.SetAmbient(true, theme.Accent, Color.Lerp(theme.River, Color.white, 0.2f), perSecond, theme.Particles);
            return fx;
        }

        protected RawImage FullBackground(RectTransform full, Texture tex)
        {
            var raw = UIFactory.Raw(full, "Background", tex, Color.white);
            // Cover-fit: keep aspect, crop overflow.
            var fitter = raw.gameObject.AddComponent<AspectRatioFitter>();
            fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fitter.aspectRatio = tex.width / (float)tex.height;
            return raw;
        }
    }
}
