using System.Collections.Generic;
using UndergroundMetroTycoon.Simulation;
using UnityEngine;
using UnityEngine.Rendering;

namespace UndergroundMetroTycoon.Game
{
    /// <summary>
    /// Procedurally builds the miniature construction-site world around the excavation plot: sand work yard,
    /// grass, roads with traffic, city backdrop, trees, site props, an animated mini excavator, a tower crane,
    /// ambient workers and warm daylight. Static geometry is combined with static batching to keep draw calls low.
    /// All geometry is original placeholder art made from primitives; replace with authored prefabs later.
    /// </summary>
    public static class SiteEnvironmentBuilder
    {
        public sealed class Result
        {
            public readonly List<ObstacleBox> Obstacles = new List<ObstacleBox>();
            public readonly List<CharacterRig> AmbientWorkers = new List<CharacterRig>();
            public Rect WalkBounds;
            public Light Sun;
        }

        private const float WalkHalf = 7.4f;
        private const float YardHalf = 7.9f;

        public static Result Build(Transform parent, MaterialLibrary m, Vector2 siteCenter)
        {
            var result = new Result { WalkBounds = new Rect(siteCenter.x - WalkHalf, siteCenter.y - WalkHalf, WalkHalf * 2f, WalkHalf * 2f) };
            Transform root = PrimitiveFactory.Pivot("Environment", parent, new Vector3(siteCenter.x, 0f, siteCenter.y));
            Transform staticRoot = PrimitiveFactory.Pivot("Static", root, Vector3.zero);
            Transform dynamicRoot = PrimitiveFactory.Pivot("Dynamic", root, Vector3.zero);
            var rng = new System.Random(20261026);
            var ctx = new Ctx(m, staticRoot, dynamicRoot, result, siteCenter, rng);

            BuildGround(ctx);
            BuildRoads(ctx);
            BuildCity(ctx);
            BuildPerimeter(ctx);
            BuildYardProps(ctx);
            BuildMiniExcavator(ctx, new Vector3(6.1f, 0f, 1.6f), -100f);
            BuildDumpTruck(ctx, new Vector3(6.4f, 0f, -3.7f));
            BuildAmbientWorkers(ctx);
            BuildGreenery(ctx);
            BuildCrane(ctx, new Vector3(11.5f, 0f, 15.5f));

            if (PrimitiveFactory.MeshesReadable()) StaticBatchingUtility.Combine(staticRoot.gameObject);
            result.Sun = BuildLighting(parent);
            return result;
        }

        private sealed class Ctx
        {
            public readonly MaterialLibrary M;
            public readonly Transform Static;
            public readonly Transform Dynamic;
            public readonly Result Result;
            public readonly Vector2 Center;
            public readonly System.Random Rng;

            public Ctx(MaterialLibrary m, Transform s, Transform d, Result r, Vector2 c, System.Random rng)
            {
                M = m;
                Static = s;
                Dynamic = d;
                Result = r;
                Center = c;
                Rng = rng;
            }

            public Material Mat(Color c) => M.Get(c);
            public float Range(float min, float max) => min + (float)Rng.NextDouble() * (max - min);
            public void Block(float x, float z, float sizeX, float sizeZ) => Result.Obstacles.Add(ObstacleBox.FromCenter(Center.x + x, Center.y + z, sizeX, sizeZ));
        }

        // ---- Ground, roads, city ---------------------------------------------------------------------

        private static void BuildGround(Ctx c)
        {
            float plot = ExcavationSiteView.PlotHalfSize;
            Material yard = c.Mat(Palette.Hex(0xE6C891));
            Material grass = c.Mat(Palette.GrassFresh);
            const float outer = 40f, depth = 3f;

            // Sand work yard (ring between the dig plot and the yard edge).
            Box(c.Static, "YardW", -YardHalf, -plot, -YardHalf, YardHalf, -depth, 0f, yard);
            Box(c.Static, "YardE", plot, YardHalf, -YardHalf, YardHalf, -depth, 0f, yard);
            Box(c.Static, "YardS", -plot, plot, -YardHalf, -plot, -depth, 0f, yard);
            Box(c.Static, "YardN", -plot, plot, plot, YardHalf, -depth, 0f, yard);

            // Grass beyond the yard.
            Box(c.Static, "GrassW", -outer, -YardHalf, -outer, outer, -depth, -0.01f, grass);
            Box(c.Static, "GrassE", YardHalf, outer, -outer, outer, -depth, -0.01f, grass);
            Box(c.Static, "GrassS", -YardHalf, YardHalf, -outer, -YardHalf, -depth, -0.01f, grass);
            Box(c.Static, "GrassN", -YardHalf, YardHalf, YardHalf, outer, -depth, -0.01f, grass);

            // Timber kerb around the yard.
            Material timber = c.Mat(Palette.Hex(0xA8743F));
            Box(c.Static, "KerbS", -YardHalf, YardHalf, -YardHalf - 0.12f, -YardHalf, 0f, 0.1f, timber);
            Box(c.Static, "KerbW", -YardHalf - 0.12f, -YardHalf, -YardHalf, YardHalf, 0f, 0.1f, timber);

            // Gravel speckles give the sand some texture.
            Material gravelA = c.Mat(Palette.Hex(0xC9AD7D));
            Material gravelB = c.Mat(Palette.Hex(0xF2DDB0));
            for (int i = 0; i < 70; i++)
            {
                float x = c.Range(-YardHalf + 0.3f, YardHalf - 0.3f);
                float z = c.Range(-YardHalf + 0.3f, YardHalf - 0.3f);
                if (Mathf.Abs(x) < plot + 0.2f && Mathf.Abs(z) < plot + 0.2f) continue;
                float s = c.Range(0.08f, 0.2f);
                PrimitiveFactory.Create("Gravel", PrimitiveType.Cylinder, c.Static, new Vector3(x, 0.005f, z), new Vector3(s, 0.006f, s * c.Range(0.7f, 1.2f)),
                    i % 2 == 0 ? gravelA : gravelB, false);
            }
            // Tyre tracks leading to the dump truck.
            Material track = c.Mat(Palette.Hex(0xCDAE78));
            Box(c.Static, "TrackA", 5.95f, 6.15f, -7.8f, -5.2f, 0f, 0.008f, track);
            Box(c.Static, "TrackB", 6.65f, 6.85f, -7.8f, -5.2f, 0f, 0.008f, track);
        }

