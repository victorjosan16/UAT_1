using UnityEngine;
using UnityEngine.UI;

namespace QuarryEmpire
{
    /// <summary>Mesaje scurte pe ecran („Stivă plină!”, „Deblocat: Buldozer!”), cu apariție animată.</summary>
    public class ToastHud : MonoBehaviour
    {
        [SerializeField] private float duration = 2.2f;

        private CanvasGroup _group;
        private RectTransform _panel;
        private UiLabel _text;
        private float _timer;

        private void Awake()
        {
            UiTheme theme = UiRoot.Theme;
            Image panel = UiFactory.CreatePanel("Toast", transform, theme.PanelColor);
            _panel = UiFactory.Place((RectTransform)panel.transform, new Vector2(0.5f, 0.64f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(880f, 116f));
            _group = panel.gameObject.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;
            _group.interactable = false;

            _text = UiFactory.CreateText("Text", _panel, "", 38);
            UiFactory.Stretch(_text.RectTransform, 14f);
        }

        private void OnEnable()
        {
            GameEvents.Toast += Show;
            GameEvents.StackFull += OnStackFull;
        }

        private void OnDisable()
        {
            GameEvents.Toast -= Show;
            GameEvents.StackFull -= OnStackFull;
        }

        private void OnStackFull(ResourceStack stack)
        {
            if (PlayerRoot.Instance != null && stack == PlayerRoot.Instance.Stack)
                Show(Loc.T("Stivă plină! Mergi la zona VINDE."));
        }

        private void Show(string message)
        {
            _text.Text = message;
            _timer = duration;
        }

        private void Update()
        {
            if (_timer <= 0f)
            {
                if (_group.alpha > 0f) _group.alpha = 0f;
                return;
            }
            _timer -= Time.unscaledDeltaTime;
            float elapsed = duration - _timer;
            // Intră de jos cu un mic „pop”, apoi dispare lin în ultimele 0.4 secunde.
            float enter = Mathf.Clamp01(elapsed / 0.18f);
            _group.alpha = Mathf.Min(enter, Mathf.Clamp01(_timer / 0.4f));
            _panel.anchoredPosition = new Vector2(0f, Mathf.Lerp(-30f, 0f, enter));
            _panel.localScale = Vector3.one * (0.94f + 0.06f * enter);
        }
    }
}
