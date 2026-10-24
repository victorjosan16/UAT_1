using System;
using UnityEngine;

namespace SugarDrop
{
    /// <summary>
    /// O piesă de pe tablă: capac colorat (cu pictograma aromei), capac special, biscuit sau capac înghețat.
    /// Are collider (bomboanele stau pe ea) și animații: mutare, dispariție, săritură, tremurat, „respirație”.
    /// </summary>
    public class Tile : MonoBehaviour
    {
        public int ColorIndex { get; private set; }
        public TileKind Kind { get; private set; }
        public bool Frozen { get; private set; }
        public int Hits { get; private set; }
        public Vector2Int Cell { get; set; }

        public bool IsBiscuit => Kind == TileKind.Biscuit;
        public bool IsRainbow => Kind == TileKind.Rainbow;
        public bool IsSpecial => Kind != TileKind.Normal && Kind != TileKind.Biscuit;
        public bool CanMove => !Frozen && !IsBiscuit;
        public bool CanMatch => !IsBiscuit && !IsRainbow && ColorIndex >= 0;

        private SpriteRenderer _shadow;
        private SpriteRenderer _base;
        private SpriteRenderer _icon;
        private SpriteRenderer _overlay;
        private SpriteRenderer _ice;
        private BoxCollider2D _collider;
        private Transform _visual;
        private float _size;

        private Vector3 _moveFrom, _moveTo;
        private float _moveT = -1f, _moveDuration;
        private float _popT = -1f, _popDuration;
        private float _bounceT = -1f;
        private float _wiggleT = -1f;
        private float _phase;
        private Action _onPopped;

        public void Init(float size)
        {
            _size = size;
            _phase = UnityEngine.Random.value * 10f;
            _visual = new GameObject("Visual").transform;
            _visual.SetParent(transform, false);

            _shadow = Layer("Shadow", SugarSprites.CapShadow, 9, size * 0.94f);
            _shadow.transform.localPosition = new Vector3(0f, -size * 0.06f, 0f);
            _shadow.color = new Color(0.35f, 0.1f, 0.25f, 0.25f);
            _base = Layer("Base", SugarSprites.Cap, 10, size * 0.94f);
            _icon = Layer("Icon", SugarSprites.Heart, 11, size * 0.5f);
            _overlay = Layer("Overlay", SugarSprites.Stripes, 12, size * 0.94f);
            _ice = Layer("Ice", SugarSprites.Ice, 13, size * 0.98f);

            // Colțuri rotunjite: bomboanele de pe marginea unui capac alunecă în gaura de lângă el, nu rămân agățate.
            _collider = gameObject.AddComponent<BoxCollider2D>();
            float radius = size * 0.2f;
            _collider.size = Vector2.one * (size - radius * 2f);
            _collider.edgeRadius = radius;
        }

