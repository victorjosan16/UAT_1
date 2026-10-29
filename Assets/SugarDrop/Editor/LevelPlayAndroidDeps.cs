using System.IO;
using UnityEditor;
using UnityEditor.Android;
using UnityEngine;

namespace SugarDrop.EditorTools
{
    /// <summary>
    /// Pachetul Unity LevelPlay aduce doar puntea C#/Java; biblioteca nativă Android (mediation-sdk) o aduce în mod
    /// normal Mobile Dependency Resolver, care nu rulează în build-ul din cloud (fără ferestre). De aceea o adăugăm
    /// direct în Gradle, după ce Unity generează proiectul Android. Fără ea, Java nu găsește
    /// com.ironsource.mediationsdk / com.unity3d.mediation și build-ul pică la compileReleaseJavaWithJavac.
    /// </summary>
    public sealed class LevelPlayAndroidDeps : IPostGenerateGradleAndroidProject
    {
        private const string Dependency = "com.unity3d.ads-mediation:mediation-sdk:9.5.0";
        private const string Marker = "// SugarDrop: LevelPlay native";

        public int callbackOrder => 100;

        public void OnPostGenerateGradleAndroidProject(string path)
        {
            if (!LevelPlaySetup.PackageInstalled) return;
            string gradle = Path.Combine(path, "build.gradle");
            if (!File.Exists(gradle))
            {
                Debug.LogWarning("[SugarDrop] build.gradle lipsește în " + path + "; biblioteca LevelPlay nu a fost adăugată.");
                return;
            }
            string text = File.ReadAllText(gradle);
            if (text.Contains("com.unity3d.ads-mediation:mediation-sdk") || text.Contains(Marker)) return;
            text += "\n" + Marker + "\ndependencies {\n    implementation '" + Dependency + "'\n}\n";
            File.WriteAllText(gradle, text);
            Debug.Log("[SugarDrop] Adăugat în Gradle: " + Dependency);
        }
    }
}
