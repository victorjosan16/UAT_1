using UnityEngine;
using UnityEngine.UI;

namespace QuarryEmpire
{
    /// <summary>
    /// Aspectul joystick-ului: inel exterior semi-transparent și buton interior.
    /// Nu atinge logica (OnScreenStick); doar înlocuiește sprite-urile și culorile la pornire.
    /// </summary>
    public class JoystickSkin : MonoBehaviour
    {
        [SerializeField] private Image background;
        [SerializeField] private Image handle;
        [SerializeField] private Color backgroundColor = new(1f, 1f, 1f, 0.35f);
        [SerializeField] private Color handleColor = new(1f, 1f, 1f, 0.95f);

        private void Awake()
        {
            if (background != null)
            {
                background.sprite = ProceduralSprites.Ring;
                background.color = backgroundColor;
            }
            if (handle != null)
            {
                handle.sprite = ProceduralSprites.Circle;
                handle.color = handleColor;
                // Umbră discretă sub buton, pentru volum.
                Image shadow = UiFactory.CreateImage("HandleShadow", handle.transform.parent, ProceduralSprites.Circle, new Color(0f, 0f, 0f, 0.25f));
                shadow.rectTransform.sizeDelta = handle.rectTransform.sizeDelta;
                shadow.rectTransform.SetSiblingIndex(handle.transform.GetSiblingIndex());
                shadow.gameObject.AddComponent<FollowRect>().Initialize(handle.rectTransform, new Vector2(0f, -8f));
            }
        }
    }
}
