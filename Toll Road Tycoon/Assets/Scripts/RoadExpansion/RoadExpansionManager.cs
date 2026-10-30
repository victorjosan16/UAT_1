using System;
using System.Collections.Generic;
using TollRoadTycoon.Core;
using TollRoadTycoon.Economy;
using TollRoadTycoon.TollSystem;
using TollRoadTycoon.Traffic;
using TollRoadTycoon.Workers;

namespace TollRoadTycoon.RoadExpansion
{
    public enum ExpansionBlock
    {
        None,
        MaxReached,
        NeedsMoreLanes,
        NotEnoughMoney,
    }

    /// <summary>
    /// Buys booth slots and road lanes. Every purchase adds real geometry (booth lane, connectors,
    /// approach/exit lanes) which routing starts using immediately.
    /// </summary>
    public sealed class RoadExpansionManager
    {
        private readonly GameConfig _config;
        private readonly EconomyManager _economy;
        private readonly LaneManager _lanes;
        private readonly WorkerManager _workers;
        private readonly List<TollBooth> _booths;

        public event Action<TollBooth> BoothBuilt;
        public event Action<int> LaneBuilt;

        public RoadExpansionManager(GameConfig config, EconomyManager economy, LaneManager lanes, WorkerManager workers, List<TollBooth> booths)
        {
            _config = config;
            _economy = economy;
            _lanes = lanes;
            _workers = workers;
            _booths = booths;
        }

        public int BoothCount => _booths.Count;
        public int LaneCount => _lanes.ActiveLaneCount;

        public long NextBoothCost => BoothCount < _config.MaxBooths ? _config.Expansion.BoothSlotPrices[BoothCount] : 0;
        public long NextLaneCost => LaneCount < _config.MaxLanes ? _config.Expansion.LanePrices[LaneCount] : 0;

        public int LanesRequiredForNextBooth =>
            BoothCount < _config.MaxBooths ? _config.Expansion.BoothSlotRequiredLanes[BoothCount] : 0;

        public ExpansionBlock CanBuyBooth()
        {
            if (BoothCount >= _config.MaxBooths) return ExpansionBlock.MaxReached;
            if (LaneCount < LanesRequiredForNextBooth) return ExpansionBlock.NeedsMoreLanes;
            if (!_economy.CanAfford(NextBoothCost)) return ExpansionBlock.NotEnoughMoney;
            return ExpansionBlock.None;
        }

        public ExpansionBlock CanBuyLane()
        {
            if (LaneCount >= _config.MaxLanes) return ExpansionBlock.MaxReached;
            if (!_economy.CanAfford(NextLaneCost)) return ExpansionBlock.NotEnoughMoney;
            return ExpansionBlock.None;
        }

        public TollBooth TryBuyBooth()
        {
            if (CanBuyBooth() != ExpansionBlock.None) return null;
            if (!_economy.TrySpendMoney(NextBoothCost, "expansion.booth")) return null;
            var booth = AddBooth(BoothCount);
            var free = _workers.FindFreeWorker();
            if (free != null) _workers.Assign(free, booth);
            BoothBuilt?.Invoke(booth);
            return booth;
        }

        public bool TryBuyLane()
        {
            if (CanBuyLane() != ExpansionBlock.None) return false;
            if (!_economy.TrySpendMoney(NextLaneCost, "expansion.lane")) return false;
            int lane = LaneCount;
            _lanes.ActivateLane(lane);
            LaneBuilt?.Invoke(lane);
            return true;
        }

        /// <summary>Creates the booth for a slot without charging (initial state and save loading).</summary>
        internal TollBooth AddBooth(int slot)
        {
            _lanes.ActivateBoothSlot(slot);
            var booth = new TollBooth(slot, _lanes.BoothLane(slot), _config.BoothUpgrades, _config.Workers);
            _booths.Add(booth);
            return booth;
        }
    }
}
