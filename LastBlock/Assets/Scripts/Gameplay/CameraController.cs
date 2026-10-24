using UnityEngine;

namespace LastBlock.Gameplay
{
    /// <summary>
    /// Orbit camera that frames the tower for a portrait screen, supports horizontal orbiting,
    /// a trauma-based shake, and keeps the gradient background quad filling the view.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraController : MonoBehaviour
    {
        [SerializeField] Transform backgroundQuad;
        [SerializeField] float pitch = 22f;
        [SerializeField] float yaw = 35f;
        [SerializeField] float orbitDegreesPerPixel = 0.25f;
        [SerializeField] float followSmoothing = 4f;
        [Tooltip("Extra space around the tower for the HUD at the top and buttons at the bottom.")]
        [SerializeField] float framingMargin = 1.45f;
        [SerializeField] float minDistance = 4f;

        [Header("Shake")]
        [SerializeField] float maxShakeOffset = 0.25f;
        [SerializeField] float shakeDecay = 1.8f;

        Camera cam;
        Vector3 focusPoint = new Vector3(0f, 1f, 0f);
        Vector3 currentFocus = new Vector3(0f, 1f, 0f);
        float targetDistance = 7f;
        float currentDistance = 7f;
        float trauma;

        public Camera Camera => cam != null ? cam : (cam = GetComponent<Camera>());

        void Awake()
        {
            cam = GetComponent<Camera>();
        }

        /// <summary>Frames a tower of the given height and footprint width (world units).</summary>
        public void FrameTower(Vector3 towerBase, float towerHeight, float towerWidth, bool snap)
        {
            Camera camera = Camera;
            float halfFovV = camera.fieldOfView * 0.5f * Mathf.Deg2Rad;
            float halfFovH = Mathf.Atan(Mathf.Tan(halfFovV) * Mathf.Max(0.1f, camera.aspect));

            float halfHeight = towerHeight * 0.5f * framingMargin + 0.3f;
            // The footprint diagonal is what is visible when viewed at an angle.
            float halfWidth = towerWidth * 0.75f * framingMargin;
            float distanceV = halfHeight / Mathf.Tan(halfFovV);
            float distanceH = halfWidth / Mathf.Tan(halfFovH);

            targetDistance = Mathf.Max(minDistance, Mathf.Max(distanceV, distanceH) + towerWidth * 0.5f);
            focusPoint = towerBase + Vector3.up * (towerHeight * 0.45f);

            if (snap)
            {
                currentDistance = targetDistance;
                currentFocus = focusPoint;
                ApplyTransform(Vector3.zero);
                FitBackground();
            }
        }

        public void Orbit(float horizontalPixels)
        {
            yaw += horizontalPixels * orbitDegreesPerPixel;
        }

        /// <summary>Adds shake trauma (0..1). Shake strength is trauma squared, so small hits stay subtle.</summary>
        public void Shake(float amount)
        {
            trauma = Mathf.Clamp01(trauma + amount);
        }

        void LateUpdate()
        {
            float k = 1f - Mathf.Exp(-followSmoothing * Time.deltaTime);
            currentDistance = Mathf.Lerp(currentDistance, targetDistance, k);
            currentFocus = Vector3.Lerp(currentFocus, focusPoint, k);

            Vector3 shakeOffset = Vector3.zero;
            if (trauma > 0f)
            {
                float strength = trauma * trauma * maxShakeOffset;
                float t = Time.time * 25f;
                shakeOffset = new Vector3(Mathf.PerlinNoise(t, 0.1f) - 0.5f, Mathf.PerlinNoise(0.2f, t) - 0.5f, 0f) * (2f * strength);
                trauma = Mathf.Max(0f, trauma - shakeDecay * Time.deltaTime);
            }

            ApplyTransform(shakeOffset);
            FitBackground();
        }

        void ApplyTransform(Vector3 localShake)
        {
            Quaternion rotation = Quaternion.Euler(pitch, yaw, 0f);
            transform.SetPositionAndRotation(currentFocus + rotation * new Vector3(0f, 0f, -currentDistance), rotation);
            transform.position += transform.rotation * localShake;
        }

        /// <summary>Keeps the background quad just inside the far plane and large enough to cover the screen.</summary>
        void FitBackground()
        {
            if (backgroundQuad == null) return;
            Camera camera = Camera;
            float distance = camera.farClipPlane * 0.9f;
            float height = 2f * distance * Mathf.Tan(camera.fieldOfView * 0.5f * Mathf.Deg2Rad);
            float width = height * camera.aspect;
            backgroundQuad.localPosition = new Vector3(0f, 0f, distance);
            backgroundQuad.localRotation = Quaternion.identity;
            backgroundQuad.localScale = new Vector3(width * 1.2f, height * 1.2f, 1f);
        }
    }
}
