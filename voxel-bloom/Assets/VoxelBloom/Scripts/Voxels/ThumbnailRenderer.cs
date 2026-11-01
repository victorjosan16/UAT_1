using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VoxelBloom.Core;

namespace VoxelBloom
{
    /// <summary>
    /// Renders model thumbnails into cached RenderTextures, one per frame, using a single hidden stage.
    /// Works in URP and Built-in (enables the camera for exactly one frame per thumbnail).
    /// </summary>
    public sealed class ThumbnailRenderer : MonoBehaviour
    {
        static ThumbnailRenderer instance;
        readonly Dictionary<string, RenderTexture> cache = new Dictionary<string, RenderTexture>();
        readonly Queue<(string id, VoxelGrid grid, RenderTexture rt)> pending = new Queue<(string, VoxelGrid, RenderTexture)>();
        VoxelStage stage;
        bool running;

        static ThumbnailRenderer Instance
        {
            get
            {
                if (instance != null) return instance;
                var go = new GameObject("Thumbnails");
                DontDestroyOnLoad(go);
                instance = go.AddComponent<ThumbnailRenderer>();
                return instance;
            }
        }

        /// <summary>Returns a texture immediately; it fills in within a few frames.</summary>
        public static Texture Get(string modelId, VoxelGrid gridOverride = null, int size = 256)
        {
            var self = Instance;
            string key = modelId + (gridOverride != null ? "#" + gridOverride.GetHashCode() : "");
            if (self.cache.TryGetValue(key, out var rt) && rt != null) return rt;
            var grid = gridOverride ?? Services.Data.GetModel(modelId);
            rt = new RenderTexture(size, size, 16, RenderTextureFormat.ARGB32) { name = "Thumb_" + key };
            rt.Create();
            self.cache[key] = rt;
            if (grid != null) self.pending.Enqueue((key, grid, rt));
            if (!self.running) self.StartCoroutine(self.Process());
            return rt;
        }

        IEnumerator Process()
        {
            running = true;
            if (stage == null)
            {
                stage = VoxelStage.Create("Thumbnails", 64, false);
                stage.Camera.enabled = false;
            }
            while (pending.Count > 0)
            {
                var job = pending.Dequeue();
                stage.ShowModel(job.grid, 2f);
                stage.Yaw = stage.TargetYaw = -35f;
                stage.Pivot.localRotation = Quaternion.Euler(0, -35f, 0);
                stage.Camera.targetTexture = job.rt;
                stage.Camera.enabled = true;
                yield return null;          // camera renders this frame
                yield return new WaitForEndOfFrame();
                stage.Camera.enabled = false;
            }
            running = false;
        }
    }
}
