using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace QuarryEmpire
{
    /// <summary>
    /// Butonul de pauză (dreapta sus) și panoul de setări: sunet, muzică, vibrații, grafică, limbă, resetare progres.
    /// </summary>
    public class SettingsPanel : MonoBehaviour
    {
        private GameObject _panel;
        private UiLabel _soundLabel;
        private UiLabel _musicLabel;
        private UiLabel _vibrationLabel;
        private UiLabel _qualityLabel;
        private UiLabel _languageLabel;
        private UiLabel _resetLabel;
        private float _resetConfirmUntil;

        private void Awake()
        {
            UiTheme theme = UiRoot.Theme;
            Button open = UiFactory.CreateIconButton("PauseButton", transform, ProceduralSprites.Pause, theme.PanelColor, Open, 0.46f);
            UiFactory.Place((RectTransform)open.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-24f, -24f), new Vector2(104f, 104f));

            Image dim = UiFactory.CreateImage("SettingsDim", transform, UiFactory.WhiteSprite, new Color(0f, 0f, 0f, 0.6f));
            dim.preserveAspect = false;
            dim.raycastTarget = true;
            UiFactory.Stretch(dim.rectTransform, -200f); // acoperă și marginile din afara zonei sigure
            _panel = dim.gameObject;

            Image box = UiFactory.CreatePanel("SettingsBox", dim.transform, theme.PanelLightColor);
            var boxRect = UiFactory.Place((RectTransform)box.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(780f, 1300f));
            box.gameObject.AddComponent<CanvasGroup>();
            box.gameObject.AddComponent<PopIn>();

            UiLabel title = UiFactory.CreateText("Title", boxRect, Loc.T("PAUZĂ"), 72, TextAnchor.MiddleCenter, theme.TextDarkColor);
            UiFactory.Place(title.RectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -30f), new Vector2(600f, 100f));

            _soundLabel = AddButton(boxRect, "Sound", -150f, ToggleSound, theme.AdColor);
            _musicLabel = AddButton(boxRect, "Music", -285f, ToggleMusic, theme.AdColor);
            _vibrationLabel = AddButton(boxRect, "Vibration", -420f, ToggleVibration, theme.AdColor);
            _qualityLabel = AddButton(boxRect, "Quality", -555f, CycleQuality, theme.AdColor);
            _languageLabel = AddButton(boxRect, "Language", -690f, ToggleLanguage, theme.AdColor);
            _resetLabel = AddButton(boxRect, "Reset", -860f, ResetProgress, theme.NegativeColor);
            AddButton(boxRect, "Resume", -1100f, Close, theme.PositiveColor).Text = Loc.T("CONTINUĂ");

            _panel.SetActive(false);
        }

        private static UiLabel AddButton(RectTransform parent, string name, float y, System.Action onClick, Color color)
        {
            Button button = UiFactory.CreateButton(name, parent, "", color, onClick, 38);
            UiFactory.Place((RectTransform)button.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, y), new Vector2(620f, 116f));
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

        private void ToggleMusic()
        {
            SaveData data = SaveSystem.Instance.Data;
            data.musicOn = !data.musicOn;
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

        private void CycleQuality()
        {
            if (QualityProfile.Instance != null) QualityProfile.Instance.CycleLevel();
            Refresh();
        }

        private void ToggleLanguage()
        {
            Loc.Set(Loc.Current == Loc.Language.Romanian ? Loc.Language.English : Loc.Language.Romanian);
            // Interfața e construită o singură dată, la pornire: salvăm și repornim scena ca textele să se schimbe.
            SaveSystem.Instance.Save();
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
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
            _soundLabel.Text = Loc.T(data.soundOn ? "SUNET: PORNIT" : "SUNET: OPRIT");
            _musicLabel.Text = Loc.T(data.musicOn ? "MUZICĂ: PORNITĂ" : "MUZICĂ: OPRITĂ");
            _vibrationLabel.Text = Loc.T(data.vibrationOn ? "VIBRAȚII: PORNITE" : "VIBRAȚII: OPRITE");
            _qualityLabel.Text = Loc.F("GRAFICĂ: {0}", QualityProfile.Instance != null ? QualityProfile.Instance.LevelName : "");
            _languageLabel.Text = Loc.T("LIMBA: ROMÂNĂ");
            _resetLabel.Text = Loc.T(_resetConfirmUntil > Time.unscaledTime ? "SIGUR? APASĂ IAR" : "RESETEAZĂ PROGRESUL");
        }
    }
}
