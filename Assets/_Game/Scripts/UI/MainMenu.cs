using UnityEngine;
using UnityEngine.UI;

namespace QuarryEmpire
{
    /// <summary>Ecranul de start: titlul jocului și butonul JOACĂ.</summary>
    public class MainMenu : MonoBehaviour
    {
        private GameObject _root;

        private void Awake()
        {
            UiTheme theme = UiRoot.Theme;
            Image background = UiFactory.CreatePanel("MenuBackground", transform, new Color(0.08f, 0.09f, 0.12f, 0.9f));
            UiFactory.Stretch((RectTransform)background.transform);
            _root = background.gameObject;

            Text title = UiFactory.CreateText("Title", background.transform, "QUARRY\nEMPIRE", 130, TextAnchor.MiddleCenter, theme.AccentColor);
            UiFactory.Place((RectTransform)title.transform, new Vector2(0.5f, 0.68f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000f, 360f));

            Text subtitle = UiFactory.CreateText("Subtitle", background.transform, "Sapă. Vinde. Construiește-ți imperiul.", 40);
            UiFactory.Place((RectTransform)subtitle.transform, new Vector2(0.5f, 0.52f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000f, 80f));

            Button play = UiFactory.CreateButton("Play", background.transform, "JOACĂ", theme.PositiveColor, OnPlay, 64);
            UiFactory.Place((RectTransform)play.transform, new Vector2(0.5f, 0.32f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560f, 170f));

            Text hint = UiFactory.CreateText("Hint", background.transform,
                "Mișcă-te cu joystick-ul. Minerul lovește singur blocurile din jur.", 32, TextAnchor.MiddleCenter,
                new Color(1f, 1f, 1f, 0.7f), FontStyle.Normal);
            UiFactory.Place((RectTransform)hint.transform, new Vector2(0.5f, 0.18f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 100f));

            Text version = UiFactory.CreateText("Version", background.transform, "v" + Application.version, 26,
                TextAnchor.LowerRight, new Color(1f, 1f, 1f, 0.4f), FontStyle.Normal);
            UiFactory.Place((RectTransform)version.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-30f, 30f), new Vector2(300f, 50f));
        }

        private void OnPlay()
        {
            _root.SetActive(false);
            if (GameManager.Instance != null) GameManager.Instance.StartGame();
        }
    }
}
