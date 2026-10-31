using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Veliqora.Core.Puzzle;
using Veliqora.UI;

namespace Veliqora.Presentation
{
    /// <summary>
    /// Responsive bottle board. Lays bottles out in 1–3 rows inside its rect, forwards taps, and renders
    /// already-committed moves (pour / undo / add bottle). It never decides legality.
    /// </summary>
    public sealed class BoardView : MonoBehaviour
    {
        public Action<int> BottleTapped;
        /// <summary>Fired mid-pour when liquid starts flowing (for SFX/haptics).</summary>
        public Action<int> PourStarted;

        private readonly List<BottleView> _views = new List<BottleView>();
        private RectTransform _rect;
        private Image _stream;
        private BottleShape _shape;
        private int _capacity;
        private bool _symbols;
        private GameState _state;
        private Vector2 _lastSize;
        private Action _pendingDone; // completion of an in-flight animation; guaranteed to run once

        public IReadOnlyList<BottleView> Views => _views;
        public bool IsAnimating { get; private set; }

        private Color _glassColor = Color.white;

        /// <summary>Glass tint: white on dark backgrounds; a cool slate on the light background for contrast.</summary>
        public void SetGlassColor(Color c) => _glassColor = c;

        public void Init(BottleShape shape, int capacity, bool symbols)
        {
            _rect = (RectTransform)transform;
            _shape = shape;
            _capacity = capacity;
            _symbols = symbols;
        }

        public void SetSymbols(bool on)
        {
            _symbols = on;
            if (_state == null) return;
            for (int i = 0; i < _views.Count; i++) _views[i].SetSymbolsEnabled(on, _state[i]);
        }

        /// <summary>(Re)creates bottle views for a state. Safe to call any time no animation is running.</summary>
        public void Build(GameState state, bool animateIn)
        {
            StopAllCoroutines();
            IsAnimating = false;
            var interrupted = _pendingDone;
            _pendingDone = null;
            foreach (var v in _views) if (v != null) Destroy(v.gameObject);
            _views.Clear();
            if (_stream != null) Destroy(_stream.gameObject);
            _state = state;

            float width = ComputeBottleWidth(state.BottleCount);
            for (int i = 0; i < state.BottleCount; i++)
            {
                var go = new GameObject("Bottle_" + i, typeof(RectTransform));
                go.transform.SetParent(transform, false);
                var view = go.AddComponent<BottleView>();
                view.Init(i, _shape, _capacity, width, _symbols, _glassColor);
                view.Tapped = idx => BottleTapped?.Invoke(idx);
                view.ShowState(state[i]);
                _views.Add(view);
            }
            Layout(false);

            _stream = UIFactory.Image(transform, "Stream", SpriteLibrary.Rounded(12), Color.white);
            _stream.rectTransform.anchorMin = _stream.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            _stream.rectTransform.pivot = new Vector2(0.5f, 1f);
            _stream.gameObject.SetActive(false);

            if (animateIn && !MotionPrefs.Reduced)
            {
                for (int i = 0; i < _views.Count; i++)
                {
                    var v = _views[i];
                    v.Rect.localScale = new Vector3(0.6f, 0.6f, 1);
                    Tween.Delay(this, i * 0.035f, () => Tween.Scale(this, v.Rect, new Vector3(0.6f, 0.6f, 1), Vector3.one, 0.3f, Ease.OutBack));
                }
            }
            _lastSize = _rect.rect.size;
            interrupted?.Invoke();
        }

        private void Update()
        {
            if (_state == null || IsAnimating) return;
            if ((_rect.rect.size - _lastSize).sqrMagnitude > 1f)
            {
                _lastSize = _rect.rect.size;
                Build(_state, false); // resolution / safe-area change
            }
        }

        private void Rows(int n, out int rows, out int perRow)
        {
            rows = n <= 5 ? 1 : (n <= 12 ? 2 : 3);
            perRow = Mathf.CeilToInt(n / (float)rows);
        }