        private static void BuildRoads(Ctx c)
        {
            Material asphalt = c.Mat(Palette.Hex(0x4A5363));
            Material line = c.Mat(Palette.White);
            Material sidewalk = c.Mat(Palette.Hex(0xD9DEE6));
            Material curb = c.Mat(Palette.Hex(0xAEB7C4));

            Box(c.Static, "SidewalkN", -30f, 9f, 7.95f, 8.85f, -0.02f, 0.05f, sidewalk);
            Box(c.Static, "SidewalkE", 7.95f, 8.85f, -30f, 7.95f, -0.02f, 0.05f, sidewalk);
            Box(c.Static, "CurbN", -30f, 9f, 8.85f, 9.05f, -0.02f, 0.09f, curb);
            Box(c.Static, "CurbE", 8.85f, 9.05f, -30f, 8.85f, -0.02f, 0.09f, curb);
            Box(c.Static, "RoadN", -30f, 30f, 9.05f, 13f, -0.02f, 0.02f, asphalt);
            Box(c.Static, "RoadE", 9.05f, 13f, -30f, 9.05f, -0.02f, 0.02f, asphalt);
            for (float x = -24f; x < 24f; x += 2.4f) Box(c.Static, "Dash", x, x + 1.2f, 10.96f, 11.08f, 0f, 0.028f, line);
            for (float z = -24f; z < 8f; z += 2.4f) Box(c.Static, "Dash", 10.96f, 11.08f, z, z + 1.2f, 0f, 0.028f, line);
            // Zebra crossing at the corner.
            for (int i = 0; i < 6; i++) Box(c.Static, "Zebra", 9.3f + i * 0.6f, 9.6f + i * 0.6f, 7.0f, 8.6f, 0f, 0.03f, line);

            // Street lamps.
            Material pole = c.Mat(Palette.Hex(0x34425A));
            Material glow = c.Mat(Palette.Hex(0xFFF1B8));
            for (float x = -14f; x <= 6f; x += 5f) Lamp(c, new Vector3(x, 0f, 8.6f), pole, glow);
            for (float z = -14f; z <= 4f; z += 5f) Lamp(c, new Vector3(8.6f, 0f, z), pole, glow);

            // Traffic.
            Car(c, Palette.Hex(0xF0544F), new Vector3(-28f, 0f, 10.1f), new Vector3(28f, 0f, 10.1f), 3.2f, 0f);
            Car(c, Palette.Hex(0x2F7BFF), new Vector3(28f, 0f, 12f), new Vector3(-28f, 0f, 12f), 2.6f, 0.4f);
            Car(c, Palette.Hex(0xFFC52E), new Vector3(10.1f, 0f, -28f), new Vector3(10.1f, 0f, 9f), 2.9f, 0.6f);
            Car(c, Palette.Hex(0x34D1C6), new Vector3(12f, 0f, 9f), new Vector3(12f, 0f, -28f), 3.4f, 0.2f);
        }

        private static void Lamp(Ctx c, Vector3 p, Material pole, Material glow)
        {
            Transform t = PrimitiveFactory.Pivot("Lamp", c.Static, p);
            PrimitiveFactory.Create("Pole", PrimitiveType.Cylinder, t, new Vector3(0f, 1.3f, 0f), new Vector3(0.08f, 1.3f, 0.08f), pole);
            PrimitiveFactory.Box("Arm", t, new Vector3(0f, 2.55f, 0.2f), new Vector3(0.07f, 0.07f, 0.45f), pole);
            PrimitiveFactory.Box("Head", t, new Vector3(0f, 2.5f, 0.42f), new Vector3(0.22f, 0.08f, 0.3f), glow, false);
        }

        private static void Car(Ctx c, Color body, Vector3 from, Vector3 to, float speed, float offset)
        {
            Transform car = PrimitiveFactory.Pivot("Car", c.Dynamic, from);
            Material paint = c.Mat(body);
            PrimitiveFactory.Box("Body", car, new Vector3(0f, 0.32f, 0f), new Vector3(0.9f, 0.32f, 1.8f), paint);
            PrimitiveFactory.Box("Cabin", car, new Vector3(0f, 0.6f, -0.1f), new Vector3(0.8f, 0.3f, 0.95f), paint);
            PrimitiveFactory.Box("Glass", car, new Vector3(0f, 0.62f, -0.1f), new Vector3(0.82f, 0.2f, 0.9f), c.Mat(Palette.Hex(0x9ED8FF)), false);
            Material tyre = c.Mat(Palette.Hex(0x22262E));
            foreach (float z in new[] { 0.55f, -0.55f })
                foreach (float x in new[] { -0.43f, 0.43f })
                {
                    GameObject w = PrimitiveFactory.Create("Wheel", PrimitiveType.Cylinder, car, new Vector3(x, 0.16f, z), new Vector3(0.3f, 0.06f, 0.3f), tyre, false);
                    w.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                }
            car.gameObject.AddComponent<AmbientMotion>().Drive(from, to, speed, offset);
        }

