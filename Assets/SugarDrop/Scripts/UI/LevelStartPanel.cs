using System;
using System.Collections.Generic;
using QuarryEmpire;
using UnityEngine;
using UnityEngine.UI;

namespace SugarDrop
{
    /// <summary>Ce arată cartea de început a unui nivel (pregătit de joc).</summary>
    public sealed class LevelStartInfo
    {
        public string Title;
        public Difficulty Difficulty;
        public bool ShowDifficulty;
        public int BestStars;
        /// <summary>O singură propoziție scurtă: de ce trebuie salvat Bobo pe nivelul ăsta.</summary>
        public string Scenario;
        public readonly List<(Sprite icon, Color color, int count)> Goals = new();
        public bool IsNew;
        public bool Boosters;
        /// <summary>Gol sau, de ex., „Serie de 3 victorii…”.</summary>
        public string Streak;
    }

    /// <summary>
    /// Cartea de început de nivel, cu cât mai puțin text: panglica cu numărul nivelului, eticheta de nivel greu
    /// (flacără), stelele cele mai bune, o propoziție despre pericol, obiectivele ca iconițe cu număr, ajutoarele de
    /// la start ca plăcuțe pătrate (atingi = bifat; jos stocul sau prețul) și un singur buton mare JOACĂ. ✕ închide.
    /// </summary>
    public class LevelStartPanel : MonoBehaviour
    {
        private const float Width = 900f;

        private sealed class BoosterTile
        {
            public StartBooster Booster;
            public Image Back;
            public Image Check;
            public UiLabel Caption;
            public Image Coin;
            public PunchScale Punch;
        }

        private GameObject _root;
        private RectTransform _box;
        private UiLabel _title;
        private RectTransform _chip;
        private Image _chipFlame;
        private UiLabel _chipLabel;
        private readonly Image[] _stars = new Image[3];
        private UiLabel _scenario;
        private RectTransform _goalRow;
        private readonly List<GameObject> _goalViews = new();
        private Image _newBadge;
        private UiLabel _newLabel;
        private RectTransform _boosterRow;
        private readonly List<BoosterTile> _tiles = new();
        private UiLabel _streak;
        private Button _play;
        private UiLabel _playLabel;

        private Func<StartBooster, (bool selected, int count, int price)> _boosterState;
        private Action<StartBooster> _toggle;
        private Action _onPlay;

        public bool IsOpen => _root != null && _root.activeSelf;

        private void Awake()
        {
            UiTheme theme = UiRoot.Theme;
            _root = SugarUi.Dim(transform).gameObject;
            _box = SugarUi.Box(_root.transform, new Vector2(Width, 1000f));

            // Panglica de sus cu numărul nivelului.
            Image ribbon = UiFactory.CreatePanel("TitleRibbon", _box, Palette.WallStripe);
            UiFactory.Place((RectTransform)ribbon.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(Width - 80f, 130f));
            _title = SugarUi.Outlined("Title", ribbon.transform, "", 66, Color.white, 4f);
            UiFactory.Stretch(_title.RectTransform);
            Button close = SugarUi.IconButton("Close", _box, SugarSprites.Cross, theme.NegativeColor, () =>
            {
                SugarUi.Click();
                Hide();
            }, 0.45f);
            UiFactory.Place((RectTransform)close.transform, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-10f, 10f), new Vector2(96f, 96f));

            // Eticheta de nivel greu: flacără + „GREU ×1,5”.
            Image chip = UiFactory.CreatePanel("Chip", _box, new Color(0.95f, 0.35f, 0.2f));
            _chip = UiFactory.Place((RectTransform)chip.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -110f), new Vector2(360f, 64f));
            _chipFlame = UiFactory.CreateImage("Flame", _chip, SugarSprites.Flame, Color.white);
            UiFactory.Place(_chipFlame.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(34f, 8f), new Vector2(70f, 70f));
            _chipFlame.gameObject.AddComponent<UiFlicker>();
            _chipLabel = SugarUi.Outlined("Label", _chip, "", 34, Color.white, 2f);
            UiFactory.Place(_chipLabel.RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(26f, 0f), new Vector2(290f, 60f));

