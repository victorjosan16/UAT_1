using System.Collections.Generic;
using UndergroundMetroTycoon.Simulation;
using UnityEngine;
using UnityEngine.Rendering;

namespace UndergroundMetroTycoon.Game
{
    /// <summary>
    /// Procedurally builds the miniature construction-site world around the excavation plot: grass, sidewalks,
    /// roads, a city backdrop, trees, site props, a tower crane and lighting. Placeholder art built from
    /// primitives; replace with authored prefabs later.
    /// </summary>
    public static class SiteEnvironmentBuilder
    {
        public sealed class Result
        {
            public readonly List<ObstacleBox> Obstacles = new List<ObstacleBox>();
            public Rect WalkBounds;
            public Light Sun;
        }

        private const float WalkHalf = 7.4f;

        public static Result Build(Transform parent, MaterialLibrary m, Vector2 siteCenter)
        {
            var result = new Result { WalkBounds = new Rect(siteCenter.x - WalkHalf, siteCenter.y - WalkHalf, WalkHalf * 2f, WalkHalf * 2f) };
            Transform root = PrimitiveFactory.Pivot("Environment", parent, new Vector3(siteCenter.x, 0f, siteCenter.y));

            BuildGround(root, m);
            BuildRoads(root, m);
            BuildCity(root, m);
            BuildPerimeter(root, m);
            BuildProps(root, m, result, siteCenter);
            BuildCrane(root, m, new Vector3(11.5f, 0f, 15.5f));
            result.Sun = BuildLighting(parent);
            return result;
        }

        private static void BuildGround(Transform root, MaterialLibrary m)
        {
            Material grass = m.Get(Palette.Grass);
            float plot = ExcavationSiteView.PlotHalfSize;
            const float outer = 40f, depth = 3f;
            Box(root, "GrassW", -outer, -plot, -outer, outer, -depth, 0f, grass);
            Box(root, "GrassE", plot, outer, -outer, outer, -depth, 0f, grass);
            Box(root, "GrassS", -plot, plot, -outer, -plot, -depth, 0f, grass);
            Box(root, "GrassN", -plot, plot, plot, outer, -depth, 0f, grass);

            // Concrete walkway ring around the plot.
            Material concrete = m.Get(Palette.Concrete);
            const float inner = 5f, ring = 6.1f, y0 = -0.02f, y1 = 0.03f;
            Box(root, "WalkS", -ring, ring, -ring, -inner, y0, y1, concrete);
            Box(root, "WalkN", -ring, ring, inner, ring, y0, y1, concrete);
            Box(root, "WalkW", -ring, -inner, -inner, inner, y0, y1, concrete);
            Box(root, "WalkE", inner, ring, -inner, inner, y0, y1, concrete);

            // Darker grass patches add variation.
            Material patch = m.Get(Palette.GrassDark);
            Box(root, "Patch1", -7.2f, -6.4f, -4f, 1f, y0, 0.02f, patch);
            Box(root, "Patch2", 1.5f, 6f, -7.3f, -6.5f, y0, 0.02f, patch);
        }

        private static void BuildRoads(Transform root, MaterialLibrary m)
        {
            Material asphalt = m.Get(Palette.Asphalt);
            Material line = m.Get(Palette.Yellow);
            Material curb = m.Get(Palette.ConcreteDark);
            Box(root, "RoadNorth", -30f, 30f, 9f, 13f, -0.02f, 0.025f, asphalt);
            Box(root, "RoadEast", 9f, 13f, -30f, 9f, -0.02f, 0.025f, asphalt);
            Box(root, "CurbNorth", -30f, 9f, 8.75f, 9f, -0.02f, 0.08f, curb);
            Box(root, "CurbEast", 8.75f, 9f, -30f, 8.75f, -0.02f, 0.08f, curb);
            for (float x = -24f; x < 24f; x += 2.4f) Box(root, "Dash", x, x + 1.2f, 10.93f, 11.07f, 0f, 0.03f, line);
            for (float z = -24f; z < 8f; z += 2.4f) Box(root, "Dash", 10.93f, 11.07f, z, z + 1.2f, 0f, 0.03f, line);
        }

        private static void BuildCity(Transform root, MaterialLibrary m)
        {
            Color[] facades = { Palette.Hex(0x7FA7D9), Palette.Hex(0xE7D8C0), Palette.Hex(0x9AD0C2), Palette.Hex(0xF2B880), Palette.Hex(0xB8B5E8), Palette.Hex(0xD9E3EC) };
            Material glass = m.Get(Palette.Hex(0x2B4C7E));
            Material roof = m.Get(Palette.ConcreteDark);
            var rng = new System.Random(1234);
            int index = 0;

            // Row behind the north road and the east road (top of the portrait screen), never between camera and player.
            for (float x = -18f; x <= 16f; x += 4.6f) Building(root, new Vector3(x, 0f, 16.5f + (float)rng.NextDouble() * 1.5f), rng, facades[index++ % facades.Length], glass, roof, m);
            for (float z = -16f; z <= 10f; z += 4.6f) Building(root, new Vector3(16.5f + (float)rng.NextDouble() * 1.5f, 0f, z), rng, facades[index++ % facades.Length], glass, roof, m);
        }

