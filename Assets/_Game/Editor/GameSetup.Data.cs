using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;

namespace QuarryEmpire.EditorTools
{
    /// <summary>Materiale și ScriptableObjects (toate valorile de echilibru ale jocului).</summary>
    public static partial class GameSetup
    {
        private class Materials : Dictionary<string, Material> { }

        private class GameData
        {
            public readonly Dictionary<string, ResourceData> Resources = new();
            public readonly Dictionary<string, BlockData> Blocks = new();
            public readonly List<LayerDefinition> Layers = new();
            public readonly List<UpgradeDefinition> Upgrades = new();
            public VehicleDefinition Bulldozer;
            public VehicleDefinition Drill;
            public WorkerDefinition Worker;
            public OrderSettings Orders;
            public PlayerStats PlayerStats;
            public UiTheme Theme;
        }

        // ------------------------------------------------------------ Materiale

        private static Materials CreateMaterials()
        {
            Shader lit = Shader.Find("Universal Render Pipeline/Lit");
            if (lit == null) throw new System.Exception("Nu găsesc shader-ul URP Lit. Este instalat pachetul URP?");

            // Paletă caldă, armonioasă, materiale mate (netezime mică) — stil „premium stylized”.
            var colors = new (string key, string hex, float smoothness)[]
            {
                // Mediu
                ("Ground", "#E2C08D", 0.04f), ("SandDark", "#D3AE76", 0.04f), ("SandLight", "#EDD3A4", 0.04f),
                ("QuarryFloor", "#8A6E50", 0.04f), ("Wall", "#A88B63", 0.05f), ("PathStone", "#C7AC86", 0.06f),
                ("Rock1", "#9D9082", 0.08f), ("Rock2", "#857869", 0.08f), ("Leaves", "#6DB35D", 0.05f),
                ("LeavesDark", "#4E9450", 0.05f), ("Trunk", "#7A5536", 0.05f), ("Wood", "#B98250", 0.08f),
                ("Barrel", "#C8553D", 0.15f), ("Lamp", "#FFF1B8", 0.2f), ("Building", "#7B7287", 0.08f),
                ("Roof", "#C8553D", 0.1f), ("Glass", "#9ED8F0", 0.5f),
                // Personaje
                ("Player", "#3D7BE0", 0.12f), ("Worker", "#F2994A", 0.12f), ("Overalls", "#2E3A59", 0.1f),
                ("Skin", "#F2C9A0", 0.15f), ("Gloves", "#8A5A3C", 0.1f), ("Boots", "#4A3A2E", 0.1f),
                ("Helmet", "#FFC93C", 0.3f), ("Backpack", "#B5652B", 0.1f), ("Eyes", "#2A2A33", 0.4f),
                ("Pickaxe", "#7A5232", 0.1f), ("Metal", "#C9CED6", 0.45f), ("Dark", "#3A3A42", 0.15f),
                // Utilaje
                ("Bulldozer", "#F5B83D", 0.3f), ("Drill", "#E8574C", 0.3f), ("Truck", "#3DBE7A", 0.3f), ("OrderTruck", "#3D8FE0", 0.3f),
                // Zone și economie
                ("Coin", "#FFC83D", 0.55f), ("PadInner", "#F6EBD9", 0.05f),
                ("PadSell", "#3DC46A", 0.1f), ("PadUpgrade", "#9B6BF0", 0.1f), ("PadBuy", "#F59A3D", 0.1f),
                ("PadRefine", "#4DC3F0", 0.1f), ("PadOrder", "#3D8FE0", 0.1f), ("PadLoad", "#6FCF97", 0.1f),
                // Resurse brute
                ("Stone", "#9A9CA3", 0.08f), ("Copper", "#D07A3E", 0.2f), ("Iron", "#667A96", 0.2f),
                ("Gold", "#F7C531", 0.4f), ("Diamond", "#62E3F2", 0.6f),
                // Resurse rafinate
                ("Brick", "#B5523B", 0.08f), ("CopperIngot", "#EB9E62", 0.5f), ("IronIngot", "#B4BFCC", 0.5f),
                ("GoldIngot", "#FFD54A", 0.6f), ("CutDiamond", "#B8F7FF", 0.8f),
            };

            var result = new Materials();
            foreach (var (key, hex, smoothness) in colors)
            {
                string path = $"{MaterialsDir}/M_{key}.mat";
                var material = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (material == null)
                {
                    material = new Material(lit);
                    AssetDatabase.CreateAsset(material, path);
                }
                material.shader = lit;
                material.SetColor("_BaseColor", Hex(hex));
                material.SetFloat("_Smoothness", smoothness);
                EditorUtility.SetDirty(material);
                result[key] = material;
            }

            // Materiale pentru particule (nu sunt afectate de lumini):
            //  - Particle: cu „alpha clip” (discuri rotunde, fără amestec transparent — mai ieftin, fără overdraw costisitor);
            //  - ParticleSolid: opac, pentru bucățile de rocă 3D.
            Shader particles = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? lit;
            Material particle = LoadOrCreateMaterial($"{MaterialsDir}/M_Particle.mat", particles);
            particle.SetColor("_BaseColor", Color.white);
            if (particle.HasProperty("_AlphaClip"))
            {
                particle.SetFloat("_AlphaClip", 1f);
                particle.SetFloat("_Cutoff", 0.4f);
                particle.EnableKeyword("_ALPHATEST_ON");
            }
            EditorUtility.SetDirty(particle);
            result["Particle"] = particle;

            Material solid = LoadOrCreateMaterial($"{MaterialsDir}/M_ParticleSolid.mat", particles);
            solid.SetColor("_BaseColor", Color.white);
            EditorUtility.SetDirty(solid);
            result["ParticleSolid"] = solid;
            return result;
        }

