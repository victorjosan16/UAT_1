using System;
using System.Collections.Generic;
using TollRoadTycoon.Core;
using TollRoadTycoon.Economy;
using TollRoadTycoon.Traffic;

namespace TollRoadTycoon.TollSystem
{
    /// <summary>Awards each vehicle trip's toll exactly once, no matter how often it is asked to.</summary>
    public sealed class PaymentProcessor
    {
        private readonly EconomyManager _economy;
        private readonly GameStats _stats;
        private readonly HashSet<long> _paidTrips = new HashSet<long>();

        /// <summary>Global toll bonus in basis points (10000 = x1). Reserved for timed boosts.</summary>
        public long ActiveBonusBasisPoints { get; set; } = 10000;

        /// <summary>Booth, vehicle, amount in cents (0 for toll-free vehicles).</summary>
        public event Action<TollBooth, Vehicle, long> TollCollected;

        public PaymentProcessor(EconomyManager economy, GameStats stats)
        {
            _economy = economy;
            _stats = stats;
        }

        public static long ComputeToll(VehicleTypeConfig type, TollBooth booth, long bonusBasisPoints)
        {
            long multiplier = (long)Math.Round((decimal)booth.TollMultiplierBasisPoints * bonusBasisPoints / 10000m);
            return Money.ApplyBasisPoints(type.TollCents, multiplier);
        }

        /// <summary>Charges the vehicle. Returns false (and awards nothing) if this trip already paid.</summary>
        public bool TryProcess(Vehicle vehicle, TollBooth booth, out long cents)
        {
            cents = 0;
            if (vehicle == null || vehicle.HasPaid || !_paidTrips.Add(vehicle.TripId)) return false;
            vehicle.HasPaid = true;
            cents = ComputeToll(vehicle.Type, booth, ActiveBonusBasisPoints);
            _economy.AddMoney(cents, MoneySource.Toll);
            _stats.RecordPayment(vehicle.Type, cents);
            TollCollected?.Invoke(booth, vehicle, cents);
            return true;
        }

        /// <summary>Called when a vehicle despawns so the set of paid trips stays small.</summary>
        internal void ForgetTrip(long tripId) => _paidTrips.Remove(tripId);
    }
}
