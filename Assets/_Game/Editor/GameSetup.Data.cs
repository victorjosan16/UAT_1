using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

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

            var colors = new (string key, string hex, float smoothness)[]
            {
                ("Ground", "#C9A574", 0.05f), ("QuarryFloor", "#8A6E50", 0.05f), ("Wall", "#A88B63", 0.05f),
                ("Player", "#2F80ED", 0.2f), ("Skin", "#F2C9A0", 0.2f), ("Pickaxe", "#5A3E2B", 0.1f),
                ("Worker", "#F2994A", 0.2f), ("Helmet", "#F2C94C", 0.4f),
                ("Bulldozer", "#F2C94C", 0.35f), ("Drill", "#EB5757", 0.35f), ("Truck", "#27AE60", 0.35f),
                ("OrderTruck", "#2D9CDB", 0.35f), ("Dark", "#3A3A3A", 0.2f), ("Metal", "#BDBDBD", 0.6f),
                ("Building", "#6D6875", 0.1f), ("Coin", "#FFC83D", 0.7f),
                ("PadSell", "#3CC060", 0.1f), ("PadUpgrade", "#9B51E0", 0.1f), ("PadBuy", "#F2994A", 0.1f),
                ("PadRefine", "#56CCF2", 0.1f), ("PadOrder", "#2D9CDB", 0.1f), ("PadLoad", "#6FCF97", 0.1f),
                // Resurse brute
                ("Stone", "#8C8F96", 0.1f), ("Copper", "#C8743A", 0.2f), ("Iron", "#5E6E86", 0.2f),
                ("Gold", "#F2C230", 0.4f), ("Diamond", "#5FE1F0", 0.8f),
                // Resurse rafinate
                ("Brick", "#B5523B", 0.1f), ("CopperIngot", "#E89B5E", 0.7f), ("IronIngot", "#AEB8C6", 0.7f),
                ("GoldIngot", "#FFD54A", 0.8f), ("CutDiamond", "#B8F7FF", 0.95f),
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

            // Material pentru particule (nu e afectat de lumini).
            Shader particles = Shader.Find("Universal Render Pipeline/Particles/Unlit") ?? lit;
            string particlePath = $"{MaterialsDir}/M_Particle.mat";
            var particleMaterial = AssetDatabase.LoadAssetAtPath<Material>(particlePath);
            if (particleMaterial == null)
            {
                particleMaterial = new Material(particles);
                AssetDatabase.CreateAsset(particleMaterial, particlePath);
            }
            particleMaterial.SetColor("_BaseColor", Color.white);
            result["Particle"] = particleMaterial;
            return result;
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
            Edit(theme, so =>
            {
                P(so, "PanelSprite").objectReferenceValue = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/UISprite.psd");
                P(so, "CircleSprite").objectReferenceValue = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            });
            return theme;
        }
    }
}
