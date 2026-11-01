using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using VoxelBloom.Core;

namespace VoxelBloom
{
    /// <summary>
    /// Real 3D rendering of the conveyor board (rendered by a VoxelStage camera into a RenderTexture behind the UI):
    /// raised loop with luminous cyan edges, bevelled glossy cubes (GPU instanced, one batch per colour), trays with walls
    /// and recessed compartments, stone lock tiles, green plus slots and the feeder. Purely visual: positions come from
    /// ConveyorSession and ConveyorBoard (board units -> world: x right, z away from the camera).
    /// </summary>
    public sealed class ConveyorBoard3D : MonoBehaviour
    {
        public const float TrackTop = 0.30f, FloorTop = 0.12f, CubeSize = 0.74f, BlockH = 0.62f;
        public VoxelStage Stage { get; private set; }
        public ConveyorBoard Geo { get; private set; }
        ConveyorLevel level;
        Mesh cube;
        Material boardMat;
        Material[] solid, ghost;
        List<Matrix4x4>[] sBatch, gBatch;
        GameObject[] trayGo, lockGo, plusGo;
        readonly Matrix4x4[] buf = new Matrix4x4[1023];

        public static ConveyorBoard3D Create(VoxelStage stage, ConveyorLevel level, Color[] colors)
        {
            var go = new GameObject("ConveyorBoard3D");
            go.transform.SetParent(stage.transform, false);
            var b = go.AddComponent<ConveyorBoard3D>();
            b.Init(stage, level, colors);
            return b;
        }

        public Vector3 World(Vector2 g, float y) => new Vector3(g.x - Geo.W / 2f, y, Geo.H / 2f - g.y);

        void Init(VoxelStage stage, ConveyorLevel l, Color[] colors)
        {
            Stage = stage; level = l; Geo = new ConveyorBoard(l);
            cube = BuildBevelCube();
            boardMat = NewMaterial(Color.white, false);
            solid = new Material[colors.Length]; ghost = new Material[colors.Length];
            sBatch = new List<Matrix4x4>[colors.Length]; gBatch = new List<Matrix4x4>[colors.Length];
            for (int i = 0; i < colors.Length; i++)
            {
                solid[i] = NewMaterial(colors[i], true);
                ghost[i] = NewMaterial(Color.Lerp(colors[i], Color.white, 0.45f) * 0.9f, true);
                sBatch[i] = new List<Matrix4x4>(128); gBatch[i] = new List<Matrix4x4>(32);
            }
            BuildStatic();
            FrameCamera();
        }

        static Material NewMaterial(Color tint, bool instanced)
        {
            var m = new Material(VoxelMaterials.VoxelShader) { name = "Conveyor", enableInstancing = instanced };
            m.SetColor("_Tint", tint);
            m.SetFloat("_SubLine", 0f);
            m.SetFloat("_Subdiv", 1f);
            m.SetFloat("_Bevel", 0.14f);
            m.SetFloat("_Rim", 0.22f);
            return m;
        }

        // ------------------------------------------------------------------ static meshes
        sealed class MeshData
        {
            public readonly List<Vector3> V = new List<Vector3>(); public readonly List<Vector3> N = new List<Vector3>();
            public readonly List<Color32> C = new List<Color32>(); public readonly List<Vector2> UV = new List<Vector2>();
            public readonly List<int> T = new List<int>();

            /// <summary>Quad (a,b,c,d in order around), both windings so it is visible whatever the orientation.</summary>
            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d, Vector3 n, Color col, bool bevel)
            {
                int s = V.Count;
                V.Add(a); V.Add(b); V.Add(c); V.Add(d);
                for (int i = 0; i < 4; i++) { N.Add(n); C.Add(col); }
                if (bevel) { UV.Add(new Vector2(0, 0)); UV.Add(new Vector2(1, 0)); UV.Add(new Vector2(1, 1)); UV.Add(new Vector2(0, 1)); }
                else for (int i = 0; i < 4; i++) UV.Add(new Vector2(0.5f, 0.5f));
                T.Add(s); T.Add(s + 1); T.Add(s + 2); T.Add(s); T.Add(s + 2); T.Add(s + 3);
                T.Add(s); T.Add(s + 2); T.Add(s + 1); T.Add(s); T.Add(s + 3); T.Add(s + 2);
            }

