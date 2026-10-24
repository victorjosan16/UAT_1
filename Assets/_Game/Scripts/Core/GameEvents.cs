using System;
using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// „Panou de anunțuri” global. Sistemele anunță ce s-a întâmplat (Raise...),
    /// iar cine e interesat ascultă (+=). Așa sistemele nu se cunosc direct între ele.
    /// Regulă: abonează-te în OnEnable, dezabonează-te în OnDisable.
    /// </summary>
    public static class GameEvents
    {
        // --- Joc
        public static event Action GameStarted;

        // --- Minat
        public static event Action<MineBlock> BlockHit;
        public static event Action<MineBlock> BlockDestroyed;
        public static event Action<ResourceData, Vector3> ResourceCollected;
        public static event Action<ResourceStack> StackFull;

        // --- Straturi
        public static event Action<int, LayerDefinition> LayerStarted;
        public static event Action<int> LayerCleared;

        // --- Economie
        /// <summary>Bani câștigați: suma, poziția în lume, dacă se animă monede spre UI.</summary>
        public static event Action<long, Vector3, bool> MoneyEarned;
        public static event Action<Vector3> CoinPaid;
        public static event Action<string, string> Purchased;
        public static event Action<UpgradeDefinition, int> Upgraded;
        public static event Action<ResourceData> ItemRefined;

        // --- Utilaje
        public static event Action<Vehicle> VehicleEntered;
        public static event Action<Vehicle> VehicleExited;

        // --- Comenzi
        public static event Action OrderChanged;
        public static event Action<long> OrderCompleted;
        public static event Action OrderFailed;

        // --- Diverse
        public static event Action<string> Toast;
        public static event Action<long> OfflineEarningsReady;

        public static void RaiseGameStarted() => GameStarted?.Invoke();
        public static void RaiseBlockHit(MineBlock block) => BlockHit?.Invoke(block);
        public static void RaiseBlockDestroyed(MineBlock block) => BlockDestroyed?.Invoke(block);
        public static void RaiseResourceCollected(ResourceData resource, Vector3 position) => ResourceCollected?.Invoke(resource, position);
        public static void RaiseStackFull(ResourceStack stack) => StackFull?.Invoke(stack);
        public static void RaiseLayerStarted(int index, LayerDefinition layer) => LayerStarted?.Invoke(index, layer);
        public static void RaiseLayerCleared(int index) => LayerCleared?.Invoke(index);
        public static void RaiseMoneyEarned(long amount, Vector3 position, bool animate) => MoneyEarned?.Invoke(amount, position, animate);
        public static void RaiseCoinPaid(Vector3 position) => CoinPaid?.Invoke(position);
        public static void RaisePurchased(string id, string title) => Purchased?.Invoke(id, title);
        public static void RaiseUpgraded(UpgradeDefinition upgrade, int level) => Upgraded?.Invoke(upgrade, level);
        public static void RaiseItemRefined(ResourceData result) => ItemRefined?.Invoke(result);
        public static void RaiseVehicleEntered(Vehicle vehicle) => VehicleEntered?.Invoke(vehicle);
        public static void RaiseVehicleExited(Vehicle vehicle) => VehicleExited?.Invoke(vehicle);
        public static void RaiseOrderChanged() => OrderChanged?.Invoke();
        public static void RaiseOrderCompleted(long reward) => OrderCompleted?.Invoke(reward);
        public static void RaiseOrderFailed() => OrderFailed?.Invoke();
        public static void RaiseToast(string message) => Toast?.Invoke(message);
        public static void RaiseOfflineEarningsReady(long amount) => OfflineEarningsReady?.Invoke(amount);

        // Curățăm abonații la pornirea Play Mode (util când „Domain Reload” e dezactivat în Editor).
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            GameStarted = null;
            BlockHit = null;
            BlockDestroyed = null;
            ResourceCollected = null;
            StackFull = null;
            LayerStarted = null;
            LayerCleared = null;
            MoneyEarned = null;
            CoinPaid = null;
            Purchased = null;
            Upgraded = null;
            ItemRefined = null;
            VehicleEntered = null;
            VehicleExited = null;
            OrderChanged = null;
            OrderCompleted = null;
            OrderFailed = null;
            Toast = null;
            OfflineEarningsReady = null;
        }
    }
}
