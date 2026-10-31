namespace TollRoadTycoon.Core
{
    /// <summary>Live counters fed by gameplay events (never estimated).</summary>
    public sealed class GameStats
    {
        public long VehiclesProcessed;
        public long HeavyVehiclesProcessed;
        public long EmergencyVehiclesPassed;
        public long TollCentsCollected;
        public long VehiclesSpawned;
        public double TotalQueueWaitSeconds;
        public long QueueWaitSamples;
        public double PlayTimeSeconds;

        // Progression counters (missions and statistics).
        public long BoothsBuilt;
        public long LanesBuilt;
        public long SpeedUpgrades;
        public long CapacityUpgrades;
        public long WorkerUpgrades;
        public long ServicesBuilt;
        public long SectionsUnlocked;
        public long ServiceCustomers;
        public long ServiceIncomeCents;
        public long OfflineIncomeCents;
        public long PassiveIncomeCents;
        public long BoostsUsed;

        public double AverageQueueWaitSeconds => QueueWaitSamples > 0 ? TotalQueueWaitSeconds / QueueWaitSamples : 0;

        internal void RecordPayment(VehicleTypeConfig type, long cents)
        {
            if (type.IsEmergency) EmergencyVehiclesPassed++;
            else VehiclesProcessed++;
            if (type.IsHeavy) HeavyVehiclesProcessed++;
            TollCentsCollected += cents;
        }

        internal void RecordQueueWait(double seconds)
        {
            if (seconds < 0) return;
            TotalQueueWaitSeconds += seconds;
            QueueWaitSamples++;
        }
    }
}