            public void Box(Vector3 c, Vector3 size, Color col, bool bevel = true)
            {
                var h = size * 0.5f;
                Vector3 p(float x, float y, float z) => c + new Vector3(x * h.x, y * h.y, z * h.z);
                Quad(p(-1, 1, -1), p(-1, 1, 1), p(1, 1, 1), p(1, 1, -1), Vector3.up, col, bevel);
                Quad(p(-1, -1, -1), p(1, -1, -1), p(1, 1, -1), p(-1, 1, -1), Vector3.back, Darken(col, 0.9f), bevel);
                Quad(p(1, -1, 1), p(-1, -1, 1), p(-1, 1, 1), p(1, 1, 1), Vector3.forward, Darken(col, 0.9f), bevel);
                Quad(p(-1, -1, 1), p(-1, -1, -1), p(-1, 1, -1), p(-1, 1, 1), Vector3.left, Darken(col, 0.85f), bevel);
                Quad(p(1, -1, -1), p(1, -1, 1), p(1, 1, 1), p(1, 1, -1), Vector3.right, Darken(col, 0.85f), bevel);
            }

            public Mesh ToMesh(string name)
            {
                var m = new Mesh { name = name, indexFormat = V.Count > 65000 ? IndexFormat.UInt32 : IndexFormat.UInt16 };
                m.SetVertices(V); m.SetNormals(N); m.SetColors(C); m.SetUVs(0, UV); m.SetTriangles(T, 0);
                m.RecalculateBounds();
                return m;
            }
        }

        static Color Darken(Color c, float k) => new Color(c.r * k, c.g * k, c.b * k, 1f);