        private static void BuildCity(Ctx c)
        {
            Color[] facades =
            {
                Palette.Hex(0x8EC5FF), Palette.Hex(0xF6E2C3), Palette.Hex(0xA8E6CF), Palette.Hex(0xFFC59E),
                Palette.Hex(0xC9C2F2), Palette.Hex(0xE8F1F8), Palette.Hex(0xFFE08A), Palette.Hex(0xB5E3F0)
            };
            int index = 0;
            for (float x = -18f; x <= 16f; x += 4.4f) Building(c, new Vector3(x, 0f, 16.3f + c.Range(0f, 1.4f)), facades[index++ % facades.Length]);
            for (float z = -16f; z <= 10f; z += 4.4f) Building(c, new Vector3(16.3f + c.Range(0f, 1.4f), 0f, z), facades[index++ % facades.Length]);
        }

        private static void Building(Ctx c, Vector3 position, Color facade)
        {
            float width = c.Range(3.2f, 3.9f);
            float height = c.Range(4.5f, 13f);
            Transform b = PrimitiveFactory.Pivot("Building", c.Static, position);
            Material wall = c.Mat(facade);
            Material trim = c.Mat(Palette.Shade(facade, 0.82f));
            Material glass = c.Mat(Palette.Hex(0x3C6EA8));
            Material roof = c.Mat(Palette.Hex(0x9AA6B6));

            PrimitiveFactory.Box("Body", b, new Vector3(0f, height * 0.5f, 0f), new Vector3(width, height, width), wall);
            PrimitiveFactory.Box("Base", b, new Vector3(0f, 0.45f, 0f), new Vector3(width + 0.1f, 0.9f, width + 0.1f), trim);
            PrimitiveFactory.Box("Roof", b, new Vector3(0f, height + 0.12f, 0f), new Vector3(width + 0.2f, 0.24f, width + 0.2f), trim);
            PrimitiveFactory.Box("RoofUnit", b, new Vector3(width * 0.2f, height + 0.5f, -width * 0.15f), new Vector3(0.9f, 0.55f, 0.7f), roof);
            PrimitiveFactory.Create("Tank", PrimitiveType.Cylinder, b, new Vector3(-width * 0.25f, height + 0.6f, width * 0.2f), new Vector3(0.6f, 0.4f, 0.6f), roof);

            // Windows on the two faces the isometric camera sees, with sills for depth.
            int floors = Mathf.FloorToInt((height - 1.4f) / 1.25f);
            for (int f = 0; f < floors; f++)
            {
                float y = 1.5f + f * 1.25f;
                for (int w = 0; w < 3; w++)
                {
                    float off = (w - 1) * width * 0.28f;
                    PrimitiveFactory.Box("WinS", b, new Vector3(off, y, -width * 0.5f - 0.02f), new Vector3(width * 0.2f, 0.6f, 0.04f), glass, false);
                    PrimitiveFactory.Box("WinW", b, new Vector3(-width * 0.5f - 0.02f, y, off), new Vector3(0.04f, 0.6f, width * 0.2f), glass, false);
                }
                PrimitiveFactory.Box("SillS", b, new Vector3(0f, y - 0.36f, -width * 0.5f - 0.04f), new Vector3(width * 0.9f, 0.06f, 0.08f), trim, false);
            }
            // Shop awning on the street side.
            PrimitiveFactory.Box("Awning", b, new Vector3(0f, 1.05f, -width * 0.5f - 0.3f), new Vector3(width * 0.8f, 0.08f, 0.6f), c.Mat(UITheme.Orange), false);
        }

        private static void BuildPerimeter(Ctx c)
        {
            Material red = c.Mat(UITheme.Red);
            Material white = c.Mat(Palette.White);
            Material foot = c.Mat(Palette.Hex(0x5B6678));
            Material lamp = c.Mat(UITheme.Gold);
            const float edge = YardHalf - 0.15f;
            // Low striped barriers on the near (south/west) edges keep the view toward the player open.
            for (float p = -edge; p < edge - 1f; p += 1.7f)
            {
                Barrier(c, new Vector3(p + 0.85f, 0f, -edge), true, red, white, foot, lamp);
                Barrier(c, new Vector3(-edge, 0f, p + 0.85f), false, red, white, foot, lamp);
            }
            // Mesh fence on the far edges.
            Material post = c.Mat(Palette.Hex(0x8C99AB));
            Material mesh = c.Mat(Palette.Hex(0xDCE5EF));
            for (float p = -edge; p <= edge; p += 2f)
            {
                PrimitiveFactory.Create("FencePost", PrimitiveType.Cylinder, c.Static, new Vector3(p, 0.6f, edge + 0.1f), new Vector3(0.07f, 0.6f, 0.07f), post);
                PrimitiveFactory.Create("FencePost", PrimitiveType.Cylinder, c.Static, new Vector3(edge + 0.1f, 0.6f, p), new Vector3(0.07f, 0.6f, 0.07f), post);
            }
            Box(c.Static, "FenceN", -edge, edge, edge + 0.08f, edge + 0.12f, 0.1f, 1.15f, mesh);
            Box(c.Static, "FenceE", edge + 0.08f, edge + 0.12f, -edge, edge, 0.1f, 1.15f, mesh);
            Box(c.Static, "FenceRailN", -edge, edge, edge + 0.06f, edge + 0.14f, 1.12f, 1.2f, post);
            Box(c.Static, "FenceRailE", edge + 0.06f, edge + 0.14f, -edge, edge, 1.12f, 1.2f, post);
        }

