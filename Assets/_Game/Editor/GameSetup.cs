using UnityEditor;
using UnityEngine;

namespace QuarryEmpire.EditorTools
{
    /// <summary>
    /// Construiește automat tot jocul: setări de proiect, materiale, ScriptableObjects, prefab-uri
    /// și scena Game. Rulează din meniul „Quarry Empire → Construiește jocul” sau automat în build-ul
    /// din cloud (QuarryCloudBuild.PreExport). Valorile de echilibru sunt scrise în asset-urile din Data/
    /// la fiecare rulare, deci aici e „sursa adevărului” cât timp lucrăm fără Editor.
    /// Fișierul e împărțit în mai multe părți (partial class): Project, Data, Prefabs, Scene, Ui, Utils.
    /// </summary>
    public static partial class GameSetup
    {
        private const string Root = "Assets/_Game";
        private const string MaterialsDir = Root + "/Art/Materials";
        private const string DataDir = Root + "/Data";
        private const string PrefabsDir = Root + "/Prefabs";
        private const string ScenesDir = Root + "/Scenes";
        private const string SettingsDir = Root + "/Settings";
        public const string ScenePath = ScenesDir + "/Game.unity";

        private const int UiLayer = 5;
        private const int BlockLayer = 6;
        private const int ResourceLayer = 7;

        [MenuItem("Quarry Empire/Construiește jocul")]
        public static void Run()
        {
            EnsureFolders();
            SetupLayers();
            SetupRenderPipeline();
            SetupPlayerSettings();

            Materials materials = CreateMaterials();
            GameData data = CreateData(materials);
            Prefabs prefabs = CreatePrefabs(materials);
            BuildScene(materials, data, prefabs);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[QuarryEmpire] Jocul a fost construit: " + ScenePath);
        }
    }
}

/// <summary>
/// Punctul de intrare pentru Unity Build Automation (câmpul „Pre-Export Method”: QuarryCloudBuild.PreExport).
/// </summary>
public static class QuarryCloudBuild
{
    public static void PreExport() => QuarryEmpire.EditorTools.GameSetup.Run();
}
