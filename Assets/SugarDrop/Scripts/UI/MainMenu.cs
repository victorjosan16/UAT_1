using System;
using QuarryEmpire;
using UnityEngine;
using UnityEngine.UI;

namespace SugarDrop
{
    /// <summary>Ecranul de titlu: logo „Sugar Drop” colorat, bomboane care plutesc și butonul „Joacă”.</summary>
    public class MainMenu : MonoBehaviour
    {
        public event Action PlayClicked;

        private GameObject _root;
        private UiLabel _logo;
        private UiLabel _tagline;
        private UiLabel _playLabel;
        private RectTransform _playRect;
        private float _enterT = -1f;
        private RectTransform[] _floaters;
        private Vector2[] _floaterBase;

        public bool IsOpen => _root.activeSelf;

        private void Awake()
        {
            UiTheme theme = UiRoot.Theme;
            Image background = UiFactory.CreateImage("Background", transform, SugarSprites.BackgroundGradient, Color.white);
            background.preserveAspect = false;
            background.raycastTarget = true;
            UiFactory.Stretch(background.rectTransform, -300f);
            _root = background.gameObject;

            // Bomboane și capace care plutesc în fundal.
            _floaters = new RectTransform[12];
            _floaterBase = new Vector2[12];
            for (int i = 0; i < _floaters.Length; i++)
            {
                Sprite sprite = i % 2 == 0 ? SugarSprites.CandyVariant(i / 2) : SugarSprites.Cap;
                Color color = i % 2 == 0 ? Palette.RandomCandy() : Palette.Tile(i % Palette.TileColorCount);
                Image image = UiFactory.CreateImage("Floater", _root.transform, sprite, color);
                Vector2 position = new(UnityEngine.Random.Range(-460f, 460f), UnityEngine.Random.Range(-850f, 850f));
                float size = UnityEngine.Random.Range(70f, 150f);
                UiFactory.Place(image.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(size, size));
                image.color = new Color(color.r, color.g, color.b, 0.75f);
                _floaters[i] = image.rectTransform;
                _floaterBase[i] = position;
                if (i % 2 == 1)
                {
                    Image icon = UiFactory.CreateImage("Icon", image.transform, SugarSprites.Icon(i % Palette.TileColorCount), Palette.TileLight(i % Palette.TileColorCount));
                    UiFactory.Place(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size * 0.5f, size * 0.5f));
                }
            }

            // Sclipiri care plutesc peste fundal.
            RectTransform sparkleLayer = UiFactory.CreateRect("Sparkles", _root.transform);
            UiFactory.Stretch(sparkleLayer, 300f);
            UIParticleController sparkles = sparkleLayer.gameObject.AddComponent<UIParticleController>();
            sparkles.Initialize(30, SugarSprites.Sparkle, new[] { Color.white, new Color(1f, 0.9f, 0.6f), new Color(1f, 0.75f, 0.85f) }, 4f);
            sparkles.SetAmbientSize(16f, 36f);

            _logo = SugarUi.Outlined("Logo", _root.transform, SugarUi.Logo("Sugar Drop"), 170, Color.white, 7f);
            UiFactory.Place(_logo.RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 420f), new Vector2(1060f, 240f));
            _tagline = SugarUi.Outlined("Tagline", _root.transform, "", 44, Color.white, 3f);
            UiFactory.Place(_tagline.RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 250f), new Vector2(980f, 120f));

            // Bobo „pictat” în meniu: un cupcake mare.
            Image cup = UiFactory.CreateImage("Cup", _root.transform, SugarSprites.CupWrapper, Color.white);
            UiFactory.Place(cup.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -110f), new Vector2(340f, 340f));
            Image frosting = UiFactory.CreateImage("Frosting", _root.transform, SugarSprites.Frosting, Color.white);
            UiFactory.Place(frosting.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 20f), new Vector2(360f, 360f));
            Image cherry = UiFactory.CreateImage("Cherry", _root.transform, SugarSprites.Circle, new Color(0.93f, 0.2f, 0.32f));
            UiFactory.Place(cherry.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(10f, 175f), new Vector2(80f, 80f));
            Image smile = UiFactory.CreateImage("Smile", _root.transform, SugarSprites.Smile, new Color(0.35f, 0.12f, 0.22f));
            UiFactory.Place(smile.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -150f), new Vector2(110f, 110f));
            for (int e = -1; e <= 1; e += 2)
            {
                Image eye = UiFactory.CreateImage("Eye", _root.transform, SugarSprites.Circle, new Color(0.25f, 0.12f, 0.2f));
                UiFactory.Place(eye.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(e * 60f, -90f), new Vector2(40f, 40f));
            }

            Button play = SugarUi.Button("Play", _root.transform, "", theme.PositiveColor, () =>
            {
                SugarUi.Click();
                PlayClicked?.Invoke();
            }, 64);
            UiFactory.Place((RectTransform)play.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -480f), new Vector2(600f, 180f));
            _playLabel = UiFactory.GetButtonLabel(play);
            _playRect = (RectTransform)play.transform;

            _root.SetActive(false);
        }

        public void Show()
        {
            _tagline.Text = SugarLoc.T("Aliniază 3 capace la fel ca să eliberezi bomboanele!");
            _playLabel.Text = SugarLoc.T("JOACĂ");
            _root.SetActive(true);
            _enterT = 0f;
        }

        public void Hide() => _root.SetActive(false);

        private void Update()
        {
            if (!_root.activeSelf) return;
            float time = Time.unscaledTime;
            for (int i = 0; i < _floaters.Length; i++)
            {
                _floaters[i].anchoredPosition = _floaterBase[i] + new Vector2(Mathf.Sin(time * 0.7f + i) * 20f, Mathf.Sin(time * 0.9f + i * 1.7f) * 30f);
                _floaters[i].localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(time * 0.5f + i) * 20f);
            }
            _logo.RectTransform.localScale = Vector3.one * (1f + Mathf.Sin(time * 2f) * 0.03f);
            _logo.RectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(time * 1.3f) * 2f);

            // Intrarea: logo-ul cade de sus cu „boing”, butonul urcă de jos.
            if (_enterT >= 0f)
            {
                _enterT += Time.unscaledDeltaTime;
                float logoT = Mathf.Clamp01(_enterT / 0.6f);
                float playT = Mathf.Clamp01((_enterT - 0.25f) / 0.5f);
                _logo.RectTransform.anchoredPosition = new Vector2(0f, 420f + (1f - UiEase.OutBack(logoT, 2f)) * 600f);
                _playRect.anchoredPosition = new Vector2(0f, -480f - (1f - UiEase.OutBack(playT, 1.6f)) * 500f);
                if (_enterT > 0.8f) _enterT = -1f;
            }
        }
    }
}
