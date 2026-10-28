using System;
using QuarryEmpire;
using UnityEngine;
using UnityEngine.UI;

namespace SugarDrop
{
    /// <summary>
    /// Antrenorul din tutorial: un card cu Bobo și o replică. Pentru pașii de joc (mută o bomboană) cardul stă sus și
    /// tabla rămâne liberă; pentru explicații (obiectiv, mutări, ajutoare) ecranul se întunecă puțin, o săgeată
    /// arată elementul despre care e vorba, iar „Am înțeles” trece mai departe.
    /// </summary>
    public class TutorialCoach : MonoBehaviour
    {
        private RectTransform _rect;
        private GameObject _dim;
        private RectTransform _card;
        private UiLabel _text;
        private Button _next;
        private UiLabel _nextLabel;
        private RectTransform _arrow;
        private RectTransform _bobo;
        private RectTransform _target;
        private bool _arrowUp;
        private Action _onNext;
        private float _popT;

        public bool IsOpen => _card != null && _card.gameObject.activeSelf;
        /// <summary>Un pas de explicație e deschis (tabla nu primește atingeri).</summary>
        public bool Blocking => _dim != null && _dim.activeSelf;

        private void Awake()
        {
            UiTheme theme = UiRoot.Theme;
            _rect = (RectTransform)transform;
            _dim = SugarUi.Dim(transform, 0.35f).gameObject;

            Image card = UiFactory.CreatePanel("Card", transform, Color.white);
            _card = (RectTransform)card.transform;
            Image ribbon = UiFactory.CreatePanel("Ribbon", _card, Palette.WallStripe);
            UiFactory.Place((RectTransform)ribbon.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560f, 22f));

            _bobo = UiFactory.CreateRect("Bobo", _card);
            UiFactory.Place(_bobo, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(95f, 0f), new Vector2(150f, 150f));
            if (BoboArt.Available) BoboArt.AddTo(_bobo, 170f, new Vector2(0f, -10f), BoboPose.Think);
            else BuildDrawnBobo();

            _text = UiFactory.CreateText("Text", _card, "", 40, TextAnchor.MiddleLeft, theme.TextDarkColor);
            _text.RectTransform.anchorMin = new Vector2(0f, 0f);
            _text.RectTransform.anchorMax = new Vector2(1f, 1f);
            _text.RectTransform.offsetMin = new Vector2(185f, 20f);
            _text.RectTransform.offsetMax = new Vector2(-30f, -24f);

            _next = SugarUi.Button("Next", _card, "", theme.PositiveColor, () =>
            {
                SugarUi.Click();
                Action next = _onNext;
                _onNext = null;
                next?.Invoke();
            }, 40);
            UiFactory.Place((RectTransform)_next.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 75f), new Vector2(420f, 110f));
            _nextLabel = UiFactory.GetButtonLabel(_next);

            Image arrow = UiFactory.CreateImage("Arrow", transform, SugarSprites.Chevron, theme.AccentColor);
            _arrow = arrow.rectTransform;
            _arrow.sizeDelta = new Vector2(120f, 120f);
            var outline = arrow.gameObject.AddComponent<Outline>();
            outline.effectColor = QuestStyle.Outline;
            outline.effectDistance = new Vector2(3f, -3f);

            Hide();
        }

        /// <summary>Bobo desenat din cod (când lipsesc imaginile pictate).</summary>
        private void BuildDrawnBobo()
        {
            Image cup = UiFactory.CreateImage("Cup", _bobo, SugarSprites.CupWrapper, Color.white);
            UiFactory.Place(cup.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -28f), new Vector2(110f, 110f));
            Image frosting = UiFactory.CreateImage("Frosting", _bobo, SugarSprites.Frosting, Color.white);
            UiFactory.Place(frosting.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 14f), new Vector2(118f, 118f));
            Image smile = UiFactory.CreateImage("Smile", _bobo, SugarSprites.Smile, new Color(0.35f, 0.12f, 0.22f));
            UiFactory.Place(smile.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(36f, 36f));
            for (int e = -1; e <= 1; e += 2)
            {
                Image eye = UiFactory.CreateImage("Eye", _bobo, SugarSprites.Circle, new Color(0.25f, 0.12f, 0.2f));
                UiFactory.Place(eye.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(e * 20f, -18f), new Vector2(14f, 14f));
            }
        }

        /// <summary>Pas de joc: cardul jos, în dreapta ajutoarelor (peste borcan), ca tabla să rămână vizibilă și liberă.</summary>
        public void ShowAction(string text)
        {
            Open(text, null, false, null);
            UiFactory.Place(_card, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-20f, 30f), new Vector2(820f, 190f));
        }

        /// <summary>Explicație: ecranul se întunecă, săgeata arată ținta, iar butonul trece mai departe.</summary>
        /// <param name="arrowUp">Ținta e deasupra cardului (săgeata arată în sus).</param>
        public void ShowInfo(string text, RectTransform target, bool arrowUp, Action onNext)
        {
            Open(text, target, arrowUp, onNext);
            UiFactory.Place(_card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, arrowUp ? 150f : -50f), new Vector2(960f, 340f));
        }

        public void Hide()
        {
            _onNext = null;
            _target = null;
            _dim.SetActive(false);
            _card.gameObject.SetActive(false);
            _arrow.gameObject.SetActive(false);
        }

        private void Open(string text, RectTransform target, bool arrowUp, Action onNext)
        {
            _text.Text = text;
            _onNext = onNext;
            _target = target;
            _arrowUp = arrowUp;
            bool info = onNext != null;
            _dim.SetActive(info);
            _next.gameObject.SetActive(info);
            _nextLabel.Text = SugarLoc.T("AM ÎNȚELES");
            _text.RectTransform.offsetMin = new Vector2(185f, info ? 140f : 20f);
            _card.gameObject.SetActive(true);
            _arrow.gameObject.SetActive(target != null);
            _arrow.SetAsLastSibling();
            _popT = 0f;
        }

        private void Update()
        {
            if (!_card.gameObject.activeSelf) return;
            float time = Time.unscaledTime;
            _popT = Mathf.Min(1f, _popT + Time.unscaledDeltaTime / 0.3f);
            _card.localScale = Vector3.one * UiEase.OutBack(_popT, 2f);
            _bobo.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(time * 3f) * 7f);

            if (_target == null) return;
            // Săgeata stă lângă țintă (deasupra sau dedesubt) și se mișcă spre ea.
            Vector3 center = _target.TransformPoint(_target.rect.center);
            Vector2 local = _rect.InverseTransformPoint(center);
            float reach = _target.rect.height * 0.5f + 80f + Mathf.Sin(time * 6f) * 14f;
            _arrow.anchorMin = _arrow.anchorMax = new Vector2(0.5f, 0.5f);
            _arrow.anchoredPosition = local + new Vector2(0f, _arrowUp ? -reach : reach) - _rect.rect.center;
            _arrow.localRotation = Quaternion.Euler(0f, 0f, _arrowUp ? 180f : 0f);
        }
    }
}
