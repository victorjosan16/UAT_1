using LastBlock.Core;
using LastBlock.Gameplay;
using NUnit.Framework;
using UnityEngine;

namespace LastBlock.Tests
{
    public class LevelConfigTests
    {
        [Test]
        public void TenLevels_LayersFollowDesign()
        {
            for (int level = 1; level <= LevelManager.TotalLevels; level++)
            {
                LevelConfig config = LevelManager.GetLevelConfig(level);
                Assert.AreEqual(level, config.levelNumber);
                Assert.AreEqual(level + 2, config.layers, $"Level {level} layer count");
            }
        }

        [Test]
        public void EveryLevel_IsSolvableWithSafeMoves()
        {
            for (int level = 1; level <= LevelManager.TotalLevels; level++)
            {
                LevelConfig config = LevelManager.GetLevelConfig(level);
                Assert.Greater(config.blocksToRemove, 0);
                Assert.LessOrEqual(config.blocksToRemove, config.SafeRemovableBlocks, $"Level {level} asks for too many blocks");
                foreach (int layer in config.preRemovedMiddleLayers)
                {
                    Assert.Less(layer, config.layers - 1, $"Level {level} pre-removes a block from the top layer");
                }
            }
        }

        [Test]
        public void Difficulty_NeverDecreases()
        {
            for (int level = 2; level <= LevelManager.TotalLevels; level++)
            {
                LevelConfig previous = LevelManager.GetLevelConfig(level - 1);
                LevelConfig current = LevelManager.GetLevelConfig(level);
                Assert.GreaterOrEqual(current.blocksToRemove, previous.blocksToRemove);
                Assert.LessOrEqual(current.staticFriction, previous.staticFriction);
                Assert.LessOrEqual(current.allowedFallenBlocks, previous.allowedFallenBlocks);
            }
        }

        [Test]
        public void OutOfRangeLevels_AreClamped()
        {
            Assert.AreEqual(1, LevelManager.GetLevelConfig(0).levelNumber);
            Assert.AreEqual(LevelManager.TotalLevels, LevelManager.GetLevelConfig(99).levelNumber);
        }
    }

    public class ProgressTests
    {
        int savedValue;
        bool hadValue;
        GameObject host;

        [SetUp]
        public void SetUp()
        {
            hadValue = PlayerPrefs.HasKey(LevelManager.HighestUnlockedKey);
            savedValue = PlayerPrefs.GetInt(LevelManager.HighestUnlockedKey, 1);
            PlayerPrefs.DeleteKey(LevelManager.HighestUnlockedKey);
            host = new GameObject("LevelManagerTest");
        }

        [TearDown]
        public void TearDown()
        {
            Object.DestroyImmediate(host);
            if (hadValue) PlayerPrefs.SetInt(LevelManager.HighestUnlockedKey, savedValue);
            else PlayerPrefs.DeleteKey(LevelManager.HighestUnlockedKey);
        }

        [Test]
        public void CompletingLevel_UnlocksNextAndPersists()
        {
            LevelManager manager = host.AddComponent<LevelManager>();
            Assert.AreEqual(1, manager.HighestUnlockedLevel);

            manager.MarkLevelCompleted(3);
            Assert.AreEqual(4, manager.HighestUnlockedLevel);
            Assert.AreEqual(4, PlayerPrefs.GetInt(LevelManager.HighestUnlockedKey));

            // Replaying an earlier level never lowers progress.
            manager.MarkLevelCompleted(1);
            Assert.AreEqual(4, manager.HighestUnlockedLevel);

            manager.MarkLevelCompleted(LevelManager.TotalLevels);
            Assert.AreEqual(LevelManager.TotalLevels, manager.HighestUnlockedLevel);
        }
    }

    public class ScoreTests
    {
        GameObject host;

        [SetUp] public void SetUp() => host = new GameObject("ScoreTest");
        [TearDown] public void TearDown() => Object.DestroyImmediate(host);

        [Test]
        public void Points_MatchRules()
        {
            ScoreManager score = host.AddComponent<ScoreManager>();
            Assert.AreEqual(100, score.AddExtraction(false));
            Assert.AreEqual(150, score.AddExtraction(true));
            Assert.AreEqual(200, score.AddLevelComplete());
            Assert.AreEqual(450, score.Score);
        }

        [Test]
        public void Retry_RestoresScoreFromLevelStart()
        {
            ScoreManager score = host.AddComponent<ScoreManager>();
            score.AddExtraction(false);
            score.BeginLevel();
            score.AddExtraction(true);
            score.ResetToLevelStart();
            Assert.AreEqual(100, score.Score);
        }
    }

    public class StabilityThresholdTests
    {
        const float Height = 0.3f;

        static bool Displaced(Vector3 baseline, Vector3 position, Quaternion rotation) =>
            TowerStabilityChecker.IsBlockDisplaced(baseline, position, rotation, Height, 0.5f, 30f, -1.5f);

        [Test]
        public void SmallVibration_IsNotAFall()
        {
            Vector3 baseline = new Vector3(0f, 1f, 0f);
            Assert.IsFalse(Displaced(baseline, baseline + new Vector3(0.01f, -0.01f, 0.02f), Quaternion.Euler(2f, 45f, 1f)));
        }

        [Test]
        public void HorizontalSlide_IsNotAFall()
        {
            Vector3 baseline = new Vector3(0f, 1f, 0f);
            Assert.IsFalse(Displaced(baseline, baseline + new Vector3(0.8f, 0f, 0f), Quaternion.identity));
        }

        [Test]
        public void DropOrTilt_IsAFall()
        {
            Vector3 baseline = new Vector3(0f, 1f, 0f);
            Assert.IsTrue(Displaced(baseline, baseline + Vector3.down * 0.2f, Quaternion.identity));
            Assert.IsTrue(Displaced(baseline, baseline, Quaternion.Euler(40f, 0f, 0f)));
            Assert.IsTrue(Displaced(baseline, new Vector3(0f, -2f, 0f), Quaternion.identity));
        }
    }
}
