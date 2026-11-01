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
        // projectiles: coloured cubes flying from the attack queue to their target voxels
        const int MaxShots = 256;
        readonly Vector3[] shotFrom = new Vector3[MaxShots], shotTo = new Vector3[MaxShots];
        readonly float[] shotT = new float[MaxShots], shotDur = new float[MaxShots], shotSize = new float[MaxShots];
        readonly Vector4[] shotColor = new Vector4[MaxShots];
        int shots;
        // hit cubes: the exact voxel swells white and shrinks away where the ball lands
        const int MaxPops = 128;
        readonly Vector3[] popPos = new Vector3[MaxPops];
        readonly float[] popT = new float[MaxPops], popSize = new float[MaxPops];
        readonly Vector4[] popColor = new Vector4[MaxPops];
        int pops;
        readonly Matrix4x4[] popMatrices = new Matrix4x4[1023];
        readonly Vector4[] popBatchColors = new Vector4[1023];
        MaterialPropertyBlock popBlock;
        Mesh cube, ball;
        MaterialPropertyBlock block, shotBlock;
        readonly Matrix4x4[] shotMatrices = new Matrix4x4[1023];
        readonly Vector4[] shotBatchColors = new Vector4[1023]; // same array length as the fragment batch (Unity fixes it per property)
        static readonly int ColorId = Shader.PropertyToID("_InstanceColor");

        public int MaxPerBurst => Services.ReducedEffects ? 6 : (Services.Initialized && Services.Save.Settings.Quality == 1 ? 18 : 40);

        void Awake()
        {
            cube = BuildCube();
            ball = BuildBall();
            block = new MaterialPropertyBlock();
            shotBlock = new MaterialPropertyBlock();
            popBlock = new MaterialPropertyBlock();
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

        /// <summary>The hit voxel itself: flashes white, swells and shrinks to nothing (0.22 s) at its exact position.</summary>
        public void PopCube(Vector3 worldPos, Color32 c, float voxelSize)
        {
            if (pops >= MaxPops) return;
            int i = pops++;
            popPos[i] = worldPos; popT[i] = 0f; popSize[i] = voxelSize;
            popColor[i] = new Vector4(c.r / 255f, c.g / 255f, c.b / 255f, 1f);
        }

        /// <summary>Fires a cube from <paramref name="from"/> to <paramref name="to"/> on an arc; bursts on arrival.</summary>
        public void Shoot(Vector3 from, Vector3 to, Color32 c, float size, float duration, float delay = 0f)
        {
            if (shots >= MaxShots) return;
            int i = shots++;
            shotFrom[i] = from; shotTo[i] = to; shotT[i] = -delay; shotDur[i] = Mathf.Max(0.05f, duration);
            shotSize[i] = size; shotColor[i] = new Vector4(c.r / 255f, c.g / 255f, c.b / 255f, 1f);
        }

        void Update()
        {
            float dt = Time.deltaTime;
            for (int i = 0; i < shots; i++)
            {
                shotT[i] += dt;
                if (shotT[i] >= shotDur[i])
                {
                    var col = shotColor[i];
                    Burst(shotTo[i], new Color(col.x, col.y, col.z, 1f), shotSize[i] * 0.8f, 2);
                    Burst(shotTo[i], Color.white, shotSize[i] * 0.5f, 1); // impact flash
                    shots--;
                    shotFrom[i] = shotFrom[shots]; shotTo[i] = shotTo[shots]; shotT[i] = shotT[shots]; shotDur[i] = shotDur[shots];
                    shotSize[i] = shotSize[shots]; shotColor[i] = shotColor[shots];
                    i--;
                }
            }
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
            // hit cubes
            int np = 0;
            for (int i = 0; i < pops; i++)
            {
                popT[i] += dt;
                const float dur = 0.22f;
                if (popT[i] >= dur) { pops--; popPos[i] = popPos[pops]; popT[i] = popT[pops]; popSize[i] = popSize[pops]; popColor[i] = popColor[pops]; i--; continue; }
                float k = popT[i] / dur;
                float sc = popSize[i] * (k < 0.35f ? Mathf.Lerp(1f, 1.45f, k / 0.35f) : Mathf.Lerp(1.45f, 0f, (k - 0.35f) / 0.65f));
                popMatrices[np] = Matrix4x4.TRS(popPos[i], Quaternion.identity, new Vector3(sc, sc, sc));
                popBatchColors[np] = Vector4.Lerp(Vector4.one, popColor[i], k); // white flash -> own colour
                np++;
            }
            if (np > 0)
            {
                popBlock.SetVectorArray(ColorId, popBatchColors);
                Graphics.DrawMeshInstanced(cube, 0, VoxelMaterials.Fragment, popMatrices, np, popBlock, UnityEngine.Rendering.ShadowCastingMode.Off, false, gameObject.layer);
            }
            if (count == 0 && shots == 0) return;
            int n = Mathf.Min(count, matrices.Length);
            for (int i = 0; i < n; i++)
            {
                float s = size[i] * Mathf.Clamp01(life[i] * 2.2f);
                matrices[i] = Matrix4x4.TRS(pos[i], rot[i], new Vector3(s, s, s));
                batchColors[i] = color[i];
            }
            int ns = 0;
            for (int i = 0; i < shots; i++)
            {
                if (shotT[i] < 0f) continue;
                float k = shotT[i] / shotDur[i];
                Vector3 mid = (shotFrom[i] + shotTo[i]) * 0.5f + Vector3.up * Vector3.Distance(shotFrom[i], shotTo[i]) * 0.3f;
                Vector3 p = Vector3.Lerp(Vector3.Lerp(shotFrom[i], mid, k), Vector3.Lerp(mid, shotTo[i], k), k);
                float s = shotSize[i] * 1.5f;
                // glowing trail: bright sparks left behind the ball
                if (!Services.ReducedEffects && count < Capacity && Random.value < 0.5f)
                {
                    int t = count++;
                    pos[t] = p; vel[t] = Random.insideUnitSphere * shotSize[i] * 1.5f; rot[t] = Random.rotation; spin[t] = Vector3.zero;
                    life[t] = 0.22f; size[t] = shotSize[i] * 0.45f;
                    color[t] = Vector4.Lerp(shotColor[i], Vector4.one, 0.55f);
                }
                shotMatrices[ns] = Matrix4x4.TRS(p, Quaternion.identity, new Vector3(s, s, s));
                shotBatchColors[ns] = Vector4.Lerp(shotColor[i], Vector4.one, 0.25f); // brighter = glowing
                ns++;
            }
            if (ns > 0)
            {
                shotBlock.SetVectorArray(ColorId, shotBatchColors);
                Graphics.DrawMeshInstanced(ball, 0, VoxelMaterials.Fragment, shotMatrices, ns, shotBlock, UnityEngine.Rendering.ShadowCastingMode.Off, false, gameObject.layer);
            }
            if (n == 0) return;
            block.SetVectorArray(ColorId, batchColors);
            Graphics.DrawMeshInstanced(cube, 0, VoxelMaterials.Fragment, matrices, n, block, UnityEngine.Rendering.ShadowCastingMode.Off, false, gameObject.layer);
        }

        /// <summary>Low-poly UV sphere (unit diameter) for projectile balls.</summary>
        static Mesh BuildBall()
        {
            const int lat = 10, lon = 14;
            var v = new System.Collections.Generic.List<Vector3>(); var nrm = new System.Collections.Generic.List<Vector3>(); var uv = new System.Collections.Generic.List<Vector2>(); var t = new System.Collections.Generic.List<int>();
            for (int a = 0; a <= lat; a++)
                for (int b = 0; b <= lon; b++)
                {
                    float th = Mathf.PI * a / lat, ph = 2 * Mathf.PI * b / lon;
                    var p = new Vector3(Mathf.Sin(th) * Mathf.Cos(ph), Mathf.Cos(th), Mathf.Sin(th) * Mathf.Sin(ph));
                    v.Add(p * 0.5f); nrm.Add(p); uv.Add(new Vector2(0.5f, 0.5f));
                }
            for (int a = 0; a < lat; a++)
                for (int b = 0; b < lon; b++)
                {
                    int i0 = a * (lon + 1) + b, i1 = i0 + lon + 1;
                    t.Add(i0); t.Add(i0 + 1); t.Add(i1); t.Add(i1); t.Add(i0 + 1); t.Add(i1 + 1);
                }
            var m = new Mesh { name = "ProjectileBall" };
            m.SetVertices(v); m.SetNormals(nrm); m.SetUVs(0, uv); m.SetTriangles(t, 0);
            var cols = new Color32[v.Count]; for (int i = 0; i < cols.Length; i++) cols[i] = new Color32(255, 255, 255, 255);
            m.colors32 = cols;
            return m;
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
