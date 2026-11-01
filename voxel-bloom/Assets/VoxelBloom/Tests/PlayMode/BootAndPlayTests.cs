using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using VoxelBloom.Core;

namespace VoxelBloom.Tests
{
    /// <summary>Smoke tests: the app boots to HOME, level 1 loads, the witness solution wins through the real controller.</summary>
    public class BootAndPlayTests
    {
        [UnityTest]
        public IEnumerator Boots_ToHome_PlaysLevel1_ToCompletion()
        {
            yield return null; // RuntimeInitializeOnLoad boot
            if (App.Instance == null) new GameObject("VoxelBloomApp").AddComponent<App>();
            yield return null;
            Assert.IsTrue(Services.Initialized);
            Assert.IsInstanceOf<HomeScreen>(Router.Instance.Current);

            Services.Lives.Refill();
            Services.Save.ActiveLevel = null; // start fresh so the witness applies
            GameplayController.StartLevel(1);
            yield return null;
            Assert.IsInstanceOf<GameplayScreen>(Router.Instance.Current);
            var c = GameplayController.Active;
            Assert.NotNull(c);
            Assert.AreEqual(GameState.Ready, App.State.Current);

            foreach (int slot in c.Level.Witness)
            {
                float guard = 0;
                while (App.State.Current != GameState.Ready) // witness assumes each tap fully resolves
                {
                    guard += Time.unscaledDeltaTime;
                    Assert.Less(guard, 10f, "controller stalled");
                    yield return null;
                }
                c.OnBlockTapped(slot);
                yield return new WaitForSecondsRealtime(0.05f);
            }
            float t = 0;
            while (App.State.Current != GameState.LevelComplete && t < 30f) { t += Time.unscaledDeltaTime; yield return null; }
            Assert.AreEqual(GameState.LevelComplete, App.State.Current);
            Assert.IsTrue(c.Session.IsWon);
        }
    }
}
