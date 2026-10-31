using System;
using Veliqora.Core.Economy;
using Veliqora.Core.Save;
using Veliqora.UI;

namespace Veliqora.Presentation
{
    /// <summary>Resolves the selected theme / bottle skin and persists selection changes.</summary>
    public sealed class ThemeService
    {
        private readonly EconomyConfig _config;
        private readonly CosmeticsSelection _selection;
        private readonly EconomyService _economy;
        private readonly Action _onChanged;

        public event Action Changed;

        public ThemeService(EconomyConfig config, CosmeticsSelection selection, EconomyService economy, Action onChanged)
        {
            _config = config;
            _selection = selection;
            _economy = economy;
            _onChanged = onChanged;
            // Repair selections that point to unknown or unowned items (content changes, tampered saves).
            if (!IsValid(_selection.Theme, CosmeticKind.Theme)) _selection.Theme = FirstOwned(CosmeticKind.Theme);
            if (!IsValid(_selection.Bottle, CosmeticKind.BottleSkin)) _selection.Bottle = FirstOwned(CosmeticKind.BottleSkin);
            UIStyle.ThemeAccent = Theme.Accent;
        }

        private bool IsValid(string id, CosmeticKind kind)
        {
            var item = _config.FindCosmetic(id);
            return item != null && item.Kind == kind && _economy.Owns(id);
        }

        private string FirstOwned(CosmeticKind kind)
        {
            foreach (var c in _config.Cosmetics) if (c.Kind == kind && _economy.Owns(c.Id)) return c.Id;
            return kind == CosmeticKind.Theme ? "theme_valley" : "bottle_classic";
        }

        public string ThemeId => _selection.Theme;
        public string BottleId => _selection.Bottle;
        public ThemeVisuals Theme => ThemeVisuals.From(_config.FindCosmetic(_selection.Theme));
        public ThemeVisuals ThemeFor(string id) => ThemeVisuals.From(_config.FindCosmetic(id));
        public BottleShape Bottle(int capacity) => BottleShape.From(_config.FindCosmetic(_selection.Bottle), capacity);
        public BottleShape BottleFor(string id, int capacity) => BottleShape.From(_config.FindCosmetic(id), capacity);

        /// <summary>Applies an owned cosmetic. Returns false if not owned.</summary>
        public bool Apply(string id)
        {
            var item = _config.FindCosmetic(id);
            if (item == null || !_economy.Owns(id)) return false;
            if (item.Kind == CosmeticKind.Theme) { _selection.Theme = id; UIStyle.ThemeAccent = Theme.Accent; }
            else _selection.Bottle = id;
            _onChanged?.Invoke();
            Changed?.Invoke();
            return true;
        }
    }
}