        private static Material LoadOrCreateMaterial(string path, Shader shader)
        {
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        // ------------------------------------------------------------ Date

        private static GameData CreateData(Materials m)
        {
            var data = new GameData();

            // Resurse rafinate (create primele, ca să le putem lega de cele brute).
            Resource(data, m, "Brick", "brick", "Cărămidă", 3, null, 1f);
            Resource(data, m, "CopperIngot", "copper_ingot", "Lingou de cupru", 8, null, 1f);
            Resource(data, m, "IronIngot", "iron_ingot", "Lingou de fier", 16, null, 1f);
            Resource(data, m, "GoldIngot", "gold_ingot", "Lingou de aur", 40, null, 1f);
            Resource(data, m, "CutDiamond", "cut_diamond", "Diamant șlefuit", 140, null, 1f);

            // Resurse brute: preț, în ce se rafinează, secunde de rafinare.
            Resource(data, m, "Stone", "stone", "Piatră", 1, data.Resources["Brick"], 1.2f);
            Resource(data, m, "Copper", "copper", "Cupru", 3, data.Resources["CopperIngot"], 2f);
            Resource(data, m, "Iron", "iron", "Fier", 6, data.Resources["IronIngot"], 2.5f);
            Resource(data, m, "Gold", "gold", "Aur", 15, data.Resources["GoldIngot"], 3f);
            Resource(data, m, "Diamond", "diamond", "Diamant", 50, data.Resources["CutDiamond"], 4f);

            // Blocuri: viață, duritate, drop minim/maxim.
            Block(data, m, "Stone", "Bloc de piatră", 3f, 1, 1, 2);
            Block(data, m, "Copper", "Bloc de cupru", 5f, 1, 1, 2);
            Block(data, m, "Iron", "Bloc de fier", 8f, 2, 1, 2);
            Block(data, m, "Gold", "Bloc de aur", 12f, 2, 1, 3);
            Block(data, m, "Diamond", "Bloc de diamant", 20f, 3, 1, 1);

            // Straturi: nume, adâncime, mărime grilă, multiplicator viață, culoare podea, ponderi blocuri.
            Layer(data, 1, "Suprafață", 10, 8, 1f, "#8A6E50", ("Stone", 10f), ("Copper", 4f), ("Iron", 1f));
            Layer(data, 2, "Lut roșu", 20, 8, 1.3f, "#7A5440", ("Stone", 8f), ("Copper", 5f), ("Iron", 3f), ("Gold", 0.5f));
            Layer(data, 3, "Rocă", 35, 9, 1.7f, "#5E5A55", ("Stone", 6f), ("Copper", 4f), ("Iron", 4f), ("Gold", 1.5f), ("Diamond", 0.3f));
            Layer(data, 4, "Filonul de aur", 50, 9, 2.2f, "#4D4A47", ("Stone", 5f), ("Copper", 3f), ("Iron", 4f), ("Gold", 3f), ("Diamond", 0.6f));
            Layer(data, 5, "Peșteri adânci", 70, 10, 3f, "#3B3A40", ("Stone", 4f), ("Copper", 2f), ("Iron", 4f), ("Gold", 3f), ("Diamond", 1.2f));
            Layer(data, 6, "Miezul de cristal", 100, 10, 4f, "#2B2E3F", ("Stone", 3f), ("Iron", 3f), ("Gold", 4f), ("Diamond", 2.5f));

            // Upgrade-uri: id, nume, tip, nivel maxim, cost de bază, multiplicator cost, bonus pe nivel.
            Upgrade(data, "power", "Forță", UpgradeType.MiningPower, 25, 30, 1.55f, 0.5f);
            Upgrade(data, "hit_speed", "Viteză lovire", UpgradeType.MiningSpeed, 15, 40, 1.6f, 0.04f);
            Upgrade(data, "move_speed", "Viteză mers", UpgradeType.MoveSpeed, 12, 25, 1.5f, 0.35f);
            Upgrade(data, "capacity", "Capacitate", UpgradeType.Capacity, 20, 35, 1.5f, 4f);
            Upgrade(data, "pickaxe", "Târnăcop", UpgradeType.PickaxeTier, 2, 1500, 4f, 1f);

            data.PlayerStats = LoadOrCreate<PlayerStats>($"{DataDir}/PlayerStats.asset");
            Edit(data.PlayerStats, so =>
            {
                P(so, "MoveSpeed").floatValue = 4.5f;
                P(so, "RotationSpeed").floatValue = 720f;
                P(so, "HitDamage").floatValue = 1f;
                P(so, "HitInterval").floatValue = 0.45f;
                P(so, "HitRange").floatValue = 1f;
                P(so, "MiningTier").intValue = 2;
                P(so, "CollectRadius").floatValue = 2.5f;
                P(so, "StackCapacity").intValue = 10;
            });

            data.Bulldozer = Vehicle("Bulldozer", "Buldozer", 4.5f, 160f, 2.5f, 0.45f, 2, 1.7f, new Vector3(2.6f, 1f, 1.4f));
            data.Drill = Vehicle("Drill", "Burghiu", 3.5f, 200f, 7f, 0.3f, 4, 1.5f, new Vector3(0.9f, 1f, 1f));

            data.Worker = LoadOrCreate<WorkerDefinition>($"{DataDir}/Worker_Miner.asset");
            Edit(data.Worker, so =>
            {
                P(so, "DisplayName").stringValue = "Muncitor";
                P(so, "MoveSpeed").floatValue = 3f;
                P(so, "HitDamage").floatValue = 1f;
                P(so, "HitInterval").floatValue = 0.6f;
                P(so, "MiningTier").intValue = 2;
                P(so, "Capacity").intValue = 8;
                P(so, "CollectRadius").floatValue = 2f;
                P(so, "OfflineIncomePerMinute").floatValue = 25f;
            });

            data.Orders = CreateOrders(data);
            data.Theme = CreateTheme();
            return data;
        }

        private static void Resource(GameData data, Materials m, string key, string id, string displayName,
            int price, ResourceData refinedInto, float refineSeconds)
        {
            var asset = LoadOrCreate<ResourceData>($"{DataDir}/Resource_{key}.asset");
            Material material = m[key];
            Edit(asset, so =>
            {
                P(so, "Id").stringValue = id;
                P(so, "DisplayName").stringValue = displayName;
                P(so, "UiColor").colorValue = material.GetColor("_BaseColor");
                P(so, "PickupMaterial").objectReferenceValue = material;
                P(so, "SellPrice").intValue = price;
                P(so, "RefinedInto").objectReferenceValue = refinedInto;
                P(so, "RefineSeconds").floatValue = refineSeconds;
            });
            data.Resources[key] = asset;
        }

        private static void Block(GameData data, Materials m, string key, string displayName, float health,
            int hardness, int minDrops, int maxDrops)
        {
            var asset = LoadOrCreate<BlockData>($"{DataDir}/Block_{key}.asset");
            Edit(asset, so =>
            {
                P(so, "DisplayName").stringValue = displayName;
                P(so, "BlockMaterial").objectReferenceValue = m[key];
                P(so, "MaxHealth").floatValue = health;
                P(so, "Hardness").intValue = hardness;
                P(so, "DroppedResource").objectReferenceValue = data.Resources[key];
                P(so, "MinDrops").intValue = minDrops;
                P(so, "MaxDrops").intValue = maxDrops;
            });
            data.Blocks[key] = asset;
        }

        private static void Layer(GameData data, int number, string displayName, int depthMeters, int size,
            float healthMultiplier, string floorColor, params (string block, float weight)[] weights)
        {
            var asset = LoadOrCreate<LayerDefinition>($"{DataDir}/Layer_{number:00}.asset");
            Edit(asset, so =>
            {
                P(so, "DisplayName").stringValue = displayName;
                P(so, "DepthMeters").intValue = depthMeters;
                P(so, "Width").intValue = size;
                P(so, "Depth").intValue = size;
                P(so, "HealthMultiplier").floatValue = healthMultiplier;
                P(so, "FloorColor").colorValue = Hex(floorColor);
                SerializedProperty list = P(so, "blocks");
                list.arraySize = weights.Length;
                for (int i = 0; i < weights.Length; i++)
                {
                    SerializedProperty element = list.GetArrayElementAtIndex(i);
                    element.FindPropertyRelative("block").objectReferenceValue = data.Blocks[weights[i].block];
                    element.FindPropertyRelative("weight").floatValue = weights[i].weight;
                }
            });
            data.Layers.Add(asset);
        }

        private static void Upgrade(GameData data, string id, string displayName, UpgradeType type, int maxLevel,
            long baseCost, float costMultiplier, float valuePerLevel)
        {
            var asset = LoadOrCreate<UpgradeDefinition>($"{DataDir}/Upgrade_{id}.asset");
            Edit(asset, so =>
            {
                P(so, "Id").stringValue = id;
                P(so, "DisplayName").stringValue = displayName;
                P(so, "Type").enumValueIndex = (int)type;
                P(so, "MaxLevel").intValue = maxLevel;
                P(so, "BaseCost").longValue = baseCost;
                P(so, "CostMultiplier").floatValue = costMultiplier;
                P(so, "ValuePerLevel").floatValue = valuePerLevel;
            });
            data.Upgrades.Add(asset);
        }

        private static VehicleDefinition Vehicle(string key, string displayName, float speed, float turn, float damage,
            float interval, int tier, float reach, Vector3 toolSize)
        {
            var asset = LoadOrCreate<VehicleDefinition>($"{DataDir}/Vehicle_{key}.asset");
            Edit(asset, so =>
            {
                P(so, "DisplayName").stringValue = displayName;
                P(so, "MoveSpeed").floatValue = speed;
                P(so, "TurnSpeed").floatValue = turn;
                P(so, "HitDamage").floatValue = damage;
                P(so, "HitInterval").floatValue = interval;
                P(so, "MiningTier").intValue = tier;
                P(so, "ToolReach").floatValue = reach;
                P(so, "ToolSize").vector3Value = toolSize;
            });
            return asset;
        }

        private static OrderSettings CreateOrders(GameData data)
        {
            var asset = LoadOrCreate<OrderSettings>($"{DataDir}/OrderSettings.asset");
            // resursă, strat minim, cantitate minimă, cantitate maximă
            var options = new (string key, int minLayer, int min, int max)[]
            {
                ("Stone", 0, 6, 12), ("Copper", 0, 4, 10), ("Iron", 1, 4, 8), ("Gold", 2, 3, 6), ("Diamond", 3, 2, 4),
                ("Brick", 1, 4, 8), ("CopperIngot", 1, 3, 6), ("IronIngot", 2, 3, 6), ("GoldIngot", 3, 2, 4),
            };
            Edit(asset, so =>
            {
                SerializedProperty list = P(so, "Options");
                list.arraySize = options.Length;
                for (int i = 0; i < options.Length; i++)
                {
                    SerializedProperty element = list.GetArrayElementAtIndex(i);
                    element.FindPropertyRelative("resource").objectReferenceValue = data.Resources[options[i].key];
                    element.FindPropertyRelative("minLayer").intValue = options[i].minLayer;
                    element.FindPropertyRelative("minAmount").intValue = options[i].min;
                    element.FindPropertyRelative("maxAmount").intValue = options[i].max;
                }
                P(so, "FirstOrderDelay").floatValue = 40f;
                P(so, "TimeBetweenOrders").floatValue = 50f;
                P(so, "BaseTimeLimit").floatValue = 30f;
                P(so, "SecondsPerItem").floatValue = 6f;
                P(so, "RewardMultiplier").floatValue = 3f;
            });
            return asset;
        }

        private static UiTheme CreateTheme()
        {
            var theme = LoadOrCreate<UiTheme>($"{DataDir}/UiTheme.asset");
            var font = AssetDatabase.LoadAssetAtPath<Font>($"{FontsDir}/Baloo2-ExtraBold.ttf");
            TMP_FontAsset tmpFont = CreateTmpFont(font);
            if (tmpFont != null) EnsureTmpSettings(tmpFont);

            Edit(theme, so =>
            {
                P(so, "Font").objectReferenceValue = font;
                P(so, "TmpFont").objectReferenceValue = tmpFont;
                // Gol = sprite-uri rotunjite generate din cod (ProceduralSprites).
                P(so, "PanelSprite").objectReferenceValue = null;
                P(so, "CircleSprite").objectReferenceValue = null;
            });
            return theme;
        }

        /// <summary>Caracterele pre-generate în atlasul TMP (ASCII + diacritice românești + câteva simboluri).</summary>
        private const string TmpCharacters =
            " !\"#$%&'()*+,-./0123456789:;<=>?@ABCDEFGHIJKLMNOPQRSTUVWXYZ[\\]^_`abcdefghijklmnopqrstuvwxyz{|}~" +
            "ĂÂÎȘȚăâîșțŞŢşţ·…×–—„”“’«»°€";

        /// <summary>
        /// Creează fontul TextMeshPro din TTF, cu toate caracterele necesare deja în atlas (static),
        /// ca textul să nu depindă de generare la rulare. Dacă TMP nu are shader-ele disponibile în proiect,
        /// întoarce null și jocul folosește textul clasic (cu același font TTF).
        /// </summary>
        private static TMP_FontAsset CreateTmpFont(Font font)
        {
            string path = $"{FontsDir}/Baloo2_SDF.asset";
            var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(path);
            if (existing != null) return existing;
            if (font == null) return null;
            if (Shader.Find("TextMeshPro/Mobile/Distance Field") == null)
            {
                Debug.LogWarning("[QuarryEmpire] Shader-ul TextMeshPro nu e disponibil; se folosește textul clasic.");
                return null;
            }

            try
            {
                TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(font, 56, 6, GlyphRenderMode.SDFAA, 1024, 1024,
                    AtlasPopulationMode.Dynamic, false);
                if (asset == null) return null;
                asset.name = "Baloo2_SDF";
                asset.TryAddCharacters(TmpCharacters, out string missing);
                if (!string.IsNullOrEmpty(missing)) Debug.Log("[QuarryEmpire] Caractere lipsă din font: " + missing);
                asset.atlasPopulationMode = AtlasPopulationMode.Static;

                AssetDatabase.CreateAsset(asset, path);
                foreach (Texture2D atlas in asset.atlasTextures)
                {
                    if (atlas == null) continue;
                    atlas.name = "Baloo2_SDF Atlas";
                    AssetDatabase.AddObjectToAsset(atlas, asset);
                }
                asset.material.name = "Baloo2_SDF Material";
                AssetDatabase.AddObjectToAsset(asset.material, asset);
                EditorUtility.SetDirty(asset);
                AssetDatabase.SaveAssets();
                return asset;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[QuarryEmpire] Nu am putut crea fontul TMP, se folosește textul clasic: " + e.Message);
                AssetDatabase.DeleteAsset(path);
                return null;
            }
        }

        /// <summary>
        /// TMP caută „TMP Settings” în Resources. Dacă proiectul nu le are (fără „TMP Essentials”), le creăm
        /// cu fontul nostru implicit, încadrare de text activă și regulile de rupere a rândurilor.
        /// </summary>
        private static void EnsureTmpSettings(TMP_FontAsset font)
        {
            if (Resources.Load<TMP_Settings>("TMP Settings") != null) return;

            TextAsset leading = CreateTextAsset("TMP LeadingCharacters.txt", "([{\u00ab\u201e\u201c$");
            TextAsset following = CreateTextAsset("TMP FollowingCharacters.txt", ")]}.,;:!?%\u00bb\u201d\u2026");

            var settings = ScriptableObject.CreateInstance<TMP_Settings>();
            AssetDatabase.CreateAsset(settings, $"{ResourcesDir}/TMP Settings.asset");
            var so = new SerializedObject(settings);
            SetIfExists(so, "m_defaultFontAsset", p => p.objectReferenceValue = font);
            SetIfExists(so, "m_leadingCharacters", p => p.objectReferenceValue = leading);
            SetIfExists(so, "m_followingCharacters", p => p.objectReferenceValue = following);
            SetIfExists(so, "m_enableWordWrapping", p => p.boolValue = true);
            SetIfExists(so, "m_TextWrappingMode", p => p.intValue = 1); // TextWrappingModes.Normal (versiunile noi)
            SetIfExists(so, "m_enableKerning", p => p.boolValue = true);
            SetIfExists(so, "m_enableParseEscapeCharacters", p => p.boolValue = true);
            SetIfExists(so, "m_defaultFontSize", p => p.floatValue = 36f);
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
        }

        private static void SetIfExists(SerializedObject so, string name, System.Action<SerializedProperty> apply)
        {
            SerializedProperty property = so.FindProperty(name);
            if (property != null) apply(property);
        }

        private static TextAsset CreateTextAsset(string fileName, string content)
        {
            string path = $"{ResourcesDir}/{fileName}";
            System.IO.File.WriteAllText(path, content, new System.Text.UTF8Encoding(false));
            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<TextAsset>(path);
        }
    }
}
