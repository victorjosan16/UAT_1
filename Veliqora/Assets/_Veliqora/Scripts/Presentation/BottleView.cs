using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Veliqora.Core.Puzzle;
using Veliqora.UI;

namespace Veliqora.Presentation
{
    /// <summary>
    /// Render-only view of one bottle. Holds no game rules: the board feeds it committed states (or interpolated
    /// layers during animations) and forwards taps to the controller.
    /// </summary>
    public sealed class BottleView : MonoBehaviour, IPointerClickHandler
    {
        public int Index { get; private set; }
        public System.Action<int> Tapped;

        public RectTransform Rect { get; private set; }
        public Vector2 HomePosition;
        public float Width { get; private set; }
        public float Height { get; private set; }
        public BottleShape Shape { get; private set; }
        public int Capacity { get; private set; }

        private LiquidGraphic _liquid;
        private Image _glass;
        private Image _glassBack;
        private Image _glow;
        private Image _hintRing;
        private RectTransform _symbolRoot;
        private readonly List<Image> _symbols = new List<Image>();
        private readonly List<LiquidGraphic.Layer> _scratch = new List<LiquidGraphic.Layer>();
        private bool _showSymbols;
        private float _wobble;
        private float _wobbleTime;
        private Coroutine _liftRoutine;
        private Coroutine _hintPulse;
        private bool _selected;

        public void Init(int index, BottleShape shape, int capacity, float width, bool showSymbols, Color? glassColor = null)
        {
            Index = index;
            Shape = shape;
            Capacity = capacity;
            Width = width;
            Height = width * shape.Aspect;
            _showSymbols = showSymbols;

            Rect = (RectTransform)transform;
            Rect.anchorMin = Rect.anchorMax = new Vector2(0.5f, 0.5f);
            Rect.pivot = new Vector2(0.5f, 0f);
            Rect.sizeDelta = new Vector2(Width, Height);

            // Generous invisible hit area (touch-friendly even for slim bottles).
            var hit = UIFactory.Image(transform, "Hit", null, new Color(0, 0, 0, 0), true);
            UIFactory.Stretch(hit.rectTransform, -Mathf.Max(18f, (UIStyle.MinTouch - Width) * 0.5f));

            _glow = UIFactory.Image(transform, "Glow", SpriteLibrary.RadialGlow, new Color(1f, 0.95f, 0.6f, 0f));
            UIFactory.Stretch(_glow.rectTransform, -Width * 0.45f);

            _hintRing = UIFactory.Image(transform, "Hint", SpriteLibrary.SoftRounded(Width * 0.4f, 16), new Color(0.35f, 1f, 0.75f, 0f));
            UIFactory.Stretch(_hintRing.rectTransform, -Width * 0.12f);

            if (shape.IsAuthored)
            {
                // Authored glass: full art behind the liquid, a faint copy in front for reflections, so liquid
                // colors stay vivid while the glass still reads as glass.
                _glassBack = UIFactory.Image(transform, "GlassBack", shape.GlassSprite(), glassColor ?? Color.white);
            }

            var liquidRt = UIFactory.Rect("Liquid", transform);
            _liquid = liquidRt.gameObject.AddComponent<LiquidGraphic>();
            _liquid.Configure(shape.InteriorPolygon(), capacity);

            _symbolRoot = UIFactory.Rect("Symbols", transform);

            _glass = UIFactory.Image(transform, "Glass", shape.GlassSprite(), Color.white);
            _glass.type = Image.Type.Simple;
            if (glassColor.HasValue) _glass.color = glassColor.Value;
            if (shape.IsAuthored) _glass.color = UIStyle.WithAlpha(_glass.color, 0.38f);
            _glass.preserveAspect = false;
        }

        public void OnPointerClick(PointerEventData eventData) => Tapped?.Invoke(Index);

        // ---------------- state display ----------------

        public void ShowState(BottleState b)
        {
            _scratch.Clear();
            int i = 0;
            while (i < b.Count)
            {
                int c = b[i];
                int run = 0;
                while (i < b.Count && b[i] == c) { run++; i++; }
                _scratch.Add(new LiquidGraphic.Layer(LiquidPalette.Get(c), run));
            }
            _liquid.SetLayers(_scratch);
            UpdateSymbols(b);
        }

        /// <summary>Directly set interpolated layers (used mid-animation). Symbols hide while animating.</summary>
        public void ShowLayers(List<LiquidGraphic.Layer> layers)
        {
            _liquid.SetLayers(layers);
            _symbolRoot.gameObject.SetActive(false);
        }

        public void SetSymbolsEnabled(bool on, BottleState state)
        {
            _showSymbols = on;
            UpdateSymbols(state);
        }

        private void UpdateSymbols(BottleState b)
        {
            _symbolRoot.gameObject.SetActive(_showSymbols);
            if (!_showSymbols) return;
            while (_symbols.Count < Capacity)
            {
                var img = UIFactory.Image(_symbolRoot, "Sym", null, Color.white);
                img.rectTransform.anchorMin = img.rectTransform.anchorMax = new Vector2(0.5f, 0f);
                img.rectTransform.pivot = new Vector2(0.5f, 0.5f);
                _symbols.Add(img);
            }
            float size = Width * 0.36f;
            for (int i = 0; i < _symbols.Count; i++)
            {
                bool on = i < b.Count;
                _symbols[i].gameObject.SetActive(on);
                if (!on) continue;
                _symbols[i].sprite = LiquidPalette.Symbol(b[i]);
                _symbols[i].rectTransform.sizeDelta = new Vector2(size, size);
                _symbols[i].rectTransform.anchoredPosition = new Vector2(0, UprightUnitCenter(i));
            }
        }

