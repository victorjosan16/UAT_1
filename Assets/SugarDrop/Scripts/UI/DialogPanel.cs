using System;
using System.Collections.Generic;
using QuarryEmpire;
using UnityEngine;
using UnityEngine.UI;

namespace SugarDrop
{
    /// <summary>Un buton dintr-un dialog: text, culoare, acțiune și dacă închide dialogul.</summary>
    public readonly struct DialogButton
    {
        public readonly string Label;
        public readonly Color Color;
        public readonly Action Action;
        public readonly bool Closes;

        public DialogButton(string label, Color color, Action action, bool closes = true)
        {
            Label = label;
            Color = color;
            Action = action;
            Closes = closes;
        }
    }

    /// <summary>
    /// Fereastră generică (pauză, setări, fără vieți, cumpărare ajutoare): titlu, mesaj (poate fi actualizat
    /// în timp real, ex. cronometrul vieților) și o listă de butoane.
    /// </summary>
    public class DialogPanel : MonoBehaviour
    {
        private const float Width = 820f;
        private const float ButtonHeight = 120f;
        private const float ButtonGap = 22f;

        private GameObject _root;
        private RectTransform _box;
        private UiLabel _title;
        private UiLabel _message;
        private readonly List<GameObject> _buttons = new();
        private Func<string> _liveMessage;
        private float _refresh;

        public bool IsOpen => _root != null && _root.activeSelf;

        /// <summary>Ce se întâmplă la butonul „Înapoi” al telefonului (setat după Show; null = doar închide).</summary>
        public Action DismissAction { get; set; }

        private void Awake()
        {
            UiTheme theme = UiRoot.Theme;
            _root = SugarUi.Dim(transform).gameObject;
            _box = SugarUi.Box(_root.transform, new Vector2(Width, 600f));
            _title = UiFactory.CreateText("Title", _box, "", 70, TextAnchor.MiddleCenter, theme.TextDarkColor);
            UiFactory.Place(_title.RectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(Width - 60f, 100f));
            _message = UiFactory.CreateText("Message", _box, "", 40, TextAnchor.MiddleCenter, Color.Lerp(theme.TextDarkColor, Color.white, 0.3f));
            UiFactory.Place(_message.RectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(Width - 80f, 110f));
            _root.SetActive(false);
        }

        public void Show(string title, string message, params DialogButton[] buttons) => Show(title, message, null, buttons);

        public void Show(string title, string message, Func<string> liveMessage, params DialogButton[] buttons)
        {
            DismissAction = null;
            foreach (GameObject button in _buttons) Destroy(button);
            _buttons.Clear();

            _title.Text = title;
            _liveMessage = liveMessage;
            _message.Text = liveMessage != null ? liveMessage() : message ?? "";
            bool hasMessage = !string.IsNullOrEmpty(_message.Text);
            _message.gameObject.SetActive(hasMessage);

            float top = hasMessage ? 280f : 170f;
            float height = top + buttons.Length * (ButtonHeight + ButtonGap) + 30f;
            SugarUi.ResizeBox(_box, new Vector2(Width, height));

            for (int i = 0; i < buttons.Length; i++)
            {
                DialogButton data = buttons[i];
                Button button = SugarUi.Button("Button" + i, _box, data.Label, data.Color, () =>
                {
                    SugarUi.Click();
                    if (data.Closes) Hide();
                    data.Action?.Invoke();
                }, 42);
                UiFactory.Place((RectTransform)button.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(0f, -top - i * (ButtonHeight + ButtonGap)), new Vector2(Width - 160f, ButtonHeight));
                _buttons.Add(button.gameObject);
            }

            // Repornim animația de apariție doar la prima deschidere (nu la reîmprospătarea unui comutator).
            if (!_root.activeSelf) _root.SetActive(true);
        }

        /// <summary>Închide dialogul ca și cum jucătorul ar fi renunțat (butonul „Înapoi”).</summary>
        public void Dismiss()
        {
            Action action = DismissAction;
            Hide();
            action?.Invoke();
        }

        public void Hide()
        {
            _liveMessage = null;
            _root.SetActive(false);
        }

        private void Update()
        {
            if (_liveMessage == null || !_root.activeSelf) return;
            _refresh -= Time.unscaledDeltaTime;
            if (_refresh > 0f) return;
            _refresh = 0.25f;
            _message.Text = _liveMessage();
        }
    }
}
