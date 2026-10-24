using UnityEngine;
using UnityEngine.UI;

namespace QuarryEmpire
{
    /// <summary>Mesaje scurte pe mijlocul ecranului („Stivă plină!”, „Deblocat: Buldozer!”).</summary>
    public class ToastHud : MonoBehaviour
    {
        [SerializeField] private float duration = 2.2f;

        private CanvasGroup _group;
        private Text _text;
        private float _timer;

        private void Awake()
        {
            Image panel = UiFactory.CreatePanel("Toast", transform, new Color(0f, 0f, 0f, 0.65f));
            panel.raycastTarget = false;
            var rect = UiFactory.Place((RectTransform)panel.transform, new Vector2(0.5f, 0.68f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(900f, 120f));
            _group = panel.gameObject.AddComponent<CanvasGroup>();
            _group.alpha = 0f;
            _group.blocksRaycasts = false;

            _text = UiFactory.CreateText("Text", rect, "", 40);
            UiFactory.Stretch((RectTransform)_text.transform, 10f);
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
                Show("Stivă plină! Mergi la zona VINDE.");
        }

        private void Show(string message)
        {
            _text.text = message;
            _timer = duration;
        }

        private void Update()
        {
            if (_timer <= 0f)
            {
                _group.alpha = 0f;
                return;
            }
            _timer -= Time.unscaledDeltaTime;
            // Apare instant, dispare lin în ultimele 0.4 secunde.
            _group.alpha = Mathf.Clamp01(_timer / 0.4f);
        }
    }
}
