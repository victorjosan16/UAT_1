using System;
using System.Collections.Generic;
using TollRoadTycoon.Core;
using TollRoadTycoon.Economy;
using TollRoadTycoon.Services;

namespace TollRoadTycoon.Shop
{
    public sealed class PlacedDecoration
    {
        public DecorationConfig Config;
        public int Slot;
    }

    /// <summary>Cosmetic decorations of one road section. They never change income.</summary>
    public sealed class DecorationManager
    {
        private readonly ProgressionConfig _progression;
        private readonly EconomyManager _economy;
        private readonly List<PlacedDecoration> _placed = new List<PlacedDecoration>();

        public IReadOnlyList<PlacedDecoration> Placed => _placed;
        public event Action Changed;

        public DecorationManager(ProgressionConfig progression, EconomyManager economy)
        {
            _progression = progression;
            _economy = economy;
        }

        public int SlotCount => _progression.DecorationSlots.Count;
        public PlacedDecoration At(int slot) => _placed.Find(d => d.Slot == slot);
        public bool IsSlotFree(int slot) => slot >= 0 && slot < SlotCount && At(slot) == null;

        public int FirstFreeSlot()
        {
            for (int i = 0; i < SlotCount; i++)
            {
                if (IsSlotFree(i)) return i;
            }
            return -1;
        }

        public PlacementResult Buy(string id, int slot)
        {
            var config = _progression.FindDecoration(id);
            if (config == null) return PlacementResult.NotFound;
            if (slot < 0 || slot >= SlotCount) return PlacementResult.InvalidSlot;
            if (!IsSlotFree(slot)) return PlacementResult.SlotTaken;
            if (!_economy.TrySpendMoney(config.PriceCents, "decor." + id)) return PlacementResult.NotEnoughMoney;
            _placed.Add(new PlacedDecoration { Config = config, Slot = slot });
            Changed?.Invoke();
            return PlacementResult.Ok;
        }

        public PlacementResult Move(int fromSlot, int toSlot)
        {
            var d = At(fromSlot);
            if (d == null) return PlacementResult.NotFound;
            if (toSlot < 0 || toSlot >= SlotCount) return PlacementResult.InvalidSlot;
            if (!IsSlotFree(toSlot)) return PlacementResult.SlotTaken;
            d.Slot = toSlot;
            Changed?.Invoke();
            return PlacementResult.Ok;
        }

        /// <summary>Removing a decoration gives no refund (it is purely cosmetic).</summary>
        public PlacementResult Remove(int slot)
        {
            var d = At(slot);
            if (d == null) return PlacementResult.NotFound;
            _placed.Remove(d);
            Changed?.Invoke();
            return PlacementResult.Ok;
        }

        public bool Restore(string id, int slot)
        {
            var config = _progression.FindDecoration(id);
            if (config == null || !IsSlotFree(slot)) return false;
            _placed.Add(new PlacedDecoration { Config = config, Slot = slot });
            return true;
        }
    }
}
