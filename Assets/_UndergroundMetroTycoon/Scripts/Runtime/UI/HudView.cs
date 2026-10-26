using System.Globalization;
using UndergroundMetroTycoon.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace UndergroundMetroTycoon.Game
{
    /// <summary>Main HUD: level and XP, coin counter, current tool, active contract card and the menu buttons.</summary>
    public sealed class HudView : MonoBehaviour
    {
        private GameSession _session;
        private IGameUIActions _actions;

        private Text _coinText;
        private RectTransform _coinPill;
        private Text _levelNumber;
        private Text _levelLabel;
        private RectTransform _xpFill;
        private Text _xpText;
        private Text _toolText;

        private Text _cardHeader;
        private Text _cardTitle;
        private Text _cardObjective;
        private Text _cardReward;
        private RectTransform _cardFill;
        private RectTransform _cardProgressRoot;
        private GameObject _cardRewardRow;
        private Button _claimButton;
        private Text _claimLabel;
        private RectTransform _contractsButton;

        private double _displayedCoins;
        private float _coinPunch;
        private float _contractsPulse;

        public RectTransform CoinIconTarget { get; private set; }

        public void Build(RectTransform root, GameSession session, IGameUIActions actions)
        {
            _session = session;
            _actions = actions;
            _displayedCoins = session.Wallet.Coins;

            BuildTopBar(root);
            BuildContractCard(root);
            BuildBottomButtons(root);
            Refresh();
        }

        /// <summary>Pulses the contracts button to draw attention (e.g. digging without an accepted contract).</summary>
        public void PulseContractsButton() => _contractsPulse = 2.4f;

        public void PunchCoins() => _coinPunch = 0.18f;

        public void Refresh()
        {
            PlayerProgression p = _session.Progression;
            _levelNumber.text = p.Level.ToString(CultureInfo.InvariantCulture);
            _levelLabel.text = "LEVEL " + p.Level;
            if (p.IsMaxLevel)
            {
                _xpText.text = "MAX";
                UIFactory.SetProgress(_xpFill, 1f);
            }
            else
            {
                _xpText.text = p.XpIntoLevel + " / " + p.XpForNextLevel + " XP";
                UIFactory.SetProgress(_xpFill, (float)p.XpIntoLevel / Mathf.Max(1, p.XpForNextLevel));
            }

            ToolDefinition tool = _session.EquippedTool;
            _toolText.text = tool != null ? tool.DisplayName + "  ·  Lv " + tool.Level : "No tool";
            RefreshContractCard();
        }

        private void RefreshContractCard()
        {
            string activeId = _session.Contracts.ActiveContractId;
            ContractDefinition def = _session.Contracts.GetDefinition(activeId);
            bool active = def != null;
            _cardProgressRoot.gameObject.SetActive(active);
            _cardRewardRow.SetActive(active);

            if (!active)
            {
                _cardHeader.text = "NO ACTIVE CONTRACT";
                _cardHeader.color = Palette.Orange;
                _cardTitle.text = "Pick a job to start digging";
                _cardObjective.text = "Tap CONTRACTS below";
                _claimButton.gameObject.SetActive(false);
                return;
            }

            ExcavationGrid grid = _session.Grid;
            int done = grid != null && grid.SiteContractId == activeId ? grid.ExcavatedContractCellCount : 0;
            int total = def.CountContractCells();
            bool ready = _session.Contracts.GetState(activeId) == ContractState.ReadyForCompletion;

            _cardHeader.text = ready ? "CONTRACT COMPLETE" : "CURRENT CONTRACT";
            _cardHeader.color = ready ? Palette.Emerald : Palette.Cyan;
            _cardTitle.text = def.Title;
            _cardObjective.text = ready ? "Collect your payment!" : "Dig blocks  " + done + " / " + total;
            _cardReward.text = FormatCoins(def.CoinReward);
            UIFactory.SetProgress(_cardFill, total > 0 ? (float)done / total : 0f);
            _claimButton.gameObject.SetActive(ready);
            _claimLabel.text = "CLAIM  " + FormatCoins(def.CoinReward);
        }

        private void Update()
        {
            double target = _session.Wallet.Coins;
            if (System.Math.Abs(_displayedCoins - target) > 0.01)
            {
                double step = System.Math.Max(1.0, System.Math.Abs(target - _displayedCoins) * Time.unscaledDeltaTime * 6.0);
                _displayedCoins = _displayedCoins < target ? System.Math.Min(target, _displayedCoins + step) : System.Math.Max(target, _displayedCoins - step);
                _coinText.text = FormatCoins((long)System.Math.Round(_displayedCoins));
            }

            float scale = 1f;
            if (_coinPunch > 0f)
            {
                _coinPunch -= Time.unscaledDeltaTime;
                scale = 1f + Mathf.Sin(Mathf.Clamp01(1f - _coinPunch / 0.18f) * Mathf.PI) * 0.12f;
            }
            _coinPill.localScale = new Vector3(scale, scale, 1f);

            if (_claimButton.gameObject.activeSelf)
            {
                float s = 1f + Mathf.Sin(Time.unscaledTime * 6f) * 0.04f;
                _claimButton.transform.localScale = new Vector3(s, s, 1f);
            }

            if (_contractsPulse > 0f)
            {
                _contractsPulse -= Time.unscaledDeltaTime;
                float s = 1f + Mathf.Abs(Mathf.Sin(Time.unscaledTime * 8f)) * 0.1f;
                _contractsButton.localScale = new Vector3(s, s, 1f);
                if (_contractsPulse <= 0f) _contractsButton.localScale = Vector3.one;
            }
        }

        public static string FormatCoins(long value) => value.ToString("N0", CultureInfo.InvariantCulture);

        private void BuildTopBar(RectTransform root)
        {
            // Level badge + XP bar (top-left).
            Image badge = UIFactory.Image("LevelBadge", root, Palette.Yellow, UISprites.Circle);
            UIFactory.Place(badge.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -28f), new Vector2(118f, 118f));
            UIFactory.AddShadow(badge, 5f);
            Image badgeInner = UIFactory.Image("Inner", badge.transform, Palette.Navy, UISprites.Circle);
            UIFactory.Stretch(badgeInner.rectTransform, 12f, 12f, 12f, 12f);
            _levelNumber = UIFactory.Text("Number", badgeInner.transform, "1", 52, Palette.Yellow, TextAnchor.MiddleCenter, true);
            UIFactory.Stretch(_levelNumber.rectTransform);

            Image xpPanel = UIFactory.Panel("XpPanel", root, new Color(Palette.Navy.r, Palette.Navy.g, Palette.Navy.b, 0.88f), 0.6f);
            UIFactory.Place(xpPanel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(160f, -36f), new Vector2(330f, 102f));
            _levelLabel = UIFactory.Text("Level", xpPanel.transform, "LEVEL 1", 32, Palette.White, TextAnchor.UpperLeft, true);
            UIFactory.Place(_levelLabel.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(22f, -10f), new Vector2(290f, 40f));
            _xpFill = UIFactory.ProgressBar("XpBar", xpPanel.transform, Palette.NavyDeep, Palette.Cyan, out RectTransform xpRoot);
            UIFactory.Place(xpRoot, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(22f, -54f), new Vector2(290f, 26f));
            _xpText = UIFactory.Text("XpText", xpPanel.transform, "", 22, Palette.Muted, TextAnchor.UpperLeft);
            UIFactory.Place(_xpText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(22f, -80f), new Vector2(290f, 26f));

            // Coin pill (top-right).
            Image pill = UIFactory.Panel("CoinPill", root, new Color(Palette.Navy.r, Palette.Navy.g, Palette.Navy.b, 0.92f), 1.2f);
            _coinPill = pill.rectTransform;
            UIFactory.Place(_coinPill, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-150f, -36f), new Vector2(330f, 96f));
            UIFactory.AddShadow(pill, 5f);
            Image coin = UIFactory.CoinIcon(pill.transform, 76f);
            UIFactory.Place(coin.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(12f, 0f), new Vector2(76f, 76f));
            CoinIconTarget = coin.rectTransform;
            _coinText = UIFactory.Text("Coins", pill.transform, FormatCoins(_session.Wallet.Coins), 48, Palette.White, TextAnchor.MiddleRight, true);
            UIFactory.Stretch(_coinText.rectTransform, 96f, 0f, 26f, 0f);

            // Settings button (top-right corner): three-bar menu icon.
            Button settings = UIFactory.Button("SettingsButton", root, "", Palette.NavyLight, Palette.White, 30, () => _actions.OpenSettings(), out _);
            UIFactory.Place((RectTransform)settings.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -36f), new Vector2(100f, 96f));
            for (int i = 0; i < 3; i++)
            {
                Image bar = UIFactory.Panel("Bar", settings.transform, Palette.White, 0.2f);
                UIFactory.Place(bar.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 18f - i * 18f), new Vector2(48f, 9f));
            }

            // Current tool chip.
            Image toolChip = UIFactory.Panel("ToolChip", root, new Color(Palette.NavyDeep.r, Palette.NavyDeep.g, Palette.NavyDeep.b, 0.8f), 1.2f);
            UIFactory.Place(toolChip.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -146f), new Vector2(400f, 58f));
            _toolText = UIFactory.Text("Tool", toolChip.transform, "", 28, Palette.Yellow, TextAnchor.MiddleCenter, true);
            UIFactory.Stretch(_toolText.rectTransform);
        }

        private void BuildContractCard(RectTransform root)
        {
            Image card = UIFactory.Panel("ContractCard", root, new Color(Palette.Navy.r, Palette.Navy.g, Palette.Navy.b, 0.9f), 0.8f);
            UIFactory.Place(card.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(30f, -170f), new Vector2(560f, 236f));
            UIFactory.AddShadow(card, 6f);
            card.raycastTarget = true;
            Button cardButton = card.gameObject.AddComponent<Button>();
            cardButton.transition = Selectable.Transition.None;
            cardButton.onClick.AddListener(() => _actions.OpenContracts());

            Image accent = UIFactory.Panel("Accent", card.transform, Palette.Yellow, 0.3f);
            UIFactory.Place(accent.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(10f, 0f), new Vector2(10f, 196f));

            _cardHeader = UIFactory.Text("Header", card.transform, "", 24, Palette.Cyan, TextAnchor.UpperLeft, true);
            UIFactory.Place(_cardHeader.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(36f, -16f), new Vector2(500f, 30f));
            _cardTitle = UIFactory.Text("Title", card.transform, "", 40, Palette.White, TextAnchor.UpperLeft, true);
            UIFactory.Place(_cardTitle.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(36f, -48f), new Vector2(500f, 50f));
            _cardObjective = UIFactory.Text("Objective", card.transform, "", 30, Palette.Muted, TextAnchor.UpperLeft);
            UIFactory.Place(_cardObjective.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(36f, -102f), new Vector2(500f, 40f));

            _cardFill = UIFactory.ProgressBar("Progress", card.transform, Palette.NavyDeep, Palette.Emerald, out _cardProgressRoot);
            UIFactory.Place(_cardProgressRoot, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(36f, -152f), new Vector2(320f, 32f));

            var rewardRow = UIFactory.Rect("Reward", card.transform);
            UIFactory.Place(rewardRow, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(372f, -144f), new Vector2(170f, 48f));
            _cardRewardRow = rewardRow.gameObject;
            Image rewardCoin = UIFactory.CoinIcon(rewardRow, 44f);
            UIFactory.Place(rewardCoin.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(44f, 44f));
            _cardReward = UIFactory.Text("Amount", rewardRow, "", 34, Palette.Yellow, TextAnchor.MiddleLeft, true);
            UIFactory.Stretch(_cardReward.rectTransform, 52f, 0f, 0f, 0f);

            _claimButton = UIFactory.Button("ClaimButton", card.transform, "CLAIM", Palette.Emerald, Palette.White, 34, () =>
            {
                string id = _session.Contracts.ActiveContractId;
                if (id != null) _actions.ClaimContract(id);
            }, out _claimLabel);
            UIFactory.Place((RectTransform)_claimButton.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, -10f), new Vector2(420f, 88f));
        }

        private void BuildBottomButtons(RectTransform root)
        {
            Button contracts = UIFactory.Button("ContractsButton", root, "", Palette.Yellow, Palette.Navy, 30, () => _actions.OpenContracts(), out _);
            _contractsButton = (RectTransform)contracts.transform;
            UIFactory.Place(_contractsButton, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-40f, 60f), new Vector2(230f, 230f));

            // Clipboard icon built from panels.
            Image board = UIFactory.Panel("Clipboard", contracts.transform, Palette.Navy, 0.5f);
            UIFactory.Place(board.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 26f), new Vector2(96f, 118f));
            Image paper = UIFactory.Panel("Paper", board.transform, Palette.White, 0.3f);
            UIFactory.Stretch(paper.rectTransform, 12f, 22f, 12f, 12f);
            Image clip = UIFactory.Panel("Clip", board.transform, Palette.Steel, 0.3f);
            UIFactory.Place(clip.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -6f), new Vector2(46f, 22f));
            for (int i = 0; i < 3; i++)
            {
                Image line = UIFactory.Panel("Line", paper.transform, Palette.Muted, 0.2f);
                UIFactory.Place(line.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -16f - i * 22f), new Vector2(50f, 8f));
            }

            Text label = UIFactory.Text("Caption", contracts.transform, "CONTRACTS", 30, Palette.Navy, TextAnchor.MiddleCenter, true);
            UIFactory.Place(label.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 16f), new Vector2(220f, 40f));
        }
    }
}
