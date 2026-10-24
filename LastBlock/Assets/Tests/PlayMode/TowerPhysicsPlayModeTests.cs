using System.Collections;
using LastBlock.Core;
using LastBlock.Gameplay;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

namespace LastBlock.Tests
{
    /// <summary>
    /// Physics tests in an empty test scene: a ground box plus a generated tower (primitive fallback blocks).
    /// </summary>
    public class TowerPhysicsPlayModeTests
    {
        GameObject root;
        TowerGenerator generator;
        TowerStabilityChecker checker;

        [SetUp]
        public void SetUp()
        {
            GameManager.ApplyPhysicsSettings();
            root = new GameObject("TestRoot");

            GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
            ground.name = "Ground";
            ground.transform.SetParent(root.transform);
            ground.transform.position = new Vector3(0f, -0.5f, 0f);
            ground.transform.localScale = new Vector3(6f, 1f, 6f);

            generator = new GameObject("Tower").AddComponent<TowerGenerator>();
            generator.transform.SetParent(root.transform);
            checker = root.AddComponent<TowerStabilityChecker>();
        }

        [TearDown]
        public void TearDown()
        {
            Object.Destroy(root);
        }

        IEnumerator BuildAndSettle(int level)
        {
            LevelConfig config = LevelManager.GetLevelConfig(level);
            var blocks = generator.Generate(config);
            checker.Begin(blocks, generator.BlockHeight, config.allowedFallenBlocks);
            yield return new WaitForSeconds(1f);
            checker.CaptureBaseline();
            checker.TakeSnapshot();
        }

        static BlockController FindBlock(TowerGenerator tower, int layer, int index)
        {
            foreach (BlockController block in tower.Blocks)
            {
                if (block != null && block.LayerIndex == layer && block.IndexInLayer == index) return block;
            }
            return null;
        }

        [UnityTest]
        public IEnumerator TallestTower_StaysStillWhenUntouched()
        {
            yield return BuildAndSettle(10);
            yield return new WaitForSeconds(3f);

            Assert.IsFalse(checker.IsCollapsed, "Untouched tower collapsed");
            Assert.AreEqual(0, checker.FallenCount);
            Assert.Less(checker.MaxDisplacementSinceSnapshot(), 0.02f, "Untouched tower drifted");
        }

        [UnityTest]
        public IEnumerator PullingMiddleBlock_ExtractsWithoutCollapse()
        {
            yield return BuildAndSettle(4);

            BlockDragController drag = root.AddComponent<BlockDragController>();
            drag.Configure(null, null, generator);
            BlockController extracted = null;
            drag.BlockExtracted += block => extracted = block;

            BlockController target = FindBlock(generator, 1, 1);
            Assert.IsNotNull(target);
            Assert.IsTrue(drag.BeginDrag(target), "Could not start drag");
            drag.SetTargetOffset(generator.BlockLength + 0.4f);

            float timeout = 5f;
            while (extracted == null && timeout > 0f)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }

            Assert.AreSame(target, extracted, "Block was not extracted");
            yield return new WaitForSeconds(2f);
            Assert.IsFalse(checker.IsCollapsed, "Removing a middle block collapsed the tower");
        }

        [UnityTest]
        public IEnumerator TopLayerBlocks_AreLocked()
        {
            yield return BuildAndSettle(1);
            BlockDragController drag = root.AddComponent<BlockDragController>();
            drag.Configure(null, null, generator);

            BlockController top = FindBlock(generator, 2, 1);
            Assert.IsNotNull(top);
            Assert.IsTrue(top.IsLocked);
            Assert.IsFalse(drag.BeginDrag(top));
        }

        [UnityTest]
        public IEnumerator RemovingBottomSupport_IsDetectedAsCollapse()
        {
            yield return BuildAndSettle(4);

            // Leave only one edge block in the bottom layer: the tower's centre of mass is unsupported.
            FindBlock(generator, 0, 0).MarkExtracted(Vector3.left);
            FindBlock(generator, 0, 1).MarkExtracted(Vector3.left);

            float timeout = 5f;
            while (!checker.IsCollapsed && timeout > 0f)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }
            Assert.IsTrue(checker.IsCollapsed, "Collapse was not detected");
        }
    }
}
