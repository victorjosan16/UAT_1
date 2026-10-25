using System;
using UnityEngine;

namespace SugarDrop
{
    /// <summary>
    /// Aspectul unei piese de pe tablă: bomboană de jeleu (cu forma aromei), rachetă, bombă, curcubeu,
    /// bomboană-ingredient, ladă, ciocolată, bomboană în gheață (1–3 straturi) sau legată cu lanț.
    /// Regulile sunt în BlastEngine; aici sunt doar animațiile, cu senzație de „jeleu”: cădere cu accelerație și
    /// întindere, aterizare cu turtire și revenire elastică, apariție, dispariție, tremurat, strălucire sub deget.
    /// </summary>
    public class Tile : MonoBehaviour
    {
        public int Id { get; set; }
        public int ColorIndex { get; private set; }
        public TileKind Kind { get; private set; }
        public int Ice { get; private set; }
        public bool Chained { get; private set; }
        public Vector2Int Cell { get; set; }

        public bool IsRainbow => Kind == TileKind.Rainbow;
        public bool IsSpecial => BlastRules.IsSpecial(Kind);

        private SpriteRenderer _glow;
        private SpriteRenderer _shadow;
        private SpriteRenderer _base;
        private SpriteRenderer _overlay;
        private SpriteRenderer _ice;
        private SpriteRenderer _chain;
        private SpriteRenderer[] _renderers;
        private Transform _visual;
        private float _size;
        private bool _masked;

        private Vector3 _moveFrom, _moveTo;
        private float _moveT = -1f, _moveDuration, _moveDelay;
        private bool _falling;
        private float _popT = -1f, _popDuration;
        private float _bounceT = -1f;
        private float _bounceStrength = 0.18f;
        private float _wiggleT = -1f;
        private float _fadeT = -1f, _fadeDuration;
        private float _phase;
        private bool _selected;
        private float _lift;
        private bool _highlighted;
        private float _glowLevel;
        private Action _onPopped;

        /// <param name="masked">Piesa se vede doar în interiorul măștii tablei (dacă masca există în build).</param>
        public void Init(float size, bool masked = true)
        {
            _size = size;
            _masked = masked;
            _phase = UnityEngine.Random.value * 10f;
            _visual = new GameObject("Visual").transform;
            _visual.SetParent(transform, false);

            _glow = Layer("Glow", SugarSprites.SoftDot, 8, size * 1.5f);
            _glow.color = new Color(1f, 1f, 0.85f, 0f);
            _shadow = Layer("Shadow", SugarSprites.CapShadow, 9, size * 0.94f);
            _shadow.transform.localPosition = new Vector3(0f, -size * 0.06f, 0f);
            _shadow.color = new Color(0.25f, 0.06f, 0.2f, 0.3f);
            _base = Layer("Base", SugarSprites.Cap, 10, size * 0.94f);
            _overlay = Layer("Overlay", SugarSprites.Stripes, 12, size * 0.94f);
            _ice = Layer("Ice", SugarSprites.Ice, 13, size * 0.98f);
            _chain = Layer("Chain", SugarSprites.Chain, 14, size * 0.98f);
            _renderers = new[] { _shadow, _base, _overlay, _ice, _chain };
        }

        private SpriteRenderer Layer(string name, Sprite sprite, int order, float worldSize)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_visual, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            // Vizibilă doar în interiorul tablei (piesele noi intră de sus, bomboanele ies jos).
            renderer.maskInteraction = _masked ? SpriteMaskInteraction.VisibleInsideMask : SpriteMaskInteraction.None;
            go.transform.localScale = Vector3.one * (worldSize / sprite.bounds.size.x);
            return renderer;
        }

        /// <summary>Configurează piesa după starea din motor (la creare și după fiecare schimbare).</summary>
        public void Setup(PieceInfo piece, bool cracked = false)
        {
            Kind = piece.Kind;
            ColorIndex = piece.Color;
            Ice = piece.Ice;
            Chained = piece.Chained;
            RefreshVisual(cracked);
        }

        public void SetColor(int colorIndex)
        {
            ColorIndex = colorIndex;
            RefreshVisual(false);
        }