        private float ComputeBottleWidth(int n)
        {
            Rows(n, out int rows, out int perRow);
            var size = _rect.rect.size;
            float byWidth = size.x / (perRow * 1.5f + 0.4f);
            float byHeight = size.y / (rows * (_shape.Aspect * 1.32f) + 0.2f);
            return Mathf.Clamp(Mathf.Min(byWidth, byHeight), 60f, 190f);
        }

        private void Layout(bool animate)
        {
            int n = _views.Count;
            if (n == 0) return;
            Rows(n, out int rows, out int perRow);
            float w = _views[0].Width, h = _views[0].Height;
            float gapX = w * 0.5f;
            float rowH = h * 1.32f;
            float totalH = rows * rowH;
            int idx = 0;
            for (int r = 0; r < rows; r++)
            {
                int inRow = Mathf.Min(perRow, n - idx);
                float rowW = inRow * w + (inRow - 1) * gapX;
                float y = totalH * 0.5f - (r + 1) * rowH + (rowH - h) * 0.35f;
                for (int c = 0; c < inRow; c++, idx++)
                {
                    float x = -rowW * 0.5f + c * (w + gapX) + w * 0.5f;
                    var v = _views[idx];
                    v.HomePosition = new Vector2(x, y);
                    if (!animate) v.Rect.anchoredPosition = v.HomePosition;
                }
            }
        }

        public void SyncTo(GameState state)
        {
            _state = state;
            for (int i = 0; i < _views.Count && i < state.BottleCount; i++)
            {
                _views[i].ShowState(state[i]);
                _views[i].Rect.localEulerAngles = Vector3.zero;
                _views[i].Rect.anchoredPosition = _views[i].HomePosition;
            }
        }

        public void SetSelected(int index)
        {
            for (int i = 0; i < _views.Count; i++) _views[i].SetSelected(i == index);
        }

        public void ClearHints()
        {
            foreach (var v in _views) v.SetHint(false);
        }

        public void ShowHint(int from, int to)
        {
            ClearHints();
            if (from >= 0 && from < _views.Count) _views[from].SetHint(true);
            if (to >= 0 && to < _views.Count) _views[to].SetHint(true);
        }

        public void Shake(int index)
        {
            if (index < 0 || index >= _views.Count) return;
            var v = _views[index];
            Tween.Shake(this, v.Rect, v.Width * 0.12f, 0.3f);
            v.Kick(5f);
        }

        // ---------------- move animations ----------------

        private static List<LiquidGraphic.Layer> LayersOf(BottleState b)
        {
            var list = new List<LiquidGraphic.Layer>();
            int i = 0;
            while (i < b.Count)
            {
                int c = b[i], run = 0;
                while (i < b.Count && b[i] == c) { run++; i++; }
                list.Add(new LiquidGraphic.Layer(LiquidPalette.Get(c), run));
            }
            return list;
        }

        /// <summary>Layers of <paramref name="baseState"/> with <paramref name="delta"/> units of color added (+) or removed (-) on top.</summary>
        private static void Interpolate(List<LiquidGraphic.Layer> baseLayers, Color color, float delta, List<LiquidGraphic.Layer> output)
        {
            output.Clear();
            output.AddRange(baseLayers);
            if (delta > 0f)
            {
                if (output.Count > 0 && output[output.Count - 1].Color == color)
                {
                    var top = output[output.Count - 1];
                    output[output.Count - 1] = new LiquidGraphic.Layer(color, top.Amount + delta);
                }
                else output.Add(new LiquidGraphic.Layer(color, delta));
            }
            else if (delta < 0f && output.Count > 0)
            {
                var top = output[output.Count - 1];
                output[output.Count - 1] = new LiquidGraphic.Layer(top.Color, Mathf.Max(0f, top.Amount + delta));
            }
        }