            for (int i = 0; i < 3; i++)
            {
                _stars[i] = UiFactory.CreateImage("Star", _box, ProceduralSprites.Star, Color.white);
                UiFactory.Place(_stars[i].rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2((i - 1) * 84f, i == 1 ? -190f : -180f), Vector2.one * (i == 1 ? 80f : 66f));
            }

            _scenario = UiFactory.CreateText("Scenario", _box, "", 36, TextAnchor.MiddleCenter, theme.TextDarkColor);
            UiFactory.Place(_scenario.RectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -250f), new Vector2(Width - 100f, 150f));
            _scenario.Fit(22f, 36f); // textul încape în casetă (traduceri lungi, două rânduri), fără să acopere stelele sau obiectivele

            // Obiectivele: o „tavă” cu iconițe mari și numărul dedesubt.
            Image tray = UiFactory.CreatePanel("Goals", _box, new Color(1f, 0.93f, 0.96f));
            _goalRow = UiFactory.Place((RectTransform)tray.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -360f), new Vector2(Width - 120f, 200f));
            _newBadge = UiFactory.CreatePanel("New", _goalRow, theme.NegativeColor);
            UiFactory.Place(_newBadge.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 0.5f), new Vector2(-10f, 0f), new Vector2(130f, 52f));
            _newBadge.transform.localRotation = Quaternion.Euler(0f, 0f, 8f);
            _newLabel = SugarUi.Outlined("Label", _newBadge.transform, "", 30, Color.white, 2f);
            UiFactory.Stretch(_newLabel.RectTransform);

            _boosterRow = UiFactory.CreateRect("Boosters", _box);
            UiFactory.Place(_boosterRow, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -590f), new Vector2(Width, 190f));
            AddBoosterTile(StartBooster.Rocket, SugarSprites.Rocket, -110f, theme);
            AddBoosterTile(StartBooster.Bomb, SugarSprites.Bomb, 110f, theme);

            _streak = UiFactory.CreateText("Streak", _box, "", 30, TextAnchor.MiddleCenter, theme.PositiveColor);

            _play = SugarUi.Button("Play", _box, "", theme.PositiveColor, () =>
            {
                SugarUi.Click();
                Action play = _onPlay;
                Hide();
                play?.Invoke();
            }, 60);
            _playLabel = UiFactory.GetButtonLabel(_play);
            _root.SetActive(false);
        }

        private void AddBoosterTile(StartBooster booster, Sprite icon, float x, UiTheme theme)
        {
            var tile = new BoosterTile { Booster = booster };
            tile.Back = UiFactory.CreatePanel("Booster", _boosterRow, Color.white);
            tile.Back.raycastTarget = true;
            var rect = UiFactory.Place(tile.Back.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(x, 0f), new Vector2(180f, 180f));
            Image image = UiFactory.CreateImage("Icon", rect, icon, Color.white);
            UiFactory.Place(image.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -14f), new Vector2(110f, 110f));
            Image chip = UiFactory.CreatePanel("Chip", rect, QuestStyle.Text);
            UiFactory.Place(chip.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 14f), new Vector2(150f, 52f));
            tile.Coin = UiFactory.CreateImage("Coin", chip.transform, ProceduralSprites.Coin, Color.white);
            UiFactory.Place(tile.Coin.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(24f, 0f), new Vector2(40f, 40f));
            tile.Caption = SugarUi.Outlined("Count", chip.transform, "", 30, Color.white, 2f);
            UiFactory.Stretch(tile.Caption.RectTransform);
            tile.Check = UiFactory.CreateImage("Check", rect, SugarSprites.Check, theme.PositiveColor);
            UiFactory.Place(tile.Check.rectTransform, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-12f, -12f), new Vector2(64f, 64f));
            tile.Punch = rect.gameObject.AddComponent<PunchScale>();
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = tile.Back;
            button.onClick.AddListener(() =>
            {
                SugarUi.Click();
                _toggle?.Invoke(booster);
                tile.Punch.Punch(1.2f);
                RefreshBoosters();
            });
            _tiles.Add(tile);
        }

        public void Show(LevelStartInfo info, Func<StartBooster, (bool selected, int count, int price)> boosterState, Action<StartBooster> toggle, Action onPlay)
        {
            _boosterState = boosterState;
            _toggle = toggle;
            _onPlay = onPlay;
            UiTheme theme = UiRoot.Theme;

            _title.Text = info.Title;
            _chip.gameObject.SetActive(info.ShowDifficulty);
            bool veryHard = info.Difficulty == Difficulty.VeryHard;
            _chipLabel.Text = SugarLoc.T(veryHard ? "FOARTE GREU" : "GREU") + (veryHard ? "  ×2" : "  ×1,5");
            float y = info.ShowDifficulty ? -170f : -110f;
            for (int i = 0; i < 3; i++)
            {
                _stars[i].color = i < info.BestStars ? theme.AccentColor : new Color(0.85f, 0.8f, 0.85f);
                _stars[i].rectTransform.anchoredPosition = new Vector2((i - 1) * 84f, y - (i == 1 ? 10f : 0f));
            }
            y -= 70f;
            _scenario.Text = info.Scenario;
            _scenario.RectTransform.anchoredPosition = new Vector2(0f, y);
            y -= 160f;

            foreach (GameObject view in _goalViews) Destroy(view);
            _goalViews.Clear();
            int count = info.Goals.Count;
            for (int i = 0; i < count; i++)
            {
                (Sprite icon, Color color, int amount) = info.Goals[i];
                RectTransform root = UiFactory.CreateRect("Goal", _goalRow);
                UiFactory.Place(root, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2((i - (count - 1) * 0.5f) * 210f, 14f), new Vector2(150f, 150f));
                Image image = UiFactory.CreateImage("Icon", root, icon, color);
                UiFactory.Place(image.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 12f), new Vector2(110f, 110f));
                UiLabel label = SugarUi.Outlined("Count", root, amount > 0 ? amount.ToString("#,0", System.Globalization.CultureInfo.InvariantCulture).Replace(',', '.') : "", 44, Color.white, 3f);
                UiFactory.Place(label.RectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, -6f), new Vector2(220f, 60f));
                _goalViews.Add(root.gameObject);
            }
            _goalRow.anchoredPosition = new Vector2(0f, y);
            _newBadge.gameObject.SetActive(info.IsNew);
            _newLabel.Text = SugarLoc.T("NOU!");
            y -= 230f;

            _boosterRow.gameObject.SetActive(info.Boosters);
            if (info.Boosters)
            {
                _boosterRow.anchoredPosition = new Vector2(0f, y);
                RefreshBoosters();
                y -= 210f;
            }
            _streak.gameObject.SetActive(!string.IsNullOrEmpty(info.Streak));
            if (!string.IsNullOrEmpty(info.Streak))
            {
                _streak.Text = info.Streak;
                UiFactory.Place(_streak.RectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(Width - 80f, 50f));
                y -= 60f;
            }
            _playLabel.Text = SugarLoc.T("JOACĂ");
            UiFactory.Place((RectTransform)_play.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y - 10f), new Vector2(620f, 150f));
            y -= 200f;
            SugarUi.ResizeBox(_box, new Vector2(Width, -y));
            _root.SetActive(true);
        }

        public void Hide() => _root.SetActive(false);

        private void RefreshBoosters()
        {
            UiTheme theme = UiRoot.Theme;
            foreach (BoosterTile tile in _tiles)
            {
                (bool selected, int count, int price) = _boosterState != null ? _boosterState(tile.Booster) : (false, 0, 0);
                tile.Back.color = selected ? Color.Lerp(theme.PositiveColor, Color.white, 0.65f) : Color.white;
                tile.Check.gameObject.SetActive(selected);
                bool owned = count > 0 || selected;
                tile.Coin.gameObject.SetActive(!owned);
                tile.Caption.Text = owned ? "×" + count : "    " + price;
            }
        }
    }
}