        /// <summary>Local height (from bottle bottom) of the liquid surface when upright holding <paramref name="units"/>.</summary>
        public float UprightSurface(float units)
        {
            var poly = Shape.InteriorPolygon();
            float total = PolyArea(poly);
            float target = Mathf.Clamp01(units / Capacity) * total;
            float lo = 0f, hi = Shape.FillTop;
            for (int it = 0; it < 20; it++)
            {
                float mid = (lo + hi) * 0.5f;
                if (AreaBelow(poly, mid) < target) lo = mid; else hi = mid;
            }
            return (lo + hi) * 0.5f * Width;
        }

        private float UprightUnitCenter(int unitIndex) => (UprightSurface(unitIndex) + UprightSurface(unitIndex + 1)) * 0.5f;

        private static float AreaBelow(List<Vector2> poly, float y)
        {
            var clipped = new List<Vector2>(poly.Count + 2);
            for (int i = 0; i < poly.Count; i++)
            {
                var a = poly[i];
                var b = poly[(i + 1) % poly.Count];
                bool ina = a.y <= y, inb = b.y <= y;
                if (ina) clipped.Add(a);
                if (ina != inb) clipped.Add(Vector2.Lerp(a, b, (y - a.y) / (b.y - a.y)));
            }
            return PolyArea(clipped);
        }

        private static float PolyArea(List<Vector2> p)
        {
            float a = 0f;
            for (int i = 0, j = p.Count - 1; i < p.Count; j = i, i++) a += p[j].x * p[i].y - p[i].x * p[j].y;
            return Mathf.Abs(a) * 0.5f;
        }

        // ---------------- feedback ----------------

        public void SetSelected(bool selected, bool animate = true)
        {
            if (_selected == selected && animate) return;
            _selected = selected;
            if (_liftRoutine != null) StopCoroutine(_liftRoutine);
            var from = Rect.anchoredPosition;
            var to = HomePosition + (selected ? new Vector2(0, Height * 0.16f) : Vector2.zero);
            float glowFrom = _glow.color.a, glowTo = selected ? 0.55f : 0f;
            if (!animate)
            {
                Rect.anchoredPosition = to;
                _glow.color = UIStyle.WithAlpha(_glow.color, glowTo);
                return;
            }
            _liftRoutine = Tween.Run(this, MotionPrefs.Dur(UIStyle.DurFast + 0.05f), k =>
            {
                Rect.anchoredPosition = Vector2.LerpUnclamped(from, to, k);
                _glow.color = UIStyle.WithAlpha(_glow.color, Mathf.Lerp(glowFrom, glowTo, Mathf.Clamp01(k)));
            }, null, Ease.OutBack);
            Kick(selected ? 3f : 1.5f);
        }

        public bool IsSelected => _selected;

        public void SetHint(bool on)
        {
            if (_hintPulse != null) { StopCoroutine(_hintPulse); _hintPulse = null; }
            if (!on) { _hintRing.color = UIStyle.WithAlpha(_hintRing.color, 0f); return; }
            if (MotionPrefs.Reduced) { _hintRing.color = UIStyle.WithAlpha(_hintRing.color, 0.7f); return; }
            _hintPulse = StartCoroutine(HintRoutine());
        }

        private System.Collections.IEnumerator HintRoutine()
        {
            float t = 0;
            while (true)
            {
                t += Time.unscaledDeltaTime;
                _hintRing.color = UIStyle.WithAlpha(_hintRing.color, 0.35f + 0.35f * Mathf.Sin(t * 6f));
                yield return null;
            }
        }

        /// <summary>Gives the liquid surface a decaying slosh.</summary>
        public void Kick(float degrees)
        {
            if (MotionPrefs.Reduced) return;
            _wobble = Mathf.Max(_wobble, degrees);
            _wobbleTime = 0f;
        }

        public void PlayCompleted()
        {
            Kick(4f);
            if (MotionPrefs.Reduced) return;
            var t = Rect;
            Tween.Run(this, 0.35f, k =>
            {
                float s = 1f + Mathf.Sin(k * Mathf.PI) * 0.08f;
                t.localScale = new Vector3(s, s, 1);
            }, () => t.localScale = Vector3.one, Ease.Linear);
            var glow = _glow;
            Tween.Run(this, 0.6f, k => glow.color = new Color(0.7f, 1f, 0.8f, Mathf.Sin(k * Mathf.PI) * 0.7f), () =>
            {
                glow.color = new Color(1f, 0.95f, 0.6f, _selected ? 0.55f : 0f);
            }, Ease.Linear);
        }

        public void Hop(float delay)
        {
            if (MotionPrefs.Reduced) return;
            var home = HomePosition;
            Tween.Delay(this, delay, () =>
            {
                Tween.Run(this, 0.4f, k => Rect.anchoredPosition = home + new Vector2(0, Mathf.Sin(k * Mathf.PI) * Height * 0.12f),
                    () => Rect.anchoredPosition = home, Ease.Linear);
            });
        }

        private void Update()
        {
            if (_wobble <= 0.01f) return;
            _wobbleTime += Time.unscaledDeltaTime;
            float decay = Mathf.Exp(-_wobbleTime * 3.2f);
            float angle = Mathf.Sin(_wobbleTime * 14f) * _wobble * decay;
            _liquid.SetWobble(angle);
            if (decay < 0.02f) { _wobble = 0f; _liquid.SetWobble(0f); }
        }
    }
}
