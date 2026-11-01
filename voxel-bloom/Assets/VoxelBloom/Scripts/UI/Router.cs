using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VoxelBloom
{
    /// <summary>Owns the main canvas: one active screen plus a stack of modal dialogs and toasts.</summary>
    public sealed class Router : MonoBehaviour
    {
        public static Router Instance { get; private set; }
        public Canvas Canvas { get; private set; }
        public RectTransform ScreenLayer { get; private set; }
        public RectTransform DialogLayer { get; private set; }
        RectTransform toastLayer;
        readonly Dictionary<Type, ScreenBase> screens = new Dictionary<Type, ScreenBase>();
        readonly List<DialogBase> dialogs = new List<DialogBase>();
        public ScreenBase Current { get; private set; }
        public bool HasDialog => dialogs.Count > 0;

        public static Router Create()
        {
            var go = new GameObject("UI");
            DontDestroyOnLoad(go);
            go.layer = 5;
            var r = go.AddComponent<Router>();
            r.Build();
            return r;
        }

        void Build()
        {
            Instance = this;
            Canvas = gameObject.AddComponent<Canvas>();
            Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Canvas.sortingOrder = 10;
            var cs = gameObject.AddComponent<CanvasScaler>();
            cs.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            cs.referenceResolution = new Vector2(1080, 1920);
            cs.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            gameObject.AddComponent<AdaptiveScaler>();
            gameObject.AddComponent<GraphicRaycaster>();

            var bg = UI.Sky(transform, Theme.SkyTop, Theme.SkyBottom);
            bg.name = "BaseSky";
            var safe = UI.Rect(transform, "SafeArea").Stretch();
            safe.gameObject.AddComponent<SafeArea>();
            ScreenLayer = UI.Rect(safe, "Screens").Stretch();
            DialogLayer = UI.Rect(transform, "Dialogs").Stretch();
            toastLayer = UI.Rect(safe, "Toasts").Stretch();
        }

        public T Show<T>() where T : ScreenBase
        {
            if (!screens.TryGetValue(typeof(T), out var s) || s == null)
            {
                var rt = UI.Rect(ScreenLayer, typeof(T).Name).Stretch();
                s = rt.gameObject.AddComponent<T>();
                s.Build(this);
                screens[typeof(T)] = s;
            }
            if (Current != null && Current != s) { Current.OnHide(); Current.gameObject.SetActive(false); }
            CloseAllDialogs();
            Current = s;
            s.gameObject.SetActive(true);
            s.transform.SetAsLastSibling();
            s.OnShow();
            var cg = s.GetComponent<CanvasGroup>();
            if (cg == null) cg = s.gameObject.AddComponent<CanvasGroup>();
            if (s.FadeIn) Tween.Fade(cg, 0f, 1f, 0.22f); else cg.alpha = 1f;
            return (T)s;
        }

        public T Open<T>(Action<T> setup = null) where T : DialogBase
        {
            var rt = UI.Rect(DialogLayer, typeof(T).Name).Stretch();
            var d = rt.gameObject.AddComponent<T>();
            setup?.Invoke(d);
            d.Build(this);
            dialogs.Add(d);
            d.AnimateIn();
            return d;
        }

        public void Close(DialogBase d)
        {
            if (d == null || !dialogs.Remove(d)) return;
            d.AnimateOut(() => { if (d != null) Destroy(d.gameObject); });
        }

        public void CloseAllDialogs()
        {
            foreach (var d in dialogs) if (d != null) Destroy(d.gameObject);
            dialogs.Clear();
        }

        public void Toast(string message, Icon? icon = null)
        {
            var p = UI.Panel(toastLayer, "Toast", new Color(0.23f, 0.18f, 0.36f, 0.92f), 40, false);
            var root = (RectTransform)p.transform.parent;
            root.Place(new Vector2(0.5f, 0f), new Vector2(0, 300), new Vector2(860, 120));
            var t = UI.Text(root, message, 44, Color.white);
            t.rectTransform.Stretch(icon.HasValue ? 120 : 30, 30, 0, 0);
            t.Wrap = true;
            if (icon.HasValue) UI.Icon(root, icon.Value, 90).rectTransform.Place(new Vector2(0, 0.5f), new Vector2(70, 0), new Vector2(90, 90));
            var cg = root.gameObject.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            Tween.Move(root, new Vector2(0, 200), new Vector2(0, 300), 0.3f, Ease.OutBack);
            Tween.Delay(2.0f, () => { if (cg != null) Tween.Fade(cg, 1, 0, 0.3f, () => { if (root != null) Destroy(root.gameObject); }); });
        }

        void Update()
        {
            // Android back button
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (dialogs.Count > 0) dialogs[dialogs.Count - 1].OnBack();
                else Current?.OnBack();
            }
        }
    }

    public abstract class ScreenBase : MonoBehaviour
    {
        protected Router Router;
        public RectTransform Root => (RectTransform)transform;
        public void Build(Router r) { Router = r; OnBuild(); }
        protected abstract void OnBuild();
        public virtual void OnShow() { }
        public virtual void OnHide() { }
        public virtual void OnBack() { }
        /// <summary>Short fade when the screen is shown (Home is fully static and opts out).</summary>
        public virtual bool FadeIn => true;
    }

    /// <summary>Modal dialog: dark backdrop, white rounded panel, purple ribbon title and close button.</summary>
    public abstract class DialogBase : MonoBehaviour
    {
        protected Router Router;
        protected RectTransform Panel;
        protected RectTransform Body;
        CanvasGroup group;
        public Action Closed;
        protected virtual string Title => "";
        protected virtual Vector2 Size => new Vector2(900, 1000);
        protected virtual bool ShowClose => true;

        public void Build(Router r)
        {
            Router = r;
            group = gameObject.AddComponent<CanvasGroup>();
            var back = UI.Image(transform, "Backdrop", null, Theme.Backdrop, false, true);
            back.rectTransform.Stretch();
            var p = UI.Panel(transform, "Panel", Color.white, 64, true, 16f);
            Panel = (RectTransform)p.transform.parent;
            Panel.Place(new Vector2(0.5f, 0.5f), new Vector2(0, -20), Size);
            Body = UI.Rect(Panel, "Body").Stretch(50, 50, 110, 50);
            if (!string.IsNullOrEmpty(Title))
            {
                var rib = UI.Ribbon(Panel, Title, Mathf.Min(Size.x * 0.82f, 760), 150);
                rib.Place(new Vector2(0.5f, 1f), new Vector2(0, 0), rib.sizeDelta);
            }
            if (ShowClose)
            {
                var close = UI.IconButton(Panel, Icon.Close, 96, ButtonStyle.White);
                ((RectTransform)close.transform).Place(new Vector2(1f, 1f), new Vector2(-30, -30), new Vector2(96, 96));
                close.OnClick = Close;
            }
            OnBuild();
        }

        protected abstract void OnBuild();
        public virtual void OnBack() { if (ShowClose) Close(); }

        public void Close()
        {
            Router.Close(this);
            Closed?.Invoke();
        }

        public void AnimateIn()
        {
            group.alpha = 0;
            Tween.Fade(group, 0, 1, 0.18f);
            Tween.Scale(Panel, Vector3.one * 0.8f, Vector3.one, 0.32f, Ease.OutBack);
        }

        public void AnimateOut(Action done)
        {
            group.blocksRaycasts = false;
            Tween.Fade(group, 1, 0, 0.15f, done);
            Tween.Scale(Panel, Vector3.one, Vector3.one * 0.9f, 0.15f);
        }
    }

    /// <summary>Top bar shown on meta screens: avatar, lives, coins (+), settings.</summary>
    public sealed class TopBar : MonoBehaviour
    {
        VText lives, coins, playerLevel, gems;
        int shownGems = -1;
        int shownLevel = -1;
        int shownCoins = -1, shownLives = -1;
        long shownSeconds = -1;
        public Action OnSettings, OnAddCoins, OnAvatar;

        public static TopBar Create(Transform parent)
        {
            var rt = UI.Rect(parent, "TopBar").TopBand(24, 110, 30);
            var tb = rt.gameObject.AddComponent<TopBar>();
            tb.Build(rt);
            return tb;
        }

        void Build(RectTransform rt)
        {
            var avatarBtn = UI.IconButton(rt, Icon.Avatar, 110, ButtonStyle.White);
            ((RectTransform)avatarBtn.transform).Place(new Vector2(0, 0.5f), new Vector2(55, 0), new Vector2(110, 110));
            avatarBtn.OnClick = () => OnAvatar?.Invoke();
            var ring = UI.Image(avatarBtn.transform, "Ring", SpriteFactory.Circle(), Theme.Gold, false);
            ring.rectTransform.Place(new Vector2(1, 0), new Vector2(-8, 8), new Vector2(54, 54));
            var lvlBg = UI.Image(ring.transform, "Level", SpriteFactory.Circle(), Theme.Ink, false);
            lvlBg.rectTransform.Stretch(5, 5, 5, 5);
            playerLevel = UI.Text(lvlBg.transform, "1", 28, Color.white);
            playerLevel.fontStyle = FontStyle.Bold;
            playerLevel.rectTransform.Stretch();

            lives = UI.Pill(rt, Icon.Heart, "5", out var lr, 200, 80);
            lr.Place(new Vector2(0, 0.5f), new Vector2(232, 0), new Vector2(200, 80));

            coins = UI.Pill(rt, Icon.Coin, "0", out var cr, 260, 80);
            cr.Place(new Vector2(0, 0.5f), new Vector2(478, 0), new Vector2(260, 80));

            gems = UI.Pill(rt, Icon.Gem, "0", out var gr, 220, 80);
            gr.Place(new Vector2(0, 0.5f), new Vector2(740, 0), new Vector2(220, 80));
            var plusG = UI.IconButton(gr, Icon.Plus, 66, ButtonStyle.Green);
            ((RectTransform)plusG.transform).Place(new Vector2(1, 0.5f), new Vector2(-8, 0), new Vector2(66, 66));
            plusG.OnClick = () => OnAddCoins?.Invoke();
            var plus = UI.IconButton(cr, Icon.Plus, 72, ButtonStyle.Green);
            ((RectTransform)plus.transform).Place(new Vector2(1, 0.5f), new Vector2(-10, 0), new Vector2(72, 72));
            plus.OnClick = () => OnAddCoins?.Invoke();

            var gear = UI.IconButton(rt, Icon.Gear, 100, ButtonStyle.Purple);
            ((RectTransform)gear.transform).Place(new Vector2(1, 0.5f), new Vector2(-50, 0), new Vector2(100, 100));
            gear.OnClick = () => OnSettings?.Invoke();
        }

        void Update()
        {
            if (!Services.Initialized) return;
            long now = Services.Now;
            Services.Lives.Update(now);
            int pl = 1 + Services.Progress.TotalStars / 10; // player level grows with stars
            if (pl != shownLevel) { shownLevel = pl; playerLevel.text = pl.ToString(); }
            int c = Services.Wallet.Coins;
            if (c != shownCoins) { shownCoins = c; coins.text = c.ToString("N0"); }
            int gm = Services.Save.Gems;
            if (gm != shownGems) { shownGems = gm; gems.text = gm.ToString(); }
            int l = Services.Lives.Count;
            long secs = Services.Lives.SecondsToNext(now);
            if (l != shownLives || secs != shownSeconds)
            {
                shownLives = l; shownSeconds = secs;
                lives.text = l >= Services.Lives.Max ? "Full" : l + "  " + (secs / 60).ToString("00") + ":" + (secs % 60).ToString("00");
                lives.fontSize = l >= Services.Lives.Max ? 40 : 32;
            }
        }
    }
}
