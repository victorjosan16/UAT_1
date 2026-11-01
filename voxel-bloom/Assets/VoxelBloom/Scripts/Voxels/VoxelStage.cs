using UnityEngine;
using VoxelBloom.Core;

namespace VoxelBloom
{
    /// <summary>
    /// An isolated 3D "photo studio": camera + voxel model + pedestal rendered into a RenderTexture
    /// that UI shows through a RawImage. Stages live far apart in world space so they never overlap.
    /// </summary>
    public sealed class VoxelStage : MonoBehaviour
    {
        static int nextSlot;

        public Camera Camera { get; private set; }
        public VoxelModelView Model { get; private set; }
        public RenderTexture Texture { get; private set; }
        public FragmentSystem Fragments { get; private set; }
        public PhysicsDebris Debris { get; private set; }
        public Transform Pivot { get; private set; }
        public Transform PedestalRoot { get; private set; }

        public float AutoRotateSpeed = 0f;
        /// <summary>Turntable speed (deg/s) used in gameplay; pauses after user interaction.</summary>
        public float TurntableSpeed = 0f;
        float turntablePausedUntil;
        Transform shadow;
        public float TargetYaw = -30f;
        public float Yaw = -30f;
        public float Bob;
        float bobTime;
        float shake;
        Vector3 camBase;

        public static VoxelStage Create(string name, int textureSize, bool pedestal, bool grass = false)
        {
            var go = new GameObject("Stage_" + name);
            DontDestroyOnLoad(go);
            go.transform.position = new Vector3(5000f + 300f * nextSlot++, 0, 0);
            var st = go.AddComponent<VoxelStage>();
            st.Init(textureSize, pedestal, grass);
            return st;
        }

        void Init(int size, bool pedestal, bool grass)
        {
            Texture = new RenderTexture(size, size, 16, RenderTextureFormat.ARGB32) { name = name + "_RT", antiAliasing = QualitySettings.antiAliasing > 1 ? 4 : 1 };
            Texture.Create();

            Pivot = new GameObject("Pivot").transform;
            Pivot.SetParent(transform, false);
            var m = new GameObject("Model", typeof(MeshFilter), typeof(MeshRenderer), typeof(VoxelModelView));
            m.transform.SetParent(Pivot, false);
            Model = m.GetComponent<VoxelModelView>();

            PedestalRoot = new GameObject("Pedestal").transform;
            PedestalRoot.SetParent(Pivot, false);
            if (pedestal || grass) BuildPedestal(grass);

            var camGo = new GameObject("Camera");
            camGo.transform.SetParent(transform, false);
            Camera = camGo.AddComponent<Camera>();
            Camera.clearFlags = CameraClearFlags.SolidColor;
            Camera.backgroundColor = new Color(0, 0, 0, 0);
            Camera.fieldOfView = 28f;
            Camera.nearClipPlane = 0.1f;
            Camera.farClipPlane = 60f;
            Camera.targetTexture = Texture;
            Camera.allowHDR = false;
            Camera.allowMSAA = Texture.antiAliasing > 1;

            shadow = CreateShadow(transform);
            Fragments = new GameObject("Fragments").AddComponent<FragmentSystem>();
            Fragments.transform.SetParent(transform, false);
            Debris = new GameObject("Debris").AddComponent<PhysicsDebris>();
            Debris.transform.SetParent(transform, false);
            Frame(1f, 1f);
        }

        void BuildPedestal(bool grass)
        {
            const int R = 9, H = 3;
            int X = R * 2 + 1, Z = R * 2 + 1;
            var cells = new sbyte[X * H * Z];
            for (int i = 0; i < cells.Length; i++) cells[i] = -1;
            for (int z = 0; z < Z; z++)
            for (int x = 0; x < X; x++)
            {
                float d = Mathf.Sqrt((x - R) * (x - R) + (z - R) * (z - R));
                if (d > R + 0.3f) continue;
                for (int y = 0; y < H; y++)
                {
                    int i = x + X * (y + H * z);
                    if (grass) cells[i] = (sbyte)(y == H - 1 ? (((x * 7 + z * 13) % 5 == 0) ? 1 : 0) : 2);
                    else cells[i] = (sbyte)(d > R - 1.2f ? (y == H - 1 ? 2 : 1) : (y == H - 1 ? 0 : 3));
                }
            }
            var palette = grass
                ? new Color32[] { new Color32(108, 205, 92, 255), new Color32(150, 228, 104, 255), new Color32(170, 120, 74, 255) }
                : new Color32[] { new Color32(250, 248, 255, 255), new Color32(236, 170, 40, 255), new Color32(255, 214, 90, 255), new Color32(226, 220, 246, 255) }; // white top, golden rim
            var go = new GameObject("PedestalMesh", typeof(MeshFilter), typeof(MeshRenderer), typeof(VoxelModelView));
            go.transform.SetParent(PedestalRoot, false);
            go.GetComponent<VoxelModelView>().SetModel(cells, X, H, Z, palette, 1f);
        }

        /// <summary>Shows a grid: scales voxels so the model's largest side spans ~<paramref name="span"/> units.</summary>
        public void ShowModel(sbyte[] cells, int x, int y, int z, Color32[] palette, float span = 2f)
        {
            float size = span / Mathf.Max(x, Mathf.Max(y, z));
            Model.SetModel(cells, x, y, z, palette, size);
            float footprint = Mathf.Max(x, z) * size;
            PedestalRoot.localScale = Vector3.one * (footprint * 0.62f / 9f);
            PedestalRoot.localPosition = new Vector3(0, -PedestalRoot.localScale.y * 3f, 0);
            Frame(Mathf.Max(x, z) * size, y * size);
            Debris.SetFloor(transform.position.y);
            float fp = Mathf.Max(x, z) * size;
            shadow.localPosition = new Vector3(0, 0.004f, 0);
            shadow.localScale = new Vector3(fp * 1.25f, 1, fp * 1.25f);
        }

