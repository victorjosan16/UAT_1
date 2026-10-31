using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Veliqora.Presentation;

namespace Veliqora.UI.Widgets
{
    /// <summary>
    /// Lightweight pooled UI particles (no allocations after warm-up, one shared sprite so they batch).
    /// Used for pour splashes, rising bubbles, completion sparkles and ambient floating motes.
    /// All emitters are skipped in reduced-motion mode.
    /// </summary>
    public sealed class UiParticles : MonoBehaviour
    {
        private struct P
        {
            public Image Img;
            public Vector2 Pos, Vel;
            public float Age, Life, Size, Gravity, Drag, Spin, Twinkle;
            public Color Color;
            public bool Alive;
        }

        private readonly List<P> _pool = new List<P>(96);
        private RectTransform _rt;
        private int _max = 96;

        // ambient motes
        private bool _ambient;
        private float _ambientRate;
        private float _ambientAcc;
        private Color _ambientA = Color.white, _ambientB = Color.white;
        private string _style = "stars";

        public static UiParticles Create(RectTransform parent, int max = 96)
        {
            var rt = UIFactory.Rect("Particles", parent);
            var p = rt.gameObject.AddComponent<UiParticles>();
            p._rt = rt;
            p._max = max;
            return p;
        }

        private int Spawn()
        {
            for (int i = 0; i < _pool.Count; i++) if (!_pool[i].Alive) return i;
            if (_pool.Count >= _max) return -1;
            var img = UIFactory.Image(_rt, "p", SpriteLibrary.RadialGlow, Color.white);
            img.rectTransform.anchorMin = img.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            img.raycastTarget = false;
            img.gameObject.SetActive(false);
            _pool.Add(new P { Img = img });
            return _pool.Count - 1;
        }

        private void Emit(Vector2 pos, Vector2 vel, float life, float size, Color color, float gravity, float drag = 0.5f, float twinkle = 0f)
        {
            int i = Spawn();
            if (i < 0) return;
            var p = _pool[i];
            p.Pos = pos; p.Vel = vel; p.Age = 0; p.Life = life; p.Size = size; p.Color = color;
            p.Gravity = gravity; p.Drag = drag; p.Twinkle = twinkle; p.Alive = true;
            p.Img.gameObject.SetActive(true);
            p.Img.rectTransform.sizeDelta = new Vector2(size, size);
            p.Img.rectTransform.anchoredPosition = pos;
            p.Img.color = color;
            _pool[i] = p;
        }

        /// <summary>Droplets jumping out of a liquid surface (pour landing).</summary>
        public void Splash(Vector2 pos, Color color, float scale = 1f)
        {
            if (MotionPrefs.Reduced) return;
            var bright = Color.Lerp(color, Color.white, 0.35f);
            for (int k = 0; k < 7; k++)
            {
                float ang = Mathf.Lerp(0.35f, 2.8f, Random.value);
                float spd = (160f + Random.value * 220f) * scale;
                Emit(pos, new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * spd, 0.45f + Random.value * 0.2f, (10f + Random.value * 10f) * scale, bright, -1400f * scale, 0.3f);
            }
        }

        /// <summary>Small bubbles rising inside a bottle from its liquid.</summary>
        public void Bubbles(Vector2 bottom, float width, float height, Color color)
        {
            if (MotionPrefs.Reduced) return;
            for (int k = 0; k < 5; k++)
            {
                var start = bottom + new Vector2((Random.value - 0.5f) * width * 0.5f, height * (0.1f + Random.value * 0.4f));
                Emit(start, new Vector2((Random.value - 0.5f) * 12f, 90f + Random.value * 90f), 0.5f + Random.value * 0.4f, 7f + Random.value * 7f,
                    new Color(1f, 1f, 1f, 0.75f), 0f, 0.1f);
            }
        }

        /// <summary>Sparkle burst for a completed bottle or a win.</summary>
        public void Sparkle(Vector2 pos, Color color, int count = 12, float radius = 1f)
        {
            if (MotionPrefs.Reduced) return;
            for (int k = 0; k < count; k++)
            {
                float ang = Random.value * Mathf.PI * 2f;
                float spd = (120f + Random.value * 260f) * radius;
                var c = k % 3 == 0 ? Color.white : Color.Lerp(color, Color.white, 0.4f);
                Emit(pos, new Vector2(Mathf.Cos(ang), Mathf.Sin(ang)) * spd, 0.6f + Random.value * 0.4f, 12f + Random.value * 14f, c, -180f, 2.2f, 18f);
            }
        }

