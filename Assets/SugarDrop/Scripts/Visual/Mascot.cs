using UnityEngine;

namespace SugarDrop
{
    /// <summary>
    /// „Bobo”, cupcake-ul care stă lângă borcan. Respiră, clipește, se uită spre ultima explozie,
    /// sare de bucurie la combo-uri, dansează la victorie și se întristează la înfrângere.
    /// Construit din sprite-uri procedurale (folie, glazură, cireașă, ochi, gură).
    /// </summary>
    public class Mascot : MonoBehaviour
    {
        private enum Mood { Idle, Happy, Sad }

        [SerializeField] private float size = 2f;

        private Transform _body;
        private Transform _pupilLeft, _pupilRight;
        private Transform _eyeLeft, _eyeRight;
        private SpriteRenderer _mouth;
        private Transform _cherry;
        private Mood _mood;
        private float _jumpT = -1f;
        private float _jumpHeight;
        private float _spin;
        private float _blinkTimer = 2f;
        private float _blinkT = -1f;
        private Vector3 _lookTarget;
        private float _lookTimer;

        private void Awake() => Build();

        private void Build()
        {
            _body = new GameObject("Body").transform;
            _body.SetParent(transform, false);

            Part("Shadow", SugarSprites.SoftDot, new Vector3(0f, -0.5f * size, 0f), new Vector2(1.2f, 0.3f) * size, new Color(0.3f, 0.1f, 0.25f, 0.25f), 1, transform);
            Part("Wrapper", SugarSprites.CupWrapper, new Vector3(0f, -0.12f * size, 0f), Vector2.one * size * 0.8f, Color.white, 2, _body);
            Part("Frosting", SugarSprites.Frosting, new Vector3(0f, 0.28f * size, 0f), Vector2.one * size * 0.85f, Color.white, 3, _body);
            _cherry = Part("Cherry", SugarSprites.Circle, new Vector3(0.05f * size, 0.68f * size, 0f), Vector2.one * size * 0.2f, new Color(0.93f, 0.2f, 0.32f), 4, _body).transform;
            Part("CherryShine", SugarSprites.Circle, new Vector3(-0.03f, 0.03f, 0f) * size, Vector2.one * size * 0.06f, new Color(1f, 1f, 1f, 0.8f), 5, _cherry);

            // Ochii stau pe folie, glazura e „pălăria”.
            _eyeLeft = Part("EyeL", SugarSprites.Circle, new Vector3(-0.17f * size, 0.02f * size, 0f), Vector2.one * size * 0.18f, Color.white, 5, _body).transform;
            _eyeRight = Part("EyeR", SugarSprites.Circle, new Vector3(0.17f * size, 0.02f * size, 0f), Vector2.one * size * 0.18f, Color.white, 5, _body).transform;
            _pupilLeft = Part("PupilL", SugarSprites.Circle, Vector3.zero, Vector2.one * size * 0.1f, new Color(0.25f, 0.12f, 0.2f), 6, _eyeLeft).transform;
            _pupilRight = Part("PupilR", SugarSprites.Circle, Vector3.zero, Vector2.one * size * 0.1f, new Color(0.25f, 0.12f, 0.2f), 6, _eyeRight).transform;
            Part("Glint", SugarSprites.Circle, new Vector3(0.025f, 0.025f, 0f) * size, Vector2.one * size * 0.035f, Color.white, 7, _pupilLeft);
            Part("Glint", SugarSprites.Circle, new Vector3(0.025f, 0.025f, 0f) * size, Vector2.one * size * 0.035f, Color.white, 7, _pupilRight);
            Part("CheekL", SugarSprites.SoftDot, new Vector3(-0.3f * size, -0.1f * size, 0f), Vector2.one * size * 0.16f, new Color(1f, 0.4f, 0.55f, 0.6f), 5, _body);
            Part("CheekR", SugarSprites.SoftDot, new Vector3(0.3f * size, -0.1f * size, 0f), Vector2.one * size * 0.16f, new Color(1f, 0.4f, 0.55f, 0.6f), 5, _body);
            _mouth = Part("Mouth", SugarSprites.Smile, new Vector3(0f, -0.12f * size, 0f), Vector2.one * size * 0.22f, new Color(0.35f, 0.12f, 0.22f), 6, _body);
        }

        private SpriteRenderer Part(string name, Sprite sprite, Vector3 localPosition, Vector2 worldSize, Color color, int order, Transform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            // Poziția și mărimea sunt în unități din lume, independent de scara părintelui.
            Vector3 parentScale = parent.lossyScale;
            go.transform.localPosition = new Vector3(localPosition.x / Mathf.Max(0.001f, parentScale.x),
                localPosition.y / Mathf.Max(0.001f, parentScale.y), 0f);
            Vector2 spriteSize = sprite.bounds.size;
            go.transform.localScale = new Vector3(worldSize.x / spriteSize.x / Mathf.Max(0.001f, parentScale.x),
                worldSize.y / spriteSize.y / Mathf.Max(0.001f, parentScale.y), 1f);
            var renderer = go.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.color = color;
            renderer.sortingOrder = 14 + order;
            return renderer;
        }

