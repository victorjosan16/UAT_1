using UnityEngine;
using VoxelBloom.Core;

namespace VoxelBloom
{
    /// <summary>
    /// Home/collection hero character: the model split into body / head / tail meshes (same voxel grid, so it is
    /// exactly the gameplay model) with idle animation - breathing, blinking, head tilt, tail wag - plus a hop
    /// and spin reaction on tap. Only 3 meshes are animated; nothing uses physics.
    /// </summary>
    public sealed class CharacterPreview : MonoBehaviour
    {
        VoxelModelView body, head, tail;
        Transform headPivot, tailPivot, root;
        sbyte[] headCells, headBlink;
        int X, Y, Z;
        float size;
        Color32[] palette;
        float t, nextBlink = 2f, blinkUntil = -1f, react = -1f, swap = -1f;
        bool blinking;
        int seed;

        public static CharacterPreview Create(Transform parent)
        {
            var go = new GameObject("CharacterPreview");
            go.transform.SetParent(parent, false);
            return go.AddComponent<CharacterPreview>();
        }

        void Awake()
        {
            root = new GameObject("Root").transform;
            root.SetParent(transform, false);
            body = MakeView("Body", root);
            headPivot = new GameObject("HeadPivot").transform; headPivot.SetParent(root, false);
            head = MakeView("Head", headPivot);
            tailPivot = new GameObject("TailPivot").transform; tailPivot.SetParent(root, false);
            tail = MakeView("Tail", tailPivot);
        }

        static VoxelModelView MakeView(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer), typeof(VoxelModelView));
            go.transform.SetParent(parent, false);
            return go.GetComponent<VoxelModelView>();
        }

        /// <summary>Shows a model; locked characters are shown as a dark silhouette.</summary>
        public void Show(VoxelGrid g, AnimProfile anim, float voxelSize, bool locked, int seed = 0)
        {
            X = g.X; Y = g.Y; Z = g.Z; size = voxelSize; this.seed = seed;
            palette = VoxelMeshBuilder.ToColors(g.Palette);
            if (locked) for (int i = 0; i < palette.Length; i++) palette[i] = new Color32(86, 78, 120, 255);
            var all = g.CopyCells();
            int n = all.Length;
            var b = new sbyte[n]; var h = new sbyte[n]; var tl = new sbyte[n];
            int headY = anim != null && anim.HasHead ? anim.HeadY : int.MaxValue;
            for (int i = 0; i < n; i++)
            {
                b[i] = h[i] = tl[i] = -1;
                if (all[i] < 0) continue;
                g.Coord(i, out int x, out int y, out int z);
                if (anim != null && anim.InTail(x, y, z)) tl[i] = all[i];
                else if (y >= headY) h[i] = all[i];
                else b[i] = all[i];
            }
            headCells = h;
            headBlink = (sbyte[])h.Clone();
            if (anim != null && anim.Lid >= 0 && !locked) foreach (int e in anim.Eyes) if (e >= 0 && e < n && headBlink[e] >= 0) headBlink[e] = (sbyte)anim.Lid;

            body.SetModel(b, X, Y, Z, palette, size);
            head.SetModel(h, X, Y, Z, palette, size);
            tail.SetModel(tl, X, Y, Z, palette, size);

            // pivots: neck at (centre, headY), tail at the tail box base; meshes offset so the model stays in place
            float neck = headY == int.MaxValue ? 0 : headY * size;
            headPivot.localPosition = new Vector3(0, neck, 0);
            head.transform.localPosition = new Vector3(0, -neck, 0);
            Vector3 tp = Vector3.zero;
            if (anim != null && anim.Tail != null)
                tp = new Vector3((anim.Tail[0] + anim.Tail[3] + 1) * 0.5f - X * 0.5f, anim.Tail[1], anim.Tail[2] - Z * 0.5f) * size;
            tailPivot.localPosition = tp;
            tail.transform.localPosition = -tp;
            blinking = false;
            swap = 0f;
        }

        /// <summary>Tap reaction: hop and spin.</summary>
        public void React() { if (react < 0f) react = 0f; }

        void Update()
        {
            float dt = Time.unscaledDeltaTime;
            t += dt;
            bool calm = Services.ReducedEffects;
            // breathing (body), head tilt, tail wag
            float breathe = Mathf.Sin(t * 2.4f) * (calm ? 0.008f : 0.02f);
            body.transform.localScale = new Vector3(1f - breathe * 0.5f, 1f + breathe, 1f - breathe * 0.5f);
            headPivot.localRotation = Quaternion.Euler(Mathf.Sin(t * 1.3f + seed) * 3f, 0, Mathf.Sin(t * 0.9f + seed) * (calm ? 2f : 5f));
            tailPivot.localRotation = Quaternion.Euler(0, Mathf.Sin(t * 6f) * (calm ? 6f : 18f), 0);

            // blink
            if (!blinking && t >= nextBlink) { blinking = true; blinkUntil = t + 0.13f; head.SetCells(headBlink); }
            else if (blinking && t >= blinkUntil) { blinking = false; nextBlink = t + Random.Range(2.2f, 4.5f); head.SetCells(headCells); }

            // change-character transition: pop in
            if (swap >= 0f)
            {
                swap += dt;
                float k = Mathf.Clamp01(swap / 0.35f);
                root.localScale = Vector3.one * Ease.OutBack(k);
                if (k >= 1f) swap = -1f;
            }
            // tap reaction: hop + spin
            if (react >= 0f)
            {
                react += dt;
                float k = Mathf.Clamp01(react / 0.6f);
                root.localPosition = new Vector3(0, Mathf.Sin(k * Mathf.PI) * size * 4f, 0);
                root.localRotation = Quaternion.Euler(0, k * 360f, 0);
                if (k >= 1f) { react = -1f; root.localPosition = Vector3.zero; root.localRotation = Quaternion.identity; }
            }
        }
    }
}
