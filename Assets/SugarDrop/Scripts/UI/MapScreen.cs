using System;
using QuarryEmpire;
using UnityEngine;
using UnityEngine.UI;

namespace SugarDrop
{
    /// <summary>
    /// Harta nivelurilor: un drum șerpuit cu 7 noduri în jurul nivelului curent (nivelurile trecute au stele,
    /// cele viitoare sunt încuiate, fiecare al 10-lea e Boss). Sus: vieți (cu cronometru) și monede; jos: „Joacă”.
    /// </summary>
    public class MapScreen : MonoBehaviour
    {
        private const int Nodes = 7;
        private const int NodesBelow = 2;

        public event Action PlayClicked;
        public event Action SettingsClicked;

        private GameObject _root;
        private UiLabel _lives;
        private UiLabel _lifeTimer;
        private UiLabel _coins;
        private UiLabel _playLabel;
        private RectTransform _path;
        private readonly RectTransform[] _nodes = new RectTransform[Nodes];
        private readonly Image[] _nodeFaces = new Image[Nodes];
        private readonly UiLabel[] _nodeLabels = new UiLabel[Nodes];
        private readonly Image[] _nodeLocks = new Image[Nodes];
        private readonly UiLabel[] _nodeBoss = new UiLabel[Nodes];
        private readonly Image[][] _nodeStars = new Image[Nodes][];
        private int _currentIndex;
        private float _refresh;

        public bool IsOpen => _root.activeSelf;

        private void Awake()
        {
            UiTheme theme = UiRoot.Theme;
            Image background = UiFactory.CreateImage("Background", transform, SugarSprites.BackgroundGradient, Color.white);
            background.preserveAspect = false;
            background.raycastTarget = true;
            UiFactory.Stretch(background.rectTransform, -300f);
            _root = background.gameObject;

            // Drumul: puncte mici între noduri, apoi nodurile.
            _path = UiFactory.CreateRect("Path", _root.transform);
            UiFactory.Stretch(_path);
            for (int i = 0; i < Nodes - 1; i++)
            for (int k = 1; k < 4; k++)
            {
                Vector2 a = NodePosition(i), b = NodePosition(i + 1);
                Vector2 p = Vector2.Lerp(a, b, k / 4f);
                Image dot = UiFactory.CreateImage("Dot", _path, theme.Circle, new Color(1f, 1f, 1f, 0.8f));
                UiFactory.Place(dot.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), p, new Vector2(26f, 26f));
            }
            for (int i = 0; i < Nodes; i++) BuildNode(i, theme);

            // Bara de sus: vieți și monede + setări.
            Image livesPill = UiFactory.CreatePanel("LivesPill", _root.transform, theme.PanelColor);
            UiFactory.Place((RectTransform)livesPill.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(330f, -24f), new Vector2(300f, 100f));
            Image heart = UiFactory.CreateImage("Heart", livesPill.transform, SugarSprites.LifeHeart, Color.white);
            UiFactory.Place(heart.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(10f, 0f), new Vector2(110f, 110f));
            _lives = SugarUi.Outlined("Count", heart.transform, "5", 44, Color.white);
            UiFactory.Stretch(_lives.RectTransform);
            _lifeTimer = UiFactory.CreateText("Timer", livesPill.transform, "", 38);
            UiFactory.Place(_lifeTimer.RectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(80f, 0f), new Vector2(210f, 80f));

            Image coinsPill = UiFactory.CreatePanel("CoinsPill", _root.transform, theme.PanelColor);
            UiFactory.Place((RectTransform)coinsPill.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-150f, -24f), new Vector2(260f, 100f));
            Image coin = UiFactory.CreateImage("Coin", coinsPill.transform, ProceduralSprites.Coin, Color.white);
            UiFactory.Place(coin.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(10f, 0f), new Vector2(90f, 90f));
            _coins = UiFactory.CreateText("Count", coinsPill.transform, "0", 42);
            UiFactory.Place(_coins.RectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(64f, 0f), new Vector2(180f, 80f));