        private void RefreshVisual(bool cracked)
        {
            // Gheața e mai groasă cu cât are mai multe straturi.
            _ice.enabled = Ice > 0;
            _ice.color = new Color(1f, 1f, 1f, Ice >= 3 ? 1f : Ice == 2 ? 0.85f : 0.65f);
            _chain.enabled = Chained;
            _overlay.enabled = false;
            _overlay.transform.localRotation = Quaternion.identity;
            _base.transform.localRotation = Quaternion.identity;
            _shadow.sprite = SugarSprites.CapShadow;
            _base.color = Color.white;

            switch (Kind)
            {
                case TileKind.Crate:
                    _base.sprite = SugarSprites.Crate;
                    if (cracked) ShowOverlay(SugarSprites.BiscuitCrack, new Color(0.3f, 0.17f, 0.08f, 0.9f));
                    return;

                case TileKind.Chocolate:
                    _base.sprite = SugarSprites.Chocolate;
                    return;

                case TileKind.Ingredient:
                    // Bomboana care trebuie dusă în borcan: mare, ambalată, în culoarea ei, cu o sclipire.
                    _base.sprite = SugarSprites.CandyWrapped;
                    _base.color = Palette.Candy(ColorIndex);
                    _shadow.sprite = PieceSprites.Shadow(-1);
                    ShowOverlay(SugarSprites.Sparkle, new Color(1f, 1f, 1f, 0.8f));
                    return;

                case TileKind.Rainbow:
                    _base.sprite = PieceSprites.Pearl;
                    _shadow.sprite = PieceSprites.Shadow(-1);
                    ShowOverlay(SugarSprites.Rainbow, Color.white);
                    return;

                case TileKind.RocketH:
                case TileKind.RocketV:
                    _base.sprite = SugarSprites.Rocket;
                    _shadow.sprite = PieceSprites.Shadow(-1);
                    if (Kind == TileKind.RocketV) _base.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                    return;

                case TileKind.Bomb:
                    _base.sprite = SugarSprites.Bomb;
                    _shadow.sprite = PieceSprites.Shadow(-1);
                    return;
            }

            // Bomboană de jeleu cu forma aromei (sprite deja colorat, cu luciu alb).
            int color = Mathf.Max(0, ColorIndex);
            _base.sprite = PieceSprites.Piece(color);
            _shadow.sprite = PieceSprites.Shadow(color);
        }

        private void ShowOverlay(Sprite sprite, Color color)
        {
            _overlay.enabled = true;
            _overlay.sprite = sprite;
            _overlay.color = color;
        }

        // ------------------------------------------------------------ Animații

        /// <summary>Mută piesa. fall = cădere (accelerează și se întinde, apoi aterizează cu turtire elastică).</summary>
        public void MoveTo(Vector3 target, float duration, bool fall = false, float delay = 0f)
        {
            _moveFrom = transform.localPosition;
            _moveTo = target;
            _moveDuration = Mathf.Max(0.01f, duration);
            _moveDelay = delay;
            _moveT = 0f;
            _falling = fall;
        }

        /// <summary>Piesă nouă: apare treptat în timp ce intră pe tablă.</summary>
        public void FadeIn(float duration)
        {
            _fadeDuration = Mathf.Max(0.01f, duration);
            _fadeT = 0f;
            SetAlpha(0f);
        }

        /// <summary>Dispare cu un „pop” de jeleu.</summary>
        public void Pop(float duration, Action onPopped)
        {
            _popDuration = Mathf.Max(0.01f, duration);
            _popT = 0f;
            _onPopped = onPopped;
        }

        public void Bounce(float strength = 0.18f)
        {
            _bounceStrength = strength;
            _bounceT = 0f;
        }

        public void Wiggle() => _wiggleT = 0f;

        /// <summary>Piesa atinsă cu degetul se ridică puțin (doar vizual).</summary>
        public void SetSelected(bool selected) => _selected = selected;

        /// <summary>Strălucire caldă (grupul care va dispărea, sau piesele speciale care se pot combina).</summary>
        public void SetHighlighted(bool highlighted) => _highlighted = highlighted;

