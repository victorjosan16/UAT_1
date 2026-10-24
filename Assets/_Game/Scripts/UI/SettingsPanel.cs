using UnityEngine;
using UnityEngine.UI;

namespace QuarryEmpire
{
    /// <summary>Butonul de pauză (dreapta sus) și panoul de setări: sunet, vibrații, resetare progres.</summary>
    public class SettingsPanel : MonoBehaviour
    {
        private GameObject _panel;
        private Text _soundLabel;
        private Text _vibrationLabel;
        private Text _resetLabel;
        private float _resetConfirmUntil;

        private void Awake()
        {
            UiTheme theme = UiRoot.Theme;
            Button open = UiFactory.CreateButton("PauseButton", transform, "II", theme.PanelColor, Open, 48);
            UiFactory.Place((RectTransform)open.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -50f), new Vector2(110f, 110f));

            Image dim = UiFactory.CreatePanel("SettingsDim", transform, new Color(0f, 0f, 0f, 0.7f));
            UiFactory.Stretch((RectTransform)dim.transform);
            _panel = dim.gameObject;

            Image box = UiFactory.CreatePanel("SettingsBox", dim.transform, theme.PanelColor);
            var boxRect = UiFactory.Place((RectTransform)box.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(760f, 900f));

            Text title = UiFactory.CreateText("Title", boxRect, "SETĂRI", 64, TextAnchor.MiddleCenter, theme.AccentColor);
            UiFactory.Place((RectTransform)title.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -40f), new Vector2(600f, 100f));

            _soundLabel = AddButton(boxRect, "Sound", -200f, ToggleSound, theme.AdColor);
            _vibrationLabel = AddButton(boxRect, "Vibration", -350f, ToggleVibration, theme.AdColor);
            _resetLabel = AddButton(boxRect, "Reset", -500f, ResetProgress, theme.NegativeColor);
            AddButton(boxRect, "Resume", -700f, Close, theme.PositiveColor).text = "CONTINUĂ";

            _panel.SetActive(false);
        }

        private static Text AddButton(RectTransform parent, string name, float y, System.Action onClick, Color color)
        {
            Button button = UiFactory.CreateButton(name, parent, "", color, onClick, 40);
            UiFactory.Place((RectTransform)button.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(600f, 120f));
            return UiFactory.GetButtonLabel(button);
        }

        private void Open()
        {
            if (GameManager.Instance == null || !GameManager.Instance.IsPlaying) return;
            _panel.SetActive(true);
            GameManager.Instance.SetPaused(true);
            Refresh();
        }

        private void Close()
        {
            _panel.SetActive(false);
            if (GameManager.Instance != null) GameManager.Instance.SetPaused(false);
        }

        private void Update()
        {
            if (_panel.activeSelf && _resetConfirmUntil > 0f && Time.unscaledTime > _resetConfirmUntil)
            {
                _resetConfirmUntil = 0f;
                Refresh();
            }
        }

        private void ToggleSound()
        {
            SaveData data = SaveSystem.Instance.Data;
            data.soundOn = !data.soundOn;
            SaveSystem.Instance.MarkDirty();
            Refresh();
        }

        private void ToggleVibration()
        {
            SaveData data = SaveSystem.Instance.Data;
            data.vibrationOn = !data.vibrationOn;
            SaveSystem.Instance.MarkDirty();
            if (data.vibrationOn) Haptics.Medium();
            Refresh();
        }

        private void ResetProgress()
        {
            // Prima apăsare cere confirmare; a doua (în 3 secunde) șterge progresul.
            if (_resetConfirmUntil <= 0f || Time.unscaledTime > _resetConfirmUntil)
            {
                _resetConfirmUntil = Time.unscaledTime + 3f;
                Refresh();
                return;
            }
            SaveSystem.Instance.ResetProgress();
        }

        private void Refresh()
        {
            if (SaveSystem.Instance == null) return;
            SaveData data = SaveSystem.Instance.Data;
            _soundLabel.text = data.soundOn ? "SUNET: PORNIT" : "SUNET: OPRIT";
            _vibrationLabel.text = data.vibrationOn ? "VIBRAȚII: PORNITE" : "VIBRAȚII: OPRITE";
            _resetLabel.text = _resetConfirmUntil > Time.unscaledTime ? "SIGUR? APASĂ IAR" : "RESETEAZĂ PROGRESUL";
        }
    }
}
