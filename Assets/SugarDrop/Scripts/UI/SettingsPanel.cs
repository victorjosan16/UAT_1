using System;
using System.Collections.Generic;
using QuarryEmpire;
using UnityEngine;
using UnityEngine.UI;

namespace SugarDrop
{
    /// <summary>Ce poate face jocul din fereastra de Setări / Pauză (panoul doar afișează și schimbă opțiunile).</summary>
    public sealed class SettingsHost
    {
        public Action Resume;
        public Action Retry;
        public Action Map;
        public Action EditNickname;
        public Action ReplayTutorial;
        /// <summary>S-a schimbat sunetul sau muzica.</summary>
        public Action AudioChanged;
        /// <summary>S-a ales altă limbă: jocul își retraduce textele.</summary>
        public Action LanguageChanged;
        /// <summary>Null = fără opțiunea de reclame personalizate (reclamele reale nu sunt active).</summary>
        public Action<bool> SetAdsConsent;
    }

    /// <summary>
    /// Setări / Pauză în stilul Drop Quest: în pauză trei butoane late (Continuă verde, Reîncearcă albastru,
    /// Harta maro); apoi o listă de rânduri crem cu iconiță, nume și comutator (sunet, muzică, vibrații,
    /// notificări, efecte, reclame), grafica cu trei trepte (Scăzută / Medie / Înaltă), limba (lista apare în
    /// aceeași fereastră) și, în meniu, profilul și tutorialul. Versiunea jos.
    /// </summary>
    public class SettingsPanel : MonoBehaviour
    {
        private const float Width = 900f;
        private const float RowWidth = Width - 110f;
        private const float RowHeight = 96f;
        private const float RowStep = 108f;
        private const float ActionHeight = 116f;
        private const float ActionStep = 134f;

        private sealed class Row
        {
            public RectTransform Root;
            public Image Icon;
            public UiLabel Label;
            public Image Track;
            public RectTransform Knob;
            public Func<bool> IsOn;
            public string Key;
            public PunchScale Punch;
        }

        private GameObject _root;
        private RectTransform _box;
        private UiLabel _title;
        private RectTransform _main;
        private RectTransform _languages;
        private RectTransform _actions;
        private readonly UiLabel[] _actionLabels = new UiLabel[3];
        private UiLabel _lifeNote;
        private readonly List<Row> _rows = new();
        private Row _adsRow;
        private RectTransform _graphicsRow;
        private UiLabel _graphicsLabel;
        private readonly Image[] _graphicsFaces = new Image[3];
        private readonly UiLabel[] _graphicsLabels = new UiLabel[3];
        private RectTransform _languageRow;
        private UiLabel _languageCaption;
        private UiLabel _languageLabel;
        private RectTransform _profileRow;
        private UiLabel _nicknameLabel;
        private UiLabel _tutorialLabel;
        private UiLabel _version;
        private UiLabel _languagesTitle;
        private readonly List<(Button button, Image face, SugarLanguage language)> _languageButtons = new();
        private UiLabel _languagesBack;

        private SettingsHost _host;
        private bool _inGame;

        public bool IsOpen => _root != null && _root.activeSelf;

        private static SugarSaveData Data => SugarSave.Data;

        private static readonly Color Off = new(0.72f, 0.66f, 0.6f);

        private void Awake()
        {
            UiTheme theme = UiRoot.Theme;
            _root = SugarUi.Dim(transform).gameObject;
            _box = SugarUi.Box(_root.transform, new Vector2(Width, 1400f));

            _title = SugarUi.Outlined("Title", _box, "", 72, theme.AccentColor, 4f);
            UiFactory.Place(_title.RectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -36f), new Vector2(600f, 100f));
            Button close = SugarUi.IconButton("Close", _box, SugarSprites.Cross, theme.NegativeColor, () =>
            {
                SugarUi.Click();
                Dismiss();
            }, 0.45f);
            UiFactory.Place((RectTransform)close.transform, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-30f, -30f), new Vector2(100f, 100f));

