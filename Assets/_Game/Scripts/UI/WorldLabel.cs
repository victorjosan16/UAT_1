using UnityEngine;
using UnityEngine.UI;

namespace QuarryEmpire
{
    /// <summary>
    /// Etichetă 3D deasupra unui obiect (preț, nume zonă, progres), mereu întoarsă spre cameră.
    /// Se construiește singură din cod: Canvas în spațiul lumii + panou rotunjit + text + bară opțională.
    /// </summary>
    public class WorldLabel : MonoBehaviour
    {
        [SerializeField] private string initialText = "";
        [SerializeField] private float height = 1.5f;
        [SerializeField] private Vector2 size = new(360f, 140f);
        [SerializeField] private int fontSize = 40;
        [SerializeField] private float worldScale = 0.01f;
        [SerializeField] private bool showBar = true;
        [SerializeField] private Color textColor = Color.white;
        [SerializeField] private Color barColor = new(1f, 0.73f, 0.16f);
        [SerializeField] private Color backgroundColor = new(0.1f, 0.12f, 0.2f, 0.78f);

        private UiLabel _text;
        private Image _fill;
        private Transform _canvasTransform;
        private Transform _camera;

        private void Awake() => Build();

        private void Build()
        {
            var canvasGo = new GameObject("LabelCanvas", typeof(RectTransform));
            _canvasTransform = canvasGo.transform;
            _canvasTransform.SetParent(transform, false);
            _canvasTransform.localPosition = Vector3.up * height;
            _canvasTransform.localScale = Vector3.one * worldScale;

            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 10;
            ((RectTransform)_canvasTransform).sizeDelta = size;

            Image background = UiFactory.CreatePanel("Background", _canvasTransform, backgroundColor);
            UiFactory.Stretch((RectTransform)background.transform);

            _text = UiFactory.CreateText("Text", _canvasTransform, initialText, fontSize, TextAnchor.MiddleCenter, textColor);
            RectTransform textRect = UiFactory.Stretch(_text.RectTransform, 10f);
            if (showBar) textRect.offsetMin = new Vector2(10f, 30f);

            if (!showBar) return;
            Image barBack = UiFactory.CreatePanel("BarBack", _canvasTransform, new Color(0f, 0f, 0f, 0.45f));
            var barRect = (RectTransform)barBack.transform;
            barRect.anchorMin = new Vector2(0f, 0f);
            barRect.anchorMax = new Vector2(1f, 0f);
            barRect.pivot = new Vector2(0.5f, 0f);
            barRect.offsetMin = new Vector2(16f, 10f);
            barRect.offsetMax = new Vector2(-16f, 30f);

            _fill = UiFactory.CreateFill("Fill", barRect, barColor);
            UiFactory.Stretch((RectTransform)_fill.transform, 3f);
        }

        public void SetText(string text)
        {
            if (_text != null) _text.Text = text;
        }

        public void SetTextColor(Color color)
        {
            if (_text != null) _text.Color = color;
        }

        public void SetFill(float value)
        {
            if (_fill != null) _fill.fillAmount = Mathf.Clamp01(value);
        }

        public void SetVisible(bool visible)
        {
            if (_canvasTransform != null && _canvasTransform.gameObject.activeSelf != visible)
                _canvasTransform.gameObject.SetActive(visible);
        }

        private void LateUpdate()
        {
            if (_canvasTransform == null) return;
            if (_camera == null)
            {
                Camera main = Camera.main;
                if (main == null) return;
                _camera = main.transform;
            }
            // Billboard: eticheta are aceeași rotație ca și camera, deci se citește mereu drept.
            _canvasTransform.rotation = _camera.rotation;
        }
    }
}
