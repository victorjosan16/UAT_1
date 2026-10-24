using UnityEngine;
using UnityEngine.UI;

namespace SugarDrop
{
    public enum LevelNodeState { Locked, Current, Completed }

    /// <summary>
    /// Animațiile unui nod de pe hartă: nivelul curent plutește, pulsează și are un halou auriu; nivelurile
    /// încuiate tremură când le atingi; la deblocare lacătul sare, nodul face „boing” și devine auriu.
    /// Update iese imediat pentru nodurile care nu au nimic de animat (majoritatea).
    /// </summary>
    public class LevelNodeAnimator : MonoBehaviour
    {
        private RectTransform _visual;
        private Image _glow;
        private RectTransform _lock;
        private Image _lockImage;
        private float _phase;
        private LevelNodeState _state;
        private float _wiggleT = -1f;
        private float _unlockT = -1f;
        private float _popT = -100f; // < -10 = fără animație de apariție

        public LevelNodeState State => _state;

        public void Bind(RectTransform visual, Image glow, RectTransform lockIcon)
        {
            _visual = visual;
            _glow = glow;
            _lock = lockIcon;
            _lockImage = lockIcon != null ? lockIcon.GetComponent<Image>() : null;
            _phase = Random.value * 6f;
        }

        public void SetState(LevelNodeState state)
        {
            _state = state;
            if (_glow != null) _glow.gameObject.SetActive(state == LevelNodeState.Current);
            if (state != LevelNodeState.Current && _unlockT < 0f && _wiggleT < 0f && _popT < 0f) ResetVisual();
        }

        /// <summary>Tremurat scurt „nu se poate” (nivel încuiat).</summary>
        public void Wiggle() => _wiggleT = 0f;

        /// <summary>Animația de deblocare (lacătul sare și dispare, nodul crește și revine).</summary>
        public void PlayUnlock()
        {
            _unlockT = 0f;
            if (_lock != null)
            {
                _lock.gameObject.SetActive(true);
                _lock.localScale = Vector3.one;
            }
        }

        /// <summary>Apariție cu „pop” (după un mic decalaj).</summary>
        public void PopIn(float delay) => _popT = -delay;

        private void ResetVisual()
        {
            if (_visual == null) return;
            _visual.localScale = Vector3.one;
            _visual.anchoredPosition = Vector2.zero;
            _visual.localRotation = Quaternion.identity;
        }

        private void Update()
        {
            bool active = _state == LevelNodeState.Current || _wiggleT >= 0f || _unlockT >= 0f || _popT > -10f && _popT < 1f;
            if (!active || _visual == null) return;

            float dt = Time.unscaledDeltaTime;
            float time = Time.unscaledTime;
            float scale = 1f, y = 0f, angle = 0f;

            if (_state == LevelNodeState.Current)
            {
                // Plutire ușoară + puls; haloul respiră.
                y = Mathf.Sin(time * 2.2f + _phase) * 9f;
                scale = 1f + Mathf.Sin(time * 3.4f) * 0.045f;
                if (_glow != null)
                {
                    float g = 0.55f + Mathf.Sin(time * 3.4f) * 0.2f;
                    Color c = _glow.color;
                    c.a = g;
                    _glow.color = c;
                    _glow.rectTransform.localScale = Vector3.one * (1f + Mathf.Sin(time * 3.4f) * 0.08f);
                }
            }

            if (_popT > -10f && _popT < 1f)
            {
                _popT += dt / 0.35f;
                if (_popT < 0f) scale = 0f;
                else scale *= UiEase.OutBack(Mathf.Clamp01(_popT), 2.2f);
                if (_popT >= 1f) _popT = -100f;
            }

            if (_wiggleT >= 0f)
            {
                _wiggleT += dt / 0.4f;
                float t = Mathf.Clamp01(_wiggleT);
                angle = Mathf.Sin(t * Mathf.PI * 6f) * 10f * (1f - t);
                if (t >= 1f) _wiggleT = -1f;
            }

            if (_unlockT >= 0f)
            {
                _unlockT += dt / 0.9f;
                float t = Mathf.Clamp01(_unlockT);
                // Nodul: apăsare scurtă, apoi săritură elastică.
                scale *= t < 0.2f ? 1f - t / 0.2f * 0.15f : 0.85f + 0.15f * UiEase.OutElastic((t - 0.2f) / 0.8f) + 0.12f * Mathf.Sin((t - 0.2f) / 0.8f * Mathf.PI);
                if (_lock != null)
                {
                    float lt = Mathf.Clamp01(t / 0.45f);
                    _lock.localScale = Vector3.one * (1f + lt * 0.8f);
                    _lock.anchoredPosition = new Vector2(0f, lt * 60f);
                    _lock.localRotation = Quaternion.Euler(0f, 0f, lt * 35f);
                    if (_lockImage != null) _lockImage.color = new Color(1f, 1f, 1f, 1f - lt);
                    if (lt >= 1f) _lock.gameObject.SetActive(false);
                }
                if (t >= 1f)
                {
                    _unlockT = -1f;
                    if (_lock != null)
                    {
                        _lock.anchoredPosition = Vector2.zero;
                        _lock.localRotation = Quaternion.identity;
                        _lock.localScale = Vector3.one;
                        if (_lockImage != null) _lockImage.color = Color.white;
                    }
                }
            }

            _visual.localScale = Vector3.one * scale;
            _visual.anchoredPosition = new Vector2(0f, y);
            _visual.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}
