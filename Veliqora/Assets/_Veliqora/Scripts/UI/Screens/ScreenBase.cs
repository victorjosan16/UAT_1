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
            var ribbon = UIFactory.Image(parent, "Ribbon", SpriteLibrary.Rounded(UIStyle.RadiusMedium), color ?? UIStyle.Coral);
            UIFactory.Place(ribbon.rectTransform, new Vector2(0.5f, 1f), new Vector2(width, 130), new Vector2(0, y), new Vector2(0.5f, 1f));
            UIFactory.AddShadow(ribbon, UIStyle.Shadow, new Vector2(0, -8));
            var t = UIFactory.Text(ribbon.transform, text, UIStyle.FontHeading, Color.white);
            UIFactory.AddShadow(t, new Color(0, 0, 0, 0.22f), new Vector2(0, -4));
            return t;
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
