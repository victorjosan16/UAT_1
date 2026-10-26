using System.Collections.Generic;
using UndergroundMetroTycoon.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace UndergroundMetroTycoon.Game
{
    /// <summary>Contract board: every contract with difficulty, objective, reward and an action matching its state.</summary>
    public sealed class ContractsPanel : ModalPanel
    {
        private const float CardHeight = 300f;
        private const float CardSpacing = 26f;

        private readonly List<GameObject> _cards = new List<GameObject>();
        private GameSession _session;
        private IGameUIActions _actions;
        private RectTransform _content;

        public void Build(RectTransform root, GameSession session, IGameUIActions actions, SfxPlayer sfx)
        {
            _session = session;
            _actions = actions;
            BuildFrame(root, "CONTRACTS", 1400f, sfx);

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

            Image card = UIFactory.Panel("Card_" + def.Id, _content, locked ? Palette.Hex(0x1A2537) : Palette.NavyLight, 0.8f);
            RectTransform rt = card.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(0f, -(index * (CardHeight + CardSpacing)) - CardHeight);
            rt.offsetMax = new Vector2(0f, -(index * (CardHeight + CardSpacing)));

            Color difficultyColor = DifficultyColor(def.Difficulty);
            Image stripe = UIFactory.Panel("Stripe", rt, difficultyColor, 0.3f);
            stripe.rectTransform.anchorMin = new Vector2(0f, 0f);
            stripe.rectTransform.anchorMax = new Vector2(0f, 1f);
            stripe.rectTransform.offsetMin = new Vector2(12f, 16f);
            stripe.rectTransform.offsetMax = new Vector2(24f, -16f);

            Text title = UIFactory.Text("Title", rt, def.Title, 44, locked ? Palette.Muted : Palette.White, TextAnchor.UpperLeft, true);
            UIFactory.Place(title.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(48f, -22f), new Vector2(560f, 56f));

            Image chip = UIFactory.Panel("Difficulty", rt, difficultyColor, 0.5f);
            UIFactory.Place(chip.rectTransform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-26f, -24f), new Vector2(230f, 52f));
            string chipText = def.Difficulty.ToString().ToUpperInvariant();
            Text chipLabel = UIFactory.Text("Label", chip.transform, chipText, 26, Palette.NavyDeep, TextAnchor.MiddleCenter, true);
            UIFactory.Stretch(chipLabel.rectTransform);

            Text description = UIFactory.Text("Description", rt, def.Description, 28, Palette.Muted, TextAnchor.UpperLeft);
            UIFactory.Place(description.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(48f, -86f), new Vector2(860f, 80f));

            string objective = "Dig " + def.CountContractCells() + " blocks";
            if (def.Repeatable) objective += "   ·   repeatable";
            if (def.Repeatable && _session.Contracts.GetProgress(def.Id).CompletionCount > 0)
                objective += ", done " + _session.Contracts.GetProgress(def.Id).CompletionCount + "x";
            Text objectiveText = UIFactory.Text("Objective", rt, objective, 30, Palette.Cyan, TextAnchor.UpperLeft, true);
            UIFactory.Place(objectiveText.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(48f, -170f), new Vector2(560f, 40f));

            Image coin = UIFactory.CoinIcon(rt, 50f);
            UIFactory.Place(coin.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(48f, 30f), new Vector2(50f, 50f));
            Text reward = UIFactory.Text("Reward", rt, HudView.FormatCoins(def.CoinReward) + "   +" + def.XpReward + " XP", 36, Palette.Yellow, TextAnchor.MiddleLeft, true);
            UIFactory.Place(reward.rectTransform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(110f, 30f), new Vector2(420f, 50f));

            BuildAction(rt, def, state);
            return card.gameObject;
        }

        private void BuildAction(RectTransform card, ContractDefinition def, ContractState state)
        {
            string activeId = _session.Contracts.ActiveContractId;
            switch (state)
            {
                case ContractState.Available when activeId == null:
                    ActionButton(card, "ACCEPT", Palette.Emerald, () =>
                    {
                        _actions.AcceptContract(def.Id);
                        Close();
                    });
                    break;
                case ContractState.ReadyForCompletion:
                    ActionButton(card, "CLAIM", Palette.Yellow, () =>
                    {
                        _actions.ClaimContract(def.Id);
                        Rebuild();
                    });
                    break;
                case ContractState.Accepted:
                case ContractState.InProgress:
                    StatusLabel(card, "IN PROGRESS", Palette.Cyan);
                    break;
                case ContractState.Available:
                    StatusLabel(card, "Finish current job first", Palette.Muted);
                    break;
                case ContractState.Locked:
                    StatusLabel(card, LockReason(def), Palette.Muted);
                    break;
                default:
                    StatusLabel(card, "COMPLETED", Palette.Emerald);
                    break;
            }
        }

        private string LockReason(ContractDefinition def)
        {
            if (_session.Progression.Level < def.RequiredLevel) return "Requires level " + def.RequiredLevel;
            ContractDefinition prerequisite = _session.Contracts.GetDefinition(def.PrerequisiteContractId);
            return prerequisite != null ? "Complete \"" + prerequisite.Title + "\"" : "Locked";
        }

        private void ActionButton(RectTransform card, string label, Color color, System.Action onClick)
        {
            Button button = UIFactory.Button("Action", card, label, color, Palette.NavyDeep, 38, () =>
            {
                Sfx?.PlayClick();
                onClick();
            }, out _);
            UIFactory.Place((RectTransform)button.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-26f, 24f), new Vector2(290f, 96f));
        }

        private static void StatusLabel(RectTransform card, string label, Color color)
        {
            Text text = UIFactory.Text("Status", card, label, 30, color, TextAnchor.MiddleRight, true);
            UIFactory.Place(text.rectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-30f, 30f), new Vector2(460f, 80f));
        }

        private static Color DifficultyColor(ContractDifficulty difficulty)
        {
            switch (difficulty)
            {
                case ContractDifficulty.Beginner: return Palette.Emerald;
                case ContractDifficulty.Intermediate: return Palette.Orange;
                default: return Palette.Red;
            }
        }
    }
}
