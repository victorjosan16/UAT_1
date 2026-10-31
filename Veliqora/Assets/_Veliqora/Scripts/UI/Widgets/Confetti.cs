using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Veliqora.Presentation;

namespace Veliqora.UI.Widgets
{
    /// <summary>Restrained, short confetti burst (UI images, ~40 pieces, auto-destroys). Skipped in reduced motion.</summary>
    public sealed class Confetti : MonoBehaviour
    {
        private struct Piece { public RectTransform Rt; public Vector2 V; public float Spin; public Image Img; }
        private readonly List<Piece> _pieces = new List<Piece>();
        private float _t;
        private const float Life = 2.2f;

        public static void Burst(RectTransform parent, Vector2 origin, int count = 40)
        {
            if (MotionPrefs.Reduced) return;
            var go = new GameObject("Confetti", typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = (RectTransform)go.transform;
            UIFactory.Stretch(rt);
            var c = go.AddComponent<Confetti>();
            c.Spawn(origin, count);
        }

        private void Spawn(Vector2 origin, int count)
        {
            var rng = new System.Random(System.Environment.TickCount);
            for (int i = 0; i < count; i++)
            {
                var img = UIFactory.Image(transform, "p", i % 3 == 0 ? SpriteLibrary.Circle : SpriteLibrary.Rounded(6), LiquidPalette.Get(1 + i % 8));
                var r = img.rectTransform;
                r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f);
                r.sizeDelta = i % 3 == 0 ? new Vector2(22, 22) : new Vector2(18, 34);
                r.anchoredPosition = origin;
                float ang = (float)(rng.NextDouble() * Mathf.PI * 0.9 + Mathf.PI * 0.05);
                float spd = 900f + (float)rng.NextDouble() * 900f;
                _pieces.Add(new Piece { Rt = r, Img = img, V = new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * spd, Spin = (float)(rng.NextDouble() * 720 - 360) });
            }
        }

        private void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            _t += dt;
            for (int i = 0; i < _pieces.Count; i++)
            {
                var p = _pieces[i];
                p.V += new Vector2(0, -2200f) * dt;
                p.V *= 1f - 0.9f * dt;
                p.Rt.anchoredPosition += p.V * dt;
                p.Rt.localEulerAngles += new Vector3(0, 0, p.Spin * dt);
                var c = p.Img.color;
                c.a = Mathf.Clamp01((Life - _t) / 0.6f);
                p.Img.color = c;
                _pieces[i] = p;
            }
            if (_t > Life) Destroy(gameObject);
        }
    }
}
