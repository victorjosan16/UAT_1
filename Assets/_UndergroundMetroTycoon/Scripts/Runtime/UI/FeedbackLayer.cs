using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace UndergroundMetroTycoon.Game
{
    /// <summary>
    /// Pooled UI feedback: coins flying into the counter, floating "+5" texts over the world, toast hints
    /// and the big celebration banner.
    /// </summary>
    public sealed class FeedbackLayer : MonoBehaviour
    {
        private sealed class FlyingCoin
        {
            public RectTransform Rect;
            public Vector2 From;
            public Vector2 Control;
            public float Delay;
            public float Time;
            public float Duration;
            public bool Active;
        }

        private sealed class FloatingText
        {
            public Text Text;
            public Vector3 World;
            public float Time;
            public bool Active;
        }

        private const int CoinPoolSize = 28;
        private const int TextPoolSize = 12;

        private readonly List<FlyingCoin> _coins = new List<FlyingCoin>();
        private readonly List<FloatingText> _texts = new List<FloatingText>();
        private readonly Queue<string> _toastQueue = new Queue<string>();
        private RectTransform _root;
        private RectTransform _coinTarget;
        private Camera _camera;
        private Action _onCoinArrived;

        private CanvasGroup _toastGroup;
        private Text _toastText;
        private float _toastTime;

        private RectTransform _banner;
        private CanvasGroup _bannerGroup;
        private Text _bannerTitle;
        private Text _bannerSubtitle;
        private float _bannerTime = -1f;

        public void Build(RectTransform root, RectTransform coinTarget, Action onCoinArrived)
        {
            _root = root;
            _coinTarget = coinTarget;
            _onCoinArrived = onCoinArrived;

            for (int i = 0; i < TextPoolSize; i++)
            {
                Text t = UIFactory.Text("FloatText", root, "", 40, Palette.Yellow, TextAnchor.MiddleCenter, true);
                t.rectTransform.sizeDelta = new Vector2(240f, 60f);
                UIFactory.AddShadow(t, 3f, 0.7f);
                t.gameObject.SetActive(false);
                _texts.Add(new FloatingText { Text = t });
            }

            for (int i = 0; i < CoinPoolSize; i++)
            {
                Image coin = UIFactory.CoinIcon(root, 58f);
                coin.gameObject.SetActive(false);
                _coins.Add(new FlyingCoin { Rect = coin.rectTransform });
            }

            Image toast = UIFactory.Panel("Toast", root, new Color(Palette.NavyDeep.r, Palette.NavyDeep.g, Palette.NavyDeep.b, 0.92f), 1.2f);
            UIFactory.Place(toast.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -440f), new Vector2(900f, 96f));
            _toastGroup = toast.gameObject.AddComponent<CanvasGroup>();
            _toastGroup.blocksRaycasts = false;
            _toastText = UIFactory.Text("Text", toast.transform, "", 32, Palette.White, TextAnchor.MiddleCenter, true);
            UIFactory.Stretch(_toastText.rectTransform, 24f, 0f, 24f, 0f);
            toast.gameObject.SetActive(false);

            _banner = UIFactory.Rect("Banner", root);
            UIFactory.Place(_banner, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 260f), new Vector2(1000f, 260f));
            _bannerGroup = _banner.gameObject.AddComponent<CanvasGroup>();
            _bannerGroup.blocksRaycasts = false;
            Image ribbon = UIFactory.Panel("Ribbon", _banner, Palette.Yellow, 1f);
            UIFactory.Stretch(ribbon.rectTransform, 0f, 30f, 0f, 60f);
            UIFactory.AddShadow(ribbon, 8f, 0.5f);
            _bannerTitle = UIFactory.Text("Title", ribbon.transform, "", 64, Palette.NavyDeep, TextAnchor.MiddleCenter, true);
            UIFactory.Stretch(_bannerTitle.rectTransform, 20f, 0f, 20f, 0f);
            _bannerSubtitle = UIFactory.Text("Subtitle", _banner, "", 36, Palette.White, TextAnchor.MiddleCenter, true);
            UIFactory.Place(_bannerSubtitle.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 6f), new Vector2(1000f, 50f));
            UIFactory.AddShadow(_bannerSubtitle, 3f, 0.8f);
            _banner.gameObject.SetActive(false);
        }

        public void SetCamera(Camera camera) => _camera = camera;

        public void ShowToast(string message)
        {
            if (_toastText.gameObject.activeInHierarchy && _toastText.text == message) return;
            if (_toastQueue.Count < 3 && !_toastQueue.Contains(message)) _toastQueue.Enqueue(message);
        }

        public void ShowBanner(string title, string subtitle)
        {
            _bannerTitle.text = title;
            _bannerSubtitle.text = subtitle;
            _bannerTime = 0f;
            _banner.gameObject.SetActive(true);
            _banner.SetAsLastSibling();
        }

        public void ShowFloatingText(Vector3 world, string value, Color color)
        {
            foreach (FloatingText t in _texts)
            {
                if (t.Active) continue;
                t.Active = true;
                t.Time = 0f;
                t.World = world;
                t.Text.text = value;
                t.Text.color = color;
                t.Text.gameObject.SetActive(true);
                PositionFloating(t);
                return;
            }
        }

        /// <summary>Flies coin icons from a world position into the coin counter.</summary>
        public void FlyCoinsFromWorld(Vector3 world, int count)
        {
            if (_camera == null) return;
            Vector2 local = WorldToLocal(world);
            FlyCoins(local, count, 0.06f);
        }

        /// <summary>Flies coin icons from the screen centre area (e.g. contract reward).</summary>
        public void FlyCoinsFromCenter(int count) => FlyCoins(new Vector2(0f, 120f), count, 0.035f);

        private void FlyCoins(Vector2 from, int count, float stagger)
        {
            Vector2 target = TargetLocal();
            int launched = 0;
            foreach (FlyingCoin c in _coins)
            {
                if (launched >= count) break;
                if (c.Active) continue;
                c.Active = true;
                c.From = from + UnityEngine.Random.insideUnitCircle * 40f;
                c.Control = Vector2.Lerp(c.From, target, 0.5f) + new Vector2(UnityEngine.Random.Range(-260f, 260f), UnityEngine.Random.Range(80f, 260f));
                c.Delay = launched * stagger;
                c.Time = 0f;
                c.Duration = UnityEngine.Random.Range(0.55f, 0.75f);
                c.Rect.anchoredPosition = c.From;
                c.Rect.localScale = Vector3.zero;
                c.Rect.gameObject.SetActive(true);
                c.Rect.SetAsLastSibling();
                launched++;
            }
        }

        private void Update()
        {
            float dt = Time.unscaledDeltaTime;
            UpdateCoins(dt);
            UpdateTexts(dt);
            UpdateToast(dt);
            UpdateBanner(dt);
        }

        private void UpdateCoins(float dt)
        {
            Vector2 target = TargetLocal();
            foreach (FlyingCoin c in _coins)
            {
                if (!c.Active) continue;
                if (c.Delay > 0f)
                {
                    c.Delay -= dt;
                    continue;
                }
                c.Time += dt;
                float t = Mathf.Clamp01(c.Time / c.Duration);
                float e = t * t * (3f - 2f * t);
                Vector2 a = Vector2.Lerp(c.From, c.Control, e);
                Vector2 b = Vector2.Lerp(c.Control, target, e);
                c.Rect.anchoredPosition = Vector2.Lerp(a, b, e);
                float s = t < 0.15f ? t / 0.15f : Mathf.Lerp(1f, 0.6f, t);
                c.Rect.localScale = new Vector3(s, s, 1f);
                if (t >= 1f)
                {
                    c.Active = false;
                    c.Rect.gameObject.SetActive(false);
                    _onCoinArrived?.Invoke();
                }
            }
        }

        private void UpdateTexts(float dt)
        {
            foreach (FloatingText t in _texts)
            {
                if (!t.Active) continue;
                t.Time += dt;
                t.World += Vector3.up * dt * 1.2f;
                PositionFloating(t);
                Color c = t.Text.color;
                c.a = Mathf.Clamp01(1.6f - t.Time * 1.8f);
                t.Text.color = c;
                if (t.Time > 0.9f)
                {
                    t.Active = false;
                    t.Text.gameObject.SetActive(false);
                }
            }
        }

        private void UpdateToast(float dt)
        {
            GameObject toast = _toastGroup.gameObject;
            if (!toast.activeSelf)
            {
                if (_toastQueue.Count == 0) return;
                _toastText.text = _toastQueue.Dequeue();
                _toastTime = 0f;
                toast.SetActive(true);
                toast.transform.SetAsLastSibling();
            }
            _toastTime += dt;
            const float duration = 2.4f;
            _toastGroup.alpha = Mathf.Clamp01(Mathf.Min(_toastTime * 6f, (duration - _toastTime) * 4f));
            if (_toastTime >= duration) toast.SetActive(false);
        }

        private void UpdateBanner(float dt)
        {
            if (_bannerTime < 0f) return;
            _bannerTime += dt;
            const float duration = 2.6f;
            float appear = Mathf.Clamp01(_bannerTime * 5f);
            float overshoot = 1f + Mathf.Sin(appear * Mathf.PI) * 0.12f;
            float s = Mathf.Lerp(0.4f, 1f, appear) * overshoot;
            _banner.localScale = new Vector3(s, s, 1f);
            _bannerGroup.alpha = Mathf.Clamp01((duration - _bannerTime) * 3f);
            if (_bannerTime >= duration)
            {
                _bannerTime = -1f;
                _banner.gameObject.SetActive(false);
            }
        }

        private void PositionFloating(FloatingText t)
        {
            if (_camera == null) return;
            Vector3 screen = _camera.WorldToScreenPoint(t.World);
            if (RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, screen, null, out Vector2 local))
                t.Text.rectTransform.anchoredPosition = local;
        }

        private Vector2 WorldToLocal(Vector3 world)
        {
            Vector3 screen = _camera.WorldToScreenPoint(world);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, screen, null, out Vector2 local);
            return local;
        }

        private Vector2 TargetLocal()
        {
            if (_coinTarget == null) return Vector2.zero;
            Vector3 screen = RectTransformUtility.WorldToScreenPoint(null, _coinTarget.position);
            RectTransformUtility.ScreenPointToLocalPointInRectangle(_root, screen, null, out Vector2 local);
            return local;
        }
    }
}