        private static void Barrier(Ctx c, Vector3 center, bool alongX, Material red, Material white, Material foot, Material lamp)
        {
            Transform b = PrimitiveFactory.Pivot("Barrier", c.Static, center);
            if (!alongX) b.localRotation = Quaternion.Euler(0f, 90f, 0f);
            for (int i = 0; i < 4; i++)
                PrimitiveFactory.Box("Stripe", b, new Vector3(-0.6f + i * 0.4f, 0.55f, 0f), new Vector3(0.4f, 0.24f, 0.07f), i % 2 == 0 ? red : white, false);
            PrimitiveFactory.Box("FootL", b, new Vector3(-0.68f, 0.28f, 0f), new Vector3(0.08f, 0.56f, 0.34f), foot);
            PrimitiveFactory.Box("FootR", b, new Vector3(0.68f, 0.28f, 0f), new Vector3(0.08f, 0.56f, 0.34f), foot);
            PrimitiveFactory.Create("Light", PrimitiveType.Sphere, b, new Vector3(-0.68f, 0.72f, 0f), Vector3.one * 0.12f, lamp, false);
        }

        // ---- Yard props ------------------------------------------------------------------------------

        private static void BuildYardProps(Ctx c)
        {
            // Site office container with windows, AC unit and steps.
            Transform office = PrimitiveFactory.Pivot("SiteOffice", c.Static, new Vector3(-6.45f, 0f, 3.0f));
            Material blue = c.Mat(UITheme.Blue);
            Material frame = c.Mat(Palette.White);
            Material glass = c.Mat(Palette.Hex(0xA6DBFF));
            PrimitiveFactory.Box("Body", office, new Vector3(0f, 0.75f, 0f), new Vector3(1.7f, 1.5f, 3.2f), blue);
            PrimitiveFactory.Box("Roof", office, new Vector3(0f, 1.55f, 0f), new Vector3(1.8f, 0.12f, 3.3f), frame);
            for (int i = 0; i < 4; i++)
                PrimitiveFactory.Box("Rib", office, new Vector3(0.86f, 0.75f, -1.2f + i * 0.8f), new Vector3(0.03f, 1.5f, 0.06f), c.Mat(UITheme.BlueDark), false);
            PrimitiveFactory.Box("WindowFrame", office, new Vector3(0.87f, 0.95f, -0.55f), new Vector3(0.03f, 0.6f, 1.1f), frame, false);
            PrimitiveFactory.Box("Window", office, new Vector3(0.885f, 0.95f, -0.55f), new Vector3(0.03f, 0.48f, 0.98f), glass, false);
            PrimitiveFactory.Box("Door", office, new Vector3(0.87f, 0.6f, 0.85f), new Vector3(0.03f, 1.1f, 0.6f), frame, false);
            PrimitiveFactory.Box("Steps", office, new Vector3(1.1f, 0.08f, 0.85f), new Vector3(0.45f, 0.16f, 0.7f), c.Mat(Palette.Hex(0x8C99AB)));
            PrimitiveFactory.Box("AC", office, new Vector3(0.2f, 1.75f, 1.0f), new Vector3(0.6f, 0.3f, 0.5f), c.Mat(Palette.Hex(0xE9EEF4)));
            PrimitiveFactory.Box("Sign", office, new Vector3(0.9f, 1.3f, 0.0f), new Vector3(0.03f, 0.22f, 1.4f), c.Mat(UITheme.Gold), false);
            c.Block(-6.45f, 3.0f, 1.8f, 3.3f);

            // Portable toilet.
            Transform toilet = PrimitiveFactory.Pivot("Toilet", c.Static, new Vector3(-6.7f, 0f, 6.1f));
            PrimitiveFactory.Box("Body", toilet, new Vector3(0f, 0.8f, 0f), new Vector3(0.8f, 1.6f, 0.8f), c.Mat(UITheme.Turquoise));
            PrimitiveFactory.Box("Roof", toilet, new Vector3(0f, 1.66f, 0f), new Vector3(0.86f, 0.1f, 0.86f), frame);
            PrimitiveFactory.Box("Door", toilet, new Vector3(0.41f, 0.75f, 0f), new Vector3(0.02f, 1.2f, 0.55f), c.Mat(Palette.Shade(UITheme.Turquoise, 0.8f)), false);
            c.Block(-6.7f, 6.1f, 0.9f, 0.9f);

            // Pipe stack on wooden chocks.
            Transform pipes = PrimitiveFactory.Pivot("Pipes", c.Static, new Vector3(6.2f, 0f, 5.4f));
            Material chock = c.Mat(Palette.Hex(0xA8743F));
            PrimitiveFactory.Box("ChockA", pipes, new Vector3(-0.7f, 0.08f, 0f), new Vector3(0.2f, 0.16f, 1.6f), chock);
            PrimitiveFactory.Box("ChockB", pipes, new Vector3(0.7f, 0.08f, 0f), new Vector3(0.2f, 0.16f, 1.6f), chock);
            Material pipeOrange = c.Mat(Palette.Hex(0xFF8A3D));
            Material pipeBlue = c.Mat(Palette.Hex(0x3D7DDB));
            Material pipeInside = c.Mat(Palette.Hex(0x40302A));
            Vector3[] pipeSpots = { new Vector3(0f, 0.5f, -0.45f), new Vector3(0f, 0.5f, 0.45f), new Vector3(0f, 1.15f, 0f) };
            for (int i = 0; i < pipeSpots.Length; i++)
            {
                GameObject pipe = PrimitiveFactory.Create("Pipe", PrimitiveType.Cylinder, pipes, pipeSpots[i], new Vector3(0.68f, 1.1f, 0.68f), i == 2 ? pipeBlue : pipeOrange);
                pipe.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                GameObject hole = PrimitiveFactory.Create("Bore", PrimitiveType.Cylinder, pipes, pipeSpots[i] + new Vector3(-1.1f, 0f, 0f), new Vector3(0.5f, 0.012f, 0.5f), pipeInside, false);
                hole.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            }
            c.Block(6.2f, 5.4f, 2.4f, 1.7f);

            // Wooden crates.
            Transform crates = PrimitiveFactory.Pivot("Crates", c.Static, new Vector3(-6.1f, 0f, -1.6f));
            Crate(c, crates, new Vector3(0f, 0.35f, 0f), 0.7f);
            Crate(c, crates, new Vector3(0.05f, 1.0f, 0.05f), 0.6f, 12f);
            Crate(c, crates, new Vector3(0f, 0.3f, 0.85f), 0.6f, -8f);
            c.Block(-6.1f, -1.2f, 0.9f, 1.8f);

            // Toolbox and wheelbarrow near the south edge.
            Transform toolbox = PrimitiveFactory.Pivot("Toolbox", c.Static, new Vector3(-4.6f, 0f, -6.4f));
            PrimitiveFactory.Box("Box", toolbox, new Vector3(0f, 0.17f, 0f), new Vector3(0.7f, 0.34f, 0.36f), c.Mat(UITheme.Red));
            PrimitiveFactory.Box("Lid", toolbox, new Vector3(0f, 0.36f, 0f), new Vector3(0.72f, 0.05f, 0.38f), c.Mat(UITheme.RedDark));
            PrimitiveFactory.Box("Handle", toolbox, new Vector3(0f, 0.47f, 0f), new Vector3(0.36f, 0.05f, 0.05f), c.Mat(Palette.Black));
            c.Block(-4.6f, -6.4f, 0.8f, 0.5f);

            Transform barrow = PrimitiveFactory.Pivot("Wheelbarrow", c.Static, new Vector3(-2.7f, 0f, -6.5f));
            barrow.localRotation = Quaternion.Euler(0f, 30f, 0f);
            PrimitiveFactory.Box("Tray", barrow, new Vector3(0f, 0.45f, 0.1f), new Vector3(0.65f, 0.3f, 0.85f), c.Mat(UITheme.Green));
            PrimitiveFactory.Box("Soil", barrow, new Vector3(0f, 0.6f, 0.1f), new Vector3(0.55f, 0.06f, 0.72f), c.Mat(Palette.TerrainTop(TerrainMaterial.SoftSoil)), false);
            GameObject wheel = PrimitiveFactory.Create("Wheel", PrimitiveType.Cylinder, barrow, new Vector3(0f, 0.2f, 0.62f), new Vector3(0.4f, 0.05f, 0.4f), c.Mat(Palette.Black));
            wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            PrimitiveFactory.Box("HandleL", barrow, new Vector3(-0.25f, 0.4f, -0.55f), new Vector3(0.05f, 0.05f, 0.7f), c.Mat(Palette.Wood));
            PrimitiveFactory.Box("HandleR", barrow, new Vector3(0.25f, 0.4f, -0.55f), new Vector3(0.05f, 0.05f, 0.7f), c.Mat(Palette.Wood));
            c.Block(-2.7f, -6.5f, 0.9f, 1.3f);

            // Brick pallet and cement bags.
            Transform pallet = PrimitiveFactory.Pivot("BrickPallet", c.Static, new Vector3(-6.4f, 0f, -4.4f));
            PrimitiveFactory.Box("Pallet", pallet, new Vector3(0f, 0.06f, 0f), new Vector3(1.1f, 0.12f, 1.1f), chock);
            Material brick = c.Mat(Palette.Hex(0xD0603E));
            Material brickDark = c.Mat(Palette.Hex(0xB24E31));
            for (int y = 0; y < 3; y++)
                for (int x = 0; x < 3; x++)
                    PrimitiveFactory.Box("Brick", pallet, new Vector3(-0.34f + x * 0.34f, 0.2f + y * 0.16f, 0f), new Vector3(0.32f, 0.14f, 1.0f), (x + y) % 2 == 0 ? brick : brickDark);
            c.Block(-6.4f, -4.4f, 1.1f, 1.1f);

            Transform bags = PrimitiveFactory.Pivot("CementBags", c.Static, new Vector3(3.6f, 0f, -6.6f));
            Material bag = c.Mat(Palette.Hex(0xE4E7EC));
            Material bagBand = c.Mat(UITheme.Blue);
            for (int i = 0; i < 5; i++)
            {
                float x = (i % 3 - 1) * 0.5f + (i >= 3 ? 0.25f : 0f);
                float y = i >= 3 ? 0.36f : 0.12f;
                Transform b = PrimitiveFactory.Box("Bag", bags, new Vector3(x, y, 0f), new Vector3(0.46f, 0.22f, 0.7f), bag).transform;
                PrimitiveFactory.Box("Band", b, new Vector3(0f, 0.01f, 0f), new Vector3(1.02f, 1.02f, 0.25f), bagBand, false);
            }
            c.Block(3.6f, -6.6f, 1.6f, 0.8f);

            // Floodlight tower.
            Transform tower = PrimitiveFactory.Pivot("Floodlight", c.Static, new Vector3(-6.9f, 0f, -6.9f));
            PrimitiveFactory.Box("Base", tower, new Vector3(0f, 0.2f, 0f), new Vector3(0.8f, 0.4f, 0.8f), c.Mat(UITheme.Gold));
            PrimitiveFactory.Create("Mast", PrimitiveType.Cylinder, tower, new Vector3(0f, 1.6f, 0f), new Vector3(0.1f, 1.4f, 0.1f), c.Mat(Palette.Hex(0x5B6678)));
            PrimitiveFactory.Box("Lamps", tower, new Vector3(0.1f, 3.0f, 0.1f), new Vector3(0.7f, 0.35f, 0.15f), c.Mat(Palette.Hex(0xFFF4CC)), false);
            c.Block(-6.9f, -6.9f, 0.9f, 0.9f);

            // Cones around the dig plot.
            Material orange = c.Mat(UITheme.Orange);
            foreach (Vector3 p in new[] { new Vector3(-5.6f, 0f, -5.6f), new Vector3(5.6f, 0f, -5.6f), new Vector3(-1.0f, 0f, 5.7f), new Vector3(-5.6f, 0f, 5.6f), new Vector3(0.4f, 0f, -5.7f) })
            {
                Transform cone = PrimitiveFactory.Pivot("Cone", c.Static, p);
                PrimitiveFactory.Box("Base", cone, new Vector3(0f, 0.03f, 0f), new Vector3(0.42f, 0.06f, 0.42f), orange);
                PrimitiveFactory.Create("Low", PrimitiveType.Cylinder, cone, new Vector3(0f, 0.17f, 0f), new Vector3(0.27f, 0.11f, 0.27f), orange);
                PrimitiveFactory.Create("Band", PrimitiveType.Cylinder, cone, new Vector3(0f, 0.32f, 0f), new Vector3(0.21f, 0.04f, 0.21f), frame, false);
                PrimitiveFactory.Create("Top", PrimitiveType.Cylinder, cone, new Vector3(0f, 0.46f, 0f), new Vector3(0.14f, 0.1f, 0.14f), orange);
            }
        }

