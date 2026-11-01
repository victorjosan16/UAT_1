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
        private RectTransform _lockTag;
        private Text _lockText;
        private Image _lockDot;
        // frozen obstacle: ice slab with countdown
        private RectTransform _ice;
        private Text _iceCount;
        private int _lastRemaining = -1;
        // locked obstacle: chains + colored padlock (the key is completing that color)
        private RectTransform _padlockRoot;
        private Image _padlock;
        private Image _padKey;
        private ObstacleKind _obstacle;
        private RectTransform _adOverlay;
        private int _lastHidden = -1;

        /// <summary>Mystery "?" marks, cork + live unlock condition, ad-lock overlay. Call after ShowState.</summary>
        public void ShowSpecial(BottleState b, string lockLabel, Color lockColor, int index, int remaining = -1)
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

            // obstacles: FROZEN (ice + countdown) or LOCKED (chains + padlock in the key color), plus a condition tag
            bool corked = b.IsCorked;
            if (corked && _lockTag == null)
            {
                var tagBg = UIFactory.Image(_specialRoot, "LockTag", SpriteLibrary.Rounded(20), new Color(0.06f, 0.09f, 0.16f, 0.92f));
                _lockTag = tagBg.rectTransform;
                _lockTag.anchorMin = _lockTag.anchorMax = new Vector2(0.5f, 0f);
                _lockTag.sizeDelta = new Vector2(Mathf.Clamp(Width * 1.45f, 96f, 190f), 40f);
                _lockTag.anchoredPosition = new Vector2(0, -26f);
                UIFactory.AddOutline(tagBg, UIStyle.WithAlpha(UIStyle.ThemeAccent, 0.8f), 2f);
                _lockDot = UIFactory.Image(_lockTag, "Dot", SpriteLibrary.Circle, Color.white);
                UIFactory.Place(_lockDot.rectTransform, new Vector2(0, 0.5f), new Vector2(20, 20), new Vector2(8, 0), new Vector2(0, 0.5f));
                _lockText = UIFactory.Text(_lockTag, "", 20, Color.white);
                _lockText.rectTransform.offsetMin = new Vector2(30, 0);
                _lockText.rectTransform.offsetMax = new Vector2(-6, 0);
                _lockText.resizeTextForBestFit = true;
                _lockText.resizeTextMinSize = 12;
                _lockText.resizeTextMaxSize = 20;
            }
            if (corked)
            {
                _obstacle = b.Lock.Obstacle;
                if (_obstacle == ObstacleKind.Frozen) EnsureIce(); else EnsurePadlock();
            }
            if (_ice != null) _ice.gameObject.SetActive(corked && _obstacle == ObstacleKind.Frozen);
            if (_padlockRoot != null) _padlockRoot.gameObject.SetActive(corked && _obstacle == ObstacleKind.Locked);
            if (_lockTag != null)
            {
                _lockTag.gameObject.SetActive(corked);
                if (corked)
                {
                    _lockText.text = lockLabel ?? "";
                    bool colorCond = b.Lock.Kind == LockKind.CompleteColor;
                    _lockDot.color = colorCond ? lockColor : (_obstacle == ObstacleKind.Frozen ? UIStyle.Hex("#9FE3FF") : Color.white);
                }
            }
            if (corked && _obstacle == ObstacleKind.Frozen && _iceCount != null)
            {
                _iceCount.text = remaining >= 0 ? remaining.ToString() : "";
                if (_lastRemaining >= 0 && remaining >= 0 && remaining < _lastRemaining && !MotionPrefs.Reduced)
                {
                    Tween.Scale(this, _iceCount.rectTransform, new Vector3(1.5f, 1.5f, 1), Vector3.one, 0.3f, Ease.OutBack);
                    Kick(2f); // ice cracks a little with every step of the countdown
                }
                _lastRemaining = remaining;
            }
            else _lastRemaining = -1;
            if (corked && _obstacle == ObstacleKind.Locked && _padlock != null)
            {
                bool colorKey = b.Lock.Kind == LockKind.CompleteColor;
                _padlock.color = colorKey ? Color.Lerp(lockColor, Color.white, 0.15f) : UIStyle.Hex("#E7C46A");
                _padKey.gameObject.SetActive(colorKey);
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

        private void EnsureIce()
        {
            if (_ice != null) return;
            _ice = UIFactory.Rect("Ice", _specialRoot);
            _ice.anchorMin = _ice.anchorMax = new Vector2(0.5f, 0f);
            _ice.pivot = new Vector2(0.5f, 0f);
            _ice.sizeDelta = new Vector2(Width * 1.08f, Height * 1.02f);
            _ice.anchoredPosition = new Vector2(0, -Width * 0.02f);
            var slab = UIFactory.Image(_ice, "Slab", SpriteLibrary.Rounded(28), new Color(0.72f, 0.9f, 1f, 0.42f));
            UIFactory.Stretch(slab.rectTransform);
            UIFactory.AddOutline(slab, new Color(0.85f, 0.97f, 1f, 0.85f), 2f);
            // frost streaks (discreet reflections)
            for (int i = 0; i < 3; i++)
            {
                var streak = UIFactory.Image(_ice, "Frost" + i, SpriteLibrary.Rounded(6), new Color(1f, 1f, 1f, 0.28f - i * 0.06f));
                UIFactory.Place(streak.rectTransform, new Vector2(0.28f + i * 0.2f, 0.62f - i * 0.12f), new Vector2(Width * 0.08f, Height * 0.36f), Vector2.zero);
                streak.rectTransform.localEulerAngles = new Vector3(0, 0, 18f);
            }
            var flake = UIFactory.Image(_ice, "Snowflake", SpriteLibrary.Get(Icon.Snowflake), new Color(1f, 1f, 1f, 0.9f));
            UIFactory.Place(flake.rectTransform, new Vector2(0.5f, 0.62f), new Vector2(Width * 0.62f, Width * 0.62f), Vector2.zero);
            _iceCount = UIFactory.Text(_ice, "", Mathf.RoundToInt(Mathf.Clamp(Width * 0.48f, 22f, 64f)), Color.white);
            UIFactory.Place(_iceCount.rectTransform, new Vector2(0.5f, 0.3f), new Vector2(Width, Width * 0.6f), Vector2.zero);
            UIFactory.AddOutline(_iceCount, UIStyle.Hex("#2A6FB0"), 3f);
            foreach (var g in _ice.GetComponentsInChildren<Graphic>()) g.raycastTarget = false;
        }

        private void EnsurePadlock()
        {
            if (_padlockRoot != null) return;
            _padlockRoot = UIFactory.Rect("Padlock", _specialRoot);
            _padlockRoot.anchorMin = _padlockRoot.anchorMax = new Vector2(0.5f, 0f);
            _padlockRoot.sizeDelta = new Vector2(Width, Height);
            _padlockRoot.pivot = new Vector2(0.5f, 0f);
            _padlockRoot.anchoredPosition = Vector2.zero;
            // two crossing chains
            for (int i = 0; i < 2; i++)
            {
                var chain = UIFactory.Image(_padlockRoot, "Chain" + i, SpriteLibrary.Rounded(8), new Color(0.78f, 0.8f, 0.86f, 0.9f));
                UIFactory.Place(chain.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(Width * 1.25f, Mathf.Max(8f, Width * 0.1f)), Vector2.zero);
                chain.rectTransform.localEulerAngles = new Vector3(0, 0, i == 0 ? 28f : -28f);
                UIFactory.AddOutline(chain, new Color(0.25f, 0.27f, 0.33f, 0.9f), 1.5f);
            }
            _padlock = UIFactory.Image(_padlockRoot, "Lock", SpriteLibrary.Get(Icon.Lock), Color.white);
            UIFactory.Place(_padlock.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(Width * 0.66f, Width * 0.66f), new Vector2(0, -Width * 0.05f));
            UIFactory.AddShadow(_padlock, new Color(0, 0, 0, 0.45f), new Vector2(0, -3));
            _padKey = UIFactory.Image(_padlock.rectTransform, "Key", SpriteLibrary.Get(Icon.Key), new Color(0.1f, 0.12f, 0.2f, 0.85f));
            UIFactory.Place(_padKey.rectTransform, new Vector2(0.5f, 0.38f), new Vector2(Width * 0.34f, Width * 0.34f), Vector2.zero);
            foreach (var g in _padlockRoot.GetComponentsInChildren<Graphic>()) g.raycastTarget = false;
        }

        /// <summary>
        /// Obstacle cleared: ice cracks and melts away (scale up + fade), or the padlock springs open and drops with
        /// the chains. The condition tag disappears.
        /// </summary>
        public void PlayUncork()
        {
            if (_lockTag != null) _lockTag.gameObject.SetActive(false);
            _lastRemaining = -1;
            RectTransform target = _obstacle == ObstacleKind.Frozen ? _ice : _padlockRoot;
            if (target == null) return;
            if (MotionPrefs.Reduced) { target.gameObject.SetActive(false); return; }
            target.gameObject.SetActive(true);
            var graphics = target.GetComponentsInChildren<Graphic>();
            var alphas = new float[graphics.Length];
            for (int i = 0; i < graphics.Length; i++) alphas[i] = graphics[i].color.a;
            var lockRt = _padlock != null ? _padlock.rectTransform : null;
            var lockStart = lockRt != null ? lockRt.anchoredPosition : Vector2.zero;
            bool frozen = _obstacle == ObstacleKind.Frozen;
            Tween.Run(this, frozen ? 0.5f : 0.55f, k =>
            {
                if (frozen) target.localScale = Vector3.one * (1f + 0.18f * k);
                else if (lockRt != null)
                {
                    lockRt.anchoredPosition = lockStart + new Vector2(0, Mathf.Sin(k * Mathf.PI) * Width * 0.35f - k * Width * 0.6f);
                    lockRt.localEulerAngles = new Vector3(0, 0, -k * 40f);
                }
                for (int i = 0; i < graphics.Length; i++) graphics[i].color = UIStyle.WithAlpha(graphics[i].color, alphas[i] * (1f - k));
            }, () =>
            {
                target.localScale = Vector3.one;
                if (lockRt != null) { lockRt.anchoredPosition = lockStart; lockRt.localEulerAngles = Vector3.zero; }
                for (int i = 0; i < graphics.Length; i++) graphics[i].color = UIStyle.WithAlpha(graphics[i].color, alphas[i]);
                target.gameObject.SetActive(false);
            }, Ease.OutCubic);
            Kick(frozen ? 6f : 4f);
        }

        /// <summary>True while the obstacle shown is ice (used by the board to pick the burst color).</summary>
        public bool ShowsIce => _obstacle == ObstacleKind.Frozen;

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