        private static void Building(Transform root, Vector3 position, System.Random rng, Color facade, Material glass, Material roof, MaterialLibrary m)
        {
            float width = 3.2f + (float)rng.NextDouble() * 0.8f;
            float height = 4f + (float)rng.NextDouble() * 9f;
            Transform b = PrimitiveFactory.Pivot("Building", root, position);
            PrimitiveFactory.Box("Body", b, new Vector3(0f, height * 0.5f, 0f), new Vector3(width, height, width), m.Get(facade));
            PrimitiveFactory.Box("Roof", b, new Vector3(0f, height + 0.1f, 0f), new Vector3(width + 0.15f, 0.2f, width + 0.15f), roof);
            PrimitiveFactory.Box("RoofUnit", b, new Vector3(width * 0.2f, height + 0.45f, -width * 0.15f), new Vector3(0.8f, 0.5f, 0.6f), roof);
            // Window bands on the two faces visible from the isometric camera.
            for (float y = 1.2f; y < height - 0.6f; y += 1.3f)
            {
                PrimitiveFactory.Box("WindowsS", b, new Vector3(0f, y, -width * 0.5f - 0.01f), new Vector3(width * 0.8f, 0.55f, 0.04f), glass, false);
                PrimitiveFactory.Box("WindowsW", b, new Vector3(-width * 0.5f - 0.01f, y, 0f), new Vector3(0.04f, 0.55f, width * 0.8f), glass, false);
            }
        }

        private static void BuildPerimeter(Transform root, MaterialLibrary m)
        {
            Material red = m.Get(Palette.Red);
            Material white = m.Get(Palette.White);
            Material foot = m.Get(Palette.ConcreteDark);
            const float edge = WalkHalf + 0.5f;
            // Low striped barriers on the near (south/west) edges keep the view open toward the player.
            for (float p = -edge; p < edge; p += 1.6f)
            {
                Barrier(root, new Vector3(p + 0.8f, 0f, -edge), true, red, white, foot);
                Barrier(root, new Vector3(-edge, 0f, p + 0.8f), false, red, white, foot);
            }
            // Fence on the far edges.
            Material fence = m.Get(Palette.Hex(0xCBD5E1));
            Box(root, "FenceN", -edge, edge, edge - 0.03f, edge + 0.03f, 0f, 1.1f, fence);
            Box(root, "FenceE", edge - 0.03f, edge + 0.03f, -edge, edge, 0f, 1.1f, fence);
        }

        private static void Barrier(Transform root, Vector3 center, bool alongX, Material red, Material white, Material foot)
        {
            Transform b = PrimitiveFactory.Pivot("Barrier", root, center);
            if (!alongX) b.localRotation = Quaternion.Euler(0f, 90f, 0f);
            for (int i = 0; i < 4; i++)
            {
                PrimitiveFactory.Box("Stripe", b, new Vector3(-0.6f + i * 0.4f, 0.55f, 0f), new Vector3(0.4f, 0.22f, 0.06f), i % 2 == 0 ? red : white, false);
            }
            PrimitiveFactory.Box("FootL", b, new Vector3(-0.65f, 0.25f, 0f), new Vector3(0.07f, 0.5f, 0.3f), foot);
            PrimitiveFactory.Box("FootR", b, new Vector3(0.65f, 0.25f, 0f), new Vector3(0.07f, 0.5f, 0.3f), foot);
        }

