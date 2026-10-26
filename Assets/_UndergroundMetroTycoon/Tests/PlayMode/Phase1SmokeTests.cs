using System.Collections;
using System.IO;
using NUnit.Framework;
using UndergroundMetroTycoon.Game;
using UndergroundMetroTycoon.Simulation;
using UnityEngine;
using UnityEngine.TestTools;

namespace UndergroundMetroTycoon.Tests
{
    /// <summary>Boots the full Phase 1 game from code and drives the player into the dig site.</summary>
    public class Phase1SmokeTests
    {
        private const string SaveFile = "umt_playmode_test.json";
        private GameObject _root;

        [SetUp]
        public void SetUp() => DeleteSave();

        [TearDown]
        public void TearDown()
        {
            if (_root != null) Object.Destroy(_root);
            DeleteSave();
        }

        [UnityTest]
        public IEnumerator Bootstrap_BuildsWorld_AndPlayerDigsAcceptedContract()
        {
            _root = new GameObject("TestBootstrap");
            _root.SetActive(false);
            GameBootstrap bootstrap = _root.AddComponent<GameBootstrap>();
            bootstrap.Configure(null, null, SaveFile);
            _root.SetActive(true);
            yield return null;

            Assert.IsNotNull(bootstrap.Session);
            Assert.IsNotNull(bootstrap.Player);
            Assert.AreEqual(0, bootstrap.Session.Wallet.Coins);
            Assert.IsNotNull(Object.FindFirstObjectByType<HudView>());

            bootstrap.AcceptContract(DefaultContent.GardenTrenchId);
            ExcavationGrid grid = bootstrap.Session.Grid;
            Vector3 cellTop = grid.Layout.CellTopCenter(grid.GetCell(0).Coord).ToUnityVector();
            bootstrap.Player.Teleport(cellTop + new Vector3(-0.9f, 0f, 0f), 90f);

            float timeout = 6f;
            while (grid.ExcavatedContractCellCount == 0 && timeout > 0f)
            {
                timeout -= Time.deltaTime;
                yield return null;
            }

            Assert.Greater(grid.ExcavatedContractCellCount, 0, "Player should auto-dig a nearby cell");
            Assert.Greater(bootstrap.Session.Wallet.Coins, 0);
        }

        private static void DeleteSave()
        {
            string path = Path.Combine(Application.persistentDataPath, SaveFile);
            foreach (string p in new[] { path, path + ".bak", path + ".tmp", path + ".corrupt" })
                if (File.Exists(p)) File.Delete(p);
        }
    }

    internal static class TestVectorExtensions
    {
        public static Vector3 ToUnityVector(this System.Numerics.Vector3 v) => new Vector3(v.X, v.Y, v.Z);
    }
}
