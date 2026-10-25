using System;
using UnityEngine;

namespace SugarDrop
{
    /// <summary>
    /// Aspectul unei piese de pe tablă: bomboană de jeleu (cu forma aromei), capac special, bomboană-ingredient,
    /// biscuit, ciocolată sau piesă înghețată. Regulile sunt în Match3Engine; aici sunt doar animațiile:
    /// mutare, cădere cu aterizare, apariție, dispariție, săritură, tremurat, „respirație” și ridicarea sub deget.
    /// </summary>
    public class Tile : MonoBehaviour
    {
        public int Id { get; set; }
        public int ColorIndex { get; private set; }
        public TileKind Kind { get; private set; }
        public bool Frozen { get; private set; }
        public Vector2Int Cell { get; set; }

        public bool IsRainbow => Kind == TileKind.Rainbow;
        public bool IsSpecial => Kind == TileKind.StripedRow || Kind == TileKind.StripedColumn || Kind == TileKind.Wrapped || Kind == TileKind.Rainbow;

        private SpriteRenderer _shadow;
        private SpriteRenderer _base;
        private SpriteRenderer _overlay;
        private SpriteRenderer _ice;
        private SpriteRenderer[] _renderers;
        private Transform _visual;
        private float _size;

        private Vector3 _moveFrom, _moveTo;
        private float _moveT = -1f, _moveDuration;
        private bool _falling;
        private float _popT = -1f, _popDuration;
        private float _bounceT = -1f;
        private float _bounceStrength = 0.18f;
        private float _wiggleT = -1f;
        private float _fadeT = -1f, _fadeDuration;
        private float _phase;
        private bool _selected;
        private float _lift;
        private Action _onPopped;

        public void Init(float size)
        {
            _size = size;
            _phase = UnityEngine.Random.value * 10f;
            _visual = new GameObject("Visual").transform;
            _visual.SetParent(transform, false);

            _shadow = Layer("Shadow", SugarSprites.CapShadow, 9, size * 0.94f);
            _shadow.transform.localPosition = new Vector3(0f, -size * 0.06f, 0f);
            _shadow.color = new Color(0.25f, 0.06f, 0.2f, 0.3f);
            _base = Layer("Base", SugarSprites.Cap, 10, size * 0.94f);
            _overlay = Layer("Overlay", SugarSprites.Stripes, 12, size * 0.94f);
            _ice = Layer("Ice", SugarSprites.Ice, 13, size * 0.98f);
            _renderers = new[] { _shadow, _base, _overlay, _ice };
        }

        private SpriteRenderer Layer(string name, Sprite sprite, int order, float worldSize)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_visual, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            // Vizibilă doar în interiorul tablei (piesele noi intră de sus, bomboanele ies jos).
            renderer.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            go.transform.localScale = Vector3.one * (worldSize / sprite.bounds.size.x);
            return renderer;
        }

        /// <summary>Configurează piesa (la creare, la capace speciale noi, la amestecare, la ciocolata care crește).</summary>
        public void Setup(TileKind kind, int colorIndex, bool frozen = false)
        {
            Kind = kind;
            ColorIndex = colorIndex;
            Frozen = frozen;
            RefreshVisual();
        }

        public void SetColor(int colorIndex)
        {
            ColorIndex = colorIndex;
            RefreshVisual();
        }

        public void Unfreeze()
        {
            Frozen = false;
            RefreshVisual();
            Bounce();
        }

        /// <summary>Biscuitul a fost lovit dar nu s-a spart: apare o crăpătură.</summary>
        public void ShowCrack()
        {
            _overlay.enabled = true;
            _overlay.sprite = SugarSprites.BiscuitCrack;
            _overlay.color = new Color(0.35f, 0.2f, 0.12f, 0.9f);
            Wiggle();
        }

        private void RefreshVisual()
        {
            _ice.enabled = Frozen;
            _overlay.enabled = false;
            _overlay.transform.localRotation = Quaternion.identity;
            _shadow.sprite = SugarSprites.CapShadow;
            _base.color = Color.white;

            switch (Kind)
            {
                case TileKind.Biscuit:
                    _base.sprite = SugarSprites.Biscuit;
                    return;

                case TileKind.Chocolate:
                    _base.sprite = SugarSprites.Chocolate;
                    return;

                case TileKind.Ingredient:
                    // Bomboana care trebuie dusă în borcan: mare, ambalată, în culoarea ei.
                    _base.sprite = SugarSprites.CandyWrapped;
                    _base.color = Palette.Candy(ColorIndex);
                    _shadow.sprite = PieceSprites.Shadow(-1);
                    _overlay.enabled = true;
                    _overlay.sprite = SugarSprites.Sparkle;
                    _overlay.color = new Color(1f, 1f, 1f, 0.8f);
                    return;

                case TileKind.Rainbow:
                    _base.sprite = PieceSprites.Pearl;
                    _shadow.sprite = PieceSprites.Shadow(-1);
                    _overlay.enabled = true;
                    _overlay.sprite = SugarSprites.Rainbow;
                    _overlay.color = Color.white;
                    return;
            }

            // Bomboană de jeleu cu forma aromei (sprite deja colorat, cu luciu alb).
            int color = Mathf.Max(0, ColorIndex);
            _base.sprite = PieceSprites.Piece(color);
            _shadow.sprite = PieceSprites.Shadow(color);
            if (Kind == TileKind.StripedRow || Kind == TileKind.StripedColumn)
            {
                _overlay.enabled = true;
                _overlay.sprite = PieceSprites.Stripes(color, Kind == TileKind.StripedRow);
                _overlay.color = Color.white;
            }
            else if (Kind == TileKind.Wrapped)
            {
                _overlay.enabled = true;
                _overlay.sprite = SugarSprites.WrapFrame;
                _overlay.color = new Color(1f, 1f, 1f, 0.9f);
            }
        }