        private static void Crate(Ctx c, Transform parent, Vector3 position, float size, float yaw = 0f)
        {
            Transform crate = PrimitiveFactory.Box("Crate", parent, position, Vector3.one * size, c.Mat(Palette.Hex(0xD49A5C))).transform;
            crate.localRotation = Quaternion.Euler(0f, yaw, 0f);
            Material slat = c.Mat(Palette.Hex(0xA8703A));
            PrimitiveFactory.Box("SlatTop", crate, new Vector3(0f, 0.5f, 0f), new Vector3(1.02f, 0.03f, 0.18f), slat, false);
            PrimitiveFactory.Box("SlatX", crate, new Vector3(0f, 0f, -0.5f), new Vector3(1.02f, 0.18f, 0.03f), slat, false);
            PrimitiveFactory.Box("SlatZ", crate, new Vector3(-0.5f, 0f, 0f), new Vector3(0.03f, 0.18f, 1.02f), slat, false);
            PrimitiveFactory.Box("EdgeA", crate, new Vector3(-0.5f, 0f, -0.5f), new Vector3(0.08f, 1.02f, 0.08f), slat, false);
        }

        // ---- Machines and workers --------------------------------------------------------------------

        private static void BuildMiniExcavator(Ctx c, Vector3 position, float yaw)
        {
            Material yellow = c.Mat(UITheme.Gold);
            Material dark = c.Mat(Palette.Hex(0x2B313C));
            Material glass = c.Mat(Palette.Hex(0x9ED8FF));
            Material steel = c.Mat(Palette.Hex(0x7E8B9C));

            Transform root = PrimitiveFactory.Pivot("MiniExcavator", c.Dynamic, position);
            root.localRotation = Quaternion.Euler(0f, yaw, 0f);
            PrimitiveFactory.Box("TrackL", root, new Vector3(-0.5f, 0.2f, 0f), new Vector3(0.32f, 0.4f, 1.6f), dark);
            PrimitiveFactory.Box("TrackR", root, new Vector3(0.5f, 0.2f, 0f), new Vector3(0.32f, 0.4f, 1.6f), dark);
            PrimitiveFactory.Box("Blade", root, new Vector3(0f, 0.2f, 0.95f), new Vector3(1.3f, 0.3f, 0.1f), yellow);

            Transform house = PrimitiveFactory.Pivot("House", root, new Vector3(0f, 0.45f, 0f));
            house.gameObject.AddComponent<AmbientMotion>().Oscillate(Vector3.up, 22f, 0.12f);
            PrimitiveFactory.Box("Body", house, new Vector3(0f, 0.3f, -0.2f), new Vector3(1.15f, 0.55f, 1.2f), yellow);
            PrimitiveFactory.Box("Counterweight", house, new Vector3(0f, 0.35f, -0.85f), new Vector3(1.1f, 0.45f, 0.25f), c.Mat(UITheme.GoldDark));
            PrimitiveFactory.Box("Cab", house, new Vector3(-0.22f, 0.95f, -0.1f), new Vector3(0.6f, 0.75f, 0.7f), yellow);
            PrimitiveFactory.Box("CabGlass", house, new Vector3(-0.22f, 1.0f, -0.1f), new Vector3(0.62f, 0.5f, 0.6f), glass, false);
            PrimitiveFactory.Box("Roof", house, new Vector3(-0.22f, 1.36f, -0.1f), new Vector3(0.66f, 0.06f, 0.76f), dark);

            Transform boom = PrimitiveFactory.Pivot("Boom", house, new Vector3(0.3f, 0.6f, 0.35f));
            boom.localRotation = Quaternion.Euler(-35f, 0f, 0f);
            boom.gameObject.AddComponent<AmbientMotion>().Oscillate(Vector3.right, 14f, 0.35f);
            PrimitiveFactory.Box("BoomArm", boom, new Vector3(0f, 0f, 0.6f), new Vector3(0.18f, 0.2f, 1.25f), yellow);
            Transform stick = PrimitiveFactory.Pivot("Stick", boom, new Vector3(0f, 0f, 1.2f));
            stick.localRotation = Quaternion.Euler(95f, 0f, 0f);
            stick.gameObject.AddComponent<AmbientMotion>().Oscillate(Vector3.right, 18f, 0.35f, 1.2f);
            PrimitiveFactory.Box("StickArm", stick, new Vector3(0f, 0f, 0.45f), new Vector3(0.14f, 0.16f, 0.9f), yellow);
            PrimitiveFactory.Create("Piston", PrimitiveType.Cylinder, stick, new Vector3(0f, 0.12f, 0.3f), new Vector3(0.06f, 0.3f, 0.06f), steel).transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            Transform bucket = PrimitiveFactory.Pivot("Bucket", stick, new Vector3(0f, 0f, 0.92f));
            bucket.localRotation = Quaternion.Euler(30f, 0f, 0f);
            bucket.gameObject.AddComponent<AmbientMotion>().Oscillate(Vector3.right, 25f, 0.35f, 2.2f);
            PrimitiveFactory.Box("BucketBody", bucket, new Vector3(0f, -0.08f, 0.12f), new Vector3(0.4f, 0.28f, 0.3f), dark);
            PrimitiveFactory.Box("Teeth", bucket, new Vector3(0f, -0.25f, 0.24f), new Vector3(0.42f, 0.06f, 0.08f), steel);

            c.Block(position.x, position.z, 1.7f, 2.0f);
        }