        private SpriteRenderer Layer(string name, Sprite sprite, int order, float worldSize)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_visual, false);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sortingOrder = order;
            go.transform.localScale = Vector3.one * (worldSize / sprite.bounds.size.x);
            return renderer;
        }

        /// <summary>Configurează piesa (folosit la creare, la capace speciale și la amestecare).</summary>
        public void Setup(TileKind kind, int colorIndex, bool frozen = false, int hits = 0)
        {
            Kind = kind;
            ColorIndex = kind == TileKind.Rainbow || kind == TileKind.Biscuit ? -1 : colorIndex;
            Frozen = frozen && kind != TileKind.Biscuit;
            Hits = hits;
            _maxHitsShown = 0;
            RefreshVisual();
        }

        public void SetColor(int colorIndex)
        {
            if (Kind == TileKind.Rainbow || Kind == TileKind.Biscuit) return;
            ColorIndex = colorIndex;
            RefreshVisual();
        }

        public void Unfreeze()
        {
            Frozen = false;
            RefreshVisual();
            Bounce();
        }

        /// <summary>Lovește biscuitul; întoarce true dacă s-a spart de tot.</summary>
        public bool HitBiscuit()
        {
            Hits = Mathf.Max(0, Hits - 1);
            RefreshVisual();
            Wiggle();
            return Hits == 0;
        }

        private void RefreshVisual()
        {
            _ice.enabled = Frozen;
            _overlay.enabled = false;
            _overlay.transform.localRotation = Quaternion.identity;
            // Forma piesei arată deja aroma; pictograma rămâne doar pentru capacele vechi (nefolosită acum).
            _icon.enabled = false;
            _shadow.sprite = SugarSprites.CapShadow;

            switch (Kind)
            {
                case TileKind.Biscuit:
                    _base.sprite = SugarSprites.Biscuit;
                    _base.color = Color.white;
                    _icon.enabled = false;
                    // Biscuitul dublu e mai închis; după prima lovitură apare o crăpătură.
                    if (Hits >= 2) _base.color = new Color(0.85f, 0.75f, 0.7f);
                    _overlay.enabled = Hits == 1 && _maxHitsShown > 1;
                    _overlay.sprite = SugarSprites.BiscuitCrack;
                    _overlay.color = new Color(0.35f, 0.2f, 0.12f, 0.9f);
                    if (Hits > _maxHitsShown) _maxHitsShown = Hits;
                    return;

                case TileKind.Rainbow:
                    _base.sprite = PieceSprites.Pearl;
                    _base.color = Color.white;
                    _icon.enabled = false;
                    _overlay.enabled = true;
                    _overlay.sprite = SugarSprites.Rainbow;
                    _overlay.color = Color.white;
                    return;
            }

            // Bomboană de jeleu cu forma aromei (sprite deja colorat, cu luciu alb).
            _base.sprite = PieceSprites.Piece(ColorIndex);
            _base.color = Color.white;
            _shadow.sprite = PieceSprites.Shadow(ColorIndex);

            if (Kind == TileKind.StripedRow || Kind == TileKind.StripedColumn)
            {
                _overlay.enabled = true;
                _overlay.sprite = PieceSprites.Stripes(ColorIndex, Kind == TileKind.StripedRow);
                _overlay.color = Color.white;
            }
            else if (Kind == TileKind.Wrapped)
            {
                _overlay.enabled = true;
                _overlay.sprite = SugarSprites.WrapFrame;
                _overlay.color = new Color(1f, 1f, 1f, 0.9f);
            }
        }

        private int _maxHitsShown;

        public void MoveTo(Vector3 target, float duration)
        {
            _moveFrom = transform.localPosition;
            _moveTo = target;
            _moveDuration = Mathf.Max(0.01f, duration);
            _moveT = 0f;
        }

        /// <summary>Dispare cu un „pop”. Colliderul se oprește imediat, ca bomboanele să înceapă să cadă.</summary>
        public void Pop(float duration, Action onPopped)
        {
            _collider.enabled = false;
            _popDuration = Mathf.Max(0.01f, duration);
            _popT = 0f;
            _onPopped = onPopped;
        }

        public void Bounce() => _bounceT = 0f;

        /// <summary>Capacul atins cu degetul se ridică puțin (doar vizual).</summary>
        public void SetSelected(bool selected) => _selected = selected;

        private bool _selected;
        private float _lift;
        public void Wiggle() => _wiggleT = 0f;

        private void Update()
        {
            float dt = Time.deltaTime;

            if (_moveT >= 0f)
            {
                _moveT += dt / _moveDuration;
                float t = Mathf.Clamp01(_moveT);
                float eased = t * t * (3f - 2f * t);
                transform.localPosition = Vector3.LerpUnclamped(_moveFrom, _moveTo, eased);
                if (t >= 1f) _moveT = -1f;
            }

            float scale = 1f;
            float angle = 0f;

            if (_popT >= 0f)
            {
                _popT += dt / _popDuration;
                float t = Mathf.Clamp01(_popT);
                // „Pop” suculent: se strânge puțin, se umflă, apoi dispare.
                scale = t < 0.12f ? Mathf.Lerp(1f, 0.86f, t / 0.12f)
                    : t < 0.4f ? Mathf.Lerp(0.86f, 1.32f, (t - 0.12f) / 0.28f)
                    : Mathf.Lerp(1.32f, 0f, (t - 0.4f) / 0.6f);
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
                    _bounceT += dt / 0.3f;
                    float t = Mathf.Clamp01(_bounceT);
                    scale *= 1f + Mathf.Sin(t * Mathf.PI) * 0.18f;
                    if (t >= 1f) _bounceT = -1f;
                }
                if (_wiggleT >= 0f)
                {
                    _wiggleT += dt / 0.45f;
                    float t = Mathf.Clamp01(_wiggleT);
                    angle = Mathf.Sin(t * Mathf.PI * 6f) * 12f * (1f - t);
                    if (t >= 1f) _wiggleT = -1f;
                }
                // Capacele speciale „respiră” și curcubeul se rotește, ca să atragă privirea.
                if (IsSpecial) scale *= 1f + Mathf.Sin(Time.time * 4f + _phase) * 0.04f;
                if (IsRainbow) _overlay.transform.localRotation = Quaternion.Euler(0f, 0f, Time.time * 90f);
            }

            // Selecția: capacul crește ușor și umbra coboară, ca și cum ar fi ridicat de pe tablă.
            _lift = Mathf.MoveTowards(_lift, _selected && _popT < 0f ? 1f : 0f, dt * 9f);
            if (_lift > 0f)
            {
                scale *= 1f + 0.12f * _lift;
                _shadow.transform.localPosition = new Vector3(0f, -_size * (0.06f + 0.08f * _lift), 0f);
            }

            _visual.localScale = Vector3.one * scale;
            _visual.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}
