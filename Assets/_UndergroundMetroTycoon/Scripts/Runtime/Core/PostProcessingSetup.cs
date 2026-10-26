using UnityEngine;
#if UMT_URP
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#endif

namespace UndergroundMetroTycoon.Game
{
    /// <summary>
    /// Mobile-friendly URP post-processing: slightly richer colour and contrast, a subtle vignette, and soft
    /// bloom on High quality only. Does nothing when URP is not the active pipeline.
    /// </summary>
    public static class PostProcessingSetup
    {
#if UMT_URP
        private static Bloom _bloom;
        private static Vignette _vignette;
#endif

        public static void Create(Transform parent, Camera camera)
        {
#if UMT_URP
            if (GraphicsSettings.currentRenderPipeline == null) return;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;

            var go = new GameObject("PostProcessing");
            go.transform.SetParent(parent, false);
            var volume = go.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.priority = 1f;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            volume.sharedProfile = profile;

            ColorAdjustments color = profile.Add<ColorAdjustments>(true);
            color.saturation.Override(14f);
            color.contrast.Override(10f);
            color.postExposure.Override(0.08f);

            _vignette = profile.Add<Vignette>(true);
            _vignette.intensity.Override(0.18f);
            _vignette.smoothness.Override(0.45f);

            _bloom = profile.Add<Bloom>(true);
            _bloom.intensity.Override(0.35f);
            _bloom.threshold.Override(1.05f);
            _bloom.scatter.Override(0.6f);
#endif
        }

        /// <summary>0 = Low, 1 = Medium, 2 = High.</summary>
        public static void ApplyQuality(Camera camera, int quality)
        {
#if UMT_URP
            if (GraphicsSettings.currentRenderPipeline == null || camera == null) return;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = quality > 0;
            if (_bloom != null) _bloom.active = quality >= 2;
            if (_vignette != null) _vignette.active = quality >= 1;
#endif
        }
    }
}
