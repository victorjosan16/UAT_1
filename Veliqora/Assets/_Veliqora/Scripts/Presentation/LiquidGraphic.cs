using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace Veliqora.Presentation
{
    /// <summary>
    /// Renders liquid layers inside a convex bottle interior as a single mesh (one draw call per bottle, no masks).
    /// Layer boundaries are kept horizontal in WORLD space and positioned by AREA, so the liquid volume is
    /// preserved while the bottle tilts during a pour, and colors never bleed between units.
    /// </summary>
    [RequireComponent(typeof(CanvasRenderer))]
    public sealed class LiquidGraphic : MaskableGraphic
    {
        public struct Layer
        {
            public Color Color;
            public float Amount; // in units (can be fractional while animating)
            public Layer(Color c, float a) { Color = c; Amount = a; }
        }

        private readonly List<Layer> _layers = new List<Layer>();
        private List<Vector2> _interior = new List<Vector2>();
        private int _capacity = 4;
        private float _wobbleDeg;
        private float _lastWorldAngle = float.NaN;

        // scratch buffers (avoid GC during gameplay)
        private readonly List<Vector2> _polyA = new List<Vector2>(32);
        private readonly List<Vector2> _polyB = new List<Vector2>(32);
        private readonly List<Vector2> _scaled = new List<Vector2>(32);
        private readonly List<float> _levels = new List<float>(8);

        public float SurfaceThickness = 0.045f; // fraction of bottle width

        public void Configure(List<Vector2> normalizedInterior, int capacity)
        {
            _interior = normalizedInterior;
            _capacity = Mathf.Max(1, capacity);
            raycastTarget = false;
            SetVerticesDirty();
        }

        public void SetLayers(IList<Layer> layers)
        {
            _layers.Clear();
            for (int i = 0; i < layers.Count; i++) if (layers[i].Amount > 0.0001f) _layers.Add(layers[i]);
            SetVerticesDirty();
        }

        public void SetWobble(float degrees)
        {
            if (Mathf.Abs(degrees - _wobbleDeg) < 0.01f) return;
            _wobbleDeg = degrees;
            SetVerticesDirty();
        }

        private void LateUpdate()
        {
            // Rebuild when the bottle rotates (pour animation) so the surface stays level.
            float a = transform.eulerAngles.z;
            if (!Mathf.Approximately(a, _lastWorldAngle))
            {
                _lastWorldAngle = a;
                SetVerticesDirty();
            }
        }

        protected override void OnPopulateMesh(VertexHelper vh)
        {
            vh.Clear();
            if (_interior == null || _interior.Count < 3 || _layers.Count == 0) return;

            var r = rectTransform.rect;
            float k = r.width;
            var origin = new Vector2(r.center.x, r.yMin);
            _scaled.Clear();
            for (int i = 0; i < _interior.Count; i++) _scaled.Add(origin + _interior[i] * k);

            float worldAngle = transform.eulerAngles.z * Mathf.Deg2Rad;
            float theta = worldAngle + _wobbleDeg * Mathf.Deg2Rad;
            // World "up" expressed in this graphic's local space.
            var up = new Vector2(Mathf.Sin(theta), Mathf.Cos(theta));

            float totalArea = Area(_scaled);
            float minC = float.MaxValue, maxC = float.MinValue;
            for (int i = 0; i < _scaled.Count; i++)
            {
                float c = Vector2.Dot(_scaled[i], up);
                minC = Mathf.Min(minC, c);
                maxC = Mathf.Max(maxC, c);
            }

            // Cumulative level for each layer top.
            _levels.Clear();
            float cumulative = 0f;
            for (int i = 0; i < _layers.Count; i++)
            {
                cumulative += _layers[i].Amount;
                float frac = Mathf.Clamp01(cumulative / _capacity);
                _levels.Add(LevelForArea(up, frac * totalArea, minC, maxC));
            }

            float lower = minC - 1f;
            float surfaceBand = SurfaceThickness * k;
            for (int i = 0; i < _layers.Count; i++)
            {
                float upper = _levels[i];
                if (upper <= lower + 0.01f) continue;
                var col = _layers[i].Color;
                bool topLayer = i == _layers.Count - 1;
                if (topLayer && upper - lower > surfaceBand * 1.5f)
                {
                    EmitBand(vh, up, lower, upper - surfaceBand, col, UIColor(col, 0.0f), UIColor(col, 0.12f));
                    var surf = Color.Lerp(col, Color.white, 0.35f);
                    EmitBand(vh, up, upper - surfaceBand, upper, surf, surf, surf);
                }
                else
                {
                    EmitBand(vh, up, lower, upper, col, UIColor(col, 0.0f), UIColor(col, 0.12f));
                }
                lower = upper;
            }
        }

        private static Color UIColor(Color c, float lighten) => Color.Lerp(c, Color.white, lighten);

        private void EmitBand(VertexHelper vh, Vector2 up, float lo, float hi, Color baseColor, Color bottomColor, Color topColor)
        {
            Clip(_scaled, up, hi, true, _polyA);
            Clip(_polyA, up, lo, false, _polyB);
            if (_polyB.Count < 3) return;
            int start = vh.currentVertCount;
            float span = Mathf.Max(0.001f, hi - lo);
            for (int i = 0; i < _polyB.Count; i++)
            {
                float t = Mathf.Clamp01((Vector2.Dot(_polyB[i], up) - lo) / span);
                var c = Color.Lerp(bottomColor, topColor, t) * color;
                vh.AddVert(_polyB[i], c, Vector2.zero);
            }
            for (int i = 1; i < _polyB.Count - 1; i++) vh.AddTriangle(start, start + i, start + i + 1);
        }

        private float LevelForArea(Vector2 up, float target, float minC, float maxC) =>
            LiquidGeometry.LevelForArea(_scaled, up, target, minC, maxC, _polyA);

        private static void Clip(List<Vector2> input, Vector2 n, float c, bool keepBelow, List<Vector2> output) =>
            LiquidGeometry.Clip(input, n, c, keepBelow, output);

        private static float Area(List<Vector2> p) => LiquidGeometry.Area(p);
    }
}