        public void ShowModel(VoxelGrid g, float span = 2f)
            => ShowModel(g.CopyCells(), g.X, g.Y, g.Z, VoxelMeshBuilder.ToColors(g.Palette), span);

        /// <summary>Frames a wider scene (diorama) instead of a single model.</summary>
        public void FrameScene(float width, float height, float lookY)
        {
            float radius = Mathf.Max(width * 0.5f, height * 0.5f);
            float dist = radius / Mathf.Tan(Camera.fieldOfView * 0.5f * Mathf.Deg2Rad) * 1.05f;
            camBase = new Vector3(0, lookY + dist * 0.22f, -dist);
            Camera.transform.localPosition = camBase;
            Camera.transform.LookAt(transform.position + new Vector3(0, lookY, 0));
        }

        void Frame(float width, float height)
        {
            float radius = Mathf.Max(width * 0.8f, height * 0.62f) + 0.25f;
            float dist = radius / Mathf.Tan(Camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            camBase = new Vector3(0, height * 0.5f + dist * 0.28f, -dist);
            Camera.transform.localPosition = camBase;
            Camera.transform.LookAt(transform.position + new Vector3(0, height * 0.45f, 0));
        }

        /// <summary>Soft contact shadow under the model (transparent quad, built-in sprite shader).</summary>
        static Transform CreateShadow(Transform parent)
        {
            var go = new GameObject("ContactShadow", typeof(MeshFilter), typeof(MeshRenderer));
            go.transform.SetParent(parent, false);
            var m = new Mesh { name = "ShadowQuad" };
            m.vertices = new[] { new Vector3(-0.5f, 0, -0.5f), new Vector3(-0.5f, 0, 0.5f), new Vector3(0.5f, 0, 0.5f), new Vector3(0.5f, 0, -0.5f) };
            m.uv = new[] { new Vector2(0, 0), new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0) };
            m.triangles = new[] { 0, 1, 2, 0, 2, 3 };
            m.RecalculateBounds();
            go.GetComponent<MeshFilter>().sharedMesh = m;
            var shader = Shader.Find("Sprites/Default") ?? Shader.Find("Universal Render Pipeline/Unlit");
            var mat = new Material(shader) { name = "ContactShadow", mainTexture = SpriteFactory.Glow().texture, color = new Color(0.18f, 0.12f, 0.32f, 0.45f) };
            var r = go.GetComponent<MeshRenderer>();
            r.sharedMaterial = mat;
            r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            return go.transform;
        }

        /// <summary>Replaces the render texture (e.g. a full-screen portrait background).</summary>
        public void UseTexture(int width, int height)
        {
            if (Texture != null) { Camera.targetTexture = null; Texture.Release(); Destroy(Texture); }
            Texture = new RenderTexture(width, height, 16, RenderTextureFormat.ARGB32) { name = name + "_RT", antiAliasing = QualitySettings.antiAliasing > 1 ? 4 : 1 };
            Texture.Create();
            Camera.targetTexture = Texture;
            Camera.allowMSAA = Texture.antiAliasing > 1;
        }

        /// <summary>Explicit camera placement (local to the stage).</summary>
        public void SetCamera(Vector3 localPosition, Vector3 lookAtLocal, float fov)
        {
            Camera.fieldOfView = fov;
            Camera.farClipPlane = 120f;
            camBase = localPosition;
            Camera.transform.localPosition = camBase;
            Camera.transform.LookAt(transform.TransformPoint(lookAtLocal));
        }

        public void SetShadowSize(float footprint) { shadow.localPosition = new Vector3(0, 0.004f, 0); shadow.localScale = new Vector3(footprint, 1, footprint); }

        /// <summary>Call when the player drags/taps the model: the turntable waits before resuming.</summary>
        public void NotifyInteraction(float pauseSeconds = 2.5f) => turntablePausedUntil = Time.unscaledTime + pauseSeconds;

        public void Shake(float strength) { if (!Services.ReducedEffects) shake = Mathf.Max(shake, strength); }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            TargetYaw += AutoRotateSpeed * dt;
            if (TurntableSpeed > 0f && Time.unscaledTime >= turntablePausedUntil) TargetYaw += TurntableSpeed * dt;
            Yaw = Mathf.LerpAngle(Yaw, TargetYaw, 1f - Mathf.Exp(-10f * dt));
            Pivot.localRotation = Quaternion.Euler(0, Yaw, 0);
            if (Bob > 0)
            {
                bobTime += dt;
                Model.transform.localPosition = new Vector3(0, Mathf.Sin(bobTime * 2.2f) * Bob, 0);
                Model.transform.localScale = new Vector3(1f + Mathf.Sin(bobTime * 4.4f) * Bob * 0.15f, 1f - Mathf.Sin(bobTime * 4.4f) * Bob * 0.15f, 1f);
            }
            if (shake > 0)
            {
                shake = Mathf.Max(0, shake - dt * 2.5f);
                Camera.transform.localPosition = camBase + (Vector3)(Random.insideUnitCircle * shake * 0.08f);
            }
            else if (Camera.transform.localPosition != camBase) Camera.transform.localPosition = camBase;
        }

        public void SetActive(bool on)
        {
            gameObject.SetActive(on);
        }

        void OnDestroy()
        {
            if (Texture != null) { Texture.Release(); Destroy(Texture); }
        }
    }
}