            _main = UiFactory.CreateRect("Main", _box);
            UiFactory.Stretch(_main);
            BuildActions(theme);
            BuildRows();
            BuildGraphics(theme);
            BuildBottom(theme);

            _languages = UiFactory.CreateRect("Languages", _box);
            UiFactory.Stretch(_languages);
            BuildLanguages(theme);

            _root.SetActive(false);
        }

        // ------------------------------------------------------------ Construcție

        /// <summary>Pauză: trei butoane late, unul sub altul (continuă, reia nivelul, înapoi la hartă).</summary>
        private void BuildActions(UiTheme theme)
        {
            _actions = UiFactory.CreateRect("Actions", _main);
            (Sprite icon, Color color, Action action)[] actions =
            {
                (ProceduralSprites.Play, theme.PositiveColor, () => Run(_host?.Resume)),
                (ProceduralSprites.Restart, QuestStyle.Blue, () => Run(_host?.Retry)),
                (SugarSprites.Home, QuestStyle.Wood, () => Run(_host?.Map)),
            };
            for (int i = 0; i < 3; i++)
            {
                Action action = actions[i].action;
                Button button = SugarUi.Button("Action" + i, _actions, "", actions[i].color, () => action(), 46);
                UiFactory.Place((RectTransform)button.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -i * ActionStep), new Vector2(RowWidth, ActionHeight));
                _actionLabels[i] = UiFactory.GetButtonLabel(button);
                AddButtonIcon(button, actions[i].icon);
            }
            _lifeNote = UiFactory.CreateText("LifeNote", _actions, "", 28, TextAnchor.MiddleCenter, theme.TextDarkColor);
            UiFactory.Place(_lifeNote.RectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -3 * ActionStep + 6f), new Vector2(RowWidth, 40f));
        }

        private void BuildRows()
        {
            AddRow("SUNET", SugarSprites.Speaker, () => Data.sound, () =>
            {
                Data.sound = !Data.sound;
                _host?.AudioChanged?.Invoke();
            });
            AddRow("MUZICĂ", SugarSprites.Note, () => Data.music, () =>
            {
                Data.music = !Data.music;
                _host?.AudioChanged?.Invoke();
            });
            AddRow("VIBRAȚII", SugarSprites.Vibrate, () => Data.vibration, () =>
            {
                Data.vibration = !Data.vibration;
                if (Data.vibration) SugarAudio.Buzz(1);
            });
            AddRow("NOTIFICĂRI", SugarSprites.Bell, () => Data.notifications, () => Data.notifications = !Data.notifications);
            // Efecte de ecran = tremurat și zoom (opusul „mișcării reduse”).
            AddRow("EFECTE", SugarSprites.Sparkle, () => !Data.reducedMotion, () => Data.reducedMotion = !Data.reducedMotion);
            _adsRow = AddRow("RECLAME", ProceduralSprites.Coin, () => Data.adsConsent == 1, () => _host?.SetAdsConsent?.Invoke(Data.adsConsent != 1));
        }

        /// <summary>Rând crem: iconița și numele în stânga, comutatorul în dreapta; tot rândul se poate apăsa.</summary>
        private Row AddRow(string key, Sprite icon, Func<bool> isOn, Action toggle)
        {
            var row = new Row { IsOn = isOn, Key = key };
            row.Root = RowCard("Row", out Image face);
            row.Icon = RowIcon(face.transform, icon);
            row.Label = RowLabel(face.transform);
            row.Track = UiFactory.CreatePanel("Switch", face.transform, Color.white);
            var track = UiFactory.Place((RectTransform)row.Track.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-24f, 0f), new Vector2(116f, 56f));
            Image knob = UiFactory.CreateImage("Knob", track, SugarSprites.Circle, Color.white);
            row.Knob = UiFactory.Place(knob.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(46f, 46f));

            row.Punch = row.Root.gameObject.AddComponent<PunchScale>();
            face.raycastTarget = true;
            var button = row.Root.gameObject.AddComponent<Button>();
            button.targetGraphic = face;
            button.onClick.AddListener(() =>
            {
                SugarUi.Click();
                toggle();
                SugarSave.Save();
                row.Punch.Punch(0.6f);
                Refresh();
            });
            _rows.Add(row);
            return row;
        }

        /// <summary>Grafica: trei trepte alăturate, cea aleasă e verde.</summary>
        private void BuildGraphics(UiTheme theme)
        {
            _graphicsRow = RowCard("Graphics", out Image face);
            RowIcon(face.transform, SugarSprites.Gem).color = QuestStyle.Wood;
            _graphicsLabel = RowLabel(face.transform);
            for (int i = 0; i < 3; i++)
            {
                int level = i;
                Button step = SugarUi.Button("Step" + i, face.transform, "", QuestStyle.Wood, () =>
                {
                    SugarUi.Click();
                    Data.graphics = level;
                    SugarQuality.Apply();
                    SugarSave.Save();
                    Refresh();
                }, 26);
                UiFactory.Place((RectTransform)step.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-18f - (2 - i) * 134f, 2f), new Vector2(128f, 66f));
                _graphicsFaces[i] = step.transform.Find("Face").GetComponent<Image>();
                _graphicsLabels[i] = UiFactory.GetButtonLabel(step);
            }
        }

        /// <summary>Limba (glob, numele limbii și săgeata), apoi în meniu: profilul (nickname) și tutorialul; versiunea jos.</summary>
        private void BuildBottom(UiTheme theme)
        {
            _languageRow = RowCard("Language", out Image face);
            RowIcon(face.transform, SugarSprites.Globe).color = QuestStyle.Blue;
            _languageCaption = RowLabel(face.transform);
            _languageLabel = UiFactory.CreateText("Value", face.transform, "", 34, TextAnchor.MiddleRight, QuestStyle.TextSoft);
            UiFactory.Place(_languageLabel.RectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-74f, 0f), new Vector2(380f, 60f));
            UiLabel arrow = UiFactory.CreateText("Arrow", face.transform, ">", 44, TextAnchor.MiddleCenter, QuestStyle.TextSoft);
            UiFactory.Place(arrow.RectTransform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-24f, 2f), new Vector2(44f, 60f));
            face.raycastTarget = true;
            var button = _languageRow.gameObject.AddComponent<Button>();
            button.targetGraphic = face;
            button.onClick.AddListener(() =>
            {
                SugarUi.Click();
                ShowLanguages(true);
            });

            _profileRow = UiFactory.CreateRect("Profile", _main);
            Button nickname = SugarUi.Button("Nickname", _profileRow, "", theme.PanelColor, () => Run(_host?.EditNickname), 34);
            UiFactory.Place((RectTransform)nickname.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(385f, 110f));
            _nicknameLabel = UiFactory.GetButtonLabel(nickname);
            AddButtonIcon(nickname, SugarSprites.Person);
            Button tutorial = SugarUi.Button("Tutorial", _profileRow, "", theme.PanelColor, () => Run(_host?.ReplayTutorial), 34);
            UiFactory.Place((RectTransform)tutorial.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(385f, 110f));
            _tutorialLabel = UiFactory.GetButtonLabel(tutorial);
            AddButtonIcon(tutorial, SugarSprites.Checklist);

            _version = UiFactory.CreateText("Version", _main, "", 24, TextAnchor.MiddleCenter, QuestStyle.TextSoft);
        }

        private RectTransform RowCard(string name, out Image face) => SugarUi.Card(name, _main, out face);

        private static Image RowIcon(Transform face, Sprite icon)
        {
            Image image = UiFactory.CreateImage("Icon", face, icon, QuestStyle.Wood);
            UiFactory.Place(image.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(22f, 0f), new Vector2(62f, 62f));
            return image;
        }

        private static UiLabel RowLabel(Transform face)
        {
            UiLabel label = UiFactory.CreateText("Label", face, "", 36, TextAnchor.MiddleLeft, QuestStyle.Text);
            UiFactory.Place(label.RectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(104f, 0f), new Vector2(300f, 60f));
            return label;
        }

        /// <summary>Iconiță în stânga unui buton; textul se mută puțin la dreapta.</summary>
        private static void AddButtonIcon(Button button, Sprite icon)
        {
            Transform face = button.transform.Find("Face");
            Image image = UiFactory.CreateImage("Icon", face, icon, Color.white);
            UiFactory.Place(image.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(26f, 0f), new Vector2(64f, 64f));
            RectTransform label = UiFactory.GetButtonLabel(button).RectTransform;
            label.offsetMin = new Vector2(96f, label.offsetMin.y);
            label.offsetMax = new Vector2(-16f, label.offsetMax.y);
        }

        /// <summary>Lista limbilor pe două coloane, fiecare scrisă în limba ei.</summary>
        private void BuildLanguages(UiTheme theme)
        {
            _languagesTitle = UiFactory.CreateText("Hint", _languages, "", 34, TextAnchor.MiddleCenter, theme.TextDarkColor);
            UiFactory.Place(_languagesTitle.RectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -140f), new Vector2(Width - 80f, 50f));
            SugarLanguage[] languages = SugarLoc.Languages;
            for (int i = 0; i < languages.Length; i++)
            {
                SugarLanguage language = languages[i];
                int column = i % 2, row = i / 2;
                bool last = i == languages.Length - 1 && languages.Length % 2 == 1;
                Button button = SugarUi.Button("Lang" + i, _languages, SugarLoc.NativeName(language), theme.PanelColor, () => ChooseLanguage(language), 38);
                UiFactory.Place((RectTransform)button.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f),
                    new Vector2(last ? 0f : (column - 0.5f) * 410f, -200f - row * 122f), new Vector2(390f, 106f));
                _languageButtons.Add((button, button.transform.Find("Face").GetComponent<Image>(), language));
            }
            int rows = (languages.Length + 1) / 2;
            Button back = SugarUi.Button("Back", _languages, "", theme.AccentColor, () =>
            {
                SugarUi.Click();
                ShowLanguages(false);
            }, 40);
            UiFactory.Place((RectTransform)back.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -230f - rows * 122f), new Vector2(480f, 110f));
            _languagesBack = UiFactory.GetButtonLabel(back);
        }

        // ------------------------------------------------------------ Afișare

        /// <param name="inGame">Pauză în timpul nivelului (cu continuă / reia / harta).</param>
        public void Show(bool inGame, SettingsHost host)
        {
            _host = host;
            _inGame = inGame;
            _root.SetActive(true);
            ShowLanguages(false);
        }

        public void Hide() => _root.SetActive(false);

        /// <summary>„X”, butonul Înapoi al telefonului sau atingerea în afară: în pauză înseamnă „continuă”.</summary>
        public void Dismiss()
        {
            if (_languages.gameObject.activeSelf)
            {
                ShowLanguages(false);
                return;
            }
            Hide();
            if (_inGame) _host?.Resume?.Invoke();
        }

        private void ShowLanguages(bool show)
        {
            _main.gameObject.SetActive(!show);
            _languages.gameObject.SetActive(show);
            Refresh();
        }

        /// <summary>Așază lista de sus în jos și traduce totul (după o schimbare de limbă sau de opțiune).</summary>
        private void Refresh()
        {
            UiTheme theme = UiRoot.Theme;
            bool languages = _languages.gameObject.activeSelf;
            _title.Text = SugarLoc.T(languages ? "LIMBA" : _inGame ? "PAUZĂ" : "SETĂRI");
            if (languages)
            {
                _languagesTitle.Text = SugarLoc.NativeName(SugarLoc.Current);
                foreach ((Button _, Image face, SugarLanguage language) in _languageButtons)
                    face.color = language == SugarLoc.Current ? theme.PositiveColor : theme.PanelColor;
                _languagesBack.Text = SugarLoc.T("ÎNAPOI");
                int rows = (SugarLoc.Languages.Length + 1) / 2;
                SugarUi.ResizeBox(_box, new Vector2(Width, 350f + rows * 122f));
                return;
            }

            float y = -140f;
            _actions.gameObject.SetActive(_inGame);
            if (_inGame)
            {
                UiFactory.Place(_actions, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(RowWidth, 3 * ActionStep + 40f));
                _actionLabels[0].Text = SugarLoc.T("CONTINUĂ");
                _actionLabels[1].Text = SugarLoc.T("REÎNCEARCĂ");
                _actionLabels[2].Text = SugarLoc.T("HARTA");
                // Avertismentul despre viață apare doar dacă s-a făcut deja o mutare (altfel ieșirea e gratuită).
                _lifeNote.Text = Data.levelInProgress ? SugarLoc.T("Nivelul costă o viață.") : "";
                y -= 3 * ActionStep + (Data.levelInProgress ? 50f : 10f);
            }

            // Comutatoarele (reclamele doar când sunt reclame reale).
            _adsRow.Root.gameObject.SetActive(_host?.SetAdsConsent != null);
            foreach (Row row in _rows)
            {
                if (!row.Root.gameObject.activeSelf) continue;
                PlaceRow(row.Root, ref y);
                row.Label.Text = SugarLoc.T(row.Key);
                bool on = row.IsOn();
                row.Icon.color = on ? QuestStyle.Wood : Off;
                row.Track.color = on ? theme.PositiveColor : Off;
                row.Knob.anchoredPosition = new Vector2(on ? 30f : -30f, 0f);
            }

            PlaceRow(_graphicsRow, ref y);
            _graphicsLabel.Text = SugarLoc.T("GRAFICĂ");
            string[] steps = { "Scăzută", "Medie", "Înaltă" };
            int current = (int)SugarQuality.Current;
            for (int i = 0; i < 3; i++)
            {
                _graphicsLabels[i].Text = SugarLoc.T(steps[i]);
                _graphicsFaces[i].color = i == current ? theme.PositiveColor : Color.Lerp(QuestStyle.Wood, Color.white, 0.25f);
            }

            PlaceRow(_languageRow, ref y);
            _languageCaption.Text = SugarLoc.T("LIMBA");
            _languageLabel.Text = SugarLoc.NativeName(SugarLoc.Current);

            _profileRow.gameObject.SetActive(!_inGame);
            if (!_inGame)
            {
                y -= 12f;
                _nicknameLabel.Text = string.IsNullOrEmpty(Data.nickname) ? "?" : Data.nickname;
                _tutorialLabel.Text = SugarLoc.T("TUTORIAL");
                UiFactory.Place(_profileRow, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(RowWidth, 110f));
                y -= 130f;
            }

            _version.Text = SugarLoc.F("Versiunea {0}", ErrorReporter.BuildId);
            UiFactory.Place(_version.RectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y - 6f), new Vector2(Width - 80f, 36f));
            SugarUi.ResizeBox(_box, new Vector2(Width, -y + 64f));
        }

        private static void PlaceRow(RectTransform row, ref float y)
        {
            UiFactory.Place(row, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(RowWidth, RowHeight));
            y -= RowStep;
        }

        private void ChooseLanguage(SugarLanguage language)
        {
            SugarUi.Click();
            Data.language = SugarLoc.Code(language);
            SugarSave.Save();
            _host?.LanguageChanged?.Invoke();
            ShowLanguages(false);
        }

        /// <param name="close">Închide fereastra înainte de acțiune (pentru continuă / reia / harta / tutorial).</param>
        private void Run(Action action, bool close = true)
        {
            SugarUi.Click();
            if (close) Hide();
            action?.Invoke();
        }
    }
}
