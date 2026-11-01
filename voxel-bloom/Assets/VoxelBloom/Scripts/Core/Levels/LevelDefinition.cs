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
        /// <summary>4 columns of blocks (index 0 = top, the only active block). Power = voxel count of the block.</summary>
        public AttackBlock[][] Columns = { new AttackBlock[0], new AttackBlock[0], new AttackBlock[0], new AttackBlock[0] };
        /// <summary>Minimum score for 3 / 2 stars.</summary>
        public int StarScoreThree, StarScoreTwo;
        public int RewardCoins;
        public int HiddenCore;
        public int VoxelCount;
        public BoosterAvailability Boosters = new BoosterAvailability();
        /// <summary>Witness solution found by the build-time solver (column per tap).</summary>
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
                RewardCoins = MiniJson.GetInt(d, "rewardCoins", 50),
                HiddenCore = MiniJson.GetInt(d, "hiddenCore"),
                Witness = MiniJson.GetIntArray(d, "witness"),
                WitnessMoves = MiniJson.GetInt(d, "witnessMoves"),
                WitnessScore = MiniJson.GetInt(d, "witnessScore"),
            };
            int[] stars = MiniJson.GetIntArray(d, "starScore");
            l.StarScoreThree = stars.Length > 0 ? stars[0] : 0;
            l.StarScoreTwo = stars.Length > 1 ? stars[1] : 0;

            var cols = MiniJson.GetArr(d, "columns") ?? throw new FormatException("Level needs columns");
            l.Columns = new AttackBlock[GameSession.Columns][];
            for (int c = 0; c < GameSession.Columns; c++)
            {
                var col = c < cols.Count ? MiniJson.Arr(cols[c]) : null;
                l.Columns[c] = new AttackBlock[col?.Count ?? 0];
                for (int i = 0; i < l.Columns[c].Length; i++)
                {
                    var pair = MiniJson.Arr(col[i]);
                    if (pair == null || pair.Count != 2) throw new FormatException("Column block must be [color, count]");
                    l.Columns[c][i] = new AttackBlock((int)(double)pair[0], (int)(double)pair[1]);
                }
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
            var totals = new int[Grid.PaletteHex.Length];
            int blocks = 0;
            foreach (var col in Columns)
                foreach (var blk in col)
                {
                    if (blk.Color < 0 || blk.Color >= totals.Length) throw new FormatException("Level " + Id + " block colour out of range");
                    if (blk.Power <= 0) throw new FormatException("Level " + Id + " block count must be positive");
                    totals[blk.Color] += blk.Power; blocks++;
                }
            if (blocks == 0) throw new FormatException("Level " + Id + " has no blocks");
            var counts = new int[totals.Length];
            for (int i = 0; i < Grid.Length; i++) { int c = Grid.Get(i); if (c >= 0) counts[c]++; }
            for (int c = 0; c < counts.Length; c++)
                if (counts[c] > totals[c]) throw new FormatException("Level " + Id + " colour " + c + " has fewer block voxels than model voxels");
        }

        public int StarsForScore(int score) => score >= StarScoreThree ? 3 : score >= StarScoreTwo ? 2 : 1;
    }
}
