using System.Collections.Generic;
using TollRoadTycoon.Core;
using TollRoadTycoon.Economy;
using TollRoadTycoon.Localization;
using TollRoadTycoon.Services;
using UnityEngine;

namespace TollRoadTycoon.Presentation
{
    /// <summary>
    /// Draws the active section's roadside services and decorations at their saved slots, highlights
    /// free slots while placing, and shows "+$" labels when a service customer pays.
    /// </summary>
    public sealed class BuildingsView : MonoBehaviour
    {
        private TycoonGame _game;
        private SectionState _section;
        private Transform _content;
        private readonly List<SpriteRenderer> _highlights = new List<SpriteRenderer>();
        private readonly List<(TextMesh text, float age)> _labels = new List<(TextMesh, float)>();
        private bool _dirty = true;

        public void Bind(TycoonGame game)
        {
            if (_section != null)
            {
                _section.Services.Changed -= MarkDirty;
                _section.Decorations.Changed -= MarkDirty;
            }
            if (_game != null && GameRoot.Instance != null) GameRoot.Instance.PlacementChanged -= MarkDirty;
            _game = game;
            _section = game.ActiveSection;
            _section.Services.Changed += MarkDirty;
            _section.Decorations.Changed += MarkDirty;
            if (GameRoot.Instance != null) GameRoot.Instance.PlacementChanged += MarkDirty;
            MarkDirty();
        }

        private void MarkDirty() => _dirty = true;

        private void LateUpdate()
        {
            if (_game == null) return;
            if (_section != _game.ActiveSection) Bind(_game);
            if (_dirty)
            {
                _dirty = false;
                Rebuild();
            }
            float pulse = 0.35f + 0.25f * Mathf.Sin(Time.time * 4f);
            foreach (var h in _highlights) h.color = Palette.WithAlpha(Palette.Gold, pulse);
            for (int i = _labels.Count - 1; i >= 0; i--)
            {
                var (text, age) = _labels[i];
                age += Time.deltaTime;
                if (age > 1.3f)
                {
                    Destroy(text.gameObject);
                    _labels.RemoveAt(i);
                    continue;
                }
                text.transform.localPosition += new Vector3(0, Time.deltaTime * 1.2f, 0);
                text.color = Palette.WithAlpha(Palette.Gold, 1f - Mathf.Clamp01((age - 0.7f) / 0.6f));
                _labels[i] = (text, age);
            }
        }

        private void Rebuild()
        {
            if (_content != null) Destroy(_content.gameObject);
            _highlights.Clear();
            _content = new GameObject("Placed").transform;
            _content.SetParent(transform, false);
            var progression = _game.Progression;

            foreach (var s in _section.Services.Placed)
            {
                var pos = ToVector(progression.ServiceSlots[s.Slot]);
                if (Shapes.Art(_content, "Service", "Buildings/service_" + s.Config.Id, 0, pos, 3.4f, Layers.Decor) == null)
                {
                    Shapes.Rounded(_content, "ServiceShadow", Palette.Shadow, pos + new Vector2(0.15f, -0.2f), new Vector2(3f, 2.4f), Layers.Decor - 1);
                    Shapes.Rounded(_content, "ServiceBody", Palette.Cream, pos, new Vector2(3f, 2.4f), Layers.Decor);
                    Shapes.Rounded(_content, "ServiceRoof", Palette.Red, pos + new Vector2(0, 0.45f), new Vector2(3.1f, 1.2f), Layers.Decor + 1);
                    Shapes.Label(_content, "ServiceName", Loc.T(s.Config.NameKey), Palette.White, pos + new Vector2(0, 0.45f), 0.3f, Layers.Decor + 2);
                }
                Shapes.Label(_content, "ServiceLevel", Loc.F("common.level", s.Level), Palette.White, pos + new Vector2(0, -1.55f), 0.32f, Layers.Popup - 2);
            }

            foreach (var d in _section.Decorations.Placed)
            {
                var pos = ToVector(progression.DecorationSlots[d.Slot]);
                if (Shapes.ArtFit(_content, "Decor", "Decorations/" + d.Config.Id, 0, pos, 1.3f, Layers.Decor + 3) == null)
                {
                    Shapes.Circle(_content, "DecorShape", Palette.Gold, pos, 0.6f, Layers.Decor + 3);
                    Shapes.Label(_content, "DecorName", Loc.T(d.Config.NameKey).Substring(0, 1), Palette.DarkBlue, pos, 0.35f, Layers.Decor + 4);
                }
            }

            // Free slots light up while placing.
            var root = GameRoot.Instance;
            if (root == null || root.Placement == PlacementKind.None) return;
            bool service = root.Placement == PlacementKind.BuildService || root.Placement == PlacementKind.MoveService;
            var slots = service ? progression.ServiceSlots : progression.DecorationSlots;
            for (int i = 0; i < slots.Count; i++)
            {
                bool free = service ? _section.Services.IsSlotFree(i) : _section.Decorations.IsSlotFree(i);
                if (!free) continue;
                var size = service ? new Vector2(3.2f, 2.6f) : new Vector2(1.1f, 1.1f);
                _highlights.Add(Shapes.Rounded(_content, "FreeSlot", Palette.WithAlpha(Palette.Gold, 0.4f), ToVector(slots[i]), size, Layers.Popup - 3));
                Shapes.Label(_content, "Plus", "+", Palette.White, ToVector(slots[i]), service ? 1.2f : 0.6f, Layers.Popup - 2);
            }
        }

        public void ShowIncome(PlacedService service, long cents)
        {
            if (_game == null || service.Slot >= _game.Progression.ServiceSlots.Count) return;
            var pos = ToVector(_game.Progression.ServiceSlots[service.Slot]) + new Vector2(0, 1.4f);
            var label = Shapes.Label(transform, "ServiceIncome", "+" + Money.Format(cents), Palette.Gold, pos, 0.4f, Layers.Popup);
            _labels.Add((label, 0f));
        }

        private static Vector2 ToVector(Vec2 v) => new Vector2((float)v.X, (float)v.Y);
    }
}
