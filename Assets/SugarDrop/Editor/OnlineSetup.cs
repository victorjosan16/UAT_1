using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace SugarDrop.EditorTools
{
    /// <summary>
    /// Activează codul serviciilor doar când pachetul lor e instalat, ca proiectul să compileze și fără ele:
    /// clasamentul (SUGARDROP_UGS, com.unity.services.leaderboards), salvarea în cloud (SUGARDROP_CLOUDSAVE,
    /// com.unity.services.cloudsave) și notificările (SUGARDROP_NOTIFICATIONS, com.unity.mobile.notifications). Dacă în
    /// <see cref="SugarOnlineConfig.ProjectId"/> e completat un Project ID, leagă proiectul de el.
    /// Rulează la deschiderea proiectului și în build-ul din cloud (din SugarDropSetup).
    /// </summary>
    [InitializeOnLoad]
    public static class OnlineSetup
    {
        public const string Define = "SUGARDROP_UGS";
        private const string Package = "com.unity.services.leaderboards";

        private static readonly (string Package, string Define)[] Optional =
        {
            (Package, Define),
            ("com.unity.services.cloudsave", "SUGARDROP_CLOUDSAVE"),
            ("com.unity.mobile.notifications", "SUGARDROP_NOTIFICATIONS"),
        };

        static OnlineSetup() => EditorApplication.delayCall += Apply;

        public static bool PackageInstalled => Installed(Package);

        private static bool Installed(string package)
        {
            if (Directory.Exists("Packages/" + package)) return true;
            return File.Exists("Packages/manifest.json") && File.ReadAllText("Packages/manifest.json").Contains("\"" + package + "\"");
        }

        [MenuItem("Sugar Drop/Clasament: verifică Unity Leaderboards")]
        public static void Apply()
        {
            bool installed = PackageInstalled;
            foreach ((string package, string define) in Optional)
            {
                bool present = Installed(package);
                foreach (NamedBuildTarget target in new[] { NamedBuildTarget.Android, NamedBuildTarget.iOS, NamedBuildTarget.Standalone })
                {
                    string defines = PlayerSettings.GetScriptingDefineSymbols(target);
                    bool has = (";" + defines + ";").Contains(";" + define + ";");
                    if (present == has) continue;
                    defines = present
                        ? (string.IsNullOrEmpty(defines) ? define : defines + ";" + define)
                        : string.Join(";", System.Array.FindAll(defines.Split(';'), d => d != define));
                    PlayerSettings.SetScriptingDefineSymbols(target, defines);
                }
            }
            LinkProject();
            Debug.Log(installed
                ? "[SugarDrop] Unity Leaderboards instalat: clasamentul online e activ (proiect: " + Linked + ")."
                : "[SugarDrop] Unity Leaderboards nu e instalat: clasamentul arată doar scorul local.");
        }

        private static string Linked => string.IsNullOrEmpty(CloudProjectSettings.projectId) ? "nelegat" : CloudProjectSettings.projectId;

        /// <summary>Scrie Project ID-ul din SugarOnlineConfig în setările proiectului (doar dacă e completat și diferit).</summary>
        private static void LinkProject()
        {
            string id = SugarOnlineConfig.ProjectId;
            if (string.IsNullOrEmpty(id) || CloudProjectSettings.projectId == id) return;
            Object[] assets = AssetDatabase.LoadAllAssetsAtPath("ProjectSettings/ProjectSettings.asset");
            if (assets.Length == 0) return;
            var so = new SerializedObject(assets[0]);
            SerializedProperty property = so.FindProperty("cloudProjectId");
            if (property == null)
            {
                Debug.LogWarning("[SugarDrop] Nu am găsit câmpul cloudProjectId în setările proiectului.");
                return;
            }
            property.stringValue = id;
            so.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.SaveAssets();
        }
    }
}
