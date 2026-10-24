using UnityEngine;
using UnityEngine.UI;

namespace QuarryEmpire
{
    /// <summary>Ecranul de start: titlul jocului și butonul JOACĂ, peste scena care se vede în fundal.</summary>
    public class MainMenu : MonoBehaviour
    {
        private GameObject _root;
        private RectTransform _title;
        private float _time;

        private void Awake()
        {
            UiTheme theme = UiRoot.Theme;
            Image background = UiFactory.CreateImage("MenuBackground", transform, UiFactory.WhiteSprite, new Color(0.07f, 0.09f, 0.16f, 0.72f));
            background.preserveAspect = false;
            background.raycastTarget = true; // blochează atingerile pe joc cât e meniul deschis
            UiFactory.Stretch(background.rectTransform);
            _root = background.gameObject;

            // Titlu cu „umbră” (două texte suprapuse, decalate).
            _title = UiFactory.CreateRect("Title", background.transform);
            UiFactory.Place(_title, new Vector2(0.5f, 0.68f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000f, 380f));
            UiLabel shadow = UiFactory.CreateText("Shadow", _title, "QUARRY\nEMPIRE", 150, TextAnchor.MiddleCenter, UiFactory.Darken(theme.AccentColor, 0.55f));
            UiFactory.Stretch(shadow.RectTransform);
            shadow.RectTransform.anchoredPosition = new Vector2(0f, -12f);
            UiLabel title = UiFactory.CreateText("Text", _title, "QUARRY\nEMPIRE", 150, TextAnchor.MiddleCenter, theme.AccentColor);
            UiFactory.Stretch(title.RectTransform);

            UiLabel subtitle = UiFactory.CreateText("Subtitle", background.transform, Loc.T("Sapă. Vinde. Construiește-ți imperiul."), 42);
            UiFactory.Place(subtitle.RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(1000f, 80f));

            Button play = UiFactory.CreateButton("Play", background.transform, Loc.T("JOACĂ"), theme.PositiveColor, OnPlay, 72);
            UiFactory.Place((RectTransform)play.transform, new Vector2(0.5f, 0.32f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(560f, 180f));

            UiLabel hint = UiFactory.CreateText("Hint", background.transform,
                Loc.T("Mișcă-te cu joystick-ul. Minerul lovește singur blocurile din jur."), 32, TextAnchor.MiddleCenter, new Color(1f, 1f, 1f, 0.75f));
            UiFactory.Place(hint.RectTransform, new Vector2(0.5f, 0.18f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(900f, 100f));

            UiLabel version = UiFactory.CreateText("Version", background.transform, "v" + Application.version, 26,
                TextAnchor.LowerRight, new Color(1f, 1f, 1f, 0.45f));
            UiFactory.Place(version.RectTransform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-30f, 60f), new Vector2(300f, 50f));
        }

        private void Update()
        {
            if (!_root.activeSelf) return;
            // Titlul „respiră” ușor.
            _time += Time.unscaledDeltaTime;
            _title.localScale = Vector3.one * (1f + Mathf.Sin(_time * 2f) * 0.02f);
        }

        private void OnPlay()
        {
            _root.SetActive(false);
            if (GameManager.Instance != null) GameManager.Instance.StartGame();
        }
    }
}
