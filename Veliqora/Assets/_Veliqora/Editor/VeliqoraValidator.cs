using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;
using Veliqora.Core.Economy;
using Veliqora.Core.Levels;
using Veliqora.Core.Solver;
using Debug = UnityEngine.Debug;

namespace Veliqora.EditorTools
{
    /// <summary>Content + project validation and offline level generation menus.</summary>
    public static class VeliqoraValidator
    {
        public const string LevelsPath = "Assets/_Veliqora/Resources/Veliqora/Levels/levels.json";
        public const string EconomyPath = "Assets/_Veliqora/Resources/Veliqora/Config/economy.json";
        public const string AdsPath = "Assets/_Veliqora/Resources/Veliqora/Config/ads.json";
        public const string DocsDir = "Docs";

        [MenuItem("Veliqora/Validate Project", priority = 1)]
        public static void ValidateMenu()
        {
            var problems = ValidateProject(true);
            EditorUtility.DisplayDialog("Veliqora validation", problems.Count == 0 ? "All checks passed." : string.Join("\n", problems), "OK");
        }

        /// <summary>Checks scenes/build settings, content files, configs and placeholder credentials.</summary>
        public static List<string> ValidateProject(bool log)
        {
            var problems = new List<string>();
            // Scenes
            foreach (var id in VeliqoraSetup.SceneOrder)
            {
                string path = VeliqoraSetup.ScenesDir + "/" + id + ".unity";
                if (!File.Exists(path)) problems.Add("Missing scene " + path + " (run Veliqora > Setup Project)");
            }
            var build = EditorBuildSettings.scenes;
            if (build.Length == 0 || !build[0].path.EndsWith("Bootstrap.unity")) problems.Add("Build Settings: Bootstrap must be the first scene.");

            // Content
            if (!File.Exists(LevelsPath)) problems.Add("Missing " + LevelsPath + " (run Veliqora > Levels > Generate)");
            else
            {
                try
                {
                    var cat = LevelCatalog.FromJson(File.ReadAllText(LevelsPath));
                    new LevelRepository(cat);
                    var rows = LevelCatalogBuilder.Validate(cat, rerunSolver: false);
                    int failed = rows.FindAll(r => !r.Passed).Count;
                    if (failed > 0) problems.Add(failed + " level(s) fail replay validation (see Veliqora > Levels > Batch Validate)");
                    if (cat.Levels.Count < 200) problems.Add("Only " + cat.Levels.Count + " levels in catalog (expected 200).");
                }
                catch (System.Exception e) { problems.Add("levels.json invalid: " + e.Message); }
            }
            try { EconomyConfig.FromJson(File.ReadAllText(EconomyPath)); }
            catch (System.Exception e) { problems.Add("economy.json invalid: " + e.Message); }
            if (File.Exists(AdsPath) && File.ReadAllText(AdsPath).Contains("REPLACE_WITH"))
                Debug.Log("[Veliqora] ads.json still has placeholder IDs (expected until real credentials are configured; mock/none mode only).");

            // Player settings
            if (PlayerSettings.defaultInterfaceOrientation != UIOrientation.Portrait) problems.Add("Orientation should be Portrait.");

            if (log)
            {
                foreach (var p in problems) Debug.LogWarning("[Veliqora] " + p);
                if (problems.Count == 0) Debug.Log("[Veliqora] Project validation passed.");
            }
            return problems;
        }

        [MenuItem("Veliqora/Levels/Generate 200 Levels (offline)", priority = 10)]
        public static void GenerateLevels()
        {
            if (!EditorUtility.DisplayDialog("Generate levels", "Regenerate the level catalog from the default seed? Existing levels.json will be overwritten (it is deterministic: same seed → same levels).", "Generate", "Cancel")) return;
            var report = new List<BuildReportRow>();
            var sw = Stopwatch.StartNew();
            try
            {
                var catalog = LevelCatalogBuilder.Build(LevelCatalogBuilder.DefaultBaseSeed, LevelCatalogBuilder.DefaultLevelCount, report,
                    (n, total) => EditorUtility.DisplayProgressBar("Veliqora", "Generating & verifying level " + n + "/" + total, n / (float)total));
                File.WriteAllText(LevelsPath, catalog.ToJson());
                Directory.CreateDirectory(DocsDir);
                File.WriteAllText(Path.Combine(DocsDir, "level_generation_report.csv"), LevelCatalogBuilder.ToCsv(report));
                Debug.Log("[Veliqora] Generated " + catalog.Levels.Count + " verified levels in " + sw.Elapsed.TotalSeconds.ToString("0.0") + "s");
            }
            finally
            {
                EditorUtility.ClearProgressBar();
                AssetDatabase.Refresh();
            }
        }

        [MenuItem("Veliqora/Levels/Batch Validate All Levels", priority = 11)]
        public static void BatchValidate()
        {
            var sw = Stopwatch.StartNew();
            var cat = LevelCatalog.FromJson(File.ReadAllText(LevelsPath));
            EditorUtility.DisplayProgressBar("Veliqora", "Validating " + cat.Levels.Count + " levels (replay + independent solver)…", 0.5f);
            List<BuildReportRow> rows;
            try { rows = LevelCatalogBuilder.Validate(cat, rerunSolver: true, SolverOptions.Validation); }
            finally { EditorUtility.ClearProgressBar(); }
            Directory.CreateDirectory(DocsDir);
            string path = Path.Combine(DocsDir, "level_validation_report.csv");
            File.WriteAllText(path, LevelCatalogBuilder.ToCsv(rows));
            int pass = rows.FindAll(r => r.Passed).Count;
            string msg = pass + "/" + rows.Count + " levels PASS in " + sw.Elapsed.TotalSeconds.ToString("0.0") + "s. Report: " + path;
            if (pass == rows.Count) Debug.Log("[Veliqora] " + msg); else Debug.LogError("[Veliqora] " + msg);
            EditorUtility.DisplayDialog("Batch validation", msg, "OK");
        }
    }
}