        // ------------------------------------------------------------ Reacții

        public void LookAt(Vector3 world)
        {
            _lookTarget = world;
            _lookTimer = 1.5f;
        }

        /// <summary>Săritură (mai mare la combo-uri mari).</summary>
        public void Cheer(float strength = 1f)
        {
            if (_mood == Mood.Sad) return;
            _jumpT = 0f;
            _jumpHeight = 0.25f * strength;
            _spin = strength >= 2f ? 360f : 0f;
        }

        public void SetHappy()
        {
            _mood = Mood.Happy;
            _mouth.sprite = SugarSprites.Smile;
        }

        public void SetSad()
        {
            _mood = Mood.Sad;
            _mouth.sprite = SugarSprites.Frown;
        }

        public void SetIdle()
        {
            _mood = Mood.Idle;
            _mouth.sprite = SugarSprites.Smile;
        }

        // ------------------------------------------------------------ Animație

        private void Update()
        {
            float dt = Time.deltaTime;
            float time = Time.time;

            // Respirație (squash & stretch) și dans la victorie.
            float breathe = Mathf.Sin(time * 2.4f) * 0.035f;
            float y = 0f, angle = 0f;
            Vector3 scale = new(1f + breathe, 1f - breathe, 1f);

            if (_mood == Mood.Happy)
            {
                y = Mathf.Abs(Mathf.Sin(time * 7f)) * 0.3f;
                angle = Mathf.Sin(time * 3.5f) * 10f;
            }
            else if (_mood == Mood.Sad)
            {
                y = -0.08f;
                scale = new Vector3(1.06f, 0.9f, 1f);
                angle = Mathf.Sin(time * 1.2f) * 3f;
            }

            if (_jumpT >= 0f)
            {
                _jumpT += dt / 0.45f;
                float t = Mathf.Clamp01(_jumpT);
                y += Mathf.Sin(t * Mathf.PI) * _jumpHeight * 2f;
                float squash = t < 0.15f ? 1f - t / 0.15f * 0.2f : t > 0.85f ? 0.8f + (1f - t) / 0.15f * 0.2f : 1f;
                scale.y *= squash < 1f ? squash : 1f + Mathf.Sin(t * Mathf.PI) * 0.12f;
                angle += _spin * EaseInOut(t);
                if (t >= 1f) _jumpT = -1f;
            }

            _body.localPosition = new Vector3(0f, y, 0f);
            _body.localRotation = Quaternion.Euler(0f, 0f, angle);
            _body.localScale = scale;
            _cherry.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(time * 5f) * 8f);

            // Clipit din când în când.
            _blinkTimer -= dt;
            if (_blinkTimer <= 0f)
            {
                _blinkT = 0f;
                _blinkTimer = Random.Range(2.5f, 5f);
            }
            float eyeY = 1f;
            if (_blinkT >= 0f)
            {
                _blinkT += dt / 0.16f;
                eyeY = Mathf.Abs(1f - Mathf.Clamp01(_blinkT) * 2f);
                if (_blinkT >= 1f) _blinkT = -1f;
            }
            if (_mood == Mood.Happy) eyeY = Mathf.Min(eyeY, 0.45f); // ochi „fericiți”
            SetEyeHeight(_eyeLeft, eyeY);
            SetEyeHeight(_eyeRight, eyeY);

            // Pupilele urmăresc ultima explozie, apoi revin.
            _lookTimer -= dt;
            Vector3 look = Vector3.zero;
            if (_lookTimer > 0f)
            {
                Vector3 delta = _lookTarget - _eyeLeft.position;
                look = Vector3.ClampMagnitude(delta, 1f) * 0.035f * size;
            }
            else if (_mood == Mood.Sad) look = new Vector3(0f, -0.03f * size, 0f);
            Vector3 eyeScale = _eyeLeft.lossyScale;
            Vector3 local = new(look.x / Mathf.Max(0.001f, eyeScale.x), look.y / Mathf.Max(0.001f, eyeScale.y), 0f);
            _pupilLeft.localPosition = Vector3.Lerp(_pupilLeft.localPosition, local, dt * 10f);
            _pupilRight.localPosition = Vector3.Lerp(_pupilRight.localPosition, local, dt * 10f);
        }

        private float _eyeBaseScale = -1f;

        private void SetEyeHeight(Transform eye, float height)
        {
            if (_eyeBaseScale < 0f) _eyeBaseScale = eye.localScale.y;
            Vector3 s = eye.localScale;
            s.y = _eyeBaseScale * Mathf.Max(0.08f, height);
            eye.localScale = s;
        }

        private static float EaseInOut(float t) => t * t * (3f - 2f * t);
    }
}
