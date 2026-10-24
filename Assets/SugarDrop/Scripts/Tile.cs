using System;
using UnityEngine;

namespace SugarDrop
{
    /// <summary>
    /// Un capac colorat de pe tablă. Are collider (bomboanele stau pe el) și animații simple:
    /// mutare (la schimb), dispariție (la aliniere) și „săritură” (la amestecare).
    /// </summary>
    public class Tile : MonoBehaviour
    {
        public int ColorIndex { get; private set; }
        public Vector2Int Cell { get; set; }
        public bool IsAnimating => _moveT >= 0f || _popT >= 0f;

        private SpriteRenderer _base;
        private SpriteRenderer _icon;
        private BoxCollider2D _collider;
        private Vector3 _moveFrom;
        private Vector3 _moveTo;
        private float _moveT = -1f;
        private float _moveDuration;
        private float _popT = -1f;
        private float _popDuration;
        private float _bounceT = -1f;
        private Action _onPopped;

        public void Init(float size, int colorIndex, Sprite baseSprite, Sprite iconSprite)
        {
            _base = gameObject.AddComponent<SpriteRenderer>();
            _base.sprite = baseSprite;
            _base.drawMode = SpriteDrawMode.Sliced;
            _base.size = Vector2.one * (size * 0.94f);
            _base.sortingOrder = 10;

            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(transform, false);
            _icon = iconGo.AddComponent<SpriteRenderer>();
            _icon.sprite = iconSprite;
            _icon.sortingOrder = 11;
            float iconWorld = size * 0.52f;
            iconGo.transform.localScale = Vector3.one * (iconWorld / iconSprite.bounds.size.x);

            _collider = gameObject.AddComponent<BoxCollider2D>();
            _collider.size = Vector2.one * size;

            SetColor(colorIndex);
        }

        public void SetColor(int colorIndex)
        {
            ColorIndex = colorIndex;
            _base.color = Palette.TileLight(colorIndex);
            _icon.color = Palette.Tile(colorIndex);
        }

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

            if (_popT >= 0f)
            {
                _popT += dt / _popDuration;
                float t = Mathf.Clamp01(_popT);
                // Crește puțin, apoi se micșorează până dispare.
                float scale = t < 0.3f ? Mathf.Lerp(1f, 1.2f, t / 0.3f) : Mathf.Lerp(1.2f, 0f, (t - 0.3f) / 0.7f);
                transform.localScale = Vector3.one * scale;
                if (t >= 1f)
                {
                    _popT = -1f;
                    Action done = _onPopped;
                    _onPopped = null;
                    done?.Invoke();
                }
            }
            else if (_bounceT >= 0f)
            {
                _bounceT += dt / 0.3f;
                float t = Mathf.Clamp01(_bounceT);
                transform.localScale = Vector3.one * (1f + Mathf.Sin(t * Mathf.PI) * 0.15f);
                if (t >= 1f) _bounceT = -1f;
            }
        }
    }
}
