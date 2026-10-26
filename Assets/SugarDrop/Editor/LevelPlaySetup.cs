using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace SugarDrop.EditorTools
{
    /// <summary>
    /// Activează codul reclamelor reale (simbolul SUGARDROP_LEVELPLAY) doar când pachetul Unity LevelPlay
    /// (com.unity.services.levelplay) e instalat, ca proiectul să compileze și fără el. Rulează la deschiderea
    /// proiectului, după instalarea pachetului și în build-ul din cloud (din SugarDropSetup).
    /// </summary>
    [InitializeOnLoad]
    public static class LevelPlaySetup
    {
        public const string Define = "SUGARDROP_LEVELPLAY";
        private const string Package = "com.unity.services.levelplay";

        static LevelPlaySetup() => EditorApplication.delayCall += Apply;

        public static bool PackageInstalled
        {
            get
            {
                if (Directory.Exists("Packages/" + Package)) return true;
                return File.Exists("Packages/manifest.json") && File.ReadAllText("Packages/manifest.json").Contains("\"" + Package + "\"");
            }
        }

        [MenuItem("Sugar Drop/Reclame: verifică LevelPlay")]
        public static void Apply()
        {
            bool installed = PackageInstalled;
            foreach (NamedBuildTarget target in new[] { NamedBuildTarget.Android, NamedBuildTarget.iOS, NamedBuildTarget.Standalone })
            {
                string defines = PlayerSettings.GetScriptingDefineSymbols(target);
                bool has = (";" + defines + ";").Contains(";" + Define + ";");
                if (installed == has) continue;
                defines = installed
                    ? (string.IsNullOrEmpty(defines) ? Define : defines + ";" + Define)
                    : string.Join(";", System.Array.FindAll(defines.Split(';'), d => d != Define));
                PlayerSettings.SetScriptingDefineSymbols(target, defines);
            }
            Debug.Log(installed
                ? "[SugarDrop] LevelPlay instalat: reclamele reale sunt active (dacă cheile sunt completate în SugarAdsConfig)."
                : "[SugarDrop] LevelPlay nu e instalat: jocul folosește reclama de test.");
        }
    }
}