            Button settings = UiFactory.CreateIconButton("Settings", _root.transform, SugarSprites.Gear, theme.PanelColor, () =>
            {
                SugarUi.Click();
                SettingsClicked?.Invoke();
            }, 0.6f);
            UiFactory.Place((RectTransform)settings.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -24f), new Vector2(104f, 104f));

            // Butonul mare „Joacă”.
            Button play = UiFactory.CreateButton("Play", _root.transform, "", theme.PositiveColor, () =>
            {
                SugarUi.Click();
                PlayClicked?.Invoke();
            }, 56);
            UiFactory.Place((RectTransform)play.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 90f), new Vector2(640f, 170f));
            play.gameObject.AddComponent<PunchScale>();
            _playLabel = UiFactory.GetButtonLabel(play);

            _root.SetActive(false);
        }

        private static Vector2 NodePosition(int index) =>
            new(Mathf.Sin(index * 1.25f) * 260f, 420f + index * 200f);

        private void BuildNode(int index, UiTheme theme)
        {
            RectTransform node = UiFactory.CreateRect("Node" + index, _root.transform);
            UiFactory.Place(node, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), NodePosition(index), new Vector2(150f, 150f));
            Image shadow = UiFactory.CreateImage("Shadow", node, theme.Circle, new Color(0.35f, 0.1f, 0.3f, 0.3f));
            UiFactory.Stretch(shadow.rectTransform);
            shadow.rectTransform.anchoredPosition = new Vector2(0f, -10f);
            _nodeFaces[index] = UiFactory.CreateImage("Face", node, SugarSprites.Cap, Color.white);
            UiFactory.Stretch(_nodeFaces[index].rectTransform);
            _nodeLabels[index] = SugarUi.Outlined("Number", node, "", 56, Color.white);
            UiFactory.Stretch(_nodeLabels[index].RectTransform);
            _nodeLocks[index] = UiFactory.CreateImage("Lock", node, SugarSprites.Lock, new Color(1f, 1f, 1f, 0.9f));
            UiFactory.Place(_nodeLocks[index].rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(70f, 70f));
            _nodeBoss[index] = SugarUi.Outlined("Boss", node, SugarLoc.T("BOSS"), 30, theme.AccentColor);
            UiFactory.Place(_nodeBoss[index].RectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0f), new Vector2(0f, 4f), new Vector2(200f, 44f));

            _nodeStars[index] = new Image[3];
            for (int s = 0; s < 3; s++)
            {
                Image star = UiFactory.CreateImage("Star" + s, node, ProceduralSprites.Star, theme.AccentColor);
                UiFactory.Place(star.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2((s - 1) * 48f, s == 1 ? -14f : -6f), new Vector2(46f, 46f));
                _nodeStars[index][s] = star;
            }
            _nodes[index] = node;
        }

        public void Show()
        {
            _root.SetActive(true);
            Refresh();
        }

        public void Hide() => _root.SetActive(false);

        public void Refresh()
        {
            SugarSaveData data = SugarSave.Data;
            int current = Mathf.Max(1, data.level);
            int first = Mathf.Max(1, current - NodesBelow);
            UiTheme theme = UiRoot.Theme;

            for (int i = 0; i < Nodes; i++)
            {
                int level = first + i;
                bool done = level < current;
                bool isCurrent = level == current;
                bool boss = LevelConfig.IsBossLevel(level);
                if (isCurrent) _currentIndex = i;

                _nodeFaces[i].color = isCurrent ? theme.AccentColor : done ? (boss ? Palette.Tile(4) : Palette.Tile(0)) : new Color(0.78f, 0.72f, 0.8f);
                _nodes[i].sizeDelta = Vector2.one * (boss ? 180f : 150f);
                _nodeLabels[i].Text = level.ToString();
                _nodeLabels[i].gameObject.SetActive(done || isCurrent);
                _nodeLocks[i].gameObject.SetActive(!done && !isCurrent);
                _nodeBoss[i].gameObject.SetActive(boss);
                _nodeBoss[i].Text = SugarLoc.T("BOSS");
                int stars = SugarSave.GetStars(level);
                for (int s = 0; s < 3; s++)
                {
                    _nodeStars[i][s].gameObject.SetActive(done);
                    _nodeStars[i][s].color = s < stars ? theme.AccentColor : new Color(1f, 1f, 1f, 0.5f);
                }
            }

            _coins.Text = data.coins.ToString();
            _playLabel.Text = SugarLoc.T("JOACĂ") + "  ·  " + SugarLoc.F("NIVELUL {0}", current);
            RefreshLives();
        }

        private void RefreshLives()
        {
            SugarSave.UpdateLives();
            SugarSaveData data = SugarSave.Data;
            _lives.Text = data.lives.ToString();
            _lifeTimer.Text = data.lives >= SugarSave.MaxLives ? "MAX" : SugarUi.Time(SugarSave.SecondsToNextLife);
        }

        private void Update()
        {
            if (!_root.activeSelf) return;
            // Nodul curent pulsează ușor, ca să atragă atenția.
            float pulse = 1f + Mathf.Sin(Time.unscaledTime * 4f) * 0.06f;
            _nodes[_currentIndex].localScale = Vector3.one * pulse;
            _refresh -= Time.unscaledDeltaTime;
            if (_refresh > 0f) return;
            _refresh = 0.5f;
            RefreshLives();
        }
    }
}