        private static void BuildDumpTruck(Ctx c, Vector3 position)
        {
            Transform truck = PrimitiveFactory.Pivot("DumpTruck", c.Static, position);
            Material yellow = c.Mat(UITheme.Gold);
            Material tyre = c.Mat(Palette.Hex(0x22262E));
            Material hub = c.Mat(Palette.Hex(0xB8C4D2));
            PrimitiveFactory.Box("Chassis", truck, new Vector3(0f, 0.45f, 0f), new Vector3(1.25f, 0.3f, 2.5f), c.Mat(UITheme.Navy));
            PrimitiveFactory.Box("Cab", truck, new Vector3(0f, 1.0f, 0.82f), new Vector3(1.15f, 0.85f, 0.75f), yellow);
            PrimitiveFactory.Box("CabGlass", truck, new Vector3(0f, 1.15f, 1.2f), new Vector3(0.95f, 0.38f, 0.03f), c.Mat(Palette.Hex(0x9ED8FF)), false);
            PrimitiveFactory.Box("Beacon", truck, new Vector3(0f, 1.48f, 0.82f), new Vector3(0.2f, 0.12f, 0.2f), c.Mat(UITheme.Orange), false);
            PrimitiveFactory.Box("Bed", truck, new Vector3(0f, 0.95f, -0.45f), new Vector3(1.25f, 0.65f, 1.5f), c.Mat(UITheme.Orange));
            PrimitiveFactory.Box("Load", truck, new Vector3(0f, 1.3f, -0.45f), new Vector3(1.05f, 0.2f, 1.3f), c.Mat(Palette.TerrainTop(TerrainMaterial.SoftSoil)), false);
            foreach (float z in new[] { 0.78f, -0.75f })
                foreach (float x in new[] { -0.64f, 0.64f })
                {
                    GameObject wheel = PrimitiveFactory.Create("Wheel", PrimitiveType.Cylinder, truck, new Vector3(x, 0.32f, z), new Vector3(0.64f, 0.11f, 0.64f), tyre);
                    wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                    GameObject cap = PrimitiveFactory.Create("Hub", PrimitiveType.Cylinder, truck, new Vector3(x * 1.1f, 0.32f, z), new Vector3(0.3f, 0.02f, 0.3f), hub, false);
                    cap.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                }
            c.Block(position.x, position.z, 1.5f, 2.7f);
        }

