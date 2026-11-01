using System;
using System.Collections.Generic;

namespace VoxelBloom.Core
{
    public readonly struct AttackBlock : IEquatable<AttackBlock>
    {
        public readonly int Color;
        public readonly int Power;
        public AttackBlock(int color, int power) { Color = color; Power = power; }
        public bool Equals(AttackBlock o) => Color == o.Color && Power == o.Power;
        public override bool Equals(object obj) => obj is AttackBlock o && Equals(o);
        public override int GetHashCode() => Color * 397 ^ Power;
        public override string ToString() => "[" + Color + ":" + Power + "]";

        public static readonly int[] ValidPowers = { 2, 4, 8, 16, 32, 64 };
        public static bool IsValidPower(int p) => Array.IndexOf(ValidPowers, p) >= 0;
    }

    public sealed class BoosterAvailability
    {
        public bool Hammer, Shuffle, ColorBomb, Eliminate = true;
    }

    /// <summary>One playable level as produced by tools/build-data.mjs.</summary>
    public sealed class LevelDefinition
    {
        public int Id, Chapter, IndexInChapter;
        public string Name, ModelId, Variant;
        public float Difficulty;
        public bool Hard;
        public int Seed;
        public VoxelGrid Grid;
        public AttackBlock[] Deck;
        public int QueueSize = 5;
        public int MoveLimit;
        public int StarsThree, StarsTwo;
        public int RewardCoins;
        public int HiddenCore;
        public int VoxelCount;
        public BoosterAvailability Boosters = new BoosterAvailability();
        /// <summary>Witness solution found by the build-time solver (board slot per tap).</summary>
        public int[] Witness;
        public int WitnessMoves, WitnessScore;

        public static LevelDefinition FromJson(string json) => FromJson(MiniJson.Obj(MiniJson.Parse(json)));

        public static LevelDefinition FromJson(Dictionary<string, object> d)
        {
            if (d == null) throw new FormatException("Level JSON is not an object");
            var l = new LevelDefinition
            {
                Id = MiniJson.GetInt(d, "id"),
                Chapter = MiniJson.GetInt(d, "chapter"),
                IndexInChapter = MiniJson.GetInt(d, "indexInChapter"),
                Name = MiniJson.GetString(d, "name", "Level"),
                ModelId = MiniJson.GetString(d, "model", ""),
                Variant = MiniJson.GetString(d, "variant", ""),
                Difficulty = (float)MiniJson.GetDouble(d, "difficulty"),
                Hard = MiniJson.GetBool(d, "hard"),
                Seed = MiniJson.GetInt(d, "seed"),
                Grid = VoxelGrid.FromJson(d),
                QueueSize = MiniJson.GetInt(d, "queueSize", 5),
                MoveLimit = MiniJson.GetInt(d, "moveLimit", 999),
                RewardCoins = MiniJson.GetInt(d, "rewardCoins", 50),
                HiddenCore = MiniJson.GetInt(d, "hiddenCore"),
                Witness = MiniJson.GetIntArray(d, "witness"),
                WitnessMoves = MiniJson.GetInt(d, "witnessMoves"),
                WitnessScore = MiniJson.GetInt(d, "witnessScore"),
            };
            int[] stars = MiniJson.GetIntArray(d, "stars");
            l.StarsThree = stars.Length > 0 ? stars[0] : l.MoveLimit;
            l.StarsTwo = stars.Length > 1 ? stars[1] : l.MoveLimit;

            var deck = MiniJson.GetArr(d, "deck") ?? new List<object>();
            l.Deck = new AttackBlock[deck.Count];
            for (int i = 0; i < deck.Count; i++)
            {
                var pair = MiniJson.Arr(deck[i]);
                if (pair == null || pair.Count != 2) throw new FormatException("Deck entry " + i + " must be [color, power]");
                l.Deck[i] = new AttackBlock((int)(double)pair[0], (int)(double)pair[1]);
            }
            var b = MiniJson.GetObj(d, "boosters");
            if (b != null)
            {
                l.Boosters.Hammer = MiniJson.GetBool(b, "hammer");
                l.Boosters.Shuffle = MiniJson.GetBool(b, "shuffle");
                l.Boosters.ColorBomb = MiniJson.GetBool(b, "colorBomb");
                l.Boosters.Eliminate = MiniJson.GetBool(b, "eliminate", true);
            }
            l.VoxelCount = l.Grid.CountSolid();
            l.Validate();
            return l;
        }

        /// <summary>Structural validation; throws on corrupt data.</summary>
        public void Validate()
        {
            if (Id <= 0) throw new FormatException("Level id must be positive");
            if (Deck == null || Deck.Length == 0) throw new FormatException("Level " + Id + " has an empty deck");
            if (QueueSize < 1 || QueueSize > 8) throw new FormatException("Level " + Id + " queue size out of range");
            var totals = new int[Grid.PaletteHex.Length];
            foreach (var blk in Deck)
            {
                if (blk.Color < 0 || blk.Color >= totals.Length) throw new FormatException("Level " + Id + " deck colour out of range");
                if (!AttackBlock.IsValidPower(blk.Power)) throw new FormatException("Level " + Id + " invalid power " + blk.Power);
                totals[blk.Color] += blk.Power;
            }
            var counts = new int[totals.Length];
            for (int i = 0; i < Grid.Length; i++) { int c = Grid.Get(i); if (c >= 0) counts[c]++; }
            for (int c = 0; c < counts.Length; c++)
                if (counts[c] > totals[c]) throw new FormatException("Level " + Id + " colour " + c + " has less attack power than voxels");
        }

        public int StarsForMoves(int moves) => moves <= StarsThree ? 3 : moves <= StarsTwo ? 2 : 1;
    }
}