        private static void BuildProps(Transform root, MaterialLibrary m, Result result, Vector2 c)
        {
            // Site office container.
            Transform office = PrimitiveFactory.Pivot("SiteOffice", root, new Vector3(-6.4f, 0f, 3.2f));
            PrimitiveFactory.Box("Body", office, new Vector3(0f, 0.7f, 0f), new Vector3(1.6f, 1.4f, 3f), m.Get(Palette.Shirt));
            PrimitiveFactory.Box("Roof", office, new Vector3(0f, 1.45f, 0f), new Vector3(1.7f, 0.1f, 3.1f), m.Get(Palette.White));
            PrimitiveFactory.Box("Door", office, new Vector3(0.81f, 0.55f, 0.7f), new Vector3(0.04f, 1.0f, 0.6f), m.Get(Palette.White));
            PrimitiveFactory.Box("Window", office, new Vector3(0.81f, 0.85f, -0.6f), new Vector3(0.04f, 0.45f, 0.9f), m.Get(Palette.Cyan), false);
            PrimitiveFactory.Box("Sign", office, new Vector3(0.83f, 1.2f, -0.6f), new Vector3(0.03f, 0.18f, 1.2f), m.Get(Palette.Yellow), false);
            result.Obstacles.Add(ObstacleBox.FromCenter(c.x - 6.4f, c.y + 3.2f, 1.7f, 3.1f));

            // Brick pallet and cement blocks.
            Transform pallet = PrimitiveFactory.Pivot("BrickPallet", root, new Vector3(-6.2f, 0f, -2.2f));
            PrimitiveFactory.Box("Pallet", pallet, new Vector3(0f, 0.06f, 0f), new Vector3(1.1f, 0.12f, 1.1f), m.Get(Palette.Wood));
            Material brick = m.Get(Palette.Hex(0xC4583A));
            for (int y = 0; y < 3; y++)
                for (int x = 0; x < 3; x++)
                    PrimitiveFactory.Box("Brick", pallet, new Vector3(-0.34f + x * 0.34f, 0.2f + y * 0.16f, 0f), new Vector3(0.32f, 0.14f, 1.0f), brick);
            result.Obstacles.Add(ObstacleBox.FromCenter(c.x - 6.2f, c.y - 2.2f, 1.1f, 1.1f));

            Material cement = m.Get(Palette.Concrete);
            for (int i = 0; i < 4; i++)
                PrimitiveFactory.Box("Block", root, new Vector3(-6.6f + (i % 2) * 0.55f, 0.2f + (i / 2) * 0.4f, -4.1f), new Vector3(0.5f, 0.4f, 0.5f), cement);
            result.Obstacles.Add(ObstacleBox.FromCenter(c.x - 6.35f, c.y - 4.1f, 1.1f, 0.6f));

            // Parked mini dump truck.
            Transform truck = PrimitiveFactory.Pivot("DumpTruck", root, new Vector3(6.4f, 0f, -3.6f));
            Material yellow = m.Get(Palette.Yellow);
            Material tire = m.Get(Palette.Black);
            PrimitiveFactory.Box("Chassis", truck, new Vector3(0f, 0.45f, 0f), new Vector3(1.2f, 0.3f, 2.4f), m.Get(Palette.NavyLight));
            PrimitiveFactory.Box("Cab", truck, new Vector3(0f, 0.95f, 0.8f), new Vector3(1.1f, 0.8f, 0.7f), yellow);
            PrimitiveFactory.Box("CabGlass", truck, new Vector3(0f, 1.1f, 1.16f), new Vector3(0.9f, 0.35f, 0.03f), m.Get(Palette.Cyan), false);
            PrimitiveFactory.Box("Bed", truck, new Vector3(0f, 0.9f, -0.45f), new Vector3(1.2f, 0.6f, 1.4f), yellow);
            PrimitiveFactory.Box("Load", truck, new Vector3(0f, 1.18f, -0.45f), new Vector3(1.0f, 0.12f, 1.2f), m.Get(Palette.Terrain(TerrainMaterial.SoftSoil)), false);
            foreach (float z in new[] { 0.75f, -0.75f })
                foreach (float x in new[] { -0.62f, 0.62f })
                {
                    GameObject wheel = PrimitiveFactory.Create("Wheel", PrimitiveType.Cylinder, truck, new Vector3(x, 0.3f, z), new Vector3(0.6f, 0.1f, 0.6f), tire);
                    wheel.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
                }
            result.Obstacles.Add(ObstacleBox.FromCenter(c.x + 6.4f, c.y - 3.6f, 1.4f, 2.6f));

            // Traffic cones.
            Material orange = m.Get(Palette.Orange);
            Material white = m.Get(Palette.White);
            foreach (Vector3 p in new[] { new Vector3(-5.6f, 0f, -5.6f), new Vector3(5.6f, 0f, -5.6f), new Vector3(5.6f, 0f, 5.6f), new Vector3(-5.6f, 0f, 5.6f) })
            {
                Transform cone = PrimitiveFactory.Pivot("Cone", root, p);
                PrimitiveFactory.Box("Base", cone, new Vector3(0f, 0.03f, 0f), new Vector3(0.4f, 0.06f, 0.4f), orange);
                PrimitiveFactory.Create("Low", PrimitiveType.Cylinder, cone, new Vector3(0f, 0.17f, 0f), new Vector3(0.26f, 0.11f, 0.26f), orange);
                PrimitiveFactory.Create("Band", PrimitiveType.Cylinder, cone, new Vector3(0f, 0.32f, 0f), new Vector3(0.2f, 0.04f, 0.2f), white, false);
                PrimitiveFactory.Create("Top", PrimitiveType.Cylinder, cone, new Vector3(0f, 0.45f, 0f), new Vector3(0.14f, 0.1f, 0.14f), orange);
            }

            // Trees.
            Material trunk = m.Get(Palette.Wood);
            Material leaves = m.Get(Palette.Hex(0x3FAE5A));
            Material leavesDark = m.Get(Palette.Hex(0x2E8C47));
            Vector3[] trees =
            {
                new Vector3(-9.5f, 0f, -9.5f), new Vector3(-9.8f, 0f, -2f), new Vector3(-9.6f, 0f, 6f), new Vector3(-3f, 0f, -9.7f),
                new Vector3(4f, 0f, -9.8f), new Vector3(7.6f, 0f, 7.6f), new Vector3(-7.4f, 0f, 7.6f), new Vector3(7.6f, 0f, 1.2f)
            };
            for (int i = 0; i < trees.Length; i++)
            {
                Transform tree = PrimitiveFactory.Pivot("Tree", root, trees[i]);
                PrimitiveFactory.Create("Trunk", PrimitiveType.Cylinder, tree, new Vector3(0f, 0.6f, 0f), new Vector3(0.22f, 0.6f, 0.22f), trunk);
                PrimitiveFactory.Create("Crown", PrimitiveType.Sphere, tree, new Vector3(0f, 1.6f, 0f), new Vector3(1.4f, 1.3f, 1.4f), i % 2 == 0 ? leaves : leavesDark);
                PrimitiveFactory.Create("CrownTop", PrimitiveType.Sphere, tree, new Vector3(0.15f, 2.25f, 0.1f), new Vector3(0.9f, 0.8f, 0.9f), i % 2 == 0 ? leavesDark : leaves);
                result.Obstacles.Add(ObstacleBox.FromCenter(c.x + trees[i].x, c.y + trees[i].z, 0.4f, 0.4f));
            }
        }

