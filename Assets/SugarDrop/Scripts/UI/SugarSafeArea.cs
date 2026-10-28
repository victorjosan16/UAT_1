using QuarryEmpire;
using UnityEngine;

namespace SugarDrop
{
    /// <summary>
    /// Zona sigură a ecranului (fără notch, colțuri rotunjite și bara de gesturi), plus locul bannerului de jos:
    /// când bannerul e afișat, marginea de jos urcă deasupra lui, ca HUD-ul, ajutoarele și bara hărții să nu fie
    /// acoperite. Tabla (în lume) nu se mută.
    /// </summary>
    [RequireComponent(typeof(RectTransform))]
    public class SugarSafeArea : MonoBehaviour
    {
        private RectTransform _rect;
        private Rect _applied;
        private Vector2Int _screen;
        private float _banner = -1f;

        private void Awake()
        {
            _rect = (RectTransform)transform;
            Apply();
        }

        private void OnEnable() => SugarAds.BannerChanged += Apply;
        private void OnDisable() => SugarAds.BannerChanged -= Apply;

        private void Update()
        {
            // Se re-aplică doar dacă s-a schimbat ceva (rotire, ecran pliabil, banner).
            if (Screen.safeArea != _applied || Screen.width != _screen.x || Screen.height != _screen.y || BannerPixels != _banner) Apply();
        }

        private static float BannerPixels => SugarAds.Instance != null ? SugarAds.Instance.BannerHeightPixels : 0f;

        private void Apply()
        {
            if (_rect == null) return;
            Rect safe = Screen.safeArea;
            _applied = safe;
            _screen = new Vector2Int(Screen.width, Screen.height);
            _banner = BannerPixels;
            if (Screen.width <= 0 || Screen.height <= 0) return;

            Vector2 min = safe.position;
            Vector2 max = safe.position + safe.size;
            // Bannerul stă lipit de marginea de jos a ecranului: interfața începe deasupra lui.
            min.y = Mathf.Max(min.y, _banner);
            _rect.anchorMin = new Vector2(min.x / Screen.width, min.y / Screen.height);
            _rect.anchorMax = new Vector2(max.x / Screen.width, max.y / Screen.height);
            _rect.offsetMin = Vector2.zero;
            _rect.offsetMax = Vector2.zero;
        }
    }
}
