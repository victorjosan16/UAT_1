using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace VoxelBloom
{
    /// <summary>Pressable button with squash animation, click sound and optional repeat-guard.</summary>
    public sealed class UIButton : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IPointerClickHandler, IPointerExitHandler
    {
        public Action OnClick;
        public bool Interactable = true;
        public Sfx Sound = Sfx.Button;
        public float Cooldown = 0.25f;
        public RectTransform Face;
        float lastClick = -10f;
        bool pressed;

        public void OnPointerDown(PointerEventData e)
        {
            if (!Interactable) return;
            pressed = true;
            transform.localScale = new Vector3(0.94f, 0.92f, 1f);
        }

        public void OnPointerUp(PointerEventData e) => Release();
        public void OnPointerExit(PointerEventData e) => Release();

        void Release()
        {
            if (!pressed) return;
            pressed = false;
            Tween.Scale(transform, transform.localScale, Vector3.one, 0.18f, Ease.OutBack);
        }

        public void OnPointerClick(PointerEventData e)
        {
            if (e.dragging) return;
            if (!Interactable) { Services.Audio?.Play(Sfx.Error, 1f, 0.5f); return; }
            if (Time.unscaledTime - lastClick < Cooldown) return;
            lastClick = Time.unscaledTime;
            Services.Audio?.Play(Sound);
            OnClick?.Invoke();
        }

        public void SetInteractable(bool on)
        {
            Interactable = on;
            var cg = GetComponent<CanvasGroup>();
            if (cg == null) cg = gameObject.AddComponent<CanvasGroup>();
            cg.alpha = on ? 1f : 0.55f;
        }
    }

    /// <summary>Keeps a RectTransform inside Screen.safeArea (notches, cutouts).</summary>
    [RequireComponent(typeof(RectTransform))]
    public sealed class SafeArea : MonoBehaviour
    {
        Rect last;
        Vector2Int lastSize;

        void Update()
        {
            var sa = Screen.safeArea;
            if (sa == last && lastSize.x == Screen.width && lastSize.y == Screen.height) return;
            last = sa; lastSize = new Vector2Int(Screen.width, Screen.height);
            var rt = (RectTransform)transform;
            if (Screen.width <= 0 || Screen.height <= 0) return;
            rt.anchorMin = new Vector2(sa.xMin / Screen.width, sa.yMin / Screen.height);
            rt.anchorMax = new Vector2(sa.xMax / Screen.width, sa.yMax / Screen.height);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
        }
    }

    /// <summary>Picks CanvasScaler matching for phones (match width) vs tablets/wide screens (match height).</summary>
    [RequireComponent(typeof(CanvasScaler))]
    public sealed class AdaptiveScaler : MonoBehaviour
    {
        void Update()
        {
            var cs = GetComponent<CanvasScaler>();
            float aspect = Screen.height > 0 ? (float)Screen.width / Screen.height : 0.5625f;
            cs.matchWidthOrHeight = aspect > 0.62f ? 1f : 0f;
        }
    }
}
