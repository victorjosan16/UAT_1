using System.Collections.Generic;
using UndergroundMetroTycoon.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace UndergroundMetroTycoon.Game
{
    /// <summary>Contract board: every contract with difficulty, objective, rewards and an action matching its real state.</summary>
    public sealed class ContractsPanel : ModalPanel
    {
        private const float CardHeight = 300f;
        private const float CardSpacing = 24f;

        private readonly List<GameObject> _cards = new List<GameObject>();
        private GameSession _session;
        private IGameUIActions _actions;
        private RectTransform _content;

        public void Build(RectTransform root, GameSession session, IGameUIActions actions, SfxPlayer sfx)
        {
            _session = session;
            _actions = actions;
            BuildFrame(root, "Contracte", 1420f, sfx);

            RectTransform viewport = UIFactory.Rect("Viewport", Body);
            UIFactory.Stretch(viewport);
            viewport.gameObject.AddComponent<RectMask2D>();
            Image hit = viewport.gameObject.AddComponent<Image>();
            hit.color = new Color(0f, 0f, 0f, 0f);

            _content = UIFactory.Rect("Content", viewport);
            _content.anchorMin = new Vector2(0f, 1f);
            _content.anchorMax = new Vector2(1f, 1f);
            _content.pivot = new Vector2(0.5f, 1f);
            _content.offsetMin = Vector2.zero;
            _content.offsetMax = Vector2.zero;

            ScrollRect scroll = viewport.gameObject.AddComponent<ScrollRect>();
            scroll.viewport = viewport;
            scroll.content = _content;
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Elastic;
            scroll.scrollSensitivity = 30f;
        }

        protected override void OnOpened() => Rebuild();

        public void RefreshIfOpen()
        {
            if (IsOpen) Rebuild();
        }

        private void Rebuild()
        {
            foreach (GameObject card in _cards) Destroy(card);
            _cards.Clear();

            IReadOnlyList<ContractDefinition> defs = _session.Contracts.Definitions;
            for (int i = 0; i < defs.Count; i++) _cards.Add(BuildCard(defs[i], i));
            _content.sizeDelta = new Vector2(0f, defs.Count * (CardHeight + CardSpacing));
        }

        private GameObject BuildCard(ContractDefinition def, int index)
        {
            ContractState state = _session.Contracts.GetState(def.Id);
            bool locked = state == ContractState.Locked;

            Image card = UIFactory.Panel("Card_" + def.Id, _content, locked ? UITheme.CardLocked : UITheme.CardWhite, UITheme.CornerCard);
            UIFactory.AddShadow(card, 5f, 0.18f);
            RectTransform rt = card.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(0f, -(index * (CardHeight + CardSpacing)) - CardHeight);
            rt.offsetMax = new Vector2(0f, -(index * (CardHeight + CardSpacing)));

            // Preview tile: soil block illustration on a sky-blue square.
            Image preview = UIFactory.Panel("Preview", rt, Palette.Hex(0xBFE3FF), UITheme.CornerCard);
            UIFactory.Place(preview.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(20f, 0f), new Vector2(230f, 250f));
            Image soil = UIFactory.Image("Soil", preview.transform, locked ? new Color(1f, 1f, 1f, 0.5f) : Color.white, UIIcons.SoilBlock);
            soil.preserveAspect = true;
            UIFactory.Stretch(soil.rectTransform, 30f, 40f, 30f, 40f);
            if (locked)
            {
                Image lockIcon = UIFactory.Image("Lock", preview.transform, UITheme.TextMuted, UIIcons.Lock);
                UIFactory.Place(lockIcon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(96f, 96f));
            }

            const float textX = 274f;
            Text title = UIFactory.Text("Title", rt, def.Title, UITheme.SizeTitle, locked ? UITheme.TextMuted : UITheme.TextDark, TextAnchor.UpperLeft, true);
            UIFactory.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(textX, -18f), new Vector2(560f, 56f));

            Color difficultyColor = DifficultyColor(def.Difficulty);
            Image chip = UIFactory.Panel("Difficulty", rt, difficultyColor, UITheme.CornerPill);
            UIFactory.Place(chip.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(textX, -80f), new Vector2(150f, 46f));
            Text chipLabel = UIFactory.Text("Label", chip.transform, RoText.Difficulty(def.Difficulty), 24, Color.white, TextAnchor.MiddleCenter, true);
            UIFactory.Stretch(chipLabel.rectTransform);

            if (def.Repeatable)
            {
                Image repeat = UIFactory.Panel("Repeatable", rt, UITheme.Turquoise, UITheme.CornerPill);
                UIFactory.Place(repeat.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(textX + 162f, -80f), new Vector2(190f, 46f));
                Text repeatLabel = UIFactory.Text("Label", repeat.transform, "REPETABIL", 24, Color.white, TextAnchor.MiddleCenter, true);
                UIFactory.Stretch(repeatLabel.rectTransform);
            }

            string objective = RoText.ContractObjective(def);
            int done = _session.Contracts.GetProgress(def.Id).CompletionCount;
            if (def.Repeatable && done > 0) objective += "  ·  realizat de " + done + " ori";
            Text objectiveText = UIFactory.Text("Objective", rt, objective, UITheme.SizeBody, UITheme.TextMuted, TextAnchor.UpperLeft);
            UIFactory.Place(objectiveText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(textX, -138f), new Vector2(620f, 44f));

            Image coin = UIFactory.CoinIcon(rt, 52f);
            UIFactory.Place(coin.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(textX, 34f), new Vector2(52f, 52f));
            Text reward = UIFactory.Text("Reward", rt, UITheme.Number(def.CoinReward), UITheme.SizeLabel, UITheme.GoldDark, TextAnchor.MiddleLeft, true);
            UIFactory.Place(reward.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(textX + 60f, 34f), new Vector2(120f, 52f));
            Image star = UIFactory.Image("Star", rt, Color.white, UIIcons.Star);
            UIFactory.Place(star.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(textX + 180f, 36f), new Vector2(48f, 48f));
            Text xp = UIFactory.Text("Xp", rt, def.XpReward + " XP", UITheme.SizeLabel, UITheme.Blue, TextAnchor.MiddleLeft, true);
            UIFactory.Place(xp.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(textX + 232f, 34f), new Vector2(120f, 52f));

            BuildAction(rt, def, state);
            return card.gameObject;
        }

        private void BuildAction(RectTransform card, ContractDefinition def, ContractState state)
        {
            string activeId = _session.Contracts.ActiveContractId;
            switch (state)
            {
                case ContractState.Available when activeId == null:
                    ActionButton(card, "ACCEPTĂ", UIButtonStyle.Primary, () =>
                    {
                        _actions.AcceptContract(def.Id);
                        Close();
                    });
                    break;
                case ContractState.ReadyForCompletion:
                    ActionButton(card, "REVENDICĂ", UIButtonStyle.Gold, () =>
                    {
                        _actions.ClaimContract(def.Id);
                        Rebuild();
                    });
                    break;
                case ContractState.Accepted:
                case ContractState.InProgress:
                    ActionButton(card, "CONTINUĂ", UIButtonStyle.Secondary, Close);
                    break;
                case ContractState.Available:
                    StatusLabel(card, "Termină lucrarea curentă", UITheme.TextMuted);
                    break;
                case ContractState.Locked:
                    StatusLabel(card, LockReason(def), UITheme.TextMuted);
                    break;
                default:
                    StatusLabel(card, "FINALIZAT", UITheme.GreenDark);
                    break;
            }
        }

        private string LockReason(ContractDefinition def)
        {
            if (_session.Progression.Level < def.RequiredLevel) return "Necesită nivelul " + def.RequiredLevel;
            ContractDefinition prerequisite = _session.Contracts.GetDefinition(def.PrerequisiteContractId);
            return prerequisite != null ? "Finalizează „" + prerequisite.Title + "”" : "Blocat";
        }

        private void ActionButton(RectTransform card, string label, UIButtonStyle style, System.Action onClick)
        {
            UIButton button = UIButton.Create(card, label, style, () =>
            {
                Sfx?.PlayClick();
                onClick();
            }, null, UITheme.SizeLabel);
            UIFactory.Place(button.Rect, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-22f, 22f), new Vector2(250f, 100f));
        }

        private static void StatusLabel(RectTransform card, string label, Color color)
        {
            Text text = UIFactory.Text("Status", card, label, UITheme.SizeBody, color, TextAnchor.MiddleRight, true);
            UIFactory.Place(text.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-26f, 22f), new Vector2(280f, 100f));
        }

        private static Color DifficultyColor(ContractDifficulty difficulty)
        {
            switch (difficulty)
            {
                case ContractDifficulty.Beginner: return UITheme.Green;
                case ContractDifficulty.Intermediate: return UITheme.Orange;
                default: return UITheme.Red;
            }
        }
    }
}
