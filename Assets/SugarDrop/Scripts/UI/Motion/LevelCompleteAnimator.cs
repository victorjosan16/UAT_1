using QuarryEmpire;
using UnityEngine;
using UnityEngine.UI;

namespace SugarDrop
{
    /// <summary>
    /// Finalul de nivel: cutia coboară cu o săritură, stelele câștigate „pocnesc” pe rând cu câte o explozie
    /// de sclipiri, iar monedele se numără de la 0. La înfrângere, cutia cade și se scutură scurt.
    /// </summary>
    public class LevelCompleteAnimator : MonoBehaviour
    {
        private const float BoxDuration = 0.45f;
        private const float StarStart = 0.45f;
        private const float StarStep = 0.32f;

        private RectTransform _box;
        private Image[] _stars;
        private UiLabel _coins;
        private UIParticleController _particles;
        private Color _starOn, _starOff;
        private string _coinsFormat;

        private float _time = -1f;
        private bool _win;
        private int _starCount;
        private int _coinTarget;
        private int _shownCoins = -1;
        private int _starsShown;
        private static readonly Color[] SparkleColors =
            { Color.white, new(1f, 0.85f, 0.35f), new(1f, 0.6f, 0.75f) };

        public void Bind(RectTransform box, Image[] stars, UiLabel coins, UIParticleController particles, Color starOn, Color starOff)
        {
            _box = box;
            _stars = stars;
            _coins = coins;
            _particles = particles;
            _starOn = starOn;
            _starOff = starOff;
        }

        public void PlayWin(int stars, int coins, string coinsFormat)
        {
            _win = true;
            _starCount = stars;
            _coinTarget = coins;
            _coinsFormat = coinsFormat;
            _shownCoins = -1;
            _starsShown = 0;
            foreach (Image star in _stars)
            {
                star.color = _starOff;
                star.rectTransform.localScale = Vector3.one;
            }
            _time = 0f;
            Step(0f);
        }

        public void PlayLose()
        {
            _win = false;
            _time = 0f;
            Step(0f);
        }

        public void Stop()
        {
            _time = -1f;
            if (_box != null)
            {
                _box.anchoredPosition = Vector2.zero;
                _box.localRotation = Quaternion.identity;
            }
        }

        private void Update()
        {
            if (_time < 0f) return;
            _time += Time.unscaledDeltaTime;
            Step(_time);
        }

        private void Step(float time)
        {
            // Cutia: coboară de sus și se așază cu depășire (victorie) sau cade și se scutură (înfrângere).
            float bt = Mathf.Clamp01(time / BoxDuration);
            float y = (1f - UiEase.OutBack(bt, 1.4f)) * 420f;
            float angle = 0f;
            if (!_win && bt >= 1f)
            {
                float st = Mathf.Clamp01((time - BoxDuration) / 0.35f);
                angle = Mathf.Sin(st * Mathf.PI * 5f) * 3f * (1f - st);
            }
            _box.anchoredPosition = new Vector2(0f, y);
            _box.localRotation = Quaternion.Euler(0f, 0f, angle);

            if (!_win)
            {
                if (time > BoxDuration + 0.4f) _time = -1f;
                return;
            }

            for (int i = 0; i < _starCount && i < _stars.Length; i++)
            {
                float start = StarStart + i * StarStep;
                if (time < start) continue;
                if (_starsShown <= i)
                {
                    _starsShown = i + 1;
                    _stars[i].color = _starOn;
                    if (_particles != null) _particles.BurstAt(_stars[i].rectTransform, 12, SugarSprites.Sparkle, SparkleColors, 620f, 44f);
                    if (SugarAudio.Instance != null) SugarAudio.Instance.Play(SugarSound.Star, 1f, 1f + i * 0.12f);
                    SugarAudio.Buzz(0);
                }
                float t = Mathf.Clamp01((time - start) / 0.35f);
                _stars[i].rectTransform.localScale = Vector3.one * (t < 1f ? 1f + Mathf.Sin(t * Mathf.PI) * 0.45f : 1f);
            }

            // Monedele se numără în 0,8 s după prima stea.
            float ct = Mathf.Clamp01((time - StarStart) / 0.8f);
            int value = Mathf.RoundToInt(_coinTarget * UiEase.OutCubic(ct));
            if (value != _shownCoins)
            {
                _shownCoins = value;
                _coins.Text = string.Format(_coinsFormat, value);
            }

            if (time > StarStart + _starCount * StarStep + 0.9f) _time = -1f;
        }
    }
}
