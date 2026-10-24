using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace QuarryEmpire.EditorTools
{
    /// <summary>
    /// Construiește scena Game: lumea, sistemele, cariera, jucătorul, zonele, utilajele, muncitorii și camera.
    /// Harta (vedere de sus, Z = „în sus” pe ecran):
    ///   cariera în centru-sus; VINDE stânga; rafinăria dreapta; comenzile stânga-sus;
    ///   muncitorii stânga; upgrade-urile pe un rând sub start; magazinul de utilaje jos.
    /// </summary>
    public static partial class GameSetup
    {
        private static readonly Vector3 PlayerStart = new(0f, 0.05f, -1f);
        private static readonly Vector3 QuarryCenter = new(0f, 0f, 9f);
        private static readonly Vector3 SellPosition = new(-6f, 0f, -3f);
        private static readonly Vector3 CameraOffset = new(-8.2f, 13.8f, -8.2f);
        private const float LabelHeight = 2.3f;
        /// <summary>Jumătate din latura inelului de roci din jurul carierei (grila maximă are 10×10 blocuri).</summary>
        private const float RimHalf = 6.9f;
        /// <summary>Poarta carierei (spre sud), în coordonate locale față de centrul carierei.</summary>
        private static readonly Vector3 GateInsideLocal = new(0f, 0f, -5.7f);
        private static readonly Vector3 GateOutsideLocal = new(0f, 0f, -8.2f);

        private static void BuildScene(Scene scene, Materials m, GameData data, Prefabs prefabs)
        {
            BuildLighting();
            BuildWorld(m);
            BuildQuarry(m, data, prefabs);
            Transform player = BuildPlayer(m, data, out Transform playerModel);
            BuildSystems(m, data, prefabs, playerModel);
            ZoneRefs zones = BuildZones(m, data);
            BuildTutorial(m, zones);
            BuildCamera(player);
            BuildUi(data.Theme);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        // ------------------------------------------------------------ Lumină

        private static void BuildLighting()
        {
            // Lumină caldă de după-amiază, umbre moi; ambient în trei tonuri (cer / orizont / sol).
            var sun = new GameObject("Directional Light");
            var light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = new Color(1f, 0.94f, 0.84f);
            light.intensity = 1.25f;
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 0.65f;
            sun.transform.rotation = Quaternion.Euler(48f, -35f, 0f);
            RenderSettings.sun = light;

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.72f, 0.8f, 0.92f);
            RenderSettings.ambientEquatorColor = new Color(0.86f, 0.8f, 0.7f);
            RenderSettings.ambientGroundColor = new Color(0.55f, 0.48f, 0.4f);

            // Ceață liniară discretă: dă adâncime și ascunde marginea hărții (aproape gratis pe mobil).
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Hex("#A9D6EE");
            RenderSettings.fogStartDistance = 30f;
            RenderSettings.fogEndDistance = 70f;
        }

        // ------------------------------------------------------------ Lume

        private static void BuildWorld(Materials m)
        {
            GameObject world = new GameObject("World");
            Transform w = world.transform;
            Primitive(PrimitiveType.Plane, "Ground", w, Vector3.zero, new Vector3(6f, 1f, 6f), m["Ground"], keepCollider: true);

            // Pete de nisip în tonuri apropiate, ca solul să nu fie uniform.
            var random = new System.Random(7);
            for (int i = 0; i < 16; i++)
            {
                Vector3 position = RandomOutside(random, 24f, 7f);
                float radius = 2f + (float)random.NextDouble() * 3.5f;
                Primitive(PrimitiveType.Cylinder, "SandPatch", w, new Vector3(position.x, 0.004f + i * 0.0004f, position.z),
                    new Vector3(radius, 0.002f, radius * 0.8f), i % 2 == 0 ? m["SandDark"] : m["SandLight"]);
            }

            BuildPaths(w, m);
            BuildQuarryRim(w, m);
            BuildDecor(w, m, random);

            // Pereți invizibili la marginea hărții.
            AddWall(w, new Vector3(0f, 1f, 30f), new Vector3(60f, 2f, 1f));
            AddWall(w, new Vector3(0f, 1f, -30f), new Vector3(60f, 2f, 1f));
            AddWall(w, new Vector3(30f, 1f, 0f), new Vector3(1f, 2f, 60f));
            AddWall(w, new Vector3(-30f, 1f, 0f), new Vector3(1f, 2f, 60f));

            MarkStatic(world);
        }

        /// <summary>Punct aleator pe hartă, dar nu în zona centrală de joc.</summary>
        private static Vector3 RandomOutside(System.Random random, float maxRadius, float minRadius)
        {
            float angle = (float)random.NextDouble() * Mathf.PI * 2f;
            float radius = Mathf.Lerp(minRadius, maxRadius, (float)random.NextDouble());
            return new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);
        }

        private static void BuildPaths(Transform parent, Materials m)
        {
            Transform paths = Empty("Paths", parent, Vector3.zero).transform;
            // Piațeta de start și drumurile spre zone (plăci joase, rotunjite).
            Primitive(PrimitiveType.Cylinder, "Plaza", paths, new Vector3(0f, 0.012f, -2.5f), new Vector3(6.5f, 0.01f, 6.5f), m["PathStone"]);
            Box("PathEastWest", paths, new Vector3(0f, 0.015f, -5.4f), new Vector3(20f, 0.03f, 2f), m["PathStone"], 0.012f, castShadows: false);
            Box("PathSouth", paths, new Vector3(0f, 0.016f, -10.5f), new Vector3(2f, 0.03f, 9f), m["PathStone"], 0.012f, castShadows: false);
            Box("PathQuarry", paths, new Vector3(0f, 0.017f, 1.2f), new Vector3(4f, 0.03f, 4.2f), m["PathStone"], 0.012f, castShadows: false);
            Box("PathRefinery", paths, new Vector3(7.6f, 0.018f, -2.2f), new Vector3(3f, 0.03f, 4f), m["PathStone"], 0.012f, castShadows: false);
            Box("PathOrders", paths, new Vector3(-8.5f, 0.018f, 3.2f), new Vector3(2f, 0.03f, 8.6f), m["PathStone"], 0.012f, castShadows: false);
            // Drumul camionului: parcare → sud → est → nord → zona VINDE.
            Box("TruckRoadSouth", paths, new Vector3(4.6f, 0.014f, -17.2f), new Vector3(11.2f, 0.03f, 2.2f), m["PathStone"], 0.012f, castShadows: false);
            Box("TruckRoadEast", paths, new Vector3(9.2f, 0.015f, -11.3f), new Vector3(2.2f, 0.03f, 13.8f), m["PathStone"], 0.012f, castShadows: false);
        }

        /// <summary>Marginea carierei: un inel de roci neregulate, cu intrare spre jucător (sud).</summary>
        private static void BuildQuarryRim(Transform parent, Materials m)
        {
            Transform rim = Empty("QuarryRim", parent, QuarryCenter).transform;
            var random = new System.Random(11);
            const float half = RimHalf;
            for (float t = -half; t <= half + 0.01f; t += 1.15f)
            {
                PlaceRimRock(rim, m, random, new Vector3(t, 0f, half));   // nord
                PlaceRimRock(rim, m, random, new Vector3(-half, 0f, t));  // vest
                PlaceRimRock(rim, m, random, new Vector3(half, 0f, t));   // est
                if (Mathf.Abs(t) > 2.4f) PlaceRimRock(rim, m, random, new Vector3(t, 0f, -half)); // sud, cu poartă
            }
        }

        private static void PlaceRimRock(Transform parent, Materials m, System.Random random, Vector3 position)
        {
            float sx = 0.9f + (float)random.NextDouble() * 0.7f;
            float sy = 0.5f + (float)random.NextDouble() * 0.6f;
            float sz = 0.8f + (float)random.NextDouble() * 0.6f;
            position.y = sy * 0.4f;
            GameObject rock = ScaledRock("RimRock", parent, position, new Vector3(sx, sy, sz),
                random.NextDouble() < 0.5 ? m["Rock1"] : m["Rock2"], (float)random.NextDouble() * 360f);
            // Collider: nu se mai trece prin margine; în carieră se intră doar prin poartă.
            rock.AddComponent<BoxCollider>().size = new Vector3(1f, 3f, 1f);
        }

        private static void BuildDecor(Transform parent, Materials m, System.Random random)
        {
            Transform decor = Empty("Decor", parent, Vector3.zero).transform;

            // Copaci stilizați la marginea hărții.
            var trees = new[]
            {
                new Vector3(-17f, 0f, 14f), new Vector3(16f, 0f, 15f), new Vector3(-19f, 0f, -6f), new Vector3(19f, 0f, -4f),
                new Vector3(-14f, 0f, -18f), new Vector3(13f, 0f, -19f), new Vector3(-22f, 0f, 4f), new Vector3(22f, 0f, 8f),
            };
            foreach (Vector3 position in trees)
            {
                Transform tree = Empty("Tree", decor, position, (float)random.NextDouble() * 360f).transform;
                Primitive(PrimitiveType.Cylinder, "Trunk", tree, new Vector3(0f, 0.8f, 0f), new Vector3(0.35f, 0.8f, 0.35f), m["Trunk"], keepCollider: true);
                Primitive(PrimitiveType.Sphere, "Crown", tree, new Vector3(0f, 2.2f, 0f), new Vector3(2f, 1.7f, 2f), m["Leaves"]);
                Primitive(PrimitiveType.Sphere, "CrownTop", tree, new Vector3(0.3f, 3f, 0.2f), new Vector3(1.3f, 1.1f, 1.3f), m["LeavesDark"]);
            }

            // Bolovani decorativi.
            for (int i = 0; i < 14; i++)
            {
                Vector3 position = RandomOutside(random, 27f, 19f);
                float size = 0.8f + (float)random.NextDouble() * 1.6f;
                position.y = size * 0.3f;
                GameObject boulder = ScaledRock("Boulder", decor, position, new Vector3(size * 1.3f, size * 0.8f, size),
                    i % 2 == 0 ? m["Rock1"] : m["Rock2"], (float)random.NextDouble() * 360f);
                boulder.AddComponent<BoxCollider>();
            }

            // Lăzi și butoaie lângă rafinărie și magazin.
            var crates = new[] { new Vector3(14.6f, 0f, -2.4f), new Vector3(15.2f, 0f, -1.2f), new Vector3(-8.8f, 0f, -14.5f), new Vector3(12f, 0f, -14.8f) };
            foreach (Vector3 position in crates)
                Box("Crate", decor, position + Vector3.up * 0.35f, new Vector3(0.7f, 0.7f, 0.7f), m["Wood"], 0.08f,
                    new Vector3(0f, (float)random.NextDouble() * 40f, 0f), collider: true);
            var barrels = new[] { new Vector3(14.4f, 0f, 3.8f), new Vector3(-13.6f, 0f, 8.5f), new Vector3(-8.2f, 0f, -15.6f) };
            foreach (Vector3 position in barrels)
                Primitive(PrimitiveType.Cylinder, "Barrel", decor, position + Vector3.up * 0.45f, new Vector3(0.6f, 0.45f, 0.6f), m["Barrel"], keepCollider: true);

            // Felinare în jurul piațetei (în afara drumului camionului).
            var lamps = new[] { new Vector3(-3.4f, 0f, -4.1f), new Vector3(3.4f, 0f, -4.1f), new Vector3(-3.4f, 0f, 0.6f), new Vector3(3.4f, 0f, 0.6f) };
            foreach (Vector3 position in lamps)
            {
                Transform lamp = Empty("Lamp", decor, position).transform;
                Primitive(PrimitiveType.Cylinder, "Pole", lamp, new Vector3(0f, 1.1f, 0f), new Vector3(0.12f, 1.1f, 0.12f), m["Dark"], keepCollider: true);
                Primitive(PrimitiveType.Sphere, "Bulb", lamp, new Vector3(0f, 2.3f, 0f), Vector3.one * 0.38f, m["Lamp"]);
            }
        }

        private static void AddWall(Transform parent, Vector3 position, Vector3 size)
        {
            GameObject wall = Empty("Wall", parent, position);
            wall.AddComponent<BoxCollider>().size = size;
        }

        // ------------------------------------------------------------ Sisteme

        private static void BuildSystems(Materials m, GameData data, Prefabs prefabs, Transform playerModel)
        {
            Transform systems = new GameObject("_Systems").transform;
            AddSystem(systems, "SaveSystem", typeof(SaveSystem));
            var catalog = AddSystem(systems, "ResourceCatalog", typeof(ResourceCatalog));
            SetArray(catalog, "resources", new System.Collections.Generic.List<ResourceData>(data.Resources.Values).ToArray());
            AddSystem(systems, "GameManager", typeof(GameManager));
            AddSystem(systems, "QualityProfile", typeof(QualityProfile));
            AddSystem(systems, "PerformanceGovernor", typeof(PerformanceGovernor));

            var pool = (ObjectPool)AddSystem(systems, "ObjectPool", typeof(ObjectPool));
            var prewarm = new (GameObject prefab, int count)[]
            {
                (prefabs.Block.gameObject, 100), (prefabs.Pickup.gameObject, 40), (prefabs.StackItem, 80),
                (prefabs.FlyingItem.gameObject, 30), (prefabs.Coin.gameObject, 20),
            };
            Edit(pool, so =>
            {
                SerializedProperty list = P(so, "prewarm");
                list.arraySize = prewarm.Length;
                for (int i = 0; i < prewarm.Length; i++)
                {
                    list.GetArrayElementAtIndex(i).FindPropertyRelative("prefab").objectReferenceValue = prewarm[i].prefab;
                    list.GetArrayElementAtIndex(i).FindPropertyRelative("count").intValue = prewarm[i].count;
                }
            });

            var effects = (EffectsLibrary)AddSystem(systems, "EffectsLibrary", typeof(EffectsLibrary));
            Set(effects, "flyingItemPrefab", prefabs.FlyingItem);
            Set(effects, "coinPrefab", prefabs.Coin);
            Set(effects, "stackItemPrefab", prefabs.StackItem);

            AddSystem(systems, "AudioManager", typeof(AudioManager));
            AddSystem(systems, "GameSfx", typeof(GameSfx));
            AddSystem(systems, "MusicPlayer", typeof(MusicPlayer));

            GameObject feedbackGo = Empty("GameFeedback", systems, Vector3.zero);
            var impulse = feedbackGo.AddComponent<CinemachineImpulseSource>();
            var feedback = feedbackGo.AddComponent<GameFeedback>();
            Set(feedback, "impulseSource", impulse);

            // Efecte vizuale (toate folosesc ObjectPool).
            Transform vfx = Empty("Vfx", systems, Vector3.zero).transform;
            var breakFx = (BlockBreakParticles)AddSystem(vfx, "BlockBreak", typeof(BlockBreakParticles));
            Set(breakFx, "particleMaterial", m["Particle"]);
            Set(breakFx, "chunkMaterial", m["ParticleSolid"]);
            Set(breakFx, "chunkMesh", RoundedMesh(Vector3.one, 0.22f));
            Set(AddSystem(vfx, "Impact", typeof(ImpactVfx)), "particleMaterial", m["Particle"]);
            Set(AddSystem(vfx, "Collect", typeof(CollectVfx)), "particleMaterial", m["Particle"]);
            Set(AddSystem(vfx, "Sell", typeof(SellVfx)), "particleMaterial", m["Particle"]);
            var upgradeFx = AddSystem(vfx, "Upgrade", typeof(UpgradeVfx));
            Set(upgradeFx, "particleMaterial", m["Particle"]);
            Set(upgradeFx, "playerModel", playerModel);

            AddSystem(systems, "MoneyManager", typeof(MoneyManager));
            var upgrades = (UpgradeManager)AddSystem(systems, "UpgradeManager", typeof(UpgradeManager));
            SetArray(upgrades, "upgrades", data.Upgrades.ToArray());
            var orders = (OrderSystem)AddSystem(systems, "OrderSystem", typeof(OrderSystem));
            Set(orders, "settings", data.Orders);
            AddSystem(systems, "OfflineEarnings", typeof(OfflineEarnings));
        }

        private static Component AddSystem(Transform parent, string name, System.Type type)
        {
            GameObject go = Empty(name, parent, Vector3.zero);
            return go.AddComponent(type);
        }

        // ------------------------------------------------------------ Cariera

        private static void BuildQuarry(Materials m, GameData data, Prefabs prefabs)
        {
            GameObject quarry = Empty("Quarry", null, QuarryCenter);
            GameObject floor = Box("QuarryFloor", quarry.transform, new Vector3(0f, 0.01f, 0f), new Vector3(RimHalf * 2f - 0.6f, 0.02f, RimHalf * 2f - 0.6f),
                m["QuarryFloor"], 0.009f, castShadows: false);

            // Poarta carierei: muncitorii intră și ies pe aici (rocile de pe margine au collidere).
            GameObject gateInside = Empty("GateInside", quarry.transform, GateInsideLocal);
            GameObject gateOutside = Empty("GateOutside", quarry.transform, GateOutsideLocal);
            var navigation = quarry.AddComponent<QuarryNavigation>();
            Set(navigation, "halfSize", new Vector2(RimHalf - 0.3f, RimHalf - 0.3f));
            Set(navigation, "gateInside", gateInside.transform);
            Set(navigation, "gateOutside", gateOutside.transform);

            var generator = quarry.AddComponent<LayerGenerator>();
            Set(generator, "blockPrefab", prefabs.Block);
            SetArray(generator, "layers", data.Layers.ToArray());
            Set(generator, "quarryFloor", floor.GetComponent<MeshRenderer>());
        }

        // ------------------------------------------------------------ Personaje

        /// <summary>Oasele unui personaj construit de BuildMiner.</summary>
        private struct MinerRig
        {
            public Transform Model, Hips, Torso, Head, LegLeft, LegRight, ArmLeft, ArmRight;
        }

        /// <summary>
        /// Miner stilizat din forme rotunjite: cască cu lanternă, cămașă, salopetă, mănuși, bocanci,
        /// rucsac și târnăcop în mâna dreaptă. Toate piesele stau sub „Model”, deci colliderul rămâne neatins.
        /// </summary>
        private static MinerRig BuildMiner(Transform parent, Material shirt, Materials m, float scale)
        {
            var rig = new MinerRig();
            rig.Model = Empty("Model", parent, Vector3.zero).transform;
            rig.Model.localScale = Vector3.one * scale;

            rig.Hips = Empty("Hips", rig.Model, new Vector3(0f, 0.95f, 0f)).transform;
            Box("Pelvis", rig.Hips, new Vector3(0f, -0.02f, 0f), new Vector3(0.5f, 0.28f, 0.32f), m["Overalls"], 0.1f);

            rig.LegLeft = BuildLeg("LegL", rig.Hips, -0.13f, m);
            rig.LegRight = BuildLeg("LegR", rig.Hips, 0.13f, m);

            rig.Torso = Empty("Torso", rig.Hips, new Vector3(0f, 0.12f, 0f)).transform;
            Box("Chest", rig.Torso, new Vector3(0f, 0.3f, 0f), new Vector3(0.6f, 0.56f, 0.38f), shirt, 0.15f);
            Box("Bib", rig.Torso, new Vector3(0f, 0.2f, 0.19f), new Vector3(0.42f, 0.3f, 0.05f), m["Overalls"], 0.02f);
            Box("StrapL", rig.Torso, new Vector3(-0.15f, 0.45f, 0.19f), new Vector3(0.07f, 0.24f, 0.04f), m["Overalls"], 0.015f);
            Box("StrapR", rig.Torso, new Vector3(0.15f, 0.45f, 0.19f), new Vector3(0.07f, 0.24f, 0.04f), m["Overalls"], 0.015f);
            Box("Belt", rig.Torso, new Vector3(0f, 0.04f, 0f), new Vector3(0.62f, 0.08f, 0.4f), m["Gloves"], 0.035f);
            Box("Backpack", rig.Torso, new Vector3(0f, 0.32f, -0.3f), new Vector3(0.46f, 0.5f, 0.24f), m["Backpack"], 0.1f);
            Box("BackpackFlap", rig.Torso, new Vector3(0f, 0.56f, -0.3f), new Vector3(0.48f, 0.12f, 0.26f), m["Gloves"], 0.05f);

            rig.Head = Empty("Head", rig.Torso, new Vector3(0f, 0.6f, 0f)).transform;
            Primitive(PrimitiveType.Sphere, "Face", rig.Head, new Vector3(0f, 0.2f, 0f), Vector3.one * 0.42f, m["Skin"]);
            Primitive(PrimitiveType.Sphere, "Nose", rig.Head, new Vector3(0f, 0.17f, 0.21f), Vector3.one * 0.09f, m["Skin"]);
            Primitive(PrimitiveType.Sphere, "EyeL", rig.Head, new Vector3(-0.08f, 0.25f, 0.18f), Vector3.one * 0.06f, m["Eyes"]);
            Primitive(PrimitiveType.Sphere, "EyeR", rig.Head, new Vector3(0.08f, 0.25f, 0.18f), Vector3.one * 0.06f, m["Eyes"]);
            Primitive(PrimitiveType.Sphere, "HelmetDome", rig.Head, new Vector3(0f, 0.33f, 0f), new Vector3(0.48f, 0.32f, 0.48f), m["Helmet"]);
            Primitive(PrimitiveType.Cylinder, "HelmetBrim", rig.Head, new Vector3(0f, 0.3f, 0.02f), new Vector3(0.56f, 0.015f, 0.56f), m["Helmet"]);
            Primitive(PrimitiveType.Cylinder, "HeadLamp", rig.Head, new Vector3(0f, 0.36f, 0.24f), new Vector3(0.11f, 0.03f, 0.11f),
                m["Lamp"], localEuler: new Vector3(90f, 0f, 0f));

            rig.ArmLeft = BuildArm("ArmL", rig.Torso, -0.37f, shirt, m);
            rig.ArmRight = BuildArm("ArmR", rig.Torso, 0.37f, shirt, m);

            // Târnăcopul, în mâna dreaptă: coada spre înainte, capul metalic la capăt.
            Transform grip = Empty("PickaxeGrip", rig.ArmRight, new Vector3(0f, -0.5f, 0.02f)).transform;
            Box("Handle", grip, new Vector3(0f, 0f, 0.3f), new Vector3(0.07f, 0.07f, 0.72f), m["Pickaxe"], 0.03f);
            Box("PickHead", grip, new Vector3(0f, 0.04f, 0.64f), new Vector3(0.09f, 0.56f, 0.1f), m["Metal"], 0.04f);
            return rig;
        }

        private static Transform BuildLeg(string name, Transform hips, float x, Materials m)
        {
            Transform leg = Empty(name, hips, new Vector3(x, -0.08f, 0f)).transform;
            Box("Thigh", leg, new Vector3(0f, -0.33f, 0f), new Vector3(0.2f, 0.62f, 0.22f), m["Overalls"], 0.09f);
            Box("Boot", leg, new Vector3(0f, -0.74f, 0.05f), new Vector3(0.24f, 0.16f, 0.36f), m["Boots"], 0.07f);
            return leg;
        }

        private static Transform BuildArm(string name, Transform torso, float x, Material shirt, Materials m)
        {
            Transform arm = Empty(name, torso, new Vector3(x, 0.5f, 0f)).transform;
            Box("Sleeve", arm, new Vector3(0f, -0.22f, 0f), new Vector3(0.17f, 0.5f, 0.17f), shirt, 0.08f);
            Primitive(PrimitiveType.Sphere, "Glove", arm, new Vector3(0f, -0.5f, 0f), Vector3.one * 0.2f, m["Gloves"]);
            return arm;
        }

        private static CharacterAnimator AddAnimator(MinerRig rig, MonoBehaviour swingSource, ResourceStack stack,
            PlayerVehicleDriver driver, float fullSpeed)
        {
            var animator = rig.Model.gameObject.AddComponent<CharacterAnimator>();
            Edit(animator, so =>
            {
                P(so, "hips").objectReferenceValue = rig.Hips;
                P(so, "torso").objectReferenceValue = rig.Torso;
                P(so, "head").objectReferenceValue = rig.Head;
                P(so, "legLeft").objectReferenceValue = rig.LegLeft;
                P(so, "legRight").objectReferenceValue = rig.LegRight;
                P(so, "armLeft").objectReferenceValue = rig.ArmLeft;
                P(so, "armRight").objectReferenceValue = rig.ArmRight;
                P(so, "swingSource").objectReferenceValue = swingSource;
                P(so, "stack").objectReferenceValue = stack;
                P(so, "driver").objectReferenceValue = driver;
                P(so, "fullSpeed").floatValue = fullSpeed;
            });
            return animator;
        }

        // ------------------------------------------------------------ Jucător

        private static Transform BuildPlayer(Materials m, GameData data, out Transform model)
        {
            var player = new GameObject("Player") { tag = "Player" };
            player.transform.position = PlayerStart;

            var controller = player.AddComponent<CharacterController>();
            controller.center = new Vector3(0f, 1f, 0f);
            controller.radius = 0.36f;
            controller.height = 2f;
            controller.skinWidth = 0.04f;
            controller.stepOffset = 0.3f;

            MinerRig rig = BuildMiner(player.transform, m["Player"], m, 1f);
            model = rig.Model;
            GameObject anchor = Empty("StackAnchor", player.transform, new Vector3(0f, 1.15f, -0.55f));

            GameObject labelGo = Empty("StackLabel", player.transform, Vector3.zero);
            WorldLabel stackLabel = AddLabel(labelGo, "", 2.9f, false, new Vector2(200f, 70f), 44);

            var root = player.AddComponent<PlayerRoot>();
            var input = player.AddComponent<PlayerInputReader>();

            var stack = player.AddComponent<ResourceStack>();
            Edit(stack, so =>
            {
                P(so, "anchor").objectReferenceValue = anchor.transform;
                P(so, "capacity").intValue = 10;
                P(so, "itemsPerColumn").intValue = 10;
                P(so, "columnsPerRow").intValue = 2;
                P(so, "itemSpacing").vector3Value = new Vector3(0.28f, 0.28f, 0.28f);
                P(so, "animateMoneyOnSell").boolValue = true;
            });

            var collector = player.AddComponent<ResourceCollector>();
            Set(collector, "stack", stack);
            Set(collector, "resourceLayer", 1 << ResourceLayer);

            var stats = player.AddComponent<PlayerStatsRuntime>();
            Set(stats, "baseStats", data.PlayerStats);
            Set(stats, "stack", stack);
            Set(stats, "collector", collector);

            var movement = player.AddComponent<PlayerMovement>();
            Set(movement, "stats", stats);
            Set(movement, "input", input);

            var miner = player.AddComponent<PlayerMiner>();
            Set(miner, "stats", stats);
            Set(miner, "movement", movement);
            Set(miner, "blockLayer", 1 << BlockLayer);

            var driver = player.AddComponent<PlayerVehicleDriver>();
            Set(driver, "characterController", controller);
            Set(driver, "movement", movement);
            Set(driver, "miner", miner);
            Set(driver, "input", input);

            Set(root, "stack", stack);
            Set(root, "driver", driver);
            Set(root, "stats", stats);

            AddAnimator(rig, miner, stack, driver, data.PlayerStats.MoveSpeed);
            var bounce = player.AddComponent<StackBounce>();
            Set(bounce, "stack", stack);

            AddPersistence(player, stack, "player");

            var capacityLabel = player.AddComponent<StackCapacityLabel>();
            Set(capacityLabel, "stack", stack);
            Set(capacityLabel, "label", stackLabel);
            return player.transform;
        }

        private static void AddPersistence(GameObject go, ResourceStack stack, string key)
        {
            var persistence = go.AddComponent<StackPersistence>();
            Set(persistence, "stack", stack);
            Set(persistence, "saveKey", key);
        }

        // ------------------------------------------------------------ Zone

        private enum PadIcon { None, Coin, ArrowUp, Plus, Crate, Ingot, ArrowDown, Pickaxe, Bolt, Boot, Backpack, Gem }

        /// <summary>Obiecte din scenă de care are nevoie tutorialul.</summary>
        private struct ZoneRefs
        {
            public Transform Sell, FirstUpgrade, QuarryGate;
        }

        private static PadIcon IconFor(UpgradeType type) => type switch
        {
            UpgradeType.MiningPower => PadIcon.Pickaxe,
            UpgradeType.MiningSpeed => PadIcon.Bolt,
            UpgradeType.MoveSpeed => PadIcon.Boot,
            UpgradeType.Capacity => PadIcon.Backpack,
            UpgradeType.PickaxeTier => PadIcon.Gem,
            _ => PadIcon.ArrowUp,
        };

        /// <summary>
        /// Placă de zonă cu semnalizare 3D: bordură colorată, interior deschis, iconiță plutitoare
        /// și efect de „puls” când jucătorul stă pe ea.
        /// </summary>
        private static GameObject Pad(string name, Transform parent, Vector3 position, Vector2 size, Material accent,
            Materials m, PadIcon icon)
        {
            GameObject root = Empty(name, parent, position);
            GameObject border = Box("Border", root.transform, new Vector3(0f, 0.04f, 0f), new Vector3(size.x, 0.08f, size.y),
                accent, 0.035f, castShadows: false);
            Box("Inner", root.transform, new Vector3(0f, 0.055f, 0f), new Vector3(size.x - 0.32f, 0.08f, size.y - 0.32f),
                m["PadInner"], 0.035f, castShadows: false);
            ZoneArea area = AddZone(root, new Vector3(size.x, 4f, size.y));

            FloatingBob bob = BuildPadIcon(root.transform, icon, accent, m);
            var highlight = root.AddComponent<ZoneHighlight>();
            Set(highlight, "area", area);
            Set(highlight, "border", border.transform);
            if (bob != null) Set(highlight, "icon", bob);
            return root;
        }

        private static FloatingBob BuildPadIcon(Transform parent, PadIcon icon, Material accent, Materials m)
        {
            if (icon == PadIcon.None) return null;
            Transform holder = Empty("Icon", parent, new Vector3(0f, 1.05f, 0f)).transform;
            switch (icon)
            {
                case PadIcon.Coin:
                    Primitive(PrimitiveType.Cylinder, "Coin", holder, Vector3.zero, new Vector3(0.6f, 0.06f, 0.6f), m["Coin"],
                        localEuler: new Vector3(90f, 0f, 0f));
                    break;
                case PadIcon.ArrowUp:
                case PadIcon.ArrowDown:
                    float direction = icon == PadIcon.ArrowUp ? 1f : -1f;
                    Box("Shaft", holder, new Vector3(0f, -0.1f * direction, 0f), new Vector3(0.18f, 0.42f, 0.18f), accent, 0.06f);
                    Box("HeadL", holder, new Vector3(-0.12f, 0.16f * direction, 0f), new Vector3(0.16f, 0.4f, 0.16f), accent, 0.06f,
                        new Vector3(0f, 0f, -45f * direction));
                    Box("HeadR", holder, new Vector3(0.12f, 0.16f * direction, 0f), new Vector3(0.16f, 0.4f, 0.16f), accent, 0.06f,
                        new Vector3(0f, 0f, 45f * direction));
                    break;
                case PadIcon.Plus:
                    Box("PlusV", holder, Vector3.zero, new Vector3(0.18f, 0.6f, 0.18f), m["Coin"], 0.06f);
                    Box("PlusH", holder, Vector3.zero, new Vector3(0.6f, 0.18f, 0.18f), m["Coin"], 0.06f);
                    break;
                case PadIcon.Crate:
                    Box("Crate", holder, Vector3.zero, new Vector3(0.55f, 0.55f, 0.55f), m["Wood"], 0.07f);
                    Box("Band", holder, Vector3.zero, new Vector3(0.58f, 0.1f, 0.58f), m["Dark"], 0.03f);
                    break;
                case PadIcon.Ingot:
                    Box("Ingot", holder, Vector3.zero, new Vector3(0.6f, 0.22f, 0.32f), m["GoldIngot"], 0.06f);
                    break;
                case PadIcon.Pickaxe:
                    Box("Handle", holder, new Vector3(0f, -0.05f, 0f), new Vector3(0.09f, 0.7f, 0.09f), m["Pickaxe"], 0.035f, new Vector3(0f, 0f, -25f));
                    Box("Head", holder, new Vector3(0.12f, 0.28f, 0f), new Vector3(0.6f, 0.12f, 0.12f), m["Metal"], 0.05f, new Vector3(0f, 0f, -25f));
                    break;
                case PadIcon.Bolt:
                    Box("BoltTop", holder, new Vector3(0.06f, 0.16f, 0f), new Vector3(0.16f, 0.4f, 0.12f), m["Arrow"], 0.05f, new Vector3(0f, 0f, -25f));
                    Box("BoltMid", holder, new Vector3(0f, 0f, 0f), new Vector3(0.34f, 0.12f, 0.12f), m["Arrow"], 0.05f);
                    Box("BoltLow", holder, new Vector3(-0.06f, -0.16f, 0f), new Vector3(0.16f, 0.4f, 0.12f), m["Arrow"], 0.05f, new Vector3(0f, 0f, -25f));
                    break;
                case PadIcon.Boot:
                    Box("Shaft", holder, new Vector3(-0.08f, 0.1f, 0f), new Vector3(0.24f, 0.42f, 0.22f), m["Boots"], 0.08f);
                    Box("Foot", holder, new Vector3(0.06f, -0.14f, 0f), new Vector3(0.5f, 0.18f, 0.24f), m["Boots"], 0.08f);
                    Box("Sole", holder, new Vector3(0.06f, -0.24f, 0f), new Vector3(0.52f, 0.05f, 0.26f), m["Dark"], 0.02f);
                    break;
                case PadIcon.Backpack:
                    Box("Bag", holder, Vector3.zero, new Vector3(0.46f, 0.52f, 0.26f), m["Backpack"], 0.12f);
                    Box("Flap", holder, new Vector3(0f, 0.2f, 0.02f), new Vector3(0.48f, 0.16f, 0.28f), m["Gloves"], 0.06f);
                    Box("Pocket", holder, new Vector3(0f, -0.1f, 0.14f), new Vector3(0.28f, 0.2f, 0.06f), m["Gloves"], 0.03f);
                    break;
                case PadIcon.Gem:
                    Box("Gem", holder, Vector3.zero, new Vector3(0.36f, 0.36f, 0.36f), m["Diamond"], 0.08f, new Vector3(45f, 0f, 45f));
                    break;
            }
            foreach (Renderer r in holder.GetComponentsInChildren<Renderer>()) r.shadowCastingMode = ShadowCastingMode.Off;
            return holder.gameObject.AddComponent<FloatingBob>();
        }

        private static ZoneRefs BuildZones(Materials m, GameData data)
        {
            var refs = new ZoneRefs();

            // VINDE
            GameObject sell = Pad("SellZone", null, SellPosition, new Vector2(3.2f, 3.2f), m["PadSell"], m, PadIcon.Coin);
            WorldLabel sellLabel = AddLabel(sell, "VINDE", LabelHeight, false, new Vector2(260f, 90f), 56);
            Set(sellLabel, "alwaysVisible", true);
            refs.Sell = sell.transform;
            GameObject sellPoint = Empty("CollectPoint", sell.transform, new Vector3(0f, 0.3f, 0f));
            var sellZone = sell.AddComponent<SellZone>();
            Set(sellZone, "area", sell.GetComponent<ZoneArea>());
            Set(sellZone, "collectPoint", sellPoint.transform);

            // UPGRADE (un rând de plăci)
            Transform upgradesRoot = new GameObject("Upgrades").transform;
            for (int i = 0; i < data.Upgrades.Count; i++)
            {
                UpgradeDefinition upgrade = data.Upgrades[i];
                GameObject padGo = Pad("Upgrade_" + upgrade.Id, upgradesRoot, new Vector3(-6f + i * 3f, 0f, -8f),
                    new Vector2(2.4f, 2.4f), m["PadUpgrade"], m, IconFor(upgrade.Type));
                if (i == 0) refs.FirstUpgrade = padGo.transform;
                WorldLabel label = AddLabel(padGo, upgrade.DisplayName, LabelHeight, true, new Vector2(290f, 130f), 30);
                var pad = padGo.AddComponent<UpgradePad>();
                Set(pad, "area", padGo.GetComponent<ZoneArea>());
                Set(pad, "label", label);
                Set(pad, "upgrade", upgrade);
            }

            BuildRefinery(m);
            BuildOrders(m);
            BuildWorkers(m, data);
            BuildMachineShop(m, data);

            refs.QuarryGate = GameObject.Find("Quarry/GateInside")?.transform;
            return refs;
        }

        private static PurchaseZone Purchase(string id, string title, long cost, Vector3 position, Materials m,
            params GameObject[] targets)
        {
            GameObject root = Pad("Buy_" + id, null, position, new Vector2(2.6f, 2.6f), m["PadBuy"], m, PadIcon.Plus);
            WorldLabel label = AddLabel(root, title, LabelHeight, true, new Vector2(360f, 140f), 36);
            var zone = root.AddComponent<PurchaseZone>();
            Set(zone, "area", root.GetComponent<ZoneArea>());
            Set(zone, "label", label);
            Set(zone, "id", id);
            Set(zone, "title", title);
            Set(zone, "cost", cost);
            SetArray(zone, "unlockTargets", targets);
            return zone;
        }

        private static void BuildRefinery(Materials m)
        {
            GameObject refinery = Empty("Refinery", null, new Vector3(10.5f, 0f, 1f));
            Transform r = refinery.transform;
            Box("Building", r, new Vector3(1.2f, 1.2f, 0f), new Vector3(3f, 2.4f, 3f), m["Building"], 0.18f, collider: true);
            Box("Roof", r, new Vector3(1.2f, 2.5f, 0f), new Vector3(3.3f, 0.25f, 3.3f), m["Roof"], 0.1f);
            Box("Window", r, new Vector3(-0.31f, 1.4f, 0f), new Vector3(0.05f, 0.7f, 1.4f), m["Glass"], 0.02f);
            Primitive(PrimitiveType.Cylinder, "Chimney", r, new Vector3(2f, 3.2f, 0.8f), new Vector3(0.5f, 0.8f, 0.5f), m["Dark"]);
            Box("Hopper", r, new Vector3(-0.1f, 2.1f, -0.9f), new Vector3(0.8f, 0.5f, 0.8f), m["Metal"], 0.08f);
            GameObject machinePoint = Empty("MachinePoint", r, new Vector3(-0.1f, 2.4f, -0.9f));

            GameObject input = Pad("RefineInput", r, new Vector3(-1.6f, 0f, -2.2f), new Vector2(2.4f, 2.4f), m["PadRefine"], m, PadIcon.ArrowDown);
            AddLabel(input, "MINEREU", 1.9f, false, new Vector2(240f, 70f), 36);
            GameObject output = Pad("RefineOutput", r, new Vector3(-1.6f, 0f, 2.2f), new Vector2(2.4f, 2.4f), m["PadRefine"], m, PadIcon.Ingot);
            AddLabel(output, "LINGOURI", 1.9f, false, new Vector2(240f, 70f), 36);
            GameObject pile = Empty("OutputPile", r, new Vector3(1.2f, 0.15f, 2.6f));

            WorldLabel label = AddLabel(refinery, "RAFINĂRIE", 3.6f, true, new Vector2(460f, 140f), 36);
            var zone = refinery.AddComponent<RefineZone>();
            Set(zone, "inputArea", input.GetComponent<ZoneArea>());
            Set(zone, "outputArea", output.GetComponent<ZoneArea>());
            Set(zone, "machinePoint", machinePoint.transform);
            Set(zone, "outputPile", pile.transform);
            Set(zone, "label", label);

            refinery.SetActive(false);
            Purchase("refinery", "Rafinărie", 250, new Vector3(8.9f, 0f, 1f), m, refinery);
        }

        private static void BuildOrders(Materials m)
        {
            GameObject orders = Pad("OrderZone", null, new Vector3(-10f, 0f, 8f), new Vector2(3f, 3f), m["PadOrder"], m, PadIcon.Crate);
            GameObject truck = BuildTruckModel("OrderTruck", orders.transform, new Vector3(-2.8f, 0f, 0f), 90f, m["OrderTruck"], m);
            GameObject dropPoint = Empty("DropPoint", truck.transform, new Vector3(0f, 1.2f, -0.6f));
            WorldLabel label = AddLabel(orders, "COMENZI", 2.5f, true, new Vector2(560f, 150f), 34);

            var zone = orders.AddComponent<OrderZone>();
            Set(zone, "area", orders.GetComponent<ZoneArea>());
            Set(zone, "truckModel", truck);
            Set(zone, "dropPoint", dropPoint.transform);
            Set(zone, "label", label);
            truck.SetActive(false);
        }

        private static GameObject BuildTruckModel(string name, Transform parent, Vector3 localPosition, float yRotation,
            Material body, Materials m)
        {
            GameObject root = Empty(name, parent, localPosition, yRotation);
            Transform t = root.transform;
            Box("Cab", t, new Vector3(0f, 1f, 1.05f), new Vector3(1.8f, 1.4f, 1.15f), body, 0.22f);
            Box("Window", t, new Vector3(0f, 1.3f, 1.63f), new Vector3(1.45f, 0.5f, 0.04f), m["Glass"], 0.02f);
            Box("Chassis", t, new Vector3(0f, 0.45f, -0.1f), new Vector3(1.7f, 0.25f, 3.2f), m["Dark"], 0.08f);
            Box("Bed", t, new Vector3(0f, 0.75f, -0.65f), new Vector3(1.9f, 0.45f, 2.2f), body, 0.12f);
            Box("BedInner", t, new Vector3(0f, 0.82f, -0.65f), new Vector3(1.6f, 0.36f, 1.9f), m["Dark"], 0.06f);
            foreach (float x in new[] { -0.95f, 0.95f })
            foreach (float z in new[] { -1.1f, 1f })
                Primitive(PrimitiveType.Cylinder, "Wheel", t, new Vector3(x, 0.38f, z), new Vector3(0.76f, 0.14f, 0.76f),
                    m["Dark"], localEuler: new Vector3(0f, 0f, 90f));
            return root;
        }

        private static void BuildWorkers(Materials m, GameData data)
        {
            var positions = new[] { new Vector3(-10.5f, 0f, 0f), new Vector3(-10.5f, 0f, -3.5f), new Vector3(-10.5f, 0f, -7f) };
            var costs = new long[] { 450, 1600, 4500 };

            // Construim de la ultimul la primul, ca fiecare zonă să poată debloca zona următoare.
            GameObject nextZone = null;
            for (int i = positions.Length - 1; i >= 0; i--)
            {
                GameObject worker = BuildWorker($"Worker{i + 1}", positions[i], m, data);
                worker.SetActive(false);
                PurchaseZone zone = nextZone != null
                    ? Purchase($"worker{i + 1}", $"Muncitor {i + 1}", costs[i], positions[i], m, worker, nextZone)
                    : Purchase($"worker{i + 1}", $"Muncitor {i + 1}", costs[i], positions[i], m, worker);
                if (i > 0) zone.gameObject.SetActive(false);
                nextZone = zone.gameObject;
            }
        }

        private static GameObject BuildWorker(string name, Vector3 position, Materials m, GameData data)
        {
            var root = new GameObject(name);
            root.transform.position = position + Vector3.up * 0.05f;

            var controller = root.AddComponent<CharacterController>();
            controller.center = new Vector3(0f, 0.85f, 0f);
            controller.radius = 0.35f;
            controller.height = 1.7f;

            MinerRig rig = BuildMiner(root.transform, m["Worker"], m, 0.85f);
            GameObject anchor = Empty("StackAnchor", root.transform, new Vector3(0f, 0.95f, -0.48f));

            var stack = root.AddComponent<ResourceStack>();
            Edit(stack, so =>
            {
                P(so, "anchor").objectReferenceValue = anchor.transform;
                P(so, "capacity").intValue = data.Worker.Capacity;
                P(so, "itemsPerColumn").intValue = 8;
                P(so, "columnsPerRow").intValue = 1;
                P(so, "itemSpacing").vector3Value = new Vector3(0.26f, 0.26f, 0.26f);
            });

            var collector = root.AddComponent<ResourceCollector>();
            Set(collector, "stack", stack);
            Set(collector, "resourceLayer", 1 << ResourceLayer);

            var ai = root.AddComponent<WorkerAI>();
            Set(ai, "definition", data.Worker);
            Set(ai, "stack", stack);
            Set(ai, "collector", collector);

            AddAnimator(rig, ai, stack, null, data.Worker.MoveSpeed);
            AddPersistence(root, stack, name.ToLowerInvariant());
            return root;
        }

        // ------------------------------------------------------------ Utilaje

        private static void BuildMachineShop(Materials m, GameData data)
        {
            GameObject bulldozer = BuildBulldozer(new Vector3(-5f, 0f, -13.5f), m, data.Bulldozer);
            bulldozer.SetActive(false);
            Purchase("bulldozer", "Buldozer", 1500, new Vector3(-5f, 0f, -13f), m, bulldozer);

            GameObject drill = BuildDrill(new Vector3(5f, 0f, -13.5f), m, data.Drill);
            drill.SetActive(false);
            Purchase("drill", "Burghiu", 4000, new Vector3(5f, 0f, -13f), m, drill);

            BuildTransportTruck(m);
        }

        private static GameObject BuildVehicleBase(string name, Vector3 position, out Transform seat, out Transform exit)
        {
            var root = new GameObject(name);
            root.transform.position = position;
            var controller = root.AddComponent<CharacterController>();
            controller.center = new Vector3(0f, 0.9f, 0f);
            controller.radius = 0.9f;
            controller.height = 1.8f;
            seat = Empty("Seat", root.transform, new Vector3(0f, 1.05f, -0.45f)).transform;
            exit = Empty("ExitPoint", root.transform, new Vector3(-2.9f, 0.05f, 0f)).transform;
            return root;
        }

        private static void AddVehicleSeat(GameObject root, Vehicle vehicle, Materials m)
        {
            GameObject seatZone = Pad("SeatZone", root.transform, new Vector3(-1.75f, 0f, 0f), new Vector2(1.5f, 1.5f), m["PadLoad"], m, PadIcon.ArrowDown);
            AddLabel(seatZone, "URCĂ", 1.8f, false, new Vector2(160f, 60f), 34);
            var seat = seatZone.AddComponent<VehicleSeat>();
            Set(seat, "vehicle", vehicle);
            Set(seat, "area", seatZone.GetComponent<ZoneArea>());
        }

        private static GameObject BuildBulldozer(Vector3 position, Materials m, VehicleDefinition definition)
        {
            GameObject root = BuildVehicleBase("Bulldozer", position, out Transform seat, out Transform exit);
            Transform t = root.transform;
            Box("Body", t, new Vector3(0f, 0.8f, 0.1f), new Vector3(1.6f, 0.9f, 1.8f), m["Bulldozer"], 0.2f);
            Box("SeatBlock", t, new Vector3(0f, 1.3f, -0.45f), new Vector3(0.7f, 0.2f, 0.7f), m["Dark"], 0.08f);
            Box("RollBar", t, new Vector3(0f, 1.9f, -0.85f), new Vector3(1.3f, 0.12f, 0.12f), m["Dark"], 0.05f);
            Box("RollBarL", t, new Vector3(-0.6f, 1.45f, -0.85f), new Vector3(0.12f, 0.9f, 0.12f), m["Dark"], 0.05f);
            Box("RollBarR", t, new Vector3(0.6f, 1.45f, -0.85f), new Vector3(0.12f, 0.9f, 0.12f), m["Dark"], 0.05f);
            Box("TrackL", t, new Vector3(-0.95f, 0.35f, 0f), new Vector3(0.45f, 0.6f, 2.3f), m["Dark"], 0.2f);
            Box("TrackR", t, new Vector3(0.95f, 0.35f, 0f), new Vector3(0.45f, 0.6f, 2.3f), m["Dark"], 0.2f);
            Primitive(PrimitiveType.Cylinder, "Exhaust", t, new Vector3(0.5f, 1.55f, 0.4f), new Vector3(0.15f, 0.45f, 0.15f), m["Dark"]);
            GameObject blade = Empty("BladePivot", t, new Vector3(0f, 0f, 1.25f));
            Box("Blade", blade.transform, new Vector3(0f, 0.45f, 0.15f), new Vector3(2.6f, 0.8f, 0.18f), m["Metal"], 0.07f);

            var tool = root.AddComponent<Bulldozer>();
            Set(tool, "definition", definition);
            Set(tool, "blockLayer", 1 << BlockLayer);
            Set(tool, "blade", blade.transform);

            Vehicle vehicle = AddVehicle(root, definition, seat, exit, tool);
            AddVehicleSeat(root, vehicle, m);
            return root;
        }

        private static GameObject BuildDrill(Vector3 position, Materials m, VehicleDefinition definition)
        {
            GameObject root = BuildVehicleBase("Drill", position, out Transform seat, out Transform exit);
            Transform t = root.transform;
            Box("Body", t, new Vector3(0f, 0.8f, 0f), new Vector3(1.6f, 1f, 2f), m["Drill"], 0.22f);
            Box("SeatBlock", t, new Vector3(0f, 1.35f, -0.45f), new Vector3(0.7f, 0.2f, 0.7f), m["Dark"], 0.08f);
            Box("Engine", t, new Vector3(0f, 1.45f, -0.95f), new Vector3(1.1f, 0.5f, 0.3f), m["Dark"], 0.1f);
            foreach (float x in new[] { -0.9f, 0.9f })
            foreach (float z in new[] { -0.7f, 0.7f })
                Primitive(PrimitiveType.Cylinder, "Wheel", t, new Vector3(x, 0.38f, z), new Vector3(0.76f, 0.14f, 0.76f),
                    m["Dark"], localEuler: new Vector3(0f, 0f, 90f));
            GameObject bit = Empty("DrillBit", t, new Vector3(0f, 0.75f, 1.1f));
            Primitive(PrimitiveType.Cylinder, "Bit", bit.transform, new Vector3(0f, 0f, 0.45f), new Vector3(0.55f, 0.45f, 0.55f),
                m["Metal"], localEuler: new Vector3(90f, 0f, 0f));
            Box("ToothA", bit.transform, new Vector3(0.3f, 0f, 0.5f), new Vector3(0.1f, 0.1f, 0.8f), m["Dark"], 0.04f);
            Box("ToothB", bit.transform, new Vector3(-0.3f, 0f, 0.5f), new Vector3(0.1f, 0.1f, 0.8f), m["Dark"], 0.04f);

            var tool = root.AddComponent<Drill>();
            Set(tool, "definition", definition);
            Set(tool, "blockLayer", 1 << BlockLayer);
            Set(tool, "bit", bit.transform);

            Vehicle vehicle = AddVehicle(root, definition, seat, exit, tool);
            AddVehicleSeat(root, vehicle, m);
            return root;
        }

        private static Vehicle AddVehicle(GameObject root, VehicleDefinition definition, Transform seat, Transform exit, VehicleTool tool)
        {
            var vehicle = root.AddComponent<Vehicle>();
            Set(vehicle, "definition", definition);
            Set(vehicle, "seat", seat);
            Set(vehicle, "exitPoint", exit);
            Set(vehicle, "tool", tool);
            return vehicle;
        }

        private static void BuildTransportTruck(Materials m)
        {
            GameObject park = Empty("TruckPark", null, new Vector3(0f, 0f, -14.5f), -90f);
            GameObject sellPoint = Empty("TruckSellPoint", null, SellPosition);

            GameObject truck = BuildTruckModel("Truck", null, park.transform.position, -90f, m["Truck"], m);
            GameObject anchor = Empty("StackAnchor", truck.transform, new Vector3(0f, 1.05f, -0.2f));
            var stack = truck.AddComponent<ResourceStack>();
            Edit(stack, so =>
            {
                P(so, "anchor").objectReferenceValue = anchor.transform;
                P(so, "capacity").intValue = 40;
                P(so, "itemsPerColumn").intValue = 5;
                P(so, "columnsPerRow").intValue = 4;
                P(so, "itemSpacing").vector3Value = new Vector3(0.3f, 0.28f, 0.3f);
            });
            WorldLabel label = AddLabel(truck, "CAMION", 3f, true, new Vector2(400f, 140f), 34);
            // Traseul pe drum (ocolește upgrade-urile, utilajele și piațeta).
            Transform route = new GameObject("TruckRoute").transform;
            var waypoints = new[]
            {
                Empty("W1", route, new Vector3(0f, 0f, -17.2f)).transform,
                Empty("W2", route, new Vector3(9.2f, 0f, -17.2f)).transform,
                Empty("W3", route, new Vector3(9.2f, 0f, -5.4f)).transform,
                Empty("W4", route, new Vector3(-3.6f, 0f, -5.4f)).transform,
            };
            var controller = truck.AddComponent<Truck>();
            Set(controller, "stack", stack);
            Set(controller, "parkPoint", park.transform);
            Set(controller, "sellPoint", sellPoint.transform);
            Set(controller, "label", label);
            SetArray(controller, "route", waypoints);
            AddPersistence(truck, stack, "truck");

            GameObject load = Pad("TruckLoad", null, new Vector3(0f, 0f, -11.5f), new Vector2(2.4f, 2.4f), m["PadLoad"], m, PadIcon.Crate);
            AddLabel(load, "ÎNCARCĂ\nCAMIONUL", 2.1f, false, new Vector2(300f, 110f), 32);
            var transfer = load.AddComponent<StackTransferZone>();
            Set(transfer, "area", load.GetComponent<ZoneArea>());
            Set(transfer, "target", stack);
            Set(transfer, "truck", controller);

            truck.SetActive(false);
            load.SetActive(false);
            Purchase("truck", "Camion", 1800, new Vector3(0f, 0f, -11.5f), m, truck, load);
        }

        // ------------------------------------------------------------ Tutorial

        /// <summary>Săgeata de pe sol (lângă jucător) și marcajul care sare deasupra țintei.</summary>
        private static void BuildTutorial(Materials m, ZoneRefs zones)
        {
            GameObject root = new GameObject("Tutorial");
            var guide = root.AddComponent<TutorialGuide>();

            Transform arrow = Empty("GroundArrow", root.transform, Vector3.zero).transform;
            Box("Shaft", arrow, new Vector3(0f, 0f, -0.15f), new Vector3(0.18f, 0.06f, 0.5f), m["Arrow"], 0.025f, castShadows: false);
            Box("HeadL", arrow, new Vector3(-0.13f, 0f, 0.16f), new Vector3(0.16f, 0.06f, 0.44f), m["Arrow"], 0.025f, new Vector3(0f, -40f, 0f), castShadows: false);
            Box("HeadR", arrow, new Vector3(0.13f, 0f, 0.16f), new Vector3(0.16f, 0.06f, 0.44f), m["Arrow"], 0.025f, new Vector3(0f, 40f, 0f), castShadows: false);

            Transform marker = Empty("TargetMarker", root.transform, Vector3.zero).transform;
            Box("Shaft", marker, new Vector3(0f, 0.3f, 0f), new Vector3(0.22f, 0.5f, 0.22f), m["Arrow"], 0.08f, castShadows: false);
            Box("HeadL", marker, new Vector3(-0.14f, -0.02f, 0f), new Vector3(0.2f, 0.46f, 0.2f), m["Arrow"], 0.08f, new Vector3(0f, 0f, 45f), castShadows: false);
            Box("HeadR", marker, new Vector3(0.14f, -0.02f, 0f), new Vector3(0.2f, 0.46f, 0.2f), m["Arrow"], 0.08f, new Vector3(0f, 0f, -45f), castShadows: false);
            marker.gameObject.AddComponent<FloatingBob>();

            Set(guide, "quarryTarget", zones.QuarryGate);
            Set(guide, "sellTarget", zones.Sell);
            Set(guide, "upgradeTarget", zones.FirstUpgrade);
            Set(guide, "groundArrow", arrow);
            Set(guide, "targetMarker", marker);
        }

        // ------------------------------------------------------------ Cameră

        private static void BuildCamera(Transform player)
        {
            Quaternion rotation = Quaternion.Euler(50f, 45f, 0f);

            var cameraGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Hex("#A9D6EE");
            camera.fieldOfView = 40f;
            camera.nearClipPlane = 0.5f;
            camera.farClipPlane = 80f;
            cameraGo.AddComponent<AudioListener>();
            cameraGo.AddComponent<CinemachineBrain>();
            cameraGo.transform.SetPositionAndRotation(player.position + CameraOffset, rotation);

            // Camera virtuală Cinemachine: urmărește jucătorul, cu rotație fixă (izometric ~50°).
            var vcamGo = new GameObject("CinemachineCamera");
            vcamGo.transform.SetPositionAndRotation(player.position + CameraOffset, rotation);
            var vcam = vcamGo.AddComponent<CinemachineCamera>();
            CameraTarget target = vcam.Target;
            target.TrackingTarget = player;
            vcam.Target = target;
            LensSettings lens = vcam.Lens;
            lens.FieldOfView = 40f;
            vcam.Lens = lens;

            var follow = vcamGo.AddComponent<CinemachineFollow>();
            follow.FollowOffset = CameraOffset;
            TrackerSettings tracker = follow.TrackerSettings;
            tracker.BindingMode = BindingMode.WorldSpace;
            tracker.PositionDamping = Vector3.one * 0.3f;
            follow.TrackerSettings = tracker;

            vcamGo.AddComponent<CinemachineImpulseListener>();
            var zoom = vcamGo.AddComponent<CameraZoom>();
            Set(zoom, "follow", follow);
            Set(zoom, "baseOffset", CameraOffset);
            var switcher = vcamGo.AddComponent<CameraTargetSwitcher>();
            Set(switcher, "virtualCamera", vcam);
            Set(switcher, "player", player);
        }
    }
}
