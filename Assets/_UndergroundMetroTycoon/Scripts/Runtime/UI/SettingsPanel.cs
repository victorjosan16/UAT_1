using System.Collections.Generic;
using UndergroundMetroTycoon.Simulation;
using UnityEngine;
using UnityEngine.UI;

namespace UndergroundMetroTycoon.Game
{
    /// <summary>Settings: sound on/off, graphics quality and a confirmed progress reset.</summary>
    public sealed class SettingsPanel : ModalPanel
    {
        private static readonly string[] QualityNames = { "LOW", "MEDIUM", "HIGH" };

        private readonly List<Image> _qualityButtons = new List<Image>();
        private SettingsData _settings;
        private IGameUIActions _actions;
        private Image _soundButton;
        private Text _soundLabel;
        private Text _resetLabel;
        private bool _confirmReset;

        public void Build(RectTransform root, SettingsData settings, IGameUIActions actions, SfxPlayer sfx)
        {
            _settings = settings;
            _actions = actions;
            BuildFrame(root, "SETTINGS", 900f, sfx);

            Row("Sound", 0f);
            Button sound = UIFactory.Button("SoundToggle", Body, "", Palette.Emerald, Palette.NavyDeep, 34, () =>
            {
                _actions.SetSoundEnabled(!_settings.SoundEnabled);
                Sfx?.PlayClick();
                Refresh();
            }, out _soundLabel);
            _soundButton = sound.GetComponent<Image>();
            UIFactory.Place((RectTransform)sound.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(0f, -10f), new Vector2(240f, 90f));

            Row("Graphics quality", 160f);
            for (int i = 0; i < QualityNames.Length; i++)
            {
                int quality = i;
                Button b = UIFactory.Button("Quality" + i, Body, QualityNames[i], Palette.NavyLight, Palette.White, 30, () =>
                {
                    _actions.SetGraphicsQuality(quality);
                    Sfx?.PlayClick();
                    Refresh();
                }, out _);
                UIFactory.Place((RectTransform)b.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(i * 300f, -240f), new Vector2(280f, 90f));
                _qualityButtons.Add(b.GetComponent<Image>());
            }

            Row("Progress", 400f);
            Button reset = UIFactory.Button("Reset", Body, "", Palette.Red, Palette.White, 30, OnResetPressed, out _resetLabel);
            UIFactory.Place((RectTransform)reset.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -480f), new Vector2(880f, 90f));
        }

        protected override void OnOpened()
        {
            _confirmReset = false;
            Refresh();
        }

        private void OnResetPressed()
        {
            Sfx?.PlayClick();
            if (!_confirmReset)
            {
                _confirmReset = true;
                Refresh();
                return;
            }
            _confirmReset = false;
            Close();
            _actions.ResetProgress();
        }

        private void Refresh()
        {
            _soundLabel.text = _settings.SoundEnabled ? "ON" : "OFF";
            _soundButton.color = _settings.SoundEnabled ? Palette.Emerald : Palette.ConcreteDark;
            for (int i = 0; i < _qualityButtons.Count; i++) _qualityButtons[i].color = i == _settings.GraphicsQuality ? Palette.Cyan : Palette.NavyLight;
            _resetLabel.text = _confirmReset ? "TAP AGAIN TO ERASE ALL PROGRESS" : "RESET PROGRESS";
        }

        private void Row(string label, float y)
        {
            Text text = UIFactory.Text(label, Body, label, 36, Palette.White, TextAnchor.MiddleLeft, true);
            UIFactory.Place(text.rectTransform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, -y - 10f), new Vector2(600f, 90f));
        }
    }
}