        // ------------------------------------------------------------ Animații

        /// <summary>Mută piesa. fall = cădere (accelerează, apoi aterizează cu o mică săritură).</summary>
        public void MoveTo(Vector3 target, float duration, bool fall = false)
        {
            _moveFrom = transform.localPosition;
            _moveTo = target;
            _moveDuration = Mathf.Max(0.01f, duration);
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

        /// <summary>Dispare cu un „pop”.</summary>
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

        private void SetAlpha(float alpha)
        {
            foreach (SpriteRenderer renderer in _renderers)
            {
                Color c = renderer.color;
                c.a = renderer == _shadow ? 0.3f * alpha : renderer == _overlay && Kind == TileKind.Ingredient ? 0.8f * alpha : alpha;
                renderer.color = c;
            }
        }

        private void Update()
        {
            float dt = Time.deltaTime;

            if (_moveT >= 0f)
            {
                _moveT += dt / _moveDuration;
                float t = Mathf.Clamp01(_moveT);
                float eased = _falling ? t * t : t * t * (3f - 2f * t);
                transform.localPosition = Vector3.LerpUnclamped(_moveFrom, _moveTo, eased);
                if (t >= 1f)
                {
                    _moveT = -1f;
                    if (_falling) Bounce(0.1f); // aterizare
                }
            }

            if (_fadeT >= 0f)
            {
                _fadeT += dt / _fadeDuration;
                SetAlpha(Mathf.Clamp01(_fadeT));
                if (_fadeT >= 1f) _fadeT = -1f;
            }

            float scaleX = 1f, scaleY = 1f;
            float angle = 0f;

            if (_popT >= 0f)
            {
                _popT += dt / _popDuration;
                float t = Mathf.Clamp01(_popT);
                // „Pop” suculent: se strânge puțin, se umflă, apoi dispare.
                float scale = t < 0.12f ? Mathf.Lerp(1f, 0.86f, t / 0.12f)
                    : t < 0.4f ? Mathf.Lerp(0.86f, 1.32f, (t - 0.12f) / 0.28f)
                    : Mathf.Lerp(1.32f, 0f, (t - 0.4f) / 0.6f);
                scaleX = scaleY = scale;
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
                    _bounceT += dt / 0.28f;
                    float t = Mathf.Clamp01(_bounceT);
                    // Turtire la aterizare: mai lată și mai joasă, apoi revine.
                    float squash = Mathf.Sin(t * Mathf.PI) * _bounceStrength;
                    scaleX *= 1f + squash;
                    scaleY *= 1f - squash * 0.6f;
                    if (t >= 1f) _bounceT = -1f;
                }
                if (_wiggleT >= 0f)
                {
                    _wiggleT += dt / 0.45f;
                    float t = Mathf.Clamp01(_wiggleT);
                    angle = Mathf.Sin(t * Mathf.PI * 6f) * 12f * (1f - t);
                    if (t >= 1f) _wiggleT = -1f;
                }
                // Capacele speciale „respiră”, curcubeul se rotește, bomboana-ingredient se legănă.
                if (IsSpecial)
                {
                    float breathe = 1f + Mathf.Sin(Time.time * 4f + _phase) * 0.04f;
                    scaleX *= breathe;
                    scaleY *= breathe;
                }
                if (IsRainbow) _overlay.transform.localRotation = Quaternion.Euler(0f, 0f, Time.time * 90f);
                if (Kind == TileKind.Ingredient) angle += Mathf.Sin(Time.time * 2.5f + _phase) * 6f;
            }

            _lift = Mathf.MoveTowards(_lift, _selected && _popT < 0f ? 1f : 0f, dt * 9f);
            if (_lift > 0f)
            {
                scaleX *= 1f + 0.12f * _lift;
                scaleY *= 1f + 0.12f * _lift;
                _shadow.transform.localPosition = new Vector3(0f, -_size * (0.06f + 0.08f * _lift), 0f);
            }

            _visual.localScale = new Vector3(scaleX, scaleY, 1f);
            _visual.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}