        private static float TiltFor(float units, int capacity)
        {
            float f = Mathf.Clamp01(units / Mathf.Max(1, capacity));
            return Mathf.Lerp(100f, 58f, f);
        }

        private static Vector2 Rotate(Vector2 v, float degrees)
        {
            float r = degrees * Mathf.Deg2Rad;
            float c = Mathf.Cos(r), s = Mathf.Sin(r);
            return new Vector2(c * v.x - s * v.y, s * v.x + c * v.y);
        }

        /// <summary>Animates a committed pour. <paramref name="done"/> always runs (also if interrupted by Build()).</summary>
        public void AnimatePour(Move move, GameState before, GameState after, Action done)
        {
            _pendingDone = done;
            StartCoroutine(PourRoutine(move, before, after, done));
        }

        private IEnumerator PourRoutine(Move move, GameState before, GameState after, Action done)
        {
            IsAnimating = true;
            var src = _views[move.From];
            var dst = _views[move.To];
            var color = LiquidPalette.Get(move.Color);
            var srcBase = LayersOf(before[move.From]);
            var dstBase = LayersOf(before[move.To]);
            var srcTmp = new List<LiquidGraphic.Layer>();
            var dstTmp = new List<LiquidGraphic.Layer>();
            int srcCountBefore = before[move.From].Count;
            int dstCountBefore = before[move.To].Count;

            if (MotionPrefs.Reduced)
            {
                // Minimal motion: crossfade the levels in place.
                yield return FillPhase(src, dst, srcBase, dstBase, srcTmp, dstTmp, color, move.Amount, 0.15f, null);
                Finish(move, after, src, dst, done);
                yield break;
            }

            src.SetSelected(false, false);
            src.Rect.SetAsLastSibling();
            float dir = dst.HomePosition.x <= src.HomePosition.x ? 1f : -1f; // + = rotate CCW (pour to the left)
            var dstMouth = dst.HomePosition + new Vector2(0, dst.Height);
            var targetMouth = dstMouth + new Vector2(dir * dst.Width * 0.12f, dst.Height * 0.16f);
            var mouthLocal = new Vector2(0, src.Height);
            float aStart = dir * TiltFor(srcCountBefore, _capacity);
            float aEnd = dir * TiltFor(srcCountBefore - move.Amount, _capacity);
            Func<float, Vector2> pivotFor = a => targetMouth - Rotate(mouthLocal, a);

            // 1) lift & travel, tilting to the pouring angle
            var startPos = src.Rect.anchoredPosition;
            float t1 = MotionPrefs.Dur(0.28f);
            yield return Run(t1, k =>
            {
                float a = Mathf.Lerp(0f, aStart, k);
                src.Rect.localEulerAngles = new Vector3(0, 0, a);
                src.Rect.anchoredPosition = Vector2.Lerp(startPos, pivotFor(aStart), k);
            }, Ease.InOutCubic);

            // 2) pour: stream + drain/fill, mouth stays put while the bottle keeps tipping
            PourStarted?.Invoke(move.To);
            _stream.color = color;
            _stream.gameObject.SetActive(true);
            _stream.transform.SetAsLastSibling();
            float t2 = MotionPrefs.Dur(0.2f + 0.12f * move.Amount);
            float streamW = Mathf.Max(8f, src.Width * 0.13f);
            yield return FillPhase(src, dst, srcBase, dstBase, srcTmp, dstTmp, color, move.Amount, t2, k =>
            {
                float a = Mathf.Lerp(aStart, aEnd, k);
                src.Rect.localEulerAngles = new Vector3(0, 0, a);
                src.Rect.anchoredPosition = pivotFor(a);
                float surface = dst.HomePosition.y + dst.UprightSurface(dstCountBefore + move.Amount * k);
                float len = Mathf.Max(0f, targetMouth.y - surface);
                // stream thins out at the very start/end
                float thin = Mathf.Clamp01(Mathf.Min(k, 1f - k) * 8f);
                _stream.rectTransform.sizeDelta = new Vector2(streamW * (0.45f + 0.55f * thin), len);
                _stream.rectTransform.anchoredPosition = new Vector2(targetMouth.x, targetMouth.y);
            });
            _stream.gameObject.SetActive(false);
            dst.Kick(4f);

            // 3) return home
            var from = src.Rect.anchoredPosition;
            float aFrom = src.Rect.localEulerAngles.z;
            if (aFrom > 180f) aFrom -= 360f;
            float t3 = MotionPrefs.Dur(0.26f);
            yield return Run(t3, k =>
            {
                src.Rect.localEulerAngles = new Vector3(0, 0, Mathf.Lerp(aFrom, 0f, k));
                src.Rect.anchoredPosition = Vector2.Lerp(from, src.HomePosition, k);
            }, Ease.OutCubic);
            src.Kick(3f);
            Finish(move, after, src, dst, done);
        }

