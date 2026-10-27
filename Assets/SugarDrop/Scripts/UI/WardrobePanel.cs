using System;
using System.Collections.Generic;
using QuarryEmpire;
using UnityEngine;
using UnityEngine.UI;

namespace SugarDrop
{
    /// <summary>
    /// Garderoba lui Bobo: costumele se deblochează cu stele (din toate nivelurile). Atingi un costum deblocat și
    /// Bobo îl poartă imediat (în meniu și lângă borcan); la cele încuiate vezi câte stele mai trebuie.
    /// </summary>
    public class WardrobePanel : MonoBehaviour
    {
        /// <summary>Jucătorul a ales alt costum.</summary>
        public event Action CostumeChanged;

        private class Slot
        {
            public Costume Costume;
            public Image Back;
            public Image Lock;
            public UiLabel Caption;
            public PunchScale Punch;
        }

        private GameObject _root;
        private UiLabel _title;
        private UiLabel _stars;
        private UiLabel _message;
        private UiLabel _closeLabel;
        private BoboView _bobo;
        private readonly List<Slot> _slots = new();

        public bool IsOpen => _root != null && _root.activeSelf;

        private void Awake()
        {
            UiTheme theme = UiRoot.Theme;
            _root = SugarUi.Dim(transform).gameObject;
            RectTransform box = SugarUi.Box(_root.transform, new Vector2(940f, 1500f));

            _title = SugarUi.Outlined("Title", box, "", 70, theme.AccentColor, 4f);
            UiFactory.Place(_title.RectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(880f, 100f));

            _bobo = BoboView.Build(box, 0.7f);
            UiFactory.Place(_bobo.Root, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(0f, -420f), _bobo.Root.sizeDelta);

            _stars = UiFactory.CreateText("Stars", box, "", 40, TextAnchor.MiddleCenter, theme.TextDarkColor);
            UiFactory.Place(_stars.RectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -620f), new Vector2(860f, 60f));

            for (int i = 0; i < CostumeRules.All.Length; i++) _slots.Add(BuildSlot(box, CostumeRules.All[i], i, theme));

            _message = UiFactory.CreateText("Message", box, "", 34, TextAnchor.MiddleCenter, Color.Lerp(theme.TextDarkColor, Color.white, 0.25f));
            UiFactory.Place(_message.RectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 175f), new Vector2(860f, 60f));

            Button close = SugarUi.Button("Close", box, "", theme.PositiveColor, () =>
            {
                SugarUi.Click();
                Hide();
            }, 44);
            UiFactory.Place((RectTransform)close.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 45f), new Vector2(480f, 115f));
            _closeLabel = UiFactory.GetButtonLabel(close);

            _root.SetActive(false);
        }

        /// <summary>Grila: 4 costume pe primul rând, 3 pe al doilea.</summary>
        private Slot BuildSlot(RectTransform box, Costume costume, int index, UiTheme theme)
        {
            int row = index / 4, column = index % 4;
            int inRow = row == 0 ? 4 : CostumeRules.All.Length - 4;
            float x = (column - (inRow - 1) * 0.5f) * 205f;
            float y = -800f - row * 250f;

            var slot = new Slot { Costume = costume };
            slot.Back = UiFactory.CreatePanel("Slot" + costume, box, Color.white);
            var rect = UiFactory.Place((RectTransform)slot.Back.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2(x, y), new Vector2(185f, 225f));
            slot.Back.raycastTarget = true;
            Sprite sprite = SugarSprites.CostumeSprite(costume);
            Image icon = UiFactory.CreateImage("Icon", rect, sprite != null ? sprite : SugarSprites.Frosting, sprite != null ? Color.white : new Color(1f, 1f, 1f, 0.6f));
            UiFactory.Place(icon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -12f), new Vector2(140f, 140f));
            slot.Caption = UiFactory.CreateText("Caption", rect, "", 26, TextAnchor.MiddleCenter, theme.TextDarkColor);
            UiFactory.Place(slot.Caption.RectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 8f), new Vector2(175f, 66f));
            slot.Lock = UiFactory.CreateImage("Lock", rect, SugarSprites.Lock, new Color(0.35f, 0.15f, 0.3f, 0.85f));
            UiFactory.Place(slot.Lock.rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(80f, 80f));
            slot.Punch = rect.gameObject.AddComponent<PunchScale>();
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = slot.Back;
            button.onClick.AddListener(() => Choose(slot));
            return slot;
        }

        public void Show()
        {
            _title.Text = SugarLoc.T("Garderoba lui Bobo");
            _closeLabel.Text = SugarLoc.T("ÎNCHIDE");
            _message.Text = SugarLoc.T("Strânge stele ca să deblochezi costume noi!");
            Refresh();
            _root.SetActive(true);
        }

        public void Hide() => _root.SetActive(false);

        private void Refresh()
        {
            int stars = SugarSave.TotalStars(SugarSave.Data);
            Costume current = CostumeRules.Current;
            _stars.Text = SugarLoc.F("Stele: {0}", stars);
            _bobo.SetCostume(current);
            UiTheme theme = UiRoot.Theme;
            foreach (Slot slot in _slots)
            {
                bool unlocked = CostumeRules.Unlocked(slot.Costume, stars);
                slot.Lock.gameObject.SetActive(!unlocked);
                slot.Back.color = slot.Costume == current ? theme.AccentColor : unlocked ? Color.white : new Color(0.85f, 0.8f, 0.88f);
                slot.Caption.Text = unlocked
                    ? SugarLoc.T(CostumeRules.NameKey(slot.Costume))
                    : SugarLoc.F("{0} stele", CostumeRules.StarsNeeded(slot.Costume));
            }
        }

        private void Choose(Slot slot)
        {
            int stars = SugarSave.TotalStars(SugarSave.Data);
            if (!CostumeRules.Unlocked(slot.Costume, stars))
            {
                SugarAudio.Buzz(0);
                if (SugarAudio.Instance != null) SugarAudio.Instance.Play(SugarSound.Invalid, 0.7f);
                _message.Text = SugarLoc.F("Mai ai nevoie de {0} stele.", CostumeRules.StarsNeeded(slot.Costume) - stars);
                slot.Punch.Punch(0.6f);
                return;
            }
            SugarUi.Click();
            SugarSave.Data.costume = (int)slot.Costume;
            SugarSave.Save();
            slot.Punch.Punch(1.5f);
            _message.Text = SugarLoc.T(CostumeRules.NameKey(slot.Costume));
            Refresh();
            CostumeChanged?.Invoke();
        }
    }
}