        private static void BuildAmbientWorkers(Ctx c)
        {
            // A worker shovelling a spoil heap on the north side of the yard.
            Material soil = c.Mat(Palette.TerrainTop(TerrainMaterial.SoftSoil));
            PrimitiveFactory.Create("SpoilHeap", PrimitiveType.Sphere, c.Static, new Vector3(2.3f, 0f, 6.6f), new Vector3(1.4f, 0.7f, 1.2f), soil);
            AddWorker(c, new Vector3(1.3f, 0f, 6.5f), 90f, 0.2f);
            c.Block(1.9f, 6.6f, 2.2f, 1.4f);

            // A worker busy at the crates.
            AddWorker(c, new Vector3(-5.2f, 0f, -1.5f), -90f, 0.65f);
            c.Block(-5.2f, -1.5f, 0.6f, 0.6f);
        }

        private static void AddWorker(Ctx c, Vector3 position, float yaw, float phase)
        {
            Transform holder = PrimitiveFactory.Pivot("AmbientWorker", c.Dynamic, position);
            holder.localRotation = Quaternion.Euler(0f, yaw, 0f);
            CharacterRig rig = CharacterRig.Build(holder, c.M);
            rig.Digging = true;
            rig.AutoDig = true;
            rig.DigPhase = phase;
            c.Result.AmbientWorkers.Add(rig);
        }

