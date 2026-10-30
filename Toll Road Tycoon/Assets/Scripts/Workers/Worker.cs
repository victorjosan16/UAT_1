using System;
using TollRoadTycoon.Core;
using TollRoadTycoon.TollSystem;

namespace TollRoadTycoon.Workers
{
    public sealed class Worker
    {
        public int Id { get; }
        public int Level { get; internal set; }
        public TollBooth AssignedBooth { get; internal set; }

        public Worker(int id, int level)
        {
            Id = id;
            Level = Math.Max(1, level);
        }

        public double GetProcessingSeconds(WorkerConfig config)
        {
            var table = config.ProcessingSecondsByLevel;
            return table[Math.Min(Level, table.Length) - 1];
        }

        /// <summary>Speed relative to a level 1 worker (1.0 = 100%).</summary>
        public double GetEfficiency(WorkerConfig config) => config.ProcessingSecondsByLevel[0] / GetProcessingSeconds(config);
    }
}
