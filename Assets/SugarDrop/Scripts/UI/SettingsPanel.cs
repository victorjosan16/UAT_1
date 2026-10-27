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
    /// Setări cu puțin text: în pauză, trei butoane mari rotunde (continuă, reia, harta); apoi o grilă de plăcuțe
    /// cu iconiță, un cuvânt și un comutator (sunet, muzică, vibrații, notificări, efecte de ecran, grafică,
    /// reclame), un buton pentru limbă (lista apare în aceeași fereastră) și, în meniu, profilul și tutorialul.
    /// </summary>
    public class SettingsPanel : MonoBehaviour
    {
        private const float Width = 920f;
        private const float TileWidth = 270f;
        private const float TileHeight = 230f;
        private const float TileGap = 22f;

        private sealed class Tile
        {
            public RectTransform Root;
            public Image Back;
            public Image Icon;
            public Image Slash;
            public UiLabel Label;
            public Image Track;
            public RectTransform Knob;
            public Image[] Bars;
            public Func<bool> IsOn;
            public Func<int> Level;
            public PunchScale Punch;
        }

        private GameObject _root;
        private RectTransform _box;
        private UiLabel _title;
        private RectTransform _main;
        private RectTransform _languages;
        private RectTransform _actions;
        private readonly UiLabel[] _actionCaptions = new UiLabel[3];
        private UiLabel _lifeNote;
        private RectTransform _grid;
        private readonly List<Tile> _tiles = new();
        private Tile _adsTile;
        private Button _languageButton;
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
        private float _mainHeight;

        public bool IsOpen => _root != null && _root.activeSelf;

        private static SugarSaveData Data => SugarSave.Data;

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
            BuildTiles(theme);
            BuildBottom(theme);

            _languages = UiFactory.CreateRect("Languages", _box);
            UiFactory.Stretch(_languages);
            BuildLanguages(theme);

            _root.SetActive(false);
        }

        // ------------------------------------------------------------ Construcție

        /// <summary>Pauză: continuă (mare, verde), reia nivelul, înapoi la hartă.</summary>
        private void BuildActions(UiTheme theme)
        {
            _actions = UiFactory.CreateRect("Actions", _main);
            UiFactory.Place(_actions, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -150f), new Vector2(Width, 300f));
            (Sprite icon, Color color, Action action)[] actions =
            {
                (ProceduralSprites.Restart, theme.AccentColor, () => Run(_host?.Retry)),
                (ProceduralSprites.Play, theme.PositiveColor, () => Run(_host?.Resume)),
                (SugarSprites.Home, theme.AccentColor, () => Run(_host?.Map)),
            };
            for (int i = 0; i < 3; i++)
            {
                bool middle = i == 1;
                float size = middle ? 190f : 150f;
                Button button = SugarUi.IconButton("Action" + i, _actions, actions[i].icon, actions[i].color, null, 0.5f);
                Action action = actions[i].action;
                button.onClick.AddListener(() => action());
                UiFactory.Place((RectTransform)button.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2((i - 1) * 270f, middle ? 0f : -20f), new Vector2(size, size));
                _actionCaptions[i] = UiFactory.CreateText("Caption", _actions, "", 30, TextAnchor.MiddleCenter, theme.TextDarkColor);
                UiFactory.Place(_actionCaptions[i].RectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2((i - 1) * 270f, -200f), new Vector2(260f, 44f));
            }
            _lifeNote = UiFactory.CreateText("LifeNote", _actions, "", 28, TextAnchor.MiddleCenter, Color.Lerp(theme.TextDarkColor, Color.white, 0.35f));
            UiFactory.Place(_lifeNote.RectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -250f), new Vector2(Width - 80f, 40f));
        }

        private void BuildTiles(UiTheme theme)
        {
            _grid = UiFactory.CreateRect("Grid", _main);
            AddTile(theme, SugarSprites.Speaker, () => Data.sound, () =>
            {
                Data.sound = !Data.sound;
                _host?.AudioChanged?.Invoke();
            });
            AddTile(theme, SugarSprites.Note, () => Data.music, () =>
            {
                Data.music = !Data.music;
                _host?.AudioChanged?.Invoke();
            });
            AddTile(theme, SugarSprites.Vibrate, () => Data.vibration, () =>
            {
                Data.vibration = !Data.vibration;
                if (Data.vibration) SugarAudio.Buzz(1);
            });
            AddTile(theme, SugarSprites.Bell, () => Data.notifications, () => Data.notifications = !Data.notifications);
            // Efecte de ecran = tremurat și zoom (opusul „mișcării reduse”).
            AddTile(theme, SugarSprites.Sparkle, () => !Data.reducedMotion, () => Data.reducedMotion = !Data.reducedMotion);
            Tile graphics = AddTile(theme, SugarSprites.Gem, null, SugarQuality.Cycle);
            graphics.Level = () => (int)SugarQuality.Current;
            _adsTile = AddTile(theme, ProceduralSprites.Coin, () => Data.adsConsent == 1, () => _host?.SetAdsConsent?.Invoke(Data.adsConsent != 1));
        }

        private Tile AddTile(UiTheme theme, Sprite icon, Func<bool> isOn, Action toggle)
        {
            var tile = new Tile { IsOn = isOn };
            tile.Back = UiFactory.CreatePanel("Tile", _grid, Color.white);
            tile.Back.raycastTarget = true;
            tile.Root = UiFactory.Place((RectTransform)tile.Back.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), Vector2.zero, new Vector2(TileWidth, TileHeight));
            tile.Icon = UiFactory.CreateImage("Icon", tile.Root, icon, Color.white);
            UiFactory.Place(tile.Icon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -18f), new Vector2(92f, 92f));
            tile.Slash = UiFactory.CreatePanel("Slash", tile.Icon.transform, theme.NegativeColor);
            UiFactory.Place((RectTransform)tile.Slash.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(118f, 14f));
            tile.Slash.transform.localRotation = Quaternion.Euler(0f, 0f, -45f);
            tile.Label = UiFactory.CreateText("Label", tile.Root, "", 30, TextAnchor.MiddleCenter, theme.TextDarkColor);
            UiFactory.Place(tile.Label.RectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -116f), new Vector2(TileWidth - 16f, 44f));

            if (isOn != null)
            {
                // Comutator: pista colorată și bulina care stă la dreapta când e pornit.
                tile.Track = UiFactory.CreatePanel("Switch", tile.Root, Color.white);
                var track = UiFactory.Place((RectTransform)tile.Track.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(110f, 52f));
                Image knob = UiFactory.CreateImage("Knob", track, SugarSprites.Circle, Color.white);
                tile.Knob = UiFactory.Place(knob.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(44f, 44f));
            }
            else
            {
                // Grafica: trei bare (scăzută, medie, înaltă).
                tile.Bars = new Image[3];
                for (int i = 0; i < 3; i++)
                {
                    tile.Bars[i] = UiFactory.CreatePanel("Bar" + i, tile.Root, Color.white);
                    UiFactory.Place(tile.Bars[i].rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2((i - 1) * 34f, 20f), new Vector2(26f, 22f + i * 14f));
                }
            }

            tile.Punch = tile.Root.gameObject.AddComponent<PunchScale>();
            var button = tile.Root.gameObject.AddComponent<Button>();
            button.targetGraphic = tile.Back;
            button.onClick.AddListener(() =>
            {
                SugarUi.Click();
                toggle();
                SugarSave.Save();
                tile.Punch.Punch(1f);
                Refresh();
            });
            _tiles.Add(tile);
            return tile;
        }

        /// <summary>Limba (glob + numele limbii), apoi în meniu: profilul (nickname) și tutorialul; versiunea jos.</summary>
        private void BuildBottom(UiTheme theme)
        {
            _languageButton = SugarUi.Button("Language", _main, "", theme.PanelColor, () =>
            {
                SugarUi.Click();
                ShowLanguages(true);
            }, 40);
            _languageLabel = UiFactory.GetButtonLabel(_languageButton);
            AddButtonIcon(_languageButton, SugarSprites.Globe);

            _profileRow = UiFactory.CreateRect("Profile", _main);
            Button nickname = SugarUi.Button("Nickname", _profileRow, "", theme.PanelColor, () => Run(_host?.EditNickname), 34);
            UiFactory.Place((RectTransform)nickname.transform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(400f, 110f));
            _nicknameLabel = UiFactory.GetButtonLabel(nickname);
            AddButtonIcon(nickname, SugarSprites.Person);
            Button tutorial = SugarUi.Button("Tutorial", _profileRow, "", theme.PanelColor, () => Run(_host?.ReplayTutorial), 34);
            UiFactory.Place((RectTransform)tutorial.transform, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), Vector2.zero, new Vector2(400f, 110f));
            _tutorialLabel = UiFactory.GetButtonLabel(tutorial);
            AddButtonIcon(tutorial, SugarSprites.Checklist);

            _version = UiFactory.CreateText("Version", _main, "", 24, TextAnchor.MiddleCenter, Color.Lerp(theme.TextDarkColor, Color.white, 0.5f));
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
                    new Vector2(last ? 0f : (column - 0.5f) * 420f, -200f - row * 122f), new Vector2(400f, 106f));
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

        /// <summary>Așază și traduce totul (după o schimbare de limbă sau de opțiune).</summary>
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

            float y = -150f;
            _actions.gameObject.SetActive(_inGame);
            if (_inGame)
            {
                _actionCaptions[0].Text = SugarLoc.T("REÎNCEARCĂ");
                _actionCaptions[1].Text = SugarLoc.T("CONTINUĂ");
                _actionCaptions[2].Text = SugarLoc.T("HARTA");
                // Avertismentul despre viață apare doar dacă s-a făcut deja o mutare (altfel ieșirea e gratuită).
                _lifeNote.Text = Data.levelInProgress ? SugarLoc.T("Nivelul costă o viață.") : "";
                y -= 300f;
            }

            // Grila de plăcuțe (reclamele doar când sunt reclame reale).
            _adsTile.Root.gameObject.SetActive(_host?.SetAdsConsent != null);
            var visible = new List<Tile>();
            foreach (Tile tile in _tiles)
                if (tile.Root.gameObject.activeSelf) visible.Add(tile);
            const int columns = 3;
            int gridRows = (visible.Count + columns - 1) / columns;
            UiFactory.Place(_grid, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(Width, gridRows * (TileHeight + TileGap)));
            string[] labels = { "SUNET", "MUZICĂ", "VIBRAȚII", "NOTIFICĂRI", "EFECTE", "GRAFICĂ", "RECLAME" };
            for (int i = 0; i < visible.Count; i++)
            {
                Tile tile = visible[i];
                int row = i / columns, column = i % columns;
                int inRow = Mathf.Min(columns, visible.Count - row * columns);
                tile.Root.anchoredPosition = new Vector2((column - (inRow - 1) * 0.5f) * (TileWidth + TileGap), -row * (TileHeight + TileGap));
                tile.Label.Text = SugarLoc.T(labels[_tiles.IndexOf(tile)]);
                RefreshTile(tile, theme);
            }
            y -= gridRows * (TileHeight + TileGap) + 10f;

            _languageLabel.Text = SugarLoc.NativeName(SugarLoc.Current);
            UiFactory.Place((RectTransform)_languageButton.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(Width - 100f, 110f));
            y -= 135f;

            _profileRow.gameObject.SetActive(!_inGame);
            if (!_inGame)
            {
                _nicknameLabel.Text = string.IsNullOrEmpty(Data.nickname) ? "?" : Data.nickname;
                _tutorialLabel.Text = SugarLoc.T("TUTORIAL");
                UiFactory.Place(_profileRow, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(Width - 100f, 110f));
                y -= 135f;
            }

            _version.Text = SugarLoc.F("Versiunea {0}", ErrorReporter.BuildId);
            UiFactory.Place(_version.RectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y - 4f), new Vector2(Width - 80f, 36f));
            _mainHeight = -y + 60f;
            SugarUi.ResizeBox(_box, new Vector2(Width, _mainHeight));
        }

        private static void RefreshTile(Tile tile, UiTheme theme)
        {
            if (tile.IsOn != null)
            {
                bool on = tile.IsOn();
                tile.Back.color = on ? Color.white : new Color(0.9f, 0.86f, 0.9f);
                tile.Icon.color = on ? theme.AccentColor : new Color(0.6f, 0.55f, 0.62f);
                tile.Slash.gameObject.SetActive(!on);
                tile.Track.color = on ? theme.PositiveColor : new Color(0.7f, 0.66f, 0.72f);
                tile.Knob.anchoredPosition = new Vector2(on ? 29f : -29f, 0f);
                return;
            }
            int level = tile.Level();
            tile.Back.color = Color.white;
            tile.Icon.color = theme.AccentColor;
            tile.Slash.gameObject.SetActive(false);
            for (int i = 0; i < tile.Bars.Length; i++)
                tile.Bars[i].color = i <= level ? theme.PositiveColor : new Color(0.8f, 0.76f, 0.82f);
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
