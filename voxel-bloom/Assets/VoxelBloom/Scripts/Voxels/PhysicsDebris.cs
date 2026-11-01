using UnityEngine;

namespace VoxelBloom
{
    /// <summary>
    /// Pooled Rigidbody cubes for pieces that detach and fall (hybrid destruction: the logical model is a single
    /// optimised mesh, only falling chunks get physics). Fixed pool, controlled lifetime, invisible floor at the
    /// pedestal height, no per-voxel physics on the intact model.
    /// </summary>
    public sealed class PhysicsDebris : MonoBehaviour
    {
        const float Life = 1.6f;
        Rigidbody[] bodies;
        MeshRenderer[] renderers;
        float[] age;
        int next;
        MaterialPropertyBlock block;
        static readonly int ColorId = Shader.PropertyToID("_InstanceColor");
        BoxCollider floor;

        public int Capacity => Services.ReducedEffects ? 24 : (Services.Initialized && Services.Save.Settings.Quality == 1 ? 48 : 96);

        void Awake()
        {
            block = new MaterialPropertyBlock();
            var mesh = new Mesh { name = "DebrisCube" };
            new VoxelMeshBuilder { AmbientOcclusion = false }.Build(mesh, new sbyte[] { 0 }, 1, 1, 1, new[] { new Color32(255, 255, 255, 255) }, 1f);
            var v = mesh.vertices;
            for (int i = 0; i < v.Length; i++) v[i].y -= 0.5f;
            mesh.vertices = v;
            mesh.RecalculateBounds();

            const int pool = 96;
            bodies = new Rigidbody[pool];
            renderers = new MeshRenderer[pool];
            age = new float[pool];
            for (int i = 0; i < pool; i++)
            {
                var go = new GameObject("Debris", typeof(MeshFilter), typeof(MeshRenderer), typeof(BoxCollider), typeof(Rigidbody));
                go.transform.SetParent(transform, false);
                go.GetComponent<MeshFilter>().sharedMesh = mesh;
                var r = go.GetComponent<MeshRenderer>();
                r.sharedMaterial = VoxelMaterials.Voxel;
                r.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                var rb = go.GetComponent<Rigidbody>();
                rb.mass = 0.2f;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                go.SetActive(false);
                bodies[i] = rb;
                renderers[i] = r;
                age[i] = Life;
            }
            var f = new GameObject("Floor", typeof(BoxCollider));
            f.transform.SetParent(transform, false);
            floor = f.GetComponent<BoxCollider>();
            floor.size = new Vector3(40f, 1f, 40f);
            floor.center = new Vector3(0, -0.5f, 0);
        }

        /// <summary>Places the floor at the pedestal top (world y of the model base).</summary>
        public void SetFloor(float worldY) => floor.transform.position = new Vector3(transform.position.x, worldY, transform.position.z);

        public void Spawn(Vector3 worldPos, Color32 color, float voxelSize)
        {
            int cap = Mathf.Min(Capacity, bodies.Length);
            int i = next;
            next = (next + 1) % cap;
            var rb = bodies[i];
            var go = rb.gameObject;
            go.SetActive(true);
            go.transform.position = worldPos;
            go.transform.rotation = Quaternion.identity;
            go.transform.localScale = Vector3.one * voxelSize * 0.96f;
            rb.linearVelocity = new Vector3(Random.Range(-0.4f, 0.4f), Random.Range(0f, 0.6f), Random.Range(-0.4f, 0.4f));
            rb.angularVelocity = Random.insideUnitSphere * 4f;
            block.SetColor(ColorId, new Color(color.r / 255f, color.g / 255f, color.b / 255f, 1f));
            renderers[i].SetPropertyBlock(block);
            age[i] = 0f;
        }

        void Update()
        {
            float dt = Time.deltaTime;
            for (int i = 0; i < bodies.Length; i++)
            {
                if (age[i] >= Life) continue;
                age[i] += dt;
                var t = bodies[i].transform;
                if (age[i] > Life - 0.35f) t.localScale *= Mathf.Max(0f, 1f - dt * 6f);
                if (age[i] >= Life) bodies[i].gameObject.SetActive(false);
            }
        }
    }
}
