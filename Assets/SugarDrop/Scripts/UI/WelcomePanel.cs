using System;
using QuarryEmpire;
using UnityEngine;
using UnityEngine.UI;

namespace SugarDrop
{
    /// <summary>
    /// Bun venit la prima pornire: Bobo te salută și te întreabă cum să-ți spună. Nickname-ul apare în clasament și
    /// se poate schimba din Setări (aceeași fereastră, cu „Anulează”).
    /// </summary>
    public class WelcomePanel : MonoBehaviour
    {
        private const float Width = 860f;
        private const float Height = 1180f;

        private GameObject _root;
        private UiLabel _title;
        private UiLabel _message;
        private UiLabel _hint;
        private InputField _input;
        private Text _placeholder;
        private UiLabel _confirmLabel;
        private Button _cancel;
        private UiLabel _cancelLabel;
        private RectTransform _bobo;
        private Action<string> _onDone;
        private bool _firstTime;
        private string _suggested;

        public bool IsOpen => _root != null && _root.activeSelf;

        private void Awake()
        {
            UiTheme theme = UiRoot.Theme;
            _root = SugarUi.Dim(transform).gameObject;
            RectTransform box = SugarUi.Box(_root.transform, new Vector2(Width, Height));

            _bobo = UiFactory.CreateRect("Bobo", box);
            UiFactory.Place(_bobo, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(300f, 300f));
            BuildBobo(_bobo);

            _title = SugarUi.Outlined("Title", box, "", 80, theme.AccentColor, 4f);
            UiFactory.Place(_title.RectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -170f), new Vector2(Width - 60f, 110f));
            _message = UiFactory.CreateText("Message", box, "", 42, TextAnchor.MiddleCenter, Color.Lerp(theme.TextDarkColor, Color.white, 0.25f));
            UiFactory.Place(_message.RectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -290f), new Vector2(Width - 100f, 200f));

            BuildInput(box, theme);

