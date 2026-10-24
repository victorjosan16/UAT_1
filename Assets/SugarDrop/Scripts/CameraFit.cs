using UnityEngine;

namespace SugarDrop
{
    /// <summary>
    /// Potrivește camera ortografică pentru orice telefon în portret: arată mereu toată lățimea
    /// jocului și cel puțin înălțimea dorită.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFit : MonoBehaviour
    {
        [SerializeField] private float worldWidth = 9.2f;
        [SerializeField] private float minWorldHeight = 20.4f;

        private Camera _camera;
        private float _lastAspect;

        private void Awake()
        {
            _camera = GetComponent<Camera>();
            Fit();
        }

        private void Update()
        {
            if (!Mathf.Approximately(_camera.aspect, _lastAspect)) Fit();
        }

        private void Fit()
        {
            _lastAspect = _camera.aspect;
            float halfHeightForWidth = worldWidth * 0.5f / Mathf.Max(0.01f, _camera.aspect);
            _camera.orthographicSize = Mathf.Max(minWorldHeight * 0.5f, halfHeightForWidth);
        }
    }
}