        private static void BuildCrane(Transform root, MaterialLibrary m, Vector3 position)
        {
            Material yellow = m.Get(Palette.Yellow);
            Material dark = m.Get(Palette.NavyLight);
            Transform crane = PrimitiveFactory.Pivot("TowerCrane", root, position);
            PrimitiveFactory.Box("Base", crane, new Vector3(0f, 0.3f, 0f), new Vector3(1.6f, 0.6f, 1.6f), m.Get(Palette.ConcreteDark));
            const float height = 15f;
            for (float y = 0.6f; y < height; y += 1.5f)
            {
                PrimitiveFactory.Box("Mast", crane, new Vector3(0f, y + 0.75f, 0f), new Vector3(0.7f, 1.4f, 0.7f), yellow);
                PrimitiveFactory.Box("Brace", crane, new Vector3(0f, y + 1.45f, 0f), new Vector3(0.78f, 0.1f, 0.78f), dark, false);
            }
            Transform jib = PrimitiveFactory.Pivot("Jib", crane, new Vector3(0f, height + 0.5f, 0f));
            jib.localRotation = Quaternion.Euler(0f, -135f, 0f);
            PrimitiveFactory.Box("Arm", jib, new Vector3(0f, 0f, 5f), new Vector3(0.5f, 0.5f, 12f), yellow);
            PrimitiveFactory.Box("Counter", jib, new Vector3(0f, -0.2f, -1.6f), new Vector3(1.2f, 0.9f, 1.2f), m.Get(Palette.ConcreteDark));
            PrimitiveFactory.Box("Cabin", jib, new Vector3(0.55f, -0.5f, 0.6f), new Vector3(0.7f, 0.8f, 0.8f), m.Get(Palette.White));
            PrimitiveFactory.Box("Cable", jib, new Vector3(0f, -3f, 8f), new Vector3(0.04f, 6f, 0.04f), dark, false);
            PrimitiveFactory.Box("Hook", jib, new Vector3(0f, -6.1f, 8f), new Vector3(0.3f, 0.3f, 0.3f), m.Get(Palette.Orange));
        }

        private static Light BuildLighting(Transform parent)
        {
            var sunGo = new GameObject("Sun");
            sunGo.transform.SetParent(parent, false);
            sunGo.transform.rotation = Quaternion.Euler(52f, -35f, 0f);
            Light sun = sunGo.AddComponent<Light>();
            sun.type = LightType.Directional;
            sun.color = new Color(1f, 0.95f, 0.86f);
            sun.intensity = 1.15f;
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.55f;

            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.62f, 0.72f, 0.86f);
            RenderSettings.ambientEquatorColor = new Color(0.56f, 0.58f, 0.6f);
            RenderSettings.ambientGroundColor = new Color(0.36f, 0.3f, 0.26f);
            RenderSettings.fog = false;
            RenderSettings.sun = sun;
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