        private static void BuildGreenery(Ctx c)
        {
            Material trunk = c.Mat(Palette.Hex(0x9A6230));
            Color[] crowns = { Palette.Hex(0x4CBF63), Palette.Hex(0x3AA555), Palette.Hex(0x67CF6B) };
            Vector3[] trees =
            {
                new Vector3(-9.6f, 0f, -9.4f), new Vector3(-10.2f, 0f, -3.5f), new Vector3(-9.8f, 0f, 2.5f), new Vector3(-10.4f, 0f, 7.5f),
                new Vector3(-3.5f, 0f, -10.1f), new Vector3(2.5f, 0f, -9.8f), new Vector3(7.5f, 0f, -10.3f), new Vector3(-13f, 0f, -12f),
                new Vector3(-14f, 0f, 1f), new Vector3(-6f, 0f, -13.5f)
            };
            for (int i = 0; i < trees.Length; i++)
            {
                Transform tree = PrimitiveFactory.Pivot("Tree", c.Static, trees[i]);
                float s = c.Range(0.85f, 1.2f);
                PrimitiveFactory.Create("Trunk", PrimitiveType.Cylinder, tree, new Vector3(0f, 0.6f * s, 0f), new Vector3(0.24f, 0.6f * s, 0.24f), trunk);
                Material a = c.Mat(crowns[i % crowns.Length]);
                Material b = c.Mat(crowns[(i + 1) % crowns.Length]);
                PrimitiveFactory.Create("Crown", PrimitiveType.Sphere, tree, new Vector3(0f, 1.65f * s, 0f), new Vector3(1.5f, 1.3f, 1.5f) * s, a);
                PrimitiveFactory.Create("CrownL", PrimitiveType.Sphere, tree, new Vector3(-0.45f * s, 1.35f * s, 0.2f), new Vector3(0.9f, 0.8f, 0.9f) * s, b);
                PrimitiveFactory.Create("CrownTop", PrimitiveType.Sphere, tree, new Vector3(0.2f, 2.3f * s, -0.1f), new Vector3(0.95f, 0.85f, 0.95f) * s, b);
            }

            Material bushA = c.Mat(Palette.GrassDeep);
            Material bushB = c.Mat(Palette.Hex(0x5BB04A));
            Color[] flowerColors = { Palette.Hex(0xFF6B8B), Palette.Hex(0xFFE066), Palette.White, Palette.Hex(0xB18CFF) };
            for (int i = 0; i < 26; i++)
            {
                float x, z;
                if (i % 2 == 0)
                {
                    x = c.Range(-13f, -8.4f);
                    z = c.Range(-12f, 8f);
                }
                else
                {
                    x = c.Range(-8f, 8f);
                    z = c.Range(-13f, -8.4f);
                }
                if (i % 3 == 0)
                {
                    float s = c.Range(0.5f, 0.9f);
                    PrimitiveFactory.Create("Bush", PrimitiveType.Sphere, c.Static, new Vector3(x, s * 0.3f, z), new Vector3(s, s * 0.7f, s), i % 2 == 0 ? bushA : bushB);
                }
                else
                {
                    Material petal = c.Mat(flowerColors[i % flowerColors.Length]);
                    for (int f = 0; f < 3; f++)
                        PrimitiveFactory.Create("Flower", PrimitiveType.Sphere, c.Static, new Vector3(x + f * 0.18f, 0.06f, z + (f % 2) * 0.15f), Vector3.one * 0.12f, petal, false);
                }
            }
        }

        private static void BuildCrane(Ctx c, Vector3 position)
        {
            Material yellow = c.Mat(UITheme.Gold);
            Material dark = c.Mat(UITheme.Navy);
            Transform crane = PrimitiveFactory.Pivot("TowerCrane", c.Static, position);
            PrimitiveFactory.Box("Base", crane, new Vector3(0f, 0.3f, 0f), new Vector3(1.6f, 0.6f, 1.6f), c.Mat(Palette.Hex(0x9AA6B6)));
            const float height = 15f;
            for (float y = 0.6f; y < height; y += 1.5f)
            {
                PrimitiveFactory.Box("Mast", crane, new Vector3(0f, y + 0.75f, 0f), new Vector3(0.7f, 1.4f, 0.7f), yellow);
                PrimitiveFactory.Box("Brace", crane, new Vector3(0f, y + 1.45f, 0f), new Vector3(0.78f, 0.1f, 0.78f), dark, false);
            }

            Transform jib = PrimitiveFactory.Pivot("Jib", c.Dynamic, position + new Vector3(0f, height + 0.5f, 0f));
            jib.localRotation = Quaternion.Euler(0f, -135f, 0f);
            jib.gameObject.AddComponent<AmbientMotion>().Spin(Vector3.up, 5f);
            PrimitiveFactory.Box("Arm", jib, new Vector3(0f, 0f, 5f), new Vector3(0.5f, 0.5f, 12f), yellow);
            PrimitiveFactory.Box("Counter", jib, new Vector3(0f, -0.2f, -1.6f), new Vector3(1.2f, 0.9f, 1.2f), c.Mat(Palette.Hex(0x9AA6B6)));
            PrimitiveFactory.Box("Cabin", jib, new Vector3(0.55f, -0.5f, 0.6f), new Vector3(0.7f, 0.8f, 0.8f), c.Mat(Palette.White));
            PrimitiveFactory.Box("Cable", jib, new Vector3(0f, -3f, 8f), new Vector3(0.04f, 6f, 0.04f), dark, false);
            PrimitiveFactory.Box("Load", jib, new Vector3(0f, -6.2f, 8f), new Vector3(1.2f, 0.25f, 0.3f), c.Mat(UITheme.Orange));
        }

        private static Light BuildLighting(Transform parent)
        {
            var sunGo = new GameObject("Sun");
            sunGo.transform.SetParent(parent, false);
            sunGo.transform.rotation = Quaternion.Euler(48f, -40f, 0f);
            Light sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.93f, 0.8f);
            sun.intensity = 1.25f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.5f;
            sun.shadowBias = 0.04f;
            sun.shadowNormalBias = 0.3f;

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.68f, 0.8f, 0.96f);
            RenderSettings.ambientEquatorColor = new Color(0.82f, 0.76f, 0.66f);
            RenderSettings.ambientGroundColor = new Color(0.45f, 0.36f, 0.28f);
            RenderSettings.sun = sun;

            // Gentle aerial perspective toward the city backdrop.
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = Palette.Sky;
            RenderSettings.fogStartDistance = 48f;
            RenderSettings.fogEndDistance = 120f;
            return sun;
        }

        private static void Box(Transform root, string name, float minX, float maxX, float minZ, float maxZ, float minY, float maxY, Material material)
        {
            PrimitiveFactory.Box(name, root,
                new Vector3((minX + maxX) * 0.5f, (minY + maxY) * 0.5f, (minZ + maxZ) * 0.5f),
                new Vector3(maxX - minX, maxY - minY, maxZ - minZ), material, false);
        }
    }
}
