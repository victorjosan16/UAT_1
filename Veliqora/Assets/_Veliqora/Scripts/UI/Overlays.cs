using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Veliqora.UI
{
    public sealed class OverlayHandle
    {
        public GameObject Root;
        public RectTransform Panel;
        public RectTransform Content;
        public bool Dismissable;
        public Action Closed;
        internal bool IsClosing;
        public void Close() => Overlays.Close(this);
    }

    /// <summary>
    /// Modal overlay stack (pause, settings, confirms, mock ads) on a persistent canvas above every screen.
    /// Android Back closes the top dismissable overlay first.
    /// </summary>
    public static class Overlays
    {
        private static readonly List<OverlayHandle> Stack = new List<OverlayHandle>();
        private static OverlayHost _host;

        public static int Count => Stack.Count;
        public static bool AnyOpen => Stack.Count > 0;

        public static RectTransform Layer
        {
            get
            {
                if (_host == null)
                {
                    var canvas = UIFactory.CreateCanvas("VeliqoraOverlayCanvas", 500);
                    UnityEngine.Object.DontDestroyOnLoad(canvas.gameObject);
                    _host = canvas.gameObject.AddComponent<OverlayHost>();
                }
                return (RectTransform)_host.transform;
            }
        }

        private static MonoBehaviour Host { get { var _ = Layer; return _host; } }

        public static OverlayHandle Open(string name, Vector2 panelSize, Action<OverlayHandle> build, bool dismissable = true, string title = null, bool panel = true)
        {
            var layer = Layer;
            var h = new OverlayHandle { Dismissable = dismissable };
            var root = UIFactory.Rect("Overlay_" + name, layer);
            h.Root = root.gameObject;
            var group = root.gameObject.AddComponent<CanvasGroup>();

            var scrim = UIFactory.Image(root, "Scrim", SpriteLibrary.Pixel, UIStyle.Scrim, true);
            var scrimBtn = scrim.gameObject.AddComponent<Button>();
            scrimBtn.transition = Selectable.Transition.None;
            scrimBtn.onClick.AddListener(() => { if (h.Dismissable) Close(h); });

            var safe = UIFactory.SafeArea(root);
            if (panel)
            {
                var bg = UIFactory.Panel(safe, "Panel", UIStyle.Ivory, UIStyle.RadiusLarge);
                var holder = (RectTransform)bg.transform.parent;
                UIFactory.Place(holder, new Vector2(0.5f, 0.5f), panelSize, Vector2.zero);
                h.Panel = holder;
                h.Content = UIFactory.Rect("Content", holder);
                UIFactory.Stretch(h.Content, UIStyle.Space3);
                if (!string.IsNullOrEmpty(title))
                {
                    var ribbon = UIFactory.Image(holder, "Ribbon", SpriteLibrary.Rounded(UIStyle.RadiusMedium), UIStyle.Coral);
                    UIFactory.Place(ribbon.rectTransform, new Vector2(0.5f, 1f), new Vector2(Mathf.Min(panelSize.x - 80, 640), 120), new Vector2(0, 30), new Vector2(0.5f, 0.5f));
                    UIFactory.AddShadow(ribbon, UIStyle.Shadow, new Vector2(0, -6));
                    var t = UIFactory.Text(ribbon.transform, title, UIStyle.FontHeading, Color.white);
                    UIFactory.AddShadow(t, new Color(0, 0, 0, 0.2f), new Vector2(0, -3));
                    h.Content.offsetMax = new Vector2(-UIStyle.Space3, -110);
                }
                if (dismissable)
                {
                    var close = UIFactory.IconButton(holder, Icon.Close, UIStyle.Coral, 96, () => Close(h));
                    UIFactory.Place(close.Rect, new Vector2(1, 1), new Vector2(96, 96), new Vector2(-10, -10), new Vector2(1, 1));
                }
            }
            else
            {
                h.Panel = safe;
                h.Content = safe;
            }

            Stack.Add(h);
            build?.Invoke(h);

            group.alpha = 0f;
            Tween.Fade(Host, group, 0f, 1f, MotionPrefs.Dur(0.18f));
            if (panel && h.Panel != null)
            {
                var p = h.Panel;
                Tween.Scale(Host, p, new Vector3(0.88f, 0.88f, 1), Vector3.one, MotionPrefs.Dur(0.28f), Ease.OutBack);
            }
            return h;
        }

        public static void Close(OverlayHandle h)
        {
            if (h == null || h.IsClosing || h.Root == null) return;
            h.IsClosing = true;
            Stack.Remove(h);
            var group = h.Root.GetComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            var root = h.Root;
            Tween.Fade(Host, group, group.alpha, 0f, MotionPrefs.Dur(0.15f), () => { if (root != null) UnityEngine.Object.Destroy(root); });
            h.Closed?.Invoke();
        }

        /// <summary>Back button: closes the top overlay if it allows dismissal. Returns true if consumed.</summary>
        public static bool HandleBack()
        {
            if (Stack.Count == 0) return false;
            var top = Stack[Stack.Count - 1];
            if (top.Dismissable) Close(top);
            return true; // non-dismissable overlays (e.g. a mock ad) swallow Back
        }

        public static void CloseAll()
        {
            for (int i = Stack.Count - 1; i >= 0; i--)
            {
                var h = Stack[i];
                if (h.Root != null) UnityEngine.Object.Destroy(h.Root);
            }
            Stack.Clear();
        }

        // ---------------- common dialogs ----------------

        public static OverlayHandle Confirm(string title, string message, string confirmLabel, Action onConfirm,
            string cancelLabel = "Cancel", UIStyle.ButtonKind confirmKind = UIStyle.ButtonKind.Primary, Action onCancel = null)
        {
            return Open("Confirm", new Vector2(900, 700), h =>
            {
                var body = UIFactory.Text(h.Content, message, UIStyle.FontBody, UIStyle.Ink, TextAnchor.MiddleCenter, FontStyle.Normal);
                body.rectTransform.offsetMin = new Vector2(10, 190);
                var ok = UIFactory.Button(h.Content, confirmLabel, confirmKind, () => { Close(h); onConfirm?.Invoke(); }, new Vector2(370, UIStyle.ButtonHeight));
                UIFactory.Place(ok.Rect, new Vector2(0.5f, 0f), ok.Rect.sizeDelta, new Vector2(200, 20), new Vector2(0.5f, 0f));
                var cancel = UIFactory.Button(h.Content, cancelLabel, UIStyle.ButtonKind.Secondary, () => { Close(h); onCancel?.Invoke(); }, new Vector2(370, UIStyle.ButtonHeight));
                UIFactory.Place(cancel.Rect, new Vector2(0.5f, 0f), cancel.Rect.sizeDelta, new Vector2(-200, 20), new Vector2(0.5f, 0f));
            }, true, title);
        }

        public static OverlayHandle Message(string title, string message, string okLabel = "OK", Action onOk = null)
        {
            return Open("Message", new Vector2(900, 640), h =>
            {
                var body = UIFactory.Text(h.Content, message, UIStyle.FontBody, UIStyle.Ink, TextAnchor.MiddleCenter, FontStyle.Normal);
                body.rectTransform.offsetMin = new Vector2(10, 190);
                var ok = UIFactory.Button(h.Content, okLabel, UIStyle.ButtonKind.Primary, () => { Close(h); onOk?.Invoke(); }, new Vector2(420, UIStyle.ButtonHeight));
                UIFactory.Place(ok.Rect, new Vector2(0.5f, 0f), ok.Rect.sizeDelta, new Vector2(0, 20), new Vector2(0.5f, 0f));
            }, true, title);
        }

        private static GameObject _toast;

        /// <summary>Short non-blocking message at the top of the screen.</summary>
        public static void Toast(string text, float seconds = 1.9f)
        {
            var layer = Layer;
            if (_toast != null) UnityEngine.Object.Destroy(_toast);
            var safe = UIFactory.SafeArea(layer);
            safe.name = "Toast";
            _toast = safe.gameObject;
            var bg = UIFactory.Panel(safe, "Bubble", new Color(0.12f, 0.14f, 0.3f, 0.94f), UIStyle.RadiusMedium);
            var holder = (RectTransform)bg.transform.parent;
            UIFactory.Place(holder, new Vector2(0.5f, 1f), new Vector2(940, 150), new Vector2(0, -230), new Vector2(0.5f, 1f));
            var t = UIFactory.Text(holder, text, UIStyle.FontLabel, Color.white, TextAnchor.MiddleCenter, FontStyle.Bold);
            UIFactory.Stretch(t.rectTransform, 24);
            foreach (var g in holder.GetComponentsInChildren<Graphic>()) g.raycastTarget = false;
            var group = safe.gameObject.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false;
            var go = _toast;
            Tween.Fade(Host, group, 0f, 1f, MotionPrefs.Dur(0.15f));
            Tween.Delay(Host, seconds, () =>
            {
                if (go == null) return;
                Tween.Fade(Host, group, 1f, 0f, MotionPrefs.Dur(0.25f), () => { if (go != null) UnityEngine.Object.Destroy(go); });
            });
        }
    }
}
