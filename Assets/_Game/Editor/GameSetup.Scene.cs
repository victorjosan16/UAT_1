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

        private static void BuildScene(Scene scene, Materials m, GameData data, Prefabs prefabs)
        {
            RenderSettings.ambientMode = AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(0.62f, 0.62f, 0.68f);

            var sun = new GameObject("Directional Light");
            var light = sun.AddComponent<Light>();
            light.type = LightType.Directional;
            light.shadows = LightShadows.Soft;
            light.intensity = 1.15f;
            sun.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            BuildWorld(m);
            BuildSystems(m, data, prefabs);
            BuildQuarry(m, data, prefabs);
            Transform player = BuildPlayer(m, data);
            BuildZones(m, data);
            BuildCamera(player);
            BuildUi(data.Theme);

            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
        }

        // ------------------------------------------------------------ Lume

        private static void BuildWorld(Materials m)
        {
            var world = new GameObject("World").transform;
            Primitive(PrimitiveType.Plane, "Ground", world, Vector3.zero, new Vector3(6f, 1f, 6f), m["Ground"], keepCollider: true);

            // Pereți invizibili la marginea hărții.
            AddWall(world, new Vector3(0f, 1f, 30f), new Vector3(60f, 2f, 1f));
            AddWall(world, new Vector3(0f, 1f, -30f), new Vector3(60f, 2f, 1f));
            AddWall(world, new Vector3(30f, 1f, 0f), new Vector3(1f, 2f, 60f));
            AddWall(world, new Vector3(-30f, 1f, 0f), new Vector3(1f, 2f, 60f));

            // Bolovani decorativi în jurul hărții.
            var random = new System.Random(42);
            for (int i = 0; i < 18; i++)
            {
                float angle = i / 18f * Mathf.PI * 2f;
                float radius = 21f + (float)random.NextDouble() * 6f;
                float size = 1f + (float)random.NextDouble() * 2f;
                Primitive(PrimitiveType.Sphere, "Boulder", world,
                    new Vector3(Mathf.Cos(angle) * radius, size * 0.3f, Mathf.Sin(angle) * radius),
                    new Vector3(size * 1.3f, size * 0.8f, size), m["Wall"], localEuler: new Vector3(0f, i * 37f, 0f));
            }
        }

        private static void AddWall(Transform parent, Vector3 position, Vector3 size)
        {
            GameObject wall = Empty("Wall", parent, position);
            wall.AddComponent<BoxCollider>().size = size;
        }

        // ------------------------------------------------------------ Sisteme

        private static void BuildSystems(Materials m, GameData data, Prefabs prefabs)
        {
            Transform systems = new GameObject("_Systems").transform;
            AddSystem(systems, "SaveSystem", typeof(SaveSystem));
            AddSystem(systems, "GameManager", typeof(GameManager));

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

            GameObject feedbackGo = Empty("GameFeedback", systems, Vector3.zero);
            var impulse = feedbackGo.AddComponent<CinemachineImpulseSource>();
            var feedback = feedbackGo.AddComponent<GameFeedback>();
            Set(feedback, "impulseSource", impulse);

            var particles = (BlockBreakParticles)AddSystem(systems, "BlockBreakParticles", typeof(BlockBreakParticles));
            Set(particles, "particleMaterial", m["Particle"]);

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
            GameObject floor = Primitive(PrimitiveType.Cube, "QuarryFloor", quarry.transform, new Vector3(0f, 0.005f, 0f),
                new Vector3(11f, 0.01f, 11f), m["QuarryFloor"]);

            var generator = quarry.AddComponent<LayerGenerator>();
            Set(generator, "blockPrefab", prefabs.Block);
            SetArray(generator, "layers", data.Layers.ToArray());
            Set(generator, "quarryFloor", floor.GetComponent<MeshRenderer>());
        }

        // ------------------------------------------------------------ Jucător

        private static Transform BuildPlayer(Materials m, GameData data)
        {
            var player = new GameObject("Player") { tag = "Player" };
            player.transform.position = PlayerStart;

            var controller = player.AddComponent<CharacterController>();
            controller.center = new Vector3(0f, 1f, 0f);
            controller.radius = 0.4f;
            controller.height = 2f;

            Transform pivot = BuildHumanoid(player.transform, m["Player"], null, m);
            GameObject anchor = Empty("StackAnchor", player.transform, new Vector3(0f, 0.75f, -0.6f));

            GameObject labelGo = Empty("StackLabel", player.transform, Vector3.zero);
            WorldLabel stackLabel = AddLabel(labelGo, "", 2.6f, false, new Vector2(200f, 70f), 44);

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

            var swing = player.AddComponent<PickaxeSwing>();
            Set(swing, "swingSource", miner);
            Set(swing, "pivot", pivot);

            var driver = player.AddComponent<PlayerVehicleDriver>();
            Set(driver, "characterController", controller);
            Set(driver, "movement", movement);
            Set(driver, "miner", miner);
            Set(driver, "input", input);

            Set(root, "stack", stack);
            Set(root, "driver", driver);
            Set(root, "stats", stats);

            var capacityLabel = player.AddComponent<StackCapacityLabel>();
            Set(capacityLabel, "stack", stack);
            Set(capacityLabel, "label", stackLabel);
            return player.transform;
        }

        /// <summary>Un personaj din primitive (corp, nas, cască opțională, târnăcop). Întoarce pivotul târnăcopului.</summary>
        private static Transform BuildHumanoid(Transform parent, Material body, Material helmet, Materials m)
        {
            Primitive(PrimitiveType.Capsule, "Body", parent, new Vector3(0f, 1f, 0f), Vector3.one, body);
            Primitive(PrimitiveType.Cube, "Nose", parent, new Vector3(0f, 1.55f, 0.45f), Vector3.one * 0.18f, m["Skin"]);
            if (helmet != null)
                Primitive(PrimitiveType.Cylinder, "Helmet", parent, new Vector3(0f, 2.02f, 0f), new Vector3(0.75f, 0.07f, 0.75f), helmet);

            Transform pivot = Empty("PickaxePivot", parent, new Vector3(0.4f, 1.25f, 0.1f)).transform;
            GameObject handle = Primitive(PrimitiveType.Cube, "Pickaxe", pivot, new Vector3(0f, 0.45f, 0f),
                new Vector3(0.08f, 0.9f, 0.08f), m["Pickaxe"]);
            Primitive(PrimitiveType.Cube, "Head", handle.transform, new Vector3(0f, 0.5f, 0f),
                new Vector3(1.25f, 0.09f, 6.25f), m["Metal"]);
            return pivot;
        }

        // ------------------------------------------------------------ Zone

        private static void BuildZones(Materials m, GameData data)
        {
            // VINDE
            GameObject sell = Pad("SellZone", null, SellPosition, new Vector2(3.2f, 3.2f), m["PadSell"]);
            AddLabel(sell, "VINDE", 1.2f, false, new Vector2(260f, 90f), 56);
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
                    new Vector2(2.4f, 2.4f), m["PadUpgrade"]);
                WorldLabel label = AddLabel(padGo, upgrade.DisplayName, 1.3f, true, new Vector2(290f, 130f), 30);
                var pad = padGo.AddComponent<UpgradePad>();
                Set(pad, "area", padGo.GetComponent<ZoneArea>());
                Set(pad, "label", label);
                Set(pad, "upgrade", upgrade);
            }

            BuildRefinery(m);
            BuildOrders(m);
            BuildWorkers(m, data);
            BuildMachineShop(m, data);
        }

        private static PurchaseZone Purchase(string id, string title, long cost, Vector3 position, Materials m,
            params GameObject[] targets)
        {
            GameObject root = Pad("Buy_" + id, null, position, new Vector2(2.6f, 2.6f), m["PadBuy"]);
            WorldLabel label = AddLabel(root, title, 1.4f, true, new Vector2(360f, 140f), 36);
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
            GameObject refinery = Empty("Refinery", null, new Vector3(9.5f, 0f, 1f));
            Primitive(PrimitiveType.Cube, "Building", refinery.transform, new Vector3(1.2f, 1.2f, 0f),
                new Vector3(3f, 2.4f, 3f), m["Building"], keepCollider: true);
            Primitive(PrimitiveType.Cylinder, "Chimney", refinery.transform, new Vector3(2f, 2.9f, 0.8f),
                new Vector3(0.5f, 1f, 0.5f), m["Dark"]);
            GameObject machinePoint = Empty("MachinePoint", refinery.transform, new Vector3(1.2f, 2.5f, 0f));

            GameObject input = Pad("RefineInput", refinery.transform, new Vector3(-1.6f, 0f, -2.2f), new Vector2(2.4f, 2.4f), m["PadRefine"]);
            AddLabel(input, "MINEREU", 1f, false, new Vector2(240f, 70f), 36);
            GameObject output = Pad("RefineOutput", refinery.transform, new Vector3(-1.6f, 0f, 2.2f), new Vector2(2.4f, 2.4f), m["PadRefine"]);
            AddLabel(output, "LINGOURI", 1f, false, new Vector2(240f, 70f), 36);
            GameObject pile = Empty("OutputPile", refinery.transform, new Vector3(1.2f, 0.15f, 2.6f));

            WorldLabel label = AddLabel(refinery, "RAFINĂRIE", 3.3f, true, new Vector2(460f, 140f), 36);
            var zone = refinery.AddComponent<RefineZone>();
            Set(zone, "inputArea", input.GetComponent<ZoneArea>());
            Set(zone, "outputArea", output.GetComponent<ZoneArea>());
            Set(zone, "machinePoint", machinePoint.transform);
            Set(zone, "outputPile", pile.transform);
            Set(zone, "label", label);

            refinery.SetActive(false);
            Purchase("refinery", "Rafinărie", 400, new Vector3(7.9f, 0f, 1f), m, refinery);
        }

        private static void BuildOrders(Materials m)
        {
            GameObject orders = Pad("OrderZone", null, new Vector3(-9f, 0f, 8f), new Vector2(3f, 3f), m["PadOrder"]);
            GameObject truck = BuildTruckModel("OrderTruck", orders.transform, new Vector3(-2.8f, 0f, 0f), 90f, m["OrderTruck"], m);
            GameObject dropPoint = Empty("DropPoint", truck.transform, new Vector3(0f, 1.2f, -0.6f));
            WorldLabel label = AddLabel(orders, "COMENZI", 1.6f, true, new Vector2(560f, 150f), 34);

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
            Primitive(PrimitiveType.Cube, "Cab", t, new Vector3(0f, 0.95f, 1.05f), new Vector3(1.8f, 1.4f, 1.1f), body);
            Primitive(PrimitiveType.Cube, "Window", t, new Vector3(0f, 1.3f, 1.62f), new Vector3(1.5f, 0.5f, 0.05f), m["Metal"]);
            Primitive(PrimitiveType.Cube, "Bed", t, new Vector3(0f, 0.6f, -0.6f), new Vector3(1.9f, 0.5f, 2.2f), m["Dark"]);
            foreach (float x in new[] { -0.95f, 0.95f })
            foreach (float z in new[] { -1.1f, 1f })
                Primitive(PrimitiveType.Cylinder, "Wheel", t, new Vector3(x, 0.35f, z), new Vector3(0.7f, 0.12f, 0.7f),
                    m["Dark"], localEuler: new Vector3(0f, 0f, 90f));
            return root;
        }

        private static void BuildWorkers(Materials m, GameData data)
        {
            var positions = new[] { new Vector3(-10.5f, 0f, 0f), new Vector3(-10.5f, 0f, -3.5f), new Vector3(-10.5f, 0f, -7f) };
            var costs = new long[] { 600, 2000, 6000 };

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

            GameObject model = Empty("Model", root.transform, Vector3.zero);
            model.transform.localScale = Vector3.one * 0.85f;
            Transform pivot = BuildHumanoid(model.transform, m["Worker"], m["Helmet"], m);
            GameObject anchor = Empty("StackAnchor", root.transform, new Vector3(0f, 0.65f, -0.5f));

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

            var swing = root.AddComponent<PickaxeSwing>();
            Set(swing, "swingSource", ai);
            Set(swing, "pivot", pivot);
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
            Purchase("drill", "Burghiu", 5000, new Vector3(5f, 0f, -13f), m, drill);

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
            GameObject seatZone = Empty("SeatZone", root.transform, new Vector3(-1.75f, 0f, 0f));
            Primitive(PrimitiveType.Cube, "Pad", seatZone.transform, new Vector3(0f, 0.03f, 0f), new Vector3(1.4f, 0.06f, 1.4f), m["PadLoad"]);
            AddLabel(seatZone, "URCĂ", 0.9f, false, new Vector2(160f, 60f), 34);
            ZoneArea area = AddZone(seatZone, new Vector3(1.5f, 4f, 1.5f));
            var seat = seatZone.AddComponent<VehicleSeat>();
            Set(seat, "vehicle", vehicle);
            Set(seat, "area", area);
        }

        private static GameObject BuildBulldozer(Vector3 position, Materials m, VehicleDefinition definition)
        {
            GameObject root = BuildVehicleBase("Bulldozer", position, out Transform seat, out Transform exit);
            Transform t = root.transform;
            Primitive(PrimitiveType.Cube, "Body", t, new Vector3(0f, 0.75f, 0f), new Vector3(1.6f, 0.9f, 2f), m["Bulldozer"]);
            Primitive(PrimitiveType.Cube, "SeatBlock", t, new Vector3(0f, 1.25f, -0.45f), new Vector3(0.7f, 0.2f, 0.7f), m["Dark"]);
            Primitive(PrimitiveType.Cube, "TrackL", t, new Vector3(-0.95f, 0.35f, 0f), new Vector3(0.45f, 0.6f, 2.3f), m["Dark"]);
            Primitive(PrimitiveType.Cube, "TrackR", t, new Vector3(0.95f, 0.35f, 0f), new Vector3(0.45f, 0.6f, 2.3f), m["Dark"]);
            Primitive(PrimitiveType.Cylinder, "Exhaust", t, new Vector3(0.5f, 1.5f, -0.8f), new Vector3(0.15f, 0.5f, 0.15f), m["Dark"]);
            GameObject blade = Empty("BladePivot", t, new Vector3(0f, 0f, 1.25f));
            Primitive(PrimitiveType.Cube, "Blade", blade.transform, new Vector3(0f, 0.45f, 0.15f), new Vector3(2.6f, 0.8f, 0.15f), m["Metal"]);

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
            Primitive(PrimitiveType.Cube, "Body", t, new Vector3(0f, 0.75f, 0f), new Vector3(1.6f, 1f, 2f), m["Drill"]);
            Primitive(PrimitiveType.Cube, "SeatBlock", t, new Vector3(0f, 1.3f, -0.45f), new Vector3(0.7f, 0.2f, 0.7f), m["Dark"]);
            foreach (float x in new[] { -0.9f, 0.9f })
            foreach (float z in new[] { -0.7f, 0.7f })
                Primitive(PrimitiveType.Cylinder, "Wheel", t, new Vector3(x, 0.35f, z), new Vector3(0.7f, 0.12f, 0.7f),
                    m["Dark"], localEuler: new Vector3(0f, 0f, 90f));
            GameObject bit = Empty("DrillBit", t, new Vector3(0f, 0.7f, 1.1f));
            Primitive(PrimitiveType.Cylinder, "Bit", bit.transform, new Vector3(0f, 0f, 0.45f), new Vector3(0.55f, 0.45f, 0.55f),
                m["Metal"], localEuler: new Vector3(90f, 0f, 0f));
            Primitive(PrimitiveType.Cube, "ToothA", bit.transform, new Vector3(0.3f, 0f, 0.5f), new Vector3(0.1f, 0.1f, 0.8f), m["Dark"]);
            Primitive(PrimitiveType.Cube, "ToothB", bit.transform, new Vector3(-0.3f, 0f, 0.5f), new Vector3(0.1f, 0.1f, 0.8f), m["Dark"]);

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
            GameObject anchor = Empty("StackAnchor", truck.transform, new Vector3(0f, 0.95f, 0.2f));
            var stack = truck.AddComponent<ResourceStack>();
            Edit(stack, so =>
            {
                P(so, "anchor").objectReferenceValue = anchor.transform;
                P(so, "capacity").intValue = 40;
                P(so, "itemsPerColumn").intValue = 5;
                P(so, "columnsPerRow").intValue = 4;
                P(so, "itemSpacing").vector3Value = new Vector3(0.3f, 0.28f, 0.3f);
            });
            WorldLabel label = AddLabel(truck, "CAMION", 2.8f, true, new Vector2(400f, 140f), 34);
            var controller = truck.AddComponent<Truck>();
            Set(controller, "stack", stack);
            Set(controller, "parkPoint", park.transform);
            Set(controller, "sellPoint", sellPoint.transform);
            Set(controller, "label", label);

            GameObject load = Pad("TruckLoad", null, new Vector3(0f, 0f, -11.5f), new Vector2(2.4f, 2.4f), m["PadLoad"]);
            AddLabel(load, "ÎNCARCĂ\nCAMIONUL", 1.1f, false, new Vector2(300f, 110f), 32);
            var transfer = load.AddComponent<StackTransferZone>();
            Set(transfer, "area", load.GetComponent<ZoneArea>());
            Set(transfer, "target", stack);
            Set(transfer, "truck", controller);

            truck.SetActive(false);
            load.SetActive(false);
            Purchase("truck", "Camion", 2500, new Vector3(0f, 0f, -11.5f), m, truck, load);
        }

        // ------------------------------------------------------------ Cameră

        private static void BuildCamera(Transform player)
        {
            Quaternion rotation = Quaternion.Euler(50f, 45f, 0f);

            var cameraGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var camera = cameraGo.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Hex("#87B9E0");
            camera.fieldOfView = 40f;
            camera.farClipPlane = 120f;
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
            var switcher = vcamGo.AddComponent<CameraTargetSwitcher>();
            Set(switcher, "virtualCamera", vcam);
            Set(switcher, "player", player);
        }
    }
}
