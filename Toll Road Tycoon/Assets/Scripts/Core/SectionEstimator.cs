using System;
using System.Collections.Generic;
using TollRoadTycoon.Economy;
using TollRoadTycoon.Save;

namespace TollRoadTycoon.Core
{
    public struct SectionEstimate
    {
        /// <summary>Paying vehicles per second (demand limited by booth throughput).</summary>
        public double VehiclesPerSecond;
        public double HeavyFraction;
        public double TollCentsPerSecond;
    }

    /// <summary>
    /// Analytical income of a road that is not being simulated (inactive sections, offline time):
    /// arrivals limited by booth throughput, times the average toll. No hidden vehicles are run.
    /// </summary>
    public static class SectionEstimator
    {
        /// <summary>Extra seconds per vehicle for driving up from the queue (measured in the simulation).</summary>
        public const double QueueAdvanceSeconds = 1.1;

        public static SectionEstimate Estimate(GameConfig roadConfig, SectionSave road, ICollection<string> unlocked)
        {
            var sim = new GameSimulation(roadConfig, 1, new EconomyManager(0), new GameStats(), new HashSet<string>(unlocked));
            sim.ApplySection(road);
            return Estimate(sim);
        }

        public static SectionEstimate Estimate(GameSimulation sim)
        {
            double weight = 0, toll = 0, service = 0, heavy = 0;
            foreach (var type in sim.Config.VehicleTypes)
            {
                if (type.SpawnWeight <= 0 || !sim.Upgrades.IsVehicleUnlocked(type.Id)) continue;
                weight += type.SpawnWeight;
                toll += type.SpawnWeight * type.TollCents;
                service += type.SpawnWeight * type.ServiceTimeMultiplier;
                if (type.IsHeavy) heavy += type.SpawnWeight;
            }
            if (weight <= 0) return default;
            double avgToll = toll / weight;
            double avgService = service / weight;

            double throughput = 0, weightedMultiplier = 0;
            foreach (var booth in sim.Booths)
            {
                if (!booth.IsAcceptingVehicles) continue;
                double cycle = booth.ProcessingSeconds * avgService + booth.BarrierSeconds * 2 + QueueAdvanceSeconds;
                double rate = 1.0 / cycle;
                throughput += rate;
                weightedMultiplier += rate * booth.TollMultiplierBasisPoints / 10000.0;
            }
            if (throughput <= 0) return default;
            double served = Math.Min(sim.Spawner.ArrivalsPerSecond, throughput);
            return new SectionEstimate
            {
                VehiclesPerSecond = served,
                HeavyFraction = heavy / weight,
                TollCentsPerSecond = served * avgToll * (weightedMultiplier / throughput),
            };
        }
    }
}
