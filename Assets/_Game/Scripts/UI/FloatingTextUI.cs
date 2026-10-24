using System.Collections.Generic;
using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// Texte plutitoare deasupra lumii: „+$12” la vânzare, „FORȚĂ NV.3!” la upgrade.
    /// Etichetele sunt refolosite (pool local), fără alocări în timpul jocului.
    /// </summary>
    public class FloatingTextUI : MonoBehaviour
    {
        [SerializeField] private float lifetime = 0.9f;
        [SerializeField] private float riseDistance = 140f;
        [SerializeField] private int maxActive = 14;

        private class Entry
        {
            public UiLabel Label;
            public CanvasGroup Group;
            public Vector3 World;
            public float Time;
            public float Scale;
            public bool Active;
        }

        private readonly List<Entry> _entries = new();
        private Camera _camera;
        private int _activeCount;

        private void OnEnable()
        {
            GameEvents.MoneyEarned += OnMoneyEarned;
            GameEvents.Upgraded += OnUpgraded;
        }

        private void OnDisable()
        {
            GameEvents.MoneyEarned -= OnMoneyEarned;
            GameEvents.Upgraded -= OnUpgraded;
        }

        private void OnMoneyEarned(long amount, Vector3 worldPosition, bool animate)
        {
            if (!animate) return;
            Show("+" + NumberFormat.Money(amount), worldPosition + Vector3.up * 1.2f, UiRoot.Theme.AccentColor, 1f);
        }

        private void OnUpgraded(UpgradeDefinition upgrade, int level)
        {
            if (PlayerRoot.Instance == null) return;
            Show(Loc.F("{0} NV.{1}!", Loc.T(upgrade.DisplayName).ToUpperInvariant(), level), PlayerRoot.Instance.Position + Vector3.up * 2.6f,
                UiRoot.Theme.PositiveColor, 1.3f);
        }

        /// <summary>Afișează un text care urcă și dispare deasupra unui punct din lume.</summary>
        public void Show(string text, Vector3 worldPosition, Color color, float scale)
        {
            if (_activeCount >= maxActive) return;
            Entry entry = GetEntry();
            entry.Label.Text = text;
            entry.Label.Color = color;
            entry.World = worldPosition;
            entry.Time = 0f;
            entry.Scale = scale;
            entry.Active = true;
            entry.Label.gameObject.SetActive(true);
            _activeCount++;
            Position(entry, 0f);
        }

        private Entry GetEntry()
        {
            foreach (Entry entry in _entries)
                if (!entry.Active) return entry;

            UiLabel label = UiFactory.CreateText("FloatingText", transform, "", 52);
            label.RectTransform.sizeDelta = new Vector2(600f, 90f);
            var created = new Entry { Label = label, Group = label.gameObject.AddComponent<CanvasGroup>() };
            created.Group.blocksRaycasts = false;
            _entries.Add(created);
            return created;
        }

        private void Update()
        {
            if (_activeCount == 0) return;
            float dt = Time.unscaledDeltaTime;
            foreach (Entry entry in _entries)
            {
                if (!entry.Active) continue;
                entry.Time += dt;
                float t = entry.Time / lifetime;
                if (t >= 1f)
                {
                    entry.Active = false;
                    entry.Label.gameObject.SetActive(false);
                    _activeCount--;
                    continue;
                }
                Position(entry, t);
            }
        }

        private void Position(Entry entry, float t)
        {
            if (_camera == null) _camera = Camera.main;
            if (_camera == null) return;
            Vector3 screen = _camera.WorldToScreenPoint(entry.World);
            float eased = 1f - (1f - t) * (1f - t);
            entry.Label.RectTransform.position = screen + Vector3.up * (eased * riseDistance * (Screen.height / 1920f));
            // Pop la început (crește peste 1 și revine), apoi dispare în ultima treime.
            float pop = t < 0.15f ? Mathf.Lerp(0.6f, 1.15f, t / 0.15f) : Mathf.Lerp(1.15f, 1f, (t - 0.15f) / 0.85f);
            entry.Label.RectTransform.localScale = Vector3.one * (pop * entry.Scale);
            entry.Group.alpha = t < 0.66f ? 1f : 1f - (t - 0.66f) / 0.34f;
        }
    }
}
