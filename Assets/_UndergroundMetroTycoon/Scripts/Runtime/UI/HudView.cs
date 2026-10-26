using System;
using UndergroundMetroTycoon.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace UndergroundMetroTycoon.Game
{
    /// <summary>
    /// Gameplay HUD: profile (avatar, level, XP), coin balance, settings, the active-contract panel, the
    /// Contracte navigation tile and the equipped-tool badge. All values are read from the GameSession.
    /// </summary>
    public sealed class HudView : MonoBehaviour
    {
        private GameSession _session;
        private IGameUIActions _actions;

        private Text _levelText;
        private ProgressBarView _xpBar;
        private CurrencyDisplay _coins;

        private RectTransform _contractPanel;
        private Text _contractCaption;
        private Text _contractObjective;
        private GameObject _progressRow;
        private Text _progressCount;
        private ProgressBarView _contractBar;
        private Text _rewardText;
        private UIButton _claimButton;
        private UIButton _browseButton;

        private UIButton _contractsTile;
        private GameObject _contractsBadge;
        private float _tilePulse;

        private RectTransform _toolRing;
        private Image _toolRingImage;
        private Text _toolLevel;

        /// <summary>Supplies whether the player is currently digging (animates the tool badge).</summary>
        public Func<bool> IsDigging { get; set; }

        public RectTransform CoinIconTarget => _coins.IconTarget;

        public void Build(RectTransform root, GameSession session, IGameUIActions actions)
        {
            _session = session;
            _actions = actions;
            BuildProfile(root);
            BuildCurrencyAndSettings(root);
            BuildContractPanel(root);
            BuildNavigation(root);
            BuildToolBadge(root);
            Refresh();
        }

        /// <summary>Pulses the Contracte tile to draw attention (e.g. digging without an accepted contract).</summary>
        public void PulseContractsButton() => _tilePulse = 2.4f;

        public void PunchCoins() => _coins.Punch();

        public void Refresh()
        {
            PlayerProgression p = _session.Progression;
            _levelText.text = "Nivel " + p.Level;
            if (p.IsMaxLevel)
            {
                _xpBar.Label.text = "MAX";
                _xpBar.SetValue(1f);
            }
            else
            {
                _xpBar.Label.text = p.XpIntoLevel + "/" + p.XpForNextLevel;
                _xpBar.SetValue((float)p.XpIntoLevel / Mathf.Max(1, p.XpForNextLevel));
            }

            ToolDefinition tool = _session.EquippedTool;
            _toolLevel.text = tool != null ? "Nv " + tool.Level : "-";
            RefreshContract();
        }

        private void RefreshContract()
        {
            string activeId = _session.Contracts.ActiveContractId;
            ContractDefinition def = _session.Contracts.GetDefinition(activeId);
            bool attention = false;

            if (def == null)
            {
                _contractCaption.text = "FĂRĂ CONTRACT ACTIV";
                _contractCaption.color = UITheme.Orange;
                _contractObjective.text = "Alege o lucrare nouă de pe panou";
                _progressRow.SetActive(false);
                _claimButton.gameObject.SetActive(false);
                _browseButton.gameObject.SetActive(true);
                foreach (ContractDefinition d in _session.Contracts.Definitions)
                    if (_session.Contracts.GetState(d.Id) == ContractState.Available) attention = true;
            }
            else
            {
                ExcavationGrid grid = _session.Grid;
                int done = grid != null && grid.SiteContractId == activeId ? grid.ExcavatedContractCellCount : 0;
                int total = def.CountContractCells();
                bool ready = _session.Contracts.GetState(activeId) == ContractState.ReadyForCompletion;

                _contractCaption.text = ready ? "CONTRACT FINALIZAT!" : "CONTRACT ACTIV";
                _contractCaption.color = ready ? UITheme.Green : UITheme.BlueLight;
                _contractObjective.text = RoText.ContractObjective(def);
                _progressRow.SetActive(!ready);
                _progressCount.text = done + "/" + total;
                _contractBar.SetValue(total > 0 ? (float)done / total : 0f);
                _rewardText.text = UITheme.Number(def.CoinReward);
                _claimButton.gameObject.SetActive(ready);
                _claimButton.SetLabel("REVENDICĂ  " + UITheme.Number(def.CoinReward));
                _browseButton.gameObject.SetActive(false);
                attention = ready;
            }
            _contractsBadge.SetActive(attention);
        }

        private void Update()
        {
            _coins.SetTarget(_session.Wallet.Coins);
            float t = Time.unscaledTime;

            if (_claimButton.gameObject.activeSelf)
            {
                float s = 1f + Mathf.Sin(t * 6f) * 0.035f;
                _claimButton.transform.localScale = new Vector3(s, s, 1f);
            }

            if (_contractsBadge.activeSelf)
            {
                float s = 1f + Mathf.Abs(Mathf.Sin(t * 4f)) * 0.15f;
                _contractsBadge.transform.localScale = new Vector3(s, s, 1f);
            }

            if (_tilePulse > 0f)
            {
                _tilePulse -= Time.unscaledDeltaTime;
                float s = 1f + Mathf.Abs(Mathf.Sin(t * 8f)) * 0.08f;
                _contractsTile.transform.localScale = _tilePulse > 0f ? new Vector3(s, s, 1f) : Vector3.one;
            }

            bool digging = IsDigging != null && IsDigging();
            _toolRing.localRotation = digging ? Quaternion.Euler(0f, 0f, -t * 220f) : Quaternion.identity;
            _toolRingImage.color = digging ? UITheme.Gold : new Color(1f, 1f, 1f, 0.55f);
        }

        private void BuildProfile(RectTransform root)
        {
            Image card = UIFactory.Panel("Profile", root, UITheme.PanelGlass, UITheme.CornerPill);
            UIFactory.Place(card.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -24f), new Vector2(440f, 140f));
            UIFactory.AddShadow(card, 5f, 0.35f);

            Image ring = UIFactory.Image("AvatarRing", card.transform, UITheme.Gold, UISprites.Circle);
            UIFactory.Place(ring.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(10f, 0f), new Vector2(124f, 124f));
            Image back = UIFactory.Image("AvatarBack", ring.transform, Palette.Hex(0x7FC8FF), UISprites.Circle);
            UIFactory.Stretch(back.rectTransform, 7f, 7f, 7f, 7f);
            Image avatar = UIFactory.Image("Avatar", back.transform, Color.white, UIIcons.Avatar);
            UIFactory.Stretch(avatar.rectTransform, 4f, 2f, 4f, 6f);

            _levelText = UIFactory.Text("Level", card.transform, "Nivel 1", UITheme.SizeTitle, UITheme.TextOnDark, TextAnchor.MiddleLeft, true);
            UIFactory.Place(_levelText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(150f, -12f), new Vector2(270f, 56f));
            UIFactory.AddShadow(_levelText, 2f, 0.5f);

            _xpBar = ProgressBarView.Create(card.transform, UITheme.Turquoise, UITheme.NavyDeep, true, 24);
            UIFactory.Place(_xpBar.Rect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(150f, -76f), new Vector2(266f, 40f));
            Image star = UIFactory.Image("Star", card.transform, Color.white, UIIcons.Star);
            UIFactory.Place(star.rectTransform, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(150f, -96f), new Vector2(54f, 54f));
        }

        private void BuildCurrencyAndSettings(RectTransform root)
        {
            UIButton settings = UIButton.Create(root, null, UIButtonStyle.Navy, () => _actions.OpenSettings(), UIIcons.Gear, round: true);
            UIFactory.Place(settings.Rect, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -24f), new Vector2(108f, 116f));

            _coins = CurrencyDisplay.Create(root, UIIcons.Coin, _session.Wallet.Coins);
            UIFactory.Place((RectTransform)_coins.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-148f, -34f), new Vector2(330f, 100f));
        }

        private void BuildContractPanel(RectTransform root)
        {
            Image panel = UIFactory.Panel("ContractPanel", root, UITheme.PanelGlass, UITheme.CornerCard);
            _contractPanel = panel.rectTransform;
            UIFactory.Place(_contractPanel, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -164f), new Vector2(560f, 250f));
            UIFactory.AddShadow(panel, 6f, 0.35f);
            panel.raycastTarget = true;
            Button open = panel.gameObject.AddComponent<Button>();
            open.transition = Selectable.Transition.None;
            open.onClick.AddListener(() => _actions.OpenContracts());

            _contractCaption = UIFactory.Text("Caption", _contractPanel, "", UITheme.SizeCaption, UITheme.BlueLight, TextAnchor.UpperLeft, true);
            UIFactory.Place(_contractCaption.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -14f), new Vector2(500f, 36f));
            _contractObjective = UIFactory.Text("Objective", _contractPanel, "", UITheme.SizeLabel, UITheme.TextOnDark, TextAnchor.UpperLeft, true);
            UIFactory.Place(_contractObjective.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -50f), new Vector2(510f, 92f));

            RectTransform row = UIFactory.Rect("ProgressRow", _contractPanel);
            UIFactory.Place(row, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(20f, 22f), new Vector2(520f, 76f));
            _progressRow = row.gameObject;
            Image soil = UIFactory.Image("Soil", row, Color.white, UIIcons.SoilBlock);
            UIFactory.Place(soil.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), Vector2.zero, new Vector2(72f, 72f));
            _progressCount = UIFactory.Text("Count", row, "", UITheme.SizeLabel, UITheme.TextOnDark, TextAnchor.MiddleLeft, true);
            UIFactory.Place(_progressCount.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(80f, 0f), new Vector2(110f, 60f));
            _contractBar = ProgressBarView.Create(row, UITheme.Orange, UITheme.NavyDeep);
            UIFactory.Place(_contractBar.Rect, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(186f, 0f), new Vector2(180f, 30f));
            Image coin = UIFactory.CoinIcon(row, 56f);
            UIFactory.Place(coin.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(380f, 0f), new Vector2(56f, 56f));
            _rewardText = UIFactory.Text("Reward", row, "", UITheme.SizeLabel, UITheme.Gold, TextAnchor.MiddleLeft, true);
            UIFactory.Place(_rewardText.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(440f, 0f), new Vector2(110f, 60f));

            _claimButton = UIButton.Create(_contractPanel, "REVENDICĂ", UIButtonStyle.Gold, () =>
            {
                string id = _session.Contracts.ActiveContractId;
                if (id != null) _actions.ClaimContract(id);
            }, UIIcons.Coin, UITheme.SizeLabel);
            UIFactory.Place(_claimButton.Rect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(510f, 100f));

            _browseButton = UIButton.Create(_contractPanel, "VEZI CONTRACTE", UIButtonStyle.Secondary, () => _actions.OpenContracts(), UIIcons.Clipboard, UITheme.SizeLabel);
            UIFactory.Place(_browseButton.Rect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 18f), new Vector2(510f, 100f));
        }

        private void BuildNavigation(RectTransform root)
        {
            _contractsTile = UIButton.Create(root, null, UIButtonStyle.Navy, () => _actions.OpenContracts());
            UIFactory.Place(_contractsTile.Rect, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(24f, -188f), new Vector2(170f, 182f));
            Transform face = _contractsTile.Rect.Find("Face");
            Image icon = UIFactory.Image("Icon", face, Color.white, UIIcons.Clipboard);
            icon.preserveAspect = true;
            UIFactory.Place(icon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -10f), new Vector2(110f, 110f));
            Text label = UIFactory.Text("Caption", face, "Contracte", UITheme.SizeCaption, UITheme.TextOnDark, TextAnchor.MiddleCenter, true);
            UIFactory.Place(label.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 8f), new Vector2(170f, 44f));

            Image badge = UIFactory.Image("Badge", _contractsTile.Rect, UITheme.Red, UISprites.Circle);
            UIFactory.Place(badge.rectTransform, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-10f, -8f), new Vector2(50f, 50f));
            Text mark = UIFactory.Text("Mark", badge.transform, "!", 34, Color.white, TextAnchor.MiddleCenter, true);
            UIFactory.Stretch(mark.rectTransform, 0f, 0f, 0f, 2f);
            _contractsBadge = badge.gameObject;
        }

        private void BuildToolBadge(RectTransform root)
        {
            RectTransform badge = UIFactory.Rect("ToolBadge", root);
            UIFactory.Place(badge, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-40f, 60f), new Vector2(220f, 220f));

            Image ring = UIFactory.Image("Ring", badge, Color.white, UISprites.Circle);
            UIFactory.Stretch(ring.rectTransform);
            _toolRing = ring.rectTransform;
            _toolRingImage = ring;
            // A small orbiting marker rides the ring; it spins while the player digs.
            Image marker = UIFactory.Image("Marker", ring.transform, UITheme.NavyDeep, UISprites.Circle);
            UIFactory.Place(marker.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -6f), new Vector2(16f, 16f));

            Image inner = UIFactory.Image("Inner", badge, UITheme.Navy, UISprites.Circle);
            UIFactory.Stretch(inner.rectTransform, 12f, 12f, 12f, 12f);
            Image shovel = UIFactory.Image("Tool", inner.transform, Color.white, UIIcons.Shovel);
            shovel.preserveAspect = true;
            UIFactory.Stretch(shovel.rectTransform, 30f, 26f, 30f, 34f);

            Image pill = UIFactory.Panel("Level", badge, UITheme.Gold, UITheme.CornerPill);
            UIFactory.Place(pill.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2(0f, 6f), new Vector2(120f, 46f));
            _toolLevel = UIFactory.Text("Text", pill.transform, "Nv 1", 28, UITheme.TextDark, TextAnchor.MiddleCenter, true);
            UIFactory.Stretch(_toolLevel.rectTransform);
        }
    }
}
