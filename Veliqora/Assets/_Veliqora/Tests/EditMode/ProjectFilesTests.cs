using System.IO;
using System.Text.RegularExpressions;
using NUnit.Framework;

namespace Veliqora.Tests.EditMode
{
    /// <summary>Guards the committed Unity scenes / build settings against drift (GUIDs, order, markers).</summary>
    public class ProjectFilesTests
    {
        private static readonly string[] Scenes = { "Bootstrap", "Home", "LevelMap", "Gameplay", "LevelComplete", "Shop", "Customization" };

        private static string Guid(string metaPath)
        {
            var m = Regex.Match(File.ReadAllText(TestUtil.FindProjectFile(metaPath)), @"guid:\s*([0-9a-f]{32})");
            Assert.IsTrue(m.Success, "no guid in " + metaPath);
            return m.Groups[1].Value;
        }

        [Test]
        public void EveryScene_HasSceneEntryMarker_ForItsScreen()
        {
            string entryGuid = Guid("Assets/_Veliqora/Scripts/Infrastructure/SceneEntry.cs.meta");
            for (int i = 0; i < Scenes.Length; i++)
            {
                string yaml = File.ReadAllText(TestUtil.FindProjectFile("Assets/_Veliqora/Scenes/" + Scenes[i] + ".unity"));
                StringAssert.Contains("guid: " + entryGuid, yaml, Scenes[i] + " must reference SceneEntry");
                StringAssert.Contains("  Screen: " + i + "\n", yaml, Scenes[i] + " must target ScreenId " + i);
            }
        }

        [Test]
        public void BuildSettings_ListAllScenes_InOrder_WithMatchingGuids()
        {
            string build = File.ReadAllText(TestUtil.FindProjectFile("ProjectSettings/EditorBuildSettings.asset"));
            int last = -1;
            foreach (var s in Scenes)
            {
                string path = "Assets/_Veliqora/Scenes/" + s + ".unity";
                int at = build.IndexOf("path: " + path);
                Assert.Greater(at, last, s + " missing or out of order in Build Settings");
                last = at;
                StringAssert.Contains("guid: " + Guid(path + ".meta"), build, s + " guid mismatch");
            }
        }

        [Test]
        public void TwelveDarkBackgrounds_AreConfigured()
        {
            var cfg = Veliqora.Core.Economy.EconomyConfig.FromJson(File.ReadAllText(TestUtil.FindProjectFile(TestUtil.EconomyRelativePath)));
            var scenes = new System.Collections.Generic.HashSet<string>();
            foreach (var c in cfg.Cosmetics)
                if (c.Id.StartsWith("dark_")) scenes.Add(Veliqora.Core.Json.MiniJson.GetString(c.Visual, "scene", ""));
            Assert.AreEqual(12, scenes.Count, "12 distinct dark scenes");
            Assert.IsFalse(scenes.Contains(""));
        }
    }
}