        private void SetAlpha(float alpha)
        {
            foreach (SpriteRenderer renderer in _renderers)
            {
                Color c = renderer.color;
                c.a = renderer == _shadow ? 0.3f * alpha
                    : renderer == _ice ? (Ice >= 3 ? 1f : Ice == 2 ? 0.85f : 0.65f) * alpha
                    : renderer == _overlay && Kind == TileKind.Ingredient ? 0.8f * alpha
                    : alpha;
                renderer.color = c;
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;
            float stretch = 0f;

            if (_moveT >= 0f)
            {
                if (_moveDelay > 0f) _moveDelay -= dt;
                else
                {
                    _moveT += dt / _moveDuration;
                    float t = Mathf.Clamp01(_moveT);
                    float eased = _falling ? t * t : t * t * (3f - 2f * t);
                    transform.localPosition = Vector3.LerpUnclamped(_moveFrom, _moveTo, eased);
                    // Bomboana se întinde puțin pe măsură ce prinde viteză.
                    if (_falling) stretch = t * 0.1f;
                    if (t >= 1f)
                    {
                        _moveT = -1f;
                        if (_falling) Bounce(0.14f); // aterizare: turtire + revenire elastică
                    }
                }
            }

            if (_fadeT >= 0f)
            {
                _fadeT += dt / _fadeDuration;
                SetAlpha(Mathf.Clamp01(_fadeT));
                if (_fadeT >= 1f) _fadeT = -1f;
            }

            float scaleX = 1f - stretch * 0.5f, scaleY = 1f + stretch;
            float angle = 0f;

            if (_popT >= 0f)
            {
                _popT += dt / _popDuration;
                float t = Mathf.Clamp01(_popT);
                // „Pop” de jeleu: se strânge, se umflă inegal (mai lat decât înalt), apoi dispare.
                float scale = t < 0.12f ? Mathf.Lerp(1f, 0.84f, t / 0.12f)
                    : t < 0.4f ? Mathf.Lerp(0.84f, 1.32f, (t - 0.12f) / 0.28f)
                    : Mathf.Lerp(1.32f, 0f, (t - 0.4f) / 0.6f);
                float wobble = Mathf.Sin(t * Mathf.PI * 3f) * 0.08f * (1f - t);
                scaleX = scale * (1f + wobble);
                scaleY = scale * (1f - wobble);
                angle = t * 25f;
                if (t >= 1f)
                {
                    _popT = -1f;
                    Action done = _onPopped;
                    _onPopped = null;
                    done?.Invoke();
                }
            }
            else
            {
                if (_bounceT >= 0f)
                {
                    _bounceT += dt / 0.32f;
                    float t = Mathf.Clamp01(_bounceT);
                    // Turtire la aterizare, apoi o mică revenire elastică (amortizată, stabilă repede).
                    float squash = Mathf.Sin(t * Mathf.PI * 2f) * Mathf.Exp(-t * 4f) * _bounceStrength;
                    scaleX *= 1f + squash;
                    scaleY *= 1f - squash * 0.7f;
                    if (t >= 1f) _bounceT = -1f;
                }
                if (_wiggleT >= 0f)
                {
                    _wiggleT += dt / 0.4f;
                    float t = Mathf.Clamp01(_wiggleT);
                    angle = Mathf.Sin(t * Mathf.PI * 6f) * 12f * (1f - t);
                    float bump = Mathf.Sin(t * Mathf.PI) * 0.08f;
                    scaleX *= 1f + bump;
                    scaleY *= 1f + bump;
                    if (t >= 1f) _wiggleT = -1f;
                }
                // Piesele speciale „respiră”, curcubeul se rotește, bomboana-ingredient se legănă.
                if (IsSpecial)
                {
                    float breathe = 1f + Mathf.Sin(Time.time * 4f + _phase) * 0.04f;
                    scaleX *= breathe;
                    scaleY *= breathe;
                }
                if (IsRainbow) _overlay.transform.localRotation = Quaternion.Euler(0f, 0f, Time.time * 90f);
                if (Kind == TileKind.Ingredient) angle += Mathf.Sin(Time.time * 2.5f + _phase) * 6f;
            }

            // Sub deget: se ridică imediat (răspuns rapid) și se turtește puțin, ca un jeleu apăsat.
            _lift = Mathf.MoveTowards(_lift, _selected && _popT < 0f ? 1f : 0f, dt * 12f);
            if (_lift > 0f)
            {
                scaleX *= 1f + 0.14f * _lift;
                scaleY *= 1f + 0.08f * _lift;
                _shadow.transform.localPosition = new Vector3(0f, -_size * (0.06f + 0.08f * _lift), 0f);
            }

            _glowLevel = Mathf.MoveTowards(_glowLevel, _highlighted && _popT < 0f ? 1f : 0f, dt * 8f);
            if (_glowLevel > 0f || _glow.color.a > 0f)
            {
                Color g = _glow.color;
                g.a = _glowLevel * (0.55f + Mathf.Sin(Time.time * 8f) * 0.15f);
                _glow.color = g;
            }

            _visual.localScale = new Vector3(scaleX, scaleY, 1f);
            _visual.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}