        GameObject Spawn(string name, MeshData d)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            go.layer = gameObject.layer;
            go.transform.SetParent(transform, false);
            go.GetComponent<MeshFilter>().sharedMesh = d.ToMesh(name);
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = boardMat;
            r.shadowCastingMode = ShadowCastingMode.Off; r.receiveShadows = false;
            return go;
        }

        /// <summary>Band between two offsets from the loop centreline (positive = outwards), with optional walls.</summary>
        void Band(MeshData d, float offIn, float offOut, float y, float bottom, Color top, Color wall)
        {
            int n = 240, pu = level.Slots * ConveyorSession.P;
            for (int i = 0; i < n; i++)
            {
                float s0 = (float)i / n * pu, s1 = (float)(i + 1) / n * pu;
                Vector2 c0 = Geo.At(s0), c1 = Geo.At(s1), t0 = Geo.Tangent(s0), t1 = Geo.Tangent(s1);
                Vector2 n0 = new Vector2(t0.y, -t0.x), n1 = new Vector2(t1.y, -t1.x); // outward (board y is down)
                Vector3 i0 = World(c0 + n0 * offIn, y), o0 = World(c0 + n0 * offOut, y), i1 = World(c1 + n1 * offIn, y), o1 = World(c1 + n1 * offOut, y);
                d.Quad(i0, o0, o1, i1, Vector3.up, top, false);
                if (bottom < y)
                {
                    var down = new Vector3(0, bottom - y, 0);
                    var wo = World(c0 + n0, 0) - World(c0, 0); wo.y = 0; wo.Normalize();
                    d.Quad(o0, o0 + down, o1 + down, o1, wo, wall, false);
                    d.Quad(i0 + down, i0, i1, i1 + down, -wo, wall, false);
                }
            }
        }

        /// <summary>Filled rounded rectangle at an offset from the centreline (fan from the centre).</summary>
        void Fill(MeshData d, float off, float y, float bottom, Color top, Color wall)
        {
            int n = 240, pu = level.Slots * ConveyorSession.P;
            var centre = World(new Vector2(Geo.W / 2f, Geo.H / 2f), y);
            for (int i = 0; i < n; i++)
            {
                float s0 = (float)i / n * pu, s1 = (float)(i + 1) / n * pu;
                Vector2 t0 = Geo.Tangent(s0), t1 = Geo.Tangent(s1);
                var a = World(Geo.At(s0) + new Vector2(t0.y, -t0.x) * off, y);
                var b = World(Geo.At(s1) + new Vector2(t1.y, -t1.x) * off, y);
                d.Quad(centre, a, b, centre, Vector3.up, top, false);
                if (bottom < y)
                {
                    var down = new Vector3(0, bottom - y, 0);
                    var wo = a - centre; wo.y = 0; wo.Normalize();
                    d.Quad(a, a + down, b + down, b, wo, wall, false);
                }
            }
        }

        void BuildStatic()
        {
            var frame = new MeshData();
            Fill(frame, 1.05f, FloorTop - 0.01f, -0.5f, ConveyorScreen.Frame, Darken(ConveyorScreen.Frame, 0.7f));
            Spawn("Frame", frame);
            var floor = new MeshData();
            Fill(floor, -0.7f, FloorTop, FloorTop, ConveyorScreen.Floor, ConveyorScreen.Floor);
            Spawn("Floor", floor);
            var track = new MeshData();
            Band(track, -0.58f, 0.58f, TrackTop, FloorTop, ConveyorScreen.Track, ConveyorScreen.TrackDark);
            // slat lines across the belt (airport conveyor plates)
            int slats = level.Slots;
            for (int i = 0; i < slats; i++)
            {
                float s = (i + 0.5f) * ConveyorSession.P;
                var c = Geo.At(s); var t = Geo.Tangent(s); var nrm = new Vector2(t.y, -t.x);
                var a = World(c + nrm * -0.56f + t * 0.03f, TrackTop + 0.004f); var b = World(c + nrm * 0.56f + t * 0.03f, TrackTop + 0.004f);
                var b2 = World(c + nrm * 0.56f - t * 0.03f, TrackTop + 0.004f); var a2 = World(c + nrm * -0.56f - t * 0.03f, TrackTop + 0.004f);
                track.Quad(a, b, b2, a2, Vector3.up, ConveyorScreen.TrackDark, false);
            }
            Spawn("Track", track);
            var edges = new MeshData();
            Band(edges, 0.58f, 0.72f, TrackTop + 0.03f, FloorTop, ConveyorScreen.Edge, Darken(ConveyorScreen.Edge, 0.8f));
            Band(edges, -0.72f, -0.58f, TrackTop + 0.03f, FloorTop, ConveyorScreen.Edge, Darken(ConveyorScreen.Edge, 0.8f));
            Spawn("Edges", edges);
            // feeder chute outside the top-left corner
            var feeder = new MeshData();
            var fp = Geo.At(level.Feeder);
            feeder.Box(World(fp + new Vector2(-0.75f, -0.75f), 0.45f), new Vector3(1.3f, 0.9f, 1.3f), Theme.Hex("#6B5FC2"));
            feeder.Box(World(fp + new Vector2(-0.75f, -0.75f), 0.92f), new Vector3(0.9f, 0.06f, 0.9f), Theme.Hex("#2A2366"), false);
            Spawn("Feeder", feeder);

            int ns = level.Stations.Length;
            trayGo = new GameObject[ns]; lockGo = new GameObject[ns]; plusGo = new GameObject[ns];
            for (int i = 0; i < ns; i++)
            {
                var st = level.Stations[i];
                Footprint(st, out var centre, out var size);
                var tray = new MeshData();
                float wall = 0.12f, h = 0.40f;
                tray.Box(new Vector3(centre.x, FloorTop + 0.07f, centre.z), new Vector3(size.x, 0.14f, size.z), ConveyorScreen.TrayInner, false);
                tray.Box(new Vector3(centre.x - size.x / 2 + wall / 2, FloorTop + h / 2, centre.z), new Vector3(wall, h, size.z), ConveyorScreen.TrayRim);
                tray.Box(new Vector3(centre.x + size.x / 2 - wall / 2, FloorTop + h / 2, centre.z), new Vector3(wall, h, size.z), ConveyorScreen.TrayRim);
                tray.Box(new Vector3(centre.x, FloorTop + h / 2, centre.z - size.z / 2 + wall / 2), new Vector3(size.x - 2 * wall, h, wall), ConveyorScreen.TrayRim);
                tray.Box(new Vector3(centre.x, FloorTop + h / 2, centre.z + size.z / 2 - wall / 2), new Vector3(size.x - 2 * wall, h, wall), ConveyorScreen.TrayRim);
                trayGo[i] = Spawn("Tray" + i, tray);
                if (st.Lock == StationLock.Completed)
                {
                    var stone = new MeshData();
                    stone.Box(new Vector3(centre.x, FloorTop + 0.22f, centre.z), new Vector3(size.x, 0.44f, size.z), ConveyorScreen.Stone);
                    stone.Box(new Vector3(centre.x, FloorTop + 0.46f, centre.z), new Vector3(size.x - 0.24f, 0.06f, size.z - 0.24f), Theme.Lighter(ConveyorScreen.Stone, 0.22f));
                    stone.Box(new Vector3(centre.x, FloorTop + 0.5f, centre.z), new Vector3(1.15f, 0.06f, 1.15f), Theme.Hex("#2C2A4A"));
                    lockGo[i] = Spawn("Lock" + i, stone);
                }
                else if (st.Lock == StationLock.Booster)
                {
                    var plus = new MeshData();
                    plus.Box(new Vector3(centre.x, FloorTop + 0.07f, centre.z), new Vector3(size.x, 0.14f, size.z), ConveyorScreen.PlusTray, false);
                    plus.Box(new Vector3(centre.x - size.x / 2 + wall / 2, FloorTop + h / 2, centre.z), new Vector3(wall, h, size.z), Theme.Lighter(ConveyorScreen.PlusTray, 0.35f));
                    plus.Box(new Vector3(centre.x + size.x / 2 - wall / 2, FloorTop + h / 2, centre.z), new Vector3(wall, h, size.z), Theme.Lighter(ConveyorScreen.PlusTray, 0.35f));
                    plus.Box(new Vector3(centre.x, FloorTop + h / 2, centre.z - size.z / 2 + wall / 2), new Vector3(size.x - 2 * wall, h, wall), Theme.Lighter(ConveyorScreen.PlusTray, 0.35f));
                    plus.Box(new Vector3(centre.x, FloorTop + h / 2, centre.z + size.z / 2 - wall / 2), new Vector3(size.x - 2 * wall, h, wall), Theme.Lighter(ConveyorScreen.PlusTray, 0.35f));
                    var green = Theme.Hex("#4CD964");
                    plus.Box(new Vector3(centre.x, FloorTop + 0.36f, centre.z), new Vector3(1.0f, 0.3f, 0.3f), green);
                    plus.Box(new Vector3(centre.x, FloorTop + 0.36f, centre.z), new Vector3(0.3f, 0.3f, 1.0f), green);
                    plusGo[i] = Spawn("Plus" + i, plus);
                }
            }
        }

        /// <summary>World centre and size (x, z) of a station's tray footprint.</summary>
        public void Footprint(ConveyorStationDef st, out Vector3 centre, out Vector3 size)
        {
            var c0 = Geo.Compartment(st, 0); var cN = Geo.Compartment(st, st.Capacity - 1);
            centre = World((c0 + cN) / 2f, 0);
            float len = st.Capacity * ConveyorBoard.Pitch + 0.24f;
            size = ConveyorBoard.IsVertical(st) ? new Vector3(ConveyorBoard.TrayWidth, 0, len) : new Vector3(len, 0, ConveyorBoard.TrayWidth);
        }

        public void SetStation(int i, StationLock lk)
        {
            if (trayGo[i] != null) trayGo[i].SetActive(lk == StationLock.None);
            if (lockGo[i] != null) lockGo[i].SetActive(lk == StationLock.Completed);
            if (plusGo[i] != null) plusGo[i].SetActive(lk == StationLock.Booster);
        }

        // ------------------------------------------------------------------ camera framing (near top-down view like the reference)
        void FrameCamera()
        {
            var cam = Stage.Camera;
            cam.fieldOfView = 26f;
            cam.aspect = (float)Stage.Texture.width / Stage.Texture.height;
            const float pitch = 78f * Mathf.Deg2Rad; // almost top-down with a little perspective, like the reference
            float hx = Geo.W / 2f + 1.15f, hz = Geo.H / 2f + 1.15f;
            var corners = new[] { new Vector3(-hx, 0, -hz), new Vector3(hx, 0, -hz), new Vector3(-hx, 0, hz), new Vector3(hx, 0, hz), new Vector3(-hx, 1f, hz), new Vector3(hx, 1f, hz) };
            float zShift = 0.2f; // look slightly behind the centre so near and far margins balance
            for (float dist = 12f; dist < 120f; dist += 0.25f)
            {
                var look = new Vector3(0, 0, zShift);
                Stage.SetCamera(look + new Vector3(0, Mathf.Sin(pitch), -Mathf.Cos(pitch)) * dist, look, 26f);
                cam.aspect = (float)Stage.Texture.width / Stage.Texture.height;
                bool fits = true;
                foreach (var c in corners)
                {
                    var vp = cam.WorldToViewportPoint(Stage.transform.TransformPoint(c));
                    if (vp.x < 0.01f || vp.x > 0.99f || vp.y < 0.01f || vp.y > 0.99f) { fits = false; break; }
                }
                if (fits) break;
            }
        }

        /// <summary>Viewport position (0..1) of a board point at height y (for UI overlays).</summary>
        public Vector2 Viewport(Vector2 g, float y)
        {
            var vp = Stage.Camera.WorldToViewportPoint(Stage.transform.TransformPoint(World(g, y)));
            return new Vector2(vp.x, vp.y);
        }

        public Vector2 ViewportOfWorld(Vector3 local)
        {
            var vp = Stage.Camera.WorldToViewportPoint(Stage.transform.TransformPoint(local));
            return new Vector2(vp.x, vp.y);
        }

        // ------------------------------------------------------------------ per-frame instanced cubes
        public void BeginFrame()
        {
            for (int i = 0; i < sBatch.Length; i++) { sBatch[i].Clear(); gBatch[i].Clear(); }
        }

        Matrix4x4 Local(Vector3 pos, Vector3 scale) => transform.localToWorldMatrix * Matrix4x4.TRS(pos, Quaternion.identity, scale);

        public void BeltCube(int color, Vector2 g, float scale = 1f)
            => sBatch[color].Add(Local(World(g, TrackTop + CubeSize * 0.5f * scale), Vector3.one * CubeSize * scale));

        public void FlyingCube(int color, Vector3 pos, float scale)
            => sBatch[color].Add(Local(pos, new Vector3(CubeSize, CubeSize, CubeSize) * scale));

        public Vector3 BeltCubePos(Vector2 g) => World(g, TrackTop + CubeSize * 0.5f);
        public Vector3 CompartmentPos(ConveyorStationDef st, int i) => World(Geo.Compartment(st, Mathf.Min(i, st.Capacity - 1)), FloorTop + 0.14f + BlockH * 0.5f);

        /// <summary>A filled compartment block (scale &lt; 1 while the completed tray leaves).</summary>
        public void Block(int color, ConveyorStationDef st, int i, float scale = 1f, float lift = 0f)
        {
            var p = CompartmentPos(st, i) + new Vector3(0, lift, 0);
            sBatch[color].Add(Local(p, new Vector3(ConveyorBoard.Cell * 0.94f, BlockH, ConveyorBoard.Cell * 0.94f) * scale));
        }

        /// <summary>A still-required compartment: low, lighter plate showing the colour it waits for.</summary>
        public void Ghost(int color, ConveyorStationDef st, int i, float scale = 1f)
        {
            var p = World(Geo.Compartment(st, Mathf.Min(i, st.Capacity - 1)), FloorTop + 0.14f + 0.06f);
            gBatch[color].Add(Local(p, new Vector3(ConveyorBoard.Cell * 0.9f, 0.12f, ConveyorBoard.Cell * 0.9f) * scale));
        }

        public void EndFrame()
        {
            var cam = Stage.Camera;
            for (int c = 0; c < sBatch.Length; c++)
            {
                Flush(sBatch[c], solid[c], cam);
                Flush(gBatch[c], ghost[c], cam);
            }
        }

        void Flush(List<Matrix4x4> list, Material mat, Camera cam)
        {
            for (int start = 0; start < list.Count; start += 1023)
            {
                int n = Mathf.Min(1023, list.Count - start);
                list.CopyTo(start, buf, 0, n);
                Graphics.DrawMeshInstanced(cube, 0, mat, buf, n, null, ShadowCastingMode.Off, false, gameObject.layer, cam);
            }
        }

        /// <summary>Unit cube centred at the origin with per-face 0..1 UVs (bevelled, glossy edges in VoxelLit).</summary>
        static Mesh BuildBevelCube()
        {
            var d = new MeshData();
            d.Box(Vector3.zero, Vector3.one, Color.white);
            var m = d.ToMesh("ConveyorCube");
            return m;
        }

        void OnDestroy()
        {
            if (boardMat != null) Destroy(boardMat);
            if (solid != null) foreach (var m in solid) if (m != null) Destroy(m);
            if (ghost != null) foreach (var m in ghost) if (m != null) Destroy(m);
        }
    }
}