        /// <summary>Continuous slow motes drifting upward (ambient life on dark backgrounds).</summary>
        public void SetAmbient(bool on, Color a, Color b, float perSecond = 5f, string style = "stars")
        {
            _ambient = on && !MotionPrefs.Reduced;
            _ambientA = a;
            _ambientB = b;
            _ambientRate = perSecond;
            _style = string.IsNullOrEmpty(style) ? "stars" : style;
        }

        /// <summary>Per-theme ambient behaviour (velocity, size, life, gravity, twinkle).</summary>
        private void EmitAmbient(Vector2 size)
        {
            var c = Color.Lerp(_ambientA, _ambientB, Random.value);
            float x = (Random.value - 0.5f) * size.x;
            switch (_style)
            {
                case "sparks":
                    c.a = 0.8f;
                    Emit(new Vector2(x, -size.y * 0.5f + Random.value * size.y * 0.4f), new Vector2((Random.value - 0.5f) * 40f, 120f + Random.value * 120f), 1.6f, 6f + Random.value * 6f, c, -30f, 0.2f, 9f);
                    break;
                case "embers":
                    c.a = 0.75f;
                    Emit(new Vector2(x, -size.y * 0.5f), new Vector2((Random.value - 0.5f) * 30f, 50f + Random.value * 60f), 6f, 8f + Random.value * 10f, c, 0f, 0f, 4f);
                    break;
                case "bubbles":
                    c = Color.Lerp(c, Color.white, 0.5f); c.a = 0.45f;
                    Emit(new Vector2(x, -size.y * 0.5f), new Vector2((Random.value - 0.5f) * 10f, 40f + Random.value * 50f), 9f, 12f + Random.value * 22f, c, 0f, 0f, 1.5f);
                    break;
                case "crystals":
                    c.a = 0.6f;
                    Emit(new Vector2(x, size.y * 0.5f), new Vector2((Random.value - 0.5f) * 12f, -(20f + Random.value * 25f)), 9f, 7f + Random.value * 9f, c, 0f, 0f, 6f);
                    break;
                case "mist":
                    c.a = 0.18f;
                    Emit(new Vector2(x, -size.y * 0.5f + Random.value * size.y * 0.3f), new Vector2((Random.value - 0.5f) * 20f, 12f + Random.value * 14f), 9f, 80f + Random.value * 120f, c, 0f, 0f, 0f);
                    break;
                case "fireflies":
                    c.a = 0.85f;
                    Emit(new Vector2(x, -size.y * 0.5f + Random.value * size.y), new Vector2((Random.value - 0.5f) * 50f, (Random.value - 0.5f) * 50f), 5f + Random.value * 3f, 9f + Random.value * 8f, c, 0f, 0f, 5f);
                    break;
                default: // stars
                    c.a = 0.55f;
                    Emit(new Vector2(x, -size.y * 0.5f + Random.value * size.y * 0.7f), new Vector2((Random.value - 0.5f) * 14f, 18f + Random.value * 30f), 5f + Random.value * 4f, 8f + Random.value * 16f, c, 0f, 0f, 2.5f);
                    break;
            }
        }

        private void Update()
        {
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.05f);
            if (_ambient && _rt != null)
            {
                _ambientAcc += dt * _ambientRate;
                var size = _rt.rect.size;
                while (_ambientAcc >= 1f)
                {
                    _ambientAcc -= 1f;
                    EmitAmbient(size);
                }
            }

            for (int i = 0; i < _pool.Count; i++)
            {
                var p = _pool[i];
                if (!p.Alive) continue;
                p.Age += dt;
                if (p.Age >= p.Life)
                {
                    p.Alive = false;
                    p.Img.gameObject.SetActive(false);
                    _pool[i] = p;
                    continue;
                }
                p.Vel += new Vector2(0, p.Gravity) * dt;
                p.Vel *= 1f - Mathf.Clamp01(p.Drag * dt);
                p.Pos += p.Vel * dt;
                float t = p.Age / p.Life;
                float fade = t < 0.15f ? t / 0.15f : 1f - (t - 0.15f) / 0.85f;
                if (p.Twinkle > 0f) fade *= 0.65f + 0.35f * Mathf.Sin(p.Age * p.Twinkle + i);
                var c = p.Color;
                c.a *= Mathf.Clamp01(fade);
                p.Img.color = c;
                p.Img.rectTransform.anchoredPosition = p.Pos;
                _pool[i] = p;
            }
        }
    }
}
