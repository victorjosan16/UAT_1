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
        private Image _aura;

        /// <summary>Neon aura behind bottles, tinted by the top liquid (enabled on dark themes).</summary>
        public static bool NeonAura = false; // clean look: no neon glow around bottles by default
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

            _aura = UIFactory.Image(transform, "Aura", SpriteLibrary.RadialGlow, new Color(1, 1, 1, 0));
            UIFactory.Stretch(_aura.rectTransform, -Width * 0.55f);
            _aura.rectTransform.offsetMax = new Vector2(Width * 0.55f, -Height * 0.15f);

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

        // ---------------- special bottles ----------------

        private RectTransform _specialRoot;
        private readonly List<Text> _questionMarks = new List<Text>();
        private Image _cork;
        private RectTransform _lockTag;
        private Text _lockText;
        private Image _lockDot;
        private RectTransform _adOverlay;
        private int _lastHidden = -1;

        /// <summary>Mystery "?" marks, cork + live unlock condition, ad-lock overlay. Call after ShowState.</summary>
        public void ShowSpecial(BottleState b, string lockLabel, Color lockColor, int index)
        {
            if (_specialRoot == null)
            {
                _specialRoot = UIFactory.Rect("Special", transform);
                _specialRoot.SetAsLastSibling();
            }
            // mystery marks
            while (_questionMarks.Count < Capacity)
            {
                var q = UIFactory.Text(_specialRoot, "?", Mathf.RoundToInt(Width * 0.42f), new Color(1f, 1f, 1f, 0.85f));
                q.rectTransform.anchorMin = q.rectTransform.anchorMax = new Vector2(0.5f, 0f);
                q.rectTransform.sizeDelta = new Vector2(Width, Width * 0.5f);
                UIFactory.AddShadow(q, new Color(0, 0, 0, 0.5f), new Vector2(0, -2));
                _questionMarks.Add(q);
            }
            for (int i = 0; i < _questionMarks.Count; i++)
            {
                bool on = i < b.Hidden;
                _questionMarks[i].gameObject.SetActive(on);
                if (on) _questionMarks[i].rectTransform.anchoredPosition = new Vector2(0, UprightUnitCenter(i));
            }
            _lastHidden = b.Hidden;

            // cork + condition tag
            bool corked = b.IsCorked;
            if (corked && _cork == null)
            {
                _cork = UIFactory.Image(_specialRoot, "Cork", SpriteLibrary.Get(Icon.Cork), Color.white);
                _cork.rectTransform.anchorMin = _cork.rectTransform.anchorMax = new Vector2(0.5f, 0f);
                _cork.rectTransform.sizeDelta = new Vector2(Width * Shape.NeckWidth * 1.25f, Width * 0.62f);
                _cork.rectTransform.anchoredPosition = new Vector2(0, Height - Width * 0.12f);
                var tagBg = UIFactory.Image(_specialRoot, "LockTag", SpriteLibrary.Rounded(20), new Color(0.08f, 0.06f, 0.2f, 0.92f));
                _lockTag = tagBg.rectTransform;
                _lockTag.anchorMin = _lockTag.anchorMax = new Vector2(0.5f, 0f);
                _lockTag.sizeDelta = new Vector2(Mathf.Max(150f, Width * 1.5f), 46f);
                _lockTag.anchoredPosition = new Vector2(0, -30f);
                UIFactory.AddOutline(tagBg, UIStyle.WithAlpha(UIStyle.ThemeAccent, 0.8f), 2f);
                var lockIcon = UIFactory.Image(_lockTag, "Lock", SpriteLibrary.Get(Icon.Lock), Color.white);
                UIFactory.Place(lockIcon.rectTransform, new Vector2(0, 0.5f), new Vector2(30, 30), new Vector2(8, 0), new Vector2(0, 0.5f));
                _lockDot = UIFactory.Image(_lockTag, "Dot", SpriteLibrary.Circle, Color.white);
                UIFactory.Place(_lockDot.rectTransform, new Vector2(1, 0.5f), new Vector2(22, 22), new Vector2(-10, 0), new Vector2(1, 0.5f));
                _lockText = UIFactory.Text(_lockTag, "", 22, Color.white);
                _lockText.rectTransform.offsetMin = new Vector2(38, 0);
                _lockText.rectTransform.offsetMax = new Vector2(-34, 0);
                _lockText.horizontalOverflow = HorizontalWrapMode.Overflow;
            }
            if (_cork != null)
            {
                _cork.gameObject.SetActive(corked);
                _lockTag.gameObject.SetActive(corked);
                if (corked)
                {
                    _lockText.text = lockLabel ?? "";
                    bool colorCond = b.Lock.Kind == LockKind.CompleteColor;
                    _lockDot.gameObject.SetActive(colorCond);
                    _lockDot.color = lockColor;
                }
            }

            // rewarded (ad-locked) bottle
            if (b.RewardLocked && _adOverlay == null)
            {
                _adOverlay = UIFactory.Rect("AdLock", _specialRoot);
                var icon = UIFactory.Image(_adOverlay, "Video", SpriteLibrary.Get(Icon.Ad), Color.white);
                UIFactory.Place(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(Width * 0.8f, Width * 0.6f), Vector2.zero);
                var t = UIFactory.Text(_adOverlay, Loc.T("Watch ad"), 22, Color.white);
                UIFactory.Place(t.rectTransform, new Vector2(0.5f, 0f), new Vector2(Width * 1.8f, 40), new Vector2(0, -26), new Vector2(0.5f, 0.5f));
                UIFactory.AddShadow(t, new Color(0, 0, 0, 0.6f), new Vector2(0, -2));
            }
            if (_adOverlay != null) _adOverlay.gameObject.SetActive(b.RewardLocked);
            _glass.color = UIStyle.WithAlpha(_glass.color, b.RewardLocked ? 0.45f : (Shape.IsAuthored ? 0.38f : 1f));
            _liquid.color = b.IsLocked ? new Color(0.82f, 0.82f, 0.82f, 1f) : Color.white;
        }

        /// <summary>Cork flies up and fades; condition tag disappears.</summary>
        public void PlayUncork()
        {
            if (_cork == null) return;
            var cork = _cork.rectTransform;
            var tag = _lockTag;
            var start = cork.anchoredPosition;
            if (tag != null) tag.gameObject.SetActive(false);
            if (MotionPrefs.Reduced) { _cork.gameObject.SetActive(false); return; }
            _cork.gameObject.SetActive(true);
            Tween.Run(this, 0.45f, k =>
            {
                cork.anchoredPosition = start + new Vector2(Mathf.Sin(k * 3f) * 10f, k * Height * 0.5f);
                cork.localEulerAngles = new Vector3(0, 0, k * 200f);
                _cork.color = new Color(1, 1, 1, 1f - k);
            }, () =>
            {
                cork.anchoredPosition = start;
                cork.localEulerAngles = Vector3.zero;
                _cork.color = Color.white;
                _cork.gameObject.SetActive(false);
            }, Ease.OutCubic);
            Kick(5f);
        }

        // ---------------- state display ----------------

        /// <summary>Smoky color used for hidden mystery layers (the real color is never shown until revealed).</summary>
        public static readonly Color MysteryColor = new Color(0.24f, 0.21f, 0.34f, 1f);

        /// <summary>Visible layers: hidden units render as mystery smoke, revealed runs in their real colors.</summary>
        public static List<LiquidGraphic.Layer> DisplayLayers(BottleState b, List<LiquidGraphic.Layer> output)
        {
            output.Clear();
            if (b.Hidden > 0) output.Add(new LiquidGraphic.Layer(MysteryColor, b.Hidden));
            int i = b.Hidden;
            while (i < b.Count)
            {
                int c = b[i];
                int run = 0;
                while (i < b.Count && b[i] == c) { run++; i++; }
                output.Add(new LiquidGraphic.Layer(LiquidPalette.Get(c), run));
            }
            return output;
        }

        public void ShowState(BottleState b)
        {
            DisplayLayers(b, _scratch);
            _liquid.SetLayers(_scratch);
            UpdateSymbols(b);
            if (_aura != null)
            {
                var c = b.IsEmpty ? Color.clear : LiquidPalette.Get(b.TopColor);
                c.a = NeonAura && !b.IsEmpty ? (b.IsCompleted ? 0.34f : 0.16f) : 0f;
                _aura.color = c;
            }
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
                bool on = i < b.Count && i >= b.Hidden;
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
