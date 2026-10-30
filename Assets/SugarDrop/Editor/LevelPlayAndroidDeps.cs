#if UNITY_ANDROID
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
            Patch(Path.Combine(path, "build.gradle"), true);
            // kotlin-stdlib 1.8+ conține deja jdk7/jdk8; versiunile vechi aduse separat dau „Duplicate class”
            // la checkReleaseDuplicateClasses, iar excluderea trebuie pusă și în launcher (consumatorul final).
            string launcher = Path.Combine(Path.GetDirectoryName(path) ?? path, "launcher", "build.gradle");
            Patch(launcher, false);
            AddAdIdPermission(Path.Combine(path, "src", "main", "AndroidManifest.xml"));
        }

        /// <summary>
        /// De la Android 13 (țintă API 33+), ID-ul de publicitate cere permisiunea AD_ID în manifest; fără ea, rețelele de
        /// reclame primesc doar zerouri (mai puține reclame, plăți mai mici). Google Play o cere și în „Data safety”.
        /// </summary>
        private static void AddAdIdPermission(string manifest)
        {
            const string permission = "com.google.android.gms.permission.AD_ID";
            if (!File.Exists(manifest)) return;
            string text = File.ReadAllText(manifest);
            if (text.Contains(permission)) return;
            int application = text.IndexOf("<application", System.StringComparison.Ordinal);
            if (application < 0) return;
            text = text.Insert(application, "<uses-permission android:name=\"" + permission + "\" />\n    ");
            File.WriteAllText(manifest, text);
            Debug.Log("[SugarDrop] Adăugat în manifest: " + permission);
        }

        private static void Patch(string gradle, bool addLibrary)
        {
            if (!File.Exists(gradle))
            {
                Debug.LogWarning("[SugarDrop] Lipsește " + gradle + "; modificarea LevelPlay nu a fost aplicată.");
                return;
            }
            string text = File.ReadAllText(gradle);
            if (text.Contains(Marker)) return;
            text += "\n" + Marker + "\n";
            if (addLibrary) text += "dependencies {\n    implementation '" + Dependency + "'\n}\n";
            text += "configurations.all {\n    exclude group: 'org.jetbrains.kotlin', module: 'kotlin-stdlib-jdk7'\n"
                  + "    exclude group: 'org.jetbrains.kotlin', module: 'kotlin-stdlib-jdk8'\n}\n";
            File.WriteAllText(gradle, text);
            Debug.Log("[SugarDrop] Gradle modificat: " + gradle);
        }
    }
}
#endif