        private IEnumerator FillPhase(BottleView src, BottleView dst, List<LiquidGraphic.Layer> srcBase, List<LiquidGraphic.Layer> dstBase,
            List<LiquidGraphic.Layer> srcTmp, List<LiquidGraphic.Layer> dstTmp, Color color, int amount, float duration, Action<float> extra)
        {
            yield return Run(duration, k =>
            {
                Interpolate(srcBase, color, -amount * k, srcTmp);
                Interpolate(dstBase, color, amount * k, dstTmp);
                src.ShowLayers(srcTmp);
                dst.ShowLayers(dstTmp);
                extra?.Invoke(k);
            }, Ease.Linear);
        }

        private void Finish(Move move, GameState after, BottleView src, BottleView dst, Action done)
        {
            _state = after;
            src.Rect.localEulerAngles = Vector3.zero;
            src.Rect.anchoredPosition = src.HomePosition;
            src.ShowState(after[move.From]);
            dst.ShowState(after[move.To]);
            IsAnimating = false;
            CompletePending(done);
        }

        private void CompletePending(Action done)
        {
            if (_pendingDone == done) _pendingDone = null;
            done?.Invoke();
        }

        /// <summary>Undo: liquid flows back quickly (no travel), then the committed state is shown.</summary>
        public void AnimateUndo(Move undone, GameState before, GameState after, Action done)
        {
            _pendingDone = done;
            StartCoroutine(UndoRoutine(undone, before, after, done));
        }

        private IEnumerator UndoRoutine(Move m, GameState before, GameState after, Action done)
        {
            IsAnimating = true;
            if (m.From >= 0 && m.To >= 0 && m.From < _views.Count && m.To < _views.Count)
            {
                var color = LiquidPalette.Get(before[m.To].TopColor);
                var a = LayersOf(before[m.To]);
                var b = LayersOf(before[m.From]);
                var ta = new List<LiquidGraphic.Layer>();
                var tb = new List<LiquidGraphic.Layer>();
                var vTo = _views[m.To];
                var vFrom = _views[m.From];
                yield return Run(MotionPrefs.Dur(0.22f), k =>
                {
                    Interpolate(a, color, -m.Amount * k, ta);
                    Interpolate(b, color, m.Amount * k, tb);
                    vTo.ShowLayers(ta);
                    vFrom.ShowLayers(tb);
                }, Ease.OutCubic);
                vTo.Kick(2.5f);
                vFrom.Kick(2.5f);
            }
            IsAnimating = false;
            SyncTo(after);
            CompletePending(done);
        }

        public void Celebrate()
        {
            for (int i = 0; i < _views.Count; i++) _views[i].Hop(i * 0.06f);
        }

        private IEnumerator Run(float duration, Action<float> step, Func<float, float> ease)
        {
            if (duration <= 0f) { step(1f); yield break; }
            float t = 0f;
            while (t < duration)
            {
                step(ease(t / duration));
                yield return null;
                t += Time.unscaledDeltaTime;
            }
            step(1f);
        }
    }
}