            _hint = UiFactory.CreateText("Hint", box, "", 32, TextAnchor.MiddleCenter, Color.Lerp(theme.TextDarkColor, Color.white, 0.4f));
            UiFactory.Place(_hint.RectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -680f), new Vector2(Width - 100f, 70f));

            Button confirm = SugarUi.Button("Confirm", box, "", theme.PositiveColor, Confirm, 54);
            UiFactory.Place((RectTransform)confirm.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 200f), new Vector2(620f, 140f));
            _confirmLabel = UiFactory.GetButtonLabel(confirm);
            _cancel = SugarUi.Button("Cancel", box, "", theme.PanelColor, () =>
            {
                SugarUi.Click();
                Hide();
            }, 42);
            UiFactory.Place((RectTransform)_cancel.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 60f), new Vector2(520f, 110f));
            _cancelLabel = UiFactory.GetButtonLabel(_cancel);

            _root.SetActive(false);
        }

        private void BuildInput(RectTransform box, UiTheme theme)
        {
            Image frame = UiFactory.CreatePanel("NameFrame", box, Palette.WallStripe);
            UiFactory.Place((RectTransform)frame.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -500f), new Vector2(680f, 150f));
            Image field = UiFactory.CreatePanel("NameField", frame.transform, Color.white);
            UiFactory.Stretch((RectTransform)field.transform, 8f);
            field.raycastTarget = true;

            Font font = theme.Font != null ? theme.Font : UiFactory.BuiltinFont;
            Text text = FieldText("Text", field.transform, font, theme.TextDarkColor);
            _placeholder = FieldText("Placeholder", field.transform, font, new Color(theme.TextDarkColor.r, theme.TextDarkColor.g, theme.TextDarkColor.b, 0.4f));

            _input = field.gameObject.AddComponent<InputField>();
            _input.targetGraphic = field;
            _input.textComponent = text;
            _input.placeholder = _placeholder;
            _input.characterLimit = Nickname.MaxLength;
            _input.lineType = InputField.LineType.SingleLine;
            _input.contentType = InputField.ContentType.Standard;
            _input.selectionColor = new Color(1f, 0.6f, 0.75f, 0.6f);
            _input.caretColor = theme.TextDarkColor;
            _input.caretWidth = 4;
            _input.onValidateInput = (_, _, c) => char.IsLetterOrDigit(c) || c == ' ' || c == '_' || c == '-' || c == '.' ? c : '\0';
            _input.onValueChanged.AddListener(_ => RefreshHint());
        }

        private static Text FieldText(string name, Transform parent, Font font, Color color)
        {
            RectTransform rect = UiFactory.CreateRect(name, parent);
            UiFactory.Stretch(rect, 24f);
            var text = rect.gameObject.AddComponent<Text>();
            text.font = font;
            text.fontSize = 58;
            text.alignment = TextAnchor.MiddleCenter;
            text.color = color;
            text.supportRichText = false;
            text.horizontalOverflow = HorizontalWrapMode.Overflow;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        /// <summary>Bobo mic (cupcake cu cireașă), ca în meniul principal.</summary>
        private static void BuildBobo(RectTransform parent)
        {
            Image cup = UiFactory.CreateImage("Cup", parent, SugarSprites.CupWrapper, Color.white);
            UiFactory.Place(cup.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -55f), new Vector2(170f, 170f));
            Image frosting = UiFactory.CreateImage("Frosting", parent, SugarSprites.Frosting, Color.white);
            UiFactory.Place(frosting.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 10f), new Vector2(180f, 180f));
            Image cherry = UiFactory.CreateImage("Cherry", parent, SugarSprites.Circle, new Color(0.93f, 0.2f, 0.32f));
            UiFactory.Place(cherry.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(5f, 88f), new Vector2(40f, 40f));
            Image smile = UiFactory.CreateImage("Smile", parent, SugarSprites.Smile, new Color(0.35f, 0.12f, 0.22f));
            UiFactory.Place(smile.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -75f), new Vector2(56f, 56f));
            for (int e = -1; e <= 1; e += 2)
            {
                Image eye = UiFactory.CreateImage("Eye", parent, SugarSprites.Circle, new Color(0.25f, 0.12f, 0.2f));
                UiFactory.Place(eye.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(e * 30f, -45f), new Vector2(20f, 20f));
            }
        }

        /// <param name="firstTime">Prima pornire: salut + nume propus, fără „Anulează”. Altfel: schimbarea numelui din Setări.</param>
        /// <param name="onDone">Primește nickname-ul ales (deja curățat și valid).</param>
        public void Show(bool firstTime, Action<string> onDone)
        {
            _firstTime = firstTime;
            _onDone = onDone;
            string current = SugarSave.Data.nickname;
            _suggested = string.IsNullOrEmpty(current) ? Nickname.Suggest(new System.Random()) : current;

            _title.Text = SugarLoc.T(firstTime ? "Bine ai venit!" : "Nickname");
            _message.Text = SugarLoc.T(firstTime
                ? "Salut! Eu sunt Bobo, cupcake-ul care te însoțește prin Sugar Drop. Cum să-ți spun? Numele tău va apărea în clasament."
                : "Alege numele cu care apari în clasament.");
            _placeholder.text = _suggested;
            _input.text = firstTime ? "" : current;
            _confirmLabel.Text = SugarLoc.T(firstTime ? "HAI SĂ JUCĂM!" : "SALVEAZĂ");
            _cancelLabel.Text = SugarLoc.T("ANULEAZĂ");
            _cancel.gameObject.SetActive(!firstTime);
            RefreshHint();
            _root.SetActive(true);
        }

        public void Hide()
        {
            _onDone = null;
            if (_root.activeSelf) _root.SetActive(false);
        }

        /// <summary>Butonul „Înapoi”: la prima pornire păstrează numele propus, altfel anulează.</summary>
        public void Dismiss()
        {
            if (_firstTime) Confirm();
            else Hide();
        }

        /// <summary>Numele care s-ar salva acum: ce a scris jucătorul (curățat) sau, cu câmpul gol, numele propus.</summary>
        private string Chosen
        {
            get
            {
                string typed = Nickname.Clean(_input.text);
                return typed.Length == 0 ? _suggested : typed;
            }
        }

        private void RefreshHint()
        {
            bool tooShort = Chosen.Length < Nickname.MinLength;
            _hint.Text = tooShort
                ? SugarLoc.F("Minimum {0} caractere", Nickname.MinLength)
                : SugarLoc.F("Litere, cifre și spații, maximum {0}", Nickname.MaxLength);
            _hint.Color = tooShort ? UiRoot.Theme.NegativeColor : Color.Lerp(UiRoot.Theme.TextDarkColor, Color.white, 0.4f);
        }

        private void Confirm()
        {
            if (!IsOpen) return;
            string name = Chosen;
            if (name.Length < Nickname.MinLength)
            {
                SugarAudio.Buzz(1);
                RefreshHint();
                return;
            }
            SugarUi.Click();
            Action<string> done = _onDone;
            Hide();
            done?.Invoke(name);
        }

        private void Update()
        {
            if (!_root.activeSelf) return;
            // Bobo se leagănă ușor cât timp vorbește.
            float time = Time.unscaledTime;
            _bobo.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(time * 2.2f) * 6f);
            _bobo.localScale = new Vector3(1f + Mathf.Sin(time * 4.4f) * 0.03f, 1f - Mathf.Sin(time * 4.4f) * 0.03f, 1f);
        }
    }
}
