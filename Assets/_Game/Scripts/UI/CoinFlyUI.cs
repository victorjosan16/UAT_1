using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace QuarryEmpire
{
    /// <summary>
    /// Monede care zboară pe ecran de la locul vânzării spre contorul de bani.
    /// Imaginile sunt refolosite (pool simplu, local).
    /// </summary>
    public class CoinFlyUI : MonoBehaviour
    {
        [SerializeField] private float flyDuration = 0.55f;
        [SerializeField] private int maxCoinsPerEvent = 4;
        [SerializeField] private float coinSize = 56f;

        private class Coin
        {
            public RectTransform Rect;
            public Vector3 Start;
            public float Time;
            public bool Active;
        }

        private readonly List<Coin> _coins = new();
        private Camera _camera;

        private void OnEnable() => GameEvents.MoneyEarned += OnMoneyEarned;
        private void OnDisable() => GameEvents.MoneyEarned -= OnMoneyEarned;

        private void OnMoneyEarned(long amount, Vector3 worldPosition, bool animate)
        {
            if (!animate || MoneyHud.Instance == null) return;
            if (_camera == null) _camera = Camera.main;
            if (_camera == null) return;

            Vector3 screen = _camera.WorldToScreenPoint(worldPosition);
            if (screen.z < 0f) return;
            int count = Mathf.Clamp((int)Mathf.Log10(Mathf.Max(1, amount)) + 1, 1, maxCoinsPerEvent);
            for (int i = 0; i < count; i++)
            {
                Coin coin = GetCoin();
                coin.Start = screen + (Vector3)(Random.insideUnitCircle * 40f);
                coin.Time = -i * 0.05f; // decalaj mic între monede
                coin.Rect.position = coin.Start;
                coin.Rect.gameObject.SetActive(true);
                coin.Active = true;
            }
        }

        private Coin GetCoin()
        {
            foreach (Coin coin in _coins)
                if (!coin.Active) return coin;

            UiTheme theme = UiRoot.Theme;
            RectTransform rect = UiFactory.CreateRect("Coin", transform);
            rect.sizeDelta = new Vector2(coinSize, coinSize);
            var image = rect.gameObject.AddComponent<Image>();
            image.sprite = theme.CircleSprite;
            image.color = theme.AccentColor;
            image.raycastTarget = false;
            var created = new Coin { Rect = rect };
            _coins.Add(created);
            return created;
        }

        private void Update()
        {
            if (MoneyHud.Instance == null) return;
            Vector3 target = MoneyHud.Instance.CoinTarget.position;
            float dt = Time.unscaledDeltaTime;

            foreach (Coin coin in _coins)
            {
                if (!coin.Active) continue;
                coin.Time += dt;
                float t = Mathf.Clamp01(coin.Time / flyDuration);
                // Ease-in: pornește încet și accelerează spre contor.
                float eased = t * t;
                coin.Rect.position = Vector3.Lerp(coin.Start, target, eased) + Vector3.up * (Mathf.Sin(t * Mathf.PI) * 60f);
                coin.Rect.localScale = Vector3.one * Mathf.Lerp(1.2f, 0.6f, t);
                if (t < 1f) continue;
                coin.Active = false;
                coin.Rect.gameObject.SetActive(false);
            }
        }
    }
}
