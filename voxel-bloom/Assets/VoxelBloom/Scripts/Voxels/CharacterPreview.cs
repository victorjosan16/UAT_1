using UnityEngine;
using VoxelBloom.Core;

namespace VoxelBloom
{
    /// <summary>
    /// Home/collection hero character: the model split into body / head / tail meshes (same voxel grid, so it is
    /// exactly the gameplay model) with idle animation - breathing, blinking, head tilt, tail wag - plus a hop
    /// and spin reaction on tap. Only 3 meshes are animated; nothing uses physics.
    /// </summary>
    public enum Expression { Open, Blink, Happy, Love }

    public sealed class CharacterPreview : MonoBehaviour
    {
        public FragmentSystem Sparkles;
        /// <summary>Static display (Home page): no idle animation, blinking, sparkles or transitions.</summary>
        public bool Static;
        sbyte[] headHappy, headLove;
        float exprUntil = -1f, lookUntil = -1f, nextLook = 4f, lookYaw, nextHappy = 7f;
        Expression current = Expression.Open;
        float nextTwinkle = 1f;
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
            var basePal = VoxelMeshBuilder.ToColors(g.Palette);
            palette = new Color32[basePal.Length + 2];
            System.Array.Copy(basePal, palette, basePal.Length);
            palette[basePal.Length] = new Color32(255, 70, 110, 255);      // heart red
            palette[basePal.Length + 1] = new Color32(255, 190, 210, 255); // heart highlight
            if (locked) for (int i = 0; i < palette.Length; i++) palette[i] = new Color32(86, 78, 120, 255);
            sbyte heart = (sbyte)basePal.Length, heartHi = (sbyte)(basePal.Length + 1);
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
            headHappy = (sbyte[])h.Clone();
            headLove = (sbyte[])h.Clone();
            if (anim != null && anim.Lid >= 0 && !locked)
            {
                // eye cells are exported per eye column/row: index k -> row (k % 6) / 2 (0 = bottom, 2 = top)
                for (int k = 0; k < anim.Eyes.Length; k++)
                {
                    int e = anim.Eyes[k];
                    if (e < 0 || e >= n || h[e] < 0) continue;
                    int row = (k % 6) / 2;
                    headBlink[e] = (sbyte)anim.Lid;
                    headHappy[e] = row == 1 ? h[e] : (sbyte)anim.Lid;     // squint "^ ^" line
                    headLove[e] = row == 2 && (k / 6) == 0 ? heartHi : heart; // heart eyes
                    if (h[e] >= 0 && row == 2 && (k / 6) == 0) headHappy[e] = (sbyte)anim.Lid;
                }
            }

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
            current = Expression.Open;
            swap = 0f;
        }

        /// <summary>Tap reaction: heart eyes, hop, spin and a burst of coloured sparkle cubes.</summary>
        public void React()
        {
            if (Static) return;
            if (react < 0f) react = 0f;
            SetExpression(Expression.Love, 1.2f);
            if (Sparkles != null && !Services.ReducedEffects)
            {
                Color32[] cols = { new Color32(255, 160, 40, 255), new Color32(255, 214, 64, 255), new Color32(255, 111, 181, 255), new Color32(89, 184, 255, 255) };
                for (int i = 0; i < 12; i++)
                {
                    var p = transform.TransformPoint(new Vector3(Random.Range(-1f, 1f), Random.Range(0.6f, 1.6f), Random.Range(-0.6f, 0.2f)) * size * Y * 0.5f);
                    Sparkles.Burst(p, cols[i % cols.Length], size * 1.4f, 1);
                }
            }
        }

        public void SetExpression(Expression e, float seconds)
        {
            current = e;
            exprUntil = t + seconds;
            blinking = false;
            head.SetCells(e == Expression.Love ? headLove : e == Expression.Happy ? headHappy : e == Expression.Blink ? headBlink : headCells);
        }

        void Update()
        {
            if (Static)
            {
                if (swap >= 0f) { swap = -1f; root.localScale = Vector3.one; }
                return;
            }
            float dt = Time.unscaledDeltaTime;
            t += dt;
            bool calm = Services.ReducedEffects;
            // breathing (body), head tilt, tail wag
            float breathe = Mathf.Sin(t * 2.4f) * (calm ? 0.008f : 0.02f);
            body.transform.localScale = new Vector3(1f - breathe * 0.5f, 1f + breathe, 1f - breathe * 0.5f);
            // look around: occasionally turn the head left/right and hold
            if (lookUntil < 0f && t >= nextLook) { lookYaw = Random.value < 0.5f ? -28f : 28f; lookUntil = t + 1.4f; }
            if (lookUntil >= 0f && t >= lookUntil) { lookYaw = 0f; lookUntil = -1f; nextLook = t + Random.Range(4f, 8f); }
            float yaw = Mathf.LerpAngle(headPivot.localEulerAngles.y, calm ? 0f : lookYaw, 1f - Mathf.Exp(-6f * dt));
            headPivot.localRotation = Quaternion.Euler(Mathf.Sin(t * 1.3f + seed) * 3f, yaw, Mathf.Sin(t * 0.9f + seed) * (calm ? 2f : 5f));
            tailPivot.localRotation = Quaternion.Euler(0, Mathf.Sin(t * 6f) * (calm ? 6f : 18f), 0);

            // expressions: timed ones return to open eyes; idle blink and an occasional happy squint
            if (current != Expression.Open && t >= exprUntil) { current = Expression.Open; head.SetCells(headCells); nextBlink = t + 1.5f; }
            if (current == Expression.Open)
            {
                if (!blinking && t >= nextBlink) { blinking = true; blinkUntil = t + 0.13f; head.SetCells(headBlink); }
                else if (blinking && t >= blinkUntil) { blinking = false; nextBlink = t + Random.Range(2.2f, 4.5f); head.SetCells(headCells); }
                else if (!blinking && t >= nextHappy) { nextHappy = t + Random.Range(8f, 14f); SetExpression(Expression.Happy, 0.9f); }
            }

            // premium twinkles around the mascot
            if (Sparkles != null && !calm && t >= nextTwinkle)
            {
                nextTwinkle = t + Random.Range(0.35f, 0.8f);
                var p = transform.TransformPoint(new Vector3(Random.Range(-0.7f, 0.7f) * X, Random.Range(0.3f, 1.05f) * Y, Random.Range(-0.6f, 0.1f) * Z) * size);
                Sparkles.Burst(p, Random.value < 0.5f ? new Color32(255, 255, 255, 255) : new Color32(255, 220, 110, 255), size * 0.7f, 1);
            }
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
