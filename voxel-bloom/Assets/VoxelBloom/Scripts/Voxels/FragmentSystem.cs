using UnityEngine;

namespace VoxelBloom
{
    /// <summary>
    /// Pooled falling voxel fragments drawn with GPU instancing from a single Update
    /// (no per-fragment GameObjects or MonoBehaviours).
    /// </summary>
    public sealed class FragmentSystem : MonoBehaviour
    {
        const int Capacity = 512;
        readonly Vector3[] pos = new Vector3[Capacity];
        readonly Vector3[] vel = new Vector3[Capacity];
        readonly Quaternion[] rot = new Quaternion[Capacity];
        readonly Vector3[] spin = new Vector3[Capacity];
        readonly float[] life = new float[Capacity];
        readonly float[] size = new float[Capacity];
        readonly Vector4[] color = new Vector4[Capacity];
        readonly Matrix4x4[] matrices = new Matrix4x4[1023];
        readonly Vector4[] batchColors = new Vector4[1023];
        int count;
        Mesh cube;
        MaterialPropertyBlock block;
        static readonly int ColorId = Shader.PropertyToID("_InstanceColor");

        public int MaxPerBurst => Services.ReducedEffects ? 6 : (Services.Initialized && Services.Save.Settings.Quality == 1 ? 18 : 40);

        void Awake()
        {
            cube = BuildCube();
            block = new MaterialPropertyBlock();
        }

        public void Burst(Vector3 worldPos, Color32 c, float voxelSize, int n = 1)
        {
            for (int k = 0; k < n; k++)
            {
                if (count >= Capacity) return;
                int i = count++;
                pos[i] = worldPos;
                vel[i] = new Vector3(Random.Range(-1f, 1f), Random.Range(0.8f, 2.2f), Random.Range(-1f, 1f)) * (voxelSize * 14f);
                rot[i] = Random.rotation;
                spin[i] = Random.insideUnitSphere * 540f;
                life[i] = Random.Range(0.6f, 1.0f);
                size[i] = voxelSize * Random.Range(0.55f, 0.95f);
                color[i] = new Vector4(c.r / 255f, c.g / 255f, c.b / 255f, 1f);
            }
        }

        void Update()
        {
            float dt = Time.deltaTime;
            float g = 9.8f * 0.35f;
            for (int i = 0; i < count; i++)
            {
                life[i] -= dt;
                if (life[i] <= 0f)
                {
                    count--;
                    pos[i] = pos[count]; vel[i] = vel[count]; rot[i] = rot[count]; spin[i] = spin[count];
                    life[i] = life[count]; size[i] = size[count]; color[i] = color[count];
                    i--;
                    continue;
                }
                vel[i].y -= g * dt;
                pos[i] += vel[i] * dt;
                rot[i] = Quaternion.Euler(spin[i] * dt) * rot[i];
            }
            if (count == 0) return;
            int n = Mathf.Min(count, matrices.Length);
            for (int i = 0; i < n; i++)
            {
                float s = size[i] * Mathf.Clamp01(life[i] * 2.2f);
                matrices[i] = Matrix4x4.TRS(pos[i], rot[i], new Vector3(s, s, s));
                batchColors[i] = color[i];
            }
            block.SetVectorArray(ColorId, batchColors);
            Graphics.DrawMeshInstanced(cube, 0, VoxelMaterials.Fragment, matrices, n, block, UnityEngine.Rendering.ShadowCastingMode.Off, false, gameObject.layer);
        }

        static Mesh BuildCube()
        {
            var m = new Mesh { name = "FragmentCube" };
            var b = new VoxelMeshBuilder { AmbientOcclusion = false };
            b.Build(m, new sbyte[] { 0 }, 1, 1, 1, new[] { new Color32(255, 255, 255, 255) }, 1f);
            // re-centre the unit cube (builder puts base at y=0)
            var v = m.vertices;
            for (int i = 0; i < v.Length; i++) v[i].y -= 0.5f;
            m.vertices = v;
            m.RecalculateBounds();
            return m;
        }
    }
}
