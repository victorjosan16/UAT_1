using System.Collections.Generic;
using UndergroundMetroTycoon.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace UndergroundMetroTycoon.Game
{
    /// <summary>Settings: sound, graphics quality and a confirmed progress reset. Values persist in the save file.</summary>
    public sealed class SettingsPanel : ModalPanel
    {
        private static readonly string[] QualityNames = { "SCĂZUTĂ", "MEDIE", "RIDICATĂ" };

        private readonly List<UIButton> _qualityButtons = new List<UIButton>();
        private SettingsData _settings;
        private IGameUIActions _actions;
        private ConfirmDialog _confirm;
        private UIButton _soundButton;

        public void Build(RectTransform root, SettingsData settings, IGameUIActions actions, SfxPlayer sfx, ConfirmDialog confirm)
        {
            _settings = settings;
            _actions = actions;
            _confirm = confirm;
            BuildFrame(root, "Setări", 960f, sfx);

            RectTransform soundRow = Row("Sunet", 0f, 150f);
            _soundButton = UIButton.Create(soundRow, "PORNIT", UIButtonStyle.Primary, () =>
            {
                _actions.SetSoundEnabled(!_settings.SoundEnabled);
                Sfx?.PlayClick();
                Refresh();
            }, null, UITheme.SizeLabel);
            UIFactory.Place(_soundButton.Rect, new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(-24f, 0f), new Vector2(250f, 100f));

            RectTransform qualityRow = Row("Calitate grafică", 174f, 250f);
            for (int i = 0; i < QualityNames.Length; i++)
            {
                int quality = i;
                UIButton b = UIButton.Create(qualityRow, QualityNames[i], UIButtonStyle.Light, () =>
                {
                    _actions.SetGraphicsQuality(quality);
                    Sfx?.PlayClick();
                    Refresh();
                }, null, 30);
                UIFactory.Place(b.Rect, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(24f + i * 288f, 24f), new Vector2(270f, 96f));
                _qualityButtons.Add(b);
            }

            RectTransform resetRow = Row("Progres", 448f, 220f);
            UIButton reset = UIButton.Create(resetRow, "RESETEAZĂ PROGRESUL", UIButtonStyle.Danger, () =>
            {
                Sfx?.PlayClick();
                _confirm.Show("Ștergi tot progresul?\nMonedele, nivelul și contractele vor fi pierdute definitiv.", "ȘTERGE", () =>
                {
                    Close();
                    _actions.ResetProgress();
                });
            }, null, UITheme.SizeLabel);
            UIFactory.Place(reset.Rect, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 24f), new Vector2(856f, 100f));
        }

        protected override void OnOpened() => Refresh();

        private void Refresh()
        {
            _soundButton.SetLabel(_settings.SoundEnabled ? "PORNIT" : "OPRIT");
            _soundButton.SetStyle(_settings.SoundEnabled ? UIButtonStyle.Primary : UIButtonStyle.Light);
            for (int i = 0; i < _qualityButtons.Count; i++)
                _qualityButtons[i].SetStyle(i == _settings.GraphicsQuality ? UIButtonStyle.Secondary : UIButtonStyle.Light);
        }

        private RectTransform Row(string label, float top, float height)
        {
            Image card = UIFactory.Panel(label, Body, UITheme.CardWhite, UITheme.CornerCard);
            UIFactory.AddShadow(card, 4f, 0.15f);
            RectTransform rt = card.rectTransform;
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(1f, 1f);
            rt.pivot = new Vector2(0.5f, 1f);
            rt.offsetMin = new Vector2(0f, -top - height);
            rt.offsetMax = new Vector2(0f, -top);

            Text text = UIFactory.Text("Label", rt, label, UITheme.SizeTitle, UITheme.TextDark, TextAnchor.UpperLeft, true);
            UIFactory.Place(text.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(28f, -20f), new Vector2(600f, 60f));
            if (height <= 160f)
            {
                text.alignment = TextAnchor.MiddleLeft;
                UIFactory.Place(text.rectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(28f, 0f), new Vector2(560f, 80f));
            }
            return rt;
        }
    }
}
