using System;
using System.Collections.Generic;
using QuarryEmpire;
using UnityEngine;
using UnityEngine.UI;

namespace SugarDrop
{
    /// <summary>
    /// Ecranul de titlu: cerul capitolului curent, ploaie de bomboane, logo-ul „Sugar Drop” colorat, Bobo care
    /// clipește, sare când îl atingi și vorbește (salut, cadoul zilei, misiunile), butonul „Joacă” care pulsează
    /// și strălucește, stelele și monedele, plus scurtături spre cadou, misiuni, clasament și setări.
    /// </summary>
    public class MainMenu : MonoBehaviour
    {
        public event Action PlayClicked;
        public event Action GiftClicked;
        public event Action MissionsClicked;
        public event Action LeaderboardClicked;
        public event Action SettingsClicked;

        private const int FloaterCount = 18;
        private const float RainTop = 1150f;
        private const float RainBottom = -1150f;

        private static readonly string[] TapLines = { "Hihi, gâdilă!", "Hai să jucăm!", "Ești cel mai tare!", "Bomboane, bomboane!", "Încă un nivel?" };

        private GameObject _root;
        private Image _background;
        private int _backgroundChapter = -1;
        private UiLabel _logo;
        private UiLabel _tagline;
        private UiLabel _chapter;
        private UiLabel _stars, _coins;
        private UiLabel _playLabel;
        private RectTransform _playRect;
        private RectTransform _shine;
        private RectTransform _bobo;
        private RectTransform[] _eyes;
        private RectTransform _bubble;
        private UiLabel _bubbleText;
        private GameObject _giftBadge, _missionsBadge;
        private readonly List<(RectTransform Rect, UiLabel Caption, string Key)> _quick = new();
        private UIParticleController _bursts;

        private RectTransform[] _floaters;
        private float[] _floaterSpeed;
        private float[] _floaterSpin;
        private float[] _floaterPhase;

        private float _enterT = -1f;
        private float _jump;
        private float _jumpVelocity;
        private float _nextBlink;
        private float _blinkT = -1f;
        private float _nextLine;
        private int _line;
        private float _bubblePop;
        private int _tapLine;

        public bool IsOpen => _root.activeSelf;

        private void Awake()
        {
            UiTheme theme = UiRoot.Theme;
            _background = UiFactory.CreateImage("Background", transform, SugarSprites.BackgroundGradient, Color.white);
            _background.preserveAspect = false;
            _background.raycastTarget = true;
            UiFactory.Stretch(_background.rectTransform, -300f);
            _root = _background.gameObject;

            BuildCandyRain();

            // Sclipiri care plutesc peste fundal (și exploziile când atingi Bobo).
            RectTransform sparkleLayer = UiFactory.CreateRect("Sparkles", _root.transform);
            UiFactory.Stretch(sparkleLayer, 300f);
            _bursts = sparkleLayer.gameObject.AddComponent<UIParticleController>();
            _bursts.Initialize(60, SugarSprites.Sparkle, new[] { Color.white, new Color(1f, 0.9f, 0.6f), new Color(1f, 0.75f, 0.85f) }, 4f);
            _bursts.SetAmbientSize(16f, 36f);

            _stars = Pill("StarsPill", ProceduralSprites.Star, theme.AccentColor, new Vector2(-250f, 770f), theme);
            _coins = Pill("CoinsPill", ProceduralSprites.Coin, Color.white, new Vector2(250f, 770f), theme);

            _logo = SugarUi.Outlined("Logo", _root.transform, SugarUi.Logo("Sugar Drop"), 170, Color.white, 7f);
            UiFactory.Place(_logo.RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 520f), new Vector2(1060f, 240f));
            _tagline = SugarUi.Outlined("Tagline", _root.transform, "", 42, Color.white, 3f);
            UiFactory.Place(_tagline.RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 370f), new Vector2(980f, 110f));

            Image chip = UiFactory.CreatePanel("ChapterChip", _root.transform, new Color(1f, 1f, 1f, 0.55f));
            UiFactory.Place((RectTransform)chip.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 270f), new Vector2(700f, 74f));
            _chapter = UiFactory.CreateText("Chapter", chip.transform, "", 32, TextAnchor.MiddleCenter, theme.TextDarkColor);
            UiFactory.Stretch(_chapter.RectTransform, 6f);

            BuildBobo();
            BuildBubble(theme);
            BuildPlayButton(theme);
            BuildQuickButtons(theme);

            _root.SetActive(false);
        }

        // ------------------------------------------------------------ Construire

        private UiLabel Pill(string name, Sprite icon, Color iconColor, Vector2 position, UiTheme theme)
        {
            Image pill = UiFactory.CreatePanel(name, _root.transform, theme.PanelColor);
            UiFactory.Place((RectTransform)pill.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(300f, 92f));
            Image image = UiFactory.CreateImage("Icon", pill.transform, icon, iconColor);
            UiFactory.Place(image.rectTransform, new Vector2(0f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(10f, 0f), new Vector2(96f, 96f));
            UiLabel label = UiFactory.CreateText("Count", pill.transform, "0", 44);
            UiFactory.Place(label.RectTransform, new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(70f, 0f), new Vector2(220f, 80f));
            return label;
        }

        /// <summary>Bomboane și capace care cad încet și se rotesc (cele mici cad mai încet: adâncime).</summary>
        private void BuildCandyRain()
        {
            _floaters = new RectTransform[FloaterCount];
            _floaterSpeed = new float[FloaterCount];
            _floaterSpin = new float[FloaterCount];
            _floaterPhase = new float[FloaterCount];
            for (int i = 0; i < FloaterCount; i++)
            {
                bool cap = i % 3 == 2;
                Sprite sprite = cap ? SugarSprites.Cap : SugarSprites.CandyVariant(i);
                Color color = cap ? Palette.Tile(i % Palette.TileColorCount) : Palette.RandomCandy();
                float size = UnityEngine.Random.Range(60f, 150f);
                Image image = UiFactory.CreateImage("Candy", _root.transform, sprite, new Color(color.r, color.g, color.b, Mathf.Lerp(0.45f, 0.85f, size / 150f)));
                UiFactory.Place(image.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(UnityEngine.Random.Range(-500f, 500f), UnityEngine.Random.Range(RainBottom, RainTop)), new Vector2(size, size));
                if (cap)
                {
                    int flavour = i % Palette.TileColorCount;
                    Image icon = UiFactory.CreateImage("Icon", image.transform, SugarSprites.Icon(flavour), Palette.TileLight(flavour));
                    UiFactory.Place(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size * 0.5f, size * 0.5f));
                }
                _floaters[i] = image.rectTransform;
                _floaterSpeed[i] = size * 0.9f + UnityEngine.Random.Range(0f, 40f);
                _floaterSpin[i] = UnityEngine.Random.Range(-40f, 40f);
                _floaterPhase[i] = UnityEngine.Random.value * 10f;
            }
        }

        /// <summary>Bobo, cupcake-ul: ochii se închid separat (clipește), iar tot corpul sare când îl atingi.</summary>
        private void BuildBobo()
        {
            _bobo = UiFactory.CreateRect("Bobo", _root.transform);
            UiFactory.Place(_bobo, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -40f), new Vector2(380f, 420f));
            Image shadow = UiFactory.CreateImage("Shadow", _bobo, SugarSprites.SoftDot, new Color(0.3f, 0.1f, 0.25f, 0.3f));
            UiFactory.Place(shadow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -200f), new Vector2(300f, 70f));
            shadow.preserveAspect = false;
            Image cup = UiFactory.CreateImage("Cup", _bobo, SugarSprites.CupWrapper, Color.white);
            UiFactory.Place(cup.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -80f), new Vector2(340f, 340f));
            Image frosting = UiFactory.CreateImage("Frosting", _bobo, SugarSprites.Frosting, Color.white);
            UiFactory.Place(frosting.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 50f), new Vector2(360f, 360f));
            Image cherry = UiFactory.CreateImage("Cherry", _bobo, SugarSprites.Circle, new Color(0.93f, 0.2f, 0.32f));
            UiFactory.Place(cherry.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(10f, 205f), new Vector2(80f, 80f));
            Image smile = UiFactory.CreateImage("Smile", _bobo, SugarSprites.Smile, new Color(0.35f, 0.12f, 0.22f));
            UiFactory.Place(smile.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -120f), new Vector2(110f, 110f));
            _eyes = new RectTransform[2];
            for (int e = 0; e < 2; e++)
            {
                Image eye = UiFactory.CreateImage("Eye", _bobo, SugarSprites.Circle, new Color(0.25f, 0.12f, 0.2f));
                _eyes[e] = UiFactory.Place(eye.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2((e * 2 - 1) * 60f, -60f), new Vector2(40f, 40f));
                Image glint = UiFactory.CreateImage("Glint", eye.transform, SugarSprites.Circle, Color.white);
                UiFactory.Place(glint.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-7f, 8f), new Vector2(12f, 12f));
            }
            for (int e = -1; e <= 1; e += 2)
            {
                Image cheek = UiFactory.CreateImage("Cheek", _bobo, SugarSprites.SoftDot, new Color(1f, 0.45f, 0.6f, 0.55f));
                UiFactory.Place(cheek.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(e * 105f, -105f), new Vector2(70f, 46f));
                cheek.preserveAspect = false;
            }

            // Toată silueta lui Bobo se poate atinge.
            var hit = _bobo.gameObject.AddComponent<Image>();
            hit.color = new Color(1f, 1f, 1f, 0f);
            hit.raycastTarget = true;
            var button = _bobo.gameObject.AddComponent<Button>();
            button.transition = Selectable.Transition.None;
            button.onClick.AddListener(PokeBobo);
        }

        private void BuildBubble(UiTheme theme)
        {
            Image bubble = UiFactory.CreatePanel("Bubble", _root.transform, Color.white);
            _bubble = UiFactory.Place((RectTransform)bubble.transform, new Vector2(0.5f, 0.5f), new Vector2(0f, 0f), new Vector2(165f, 70f), new Vector2(390f, 150f));
            Image tail = UiFactory.CreateImage("Tail", _bubble, UiFactory.WhiteSprite, Color.white);
            UiFactory.Place(tail.rectTransform, new Vector2(0f, 0f), new Vector2(0.5f, 0.5f), new Vector2(46f, 6f), new Vector2(36f, 36f));
            tail.rectTransform.localRotation = Quaternion.Euler(0f, 0f, 45f);
            _bubbleText = UiFactory.CreateText("Text", _bubble, "", 34, TextAnchor.MiddleCenter, theme.TextDarkColor);
            UiFactory.Stretch(_bubbleText.RectTransform, 14f);
        }

        private void BuildPlayButton(UiTheme theme)
        {
            Image glow = UiFactory.CreateImage("PlayGlow", _root.transform, SugarSprites.SoftDot, new Color(1f, 0.95f, 0.55f, 0.55f));
            UiFactory.Place(glow.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -420f), new Vector2(900f, 330f));
            glow.preserveAspect = false;
            Button play = SugarUi.Button("Play", _root.transform, "", theme.PositiveColor, () =>
            {
                SugarUi.Click();
                PlayClicked?.Invoke();
            }, 62);
            _playRect = UiFactory.Place((RectTransform)play.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -420f), new Vector2(640f, 190f));
            _playLabel = UiFactory.GetButtonLabel(play);

            // O dungă de lumină care trece peste buton, din când în când.
            Transform face = play.transform.Find("Face");
            face.gameObject.AddComponent<RectMask2D>();
            Image shine = UiFactory.CreateImage("Shine", face, UiFactory.WhiteSprite, new Color(1f, 1f, 1f, 0.35f));
            shine.preserveAspect = false;
            _shine = UiFactory.Place(shine.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-600f, 0f), new Vector2(70f, 320f));
            _shine.localRotation = Quaternion.Euler(0f, 0f, -20f);
            _playLabel.transform.SetAsLastSibling();
        }

        private void BuildQuickButtons(UiTheme theme)
        {
            _giftBadge = Quick("Gift", SugarSprites.Gift, Palette.Tile(0), -330f, "Cadou", () => GiftClicked?.Invoke(), true);
            _missionsBadge = Quick("Missions", SugarSprites.Checklist, Palette.Tile(1), -110f, "Misiuni", () => MissionsClicked?.Invoke(), true);
            Quick("Leaderboard", SugarSprites.Trophy, theme.AccentColor, 110f, "Clasament", () => LeaderboardClicked?.Invoke(), false);
            Quick("Settings", SugarSprites.Gear, theme.PanelColor, 330f, "Setări", () => SettingsClicked?.Invoke(), false);
        }

        private GameObject Quick(string name, Sprite icon, Color color, float x, string captionKey, Action onClick, bool badge)
        {
            Button button = SugarUi.IconButton(name, _root.transform, icon, color, () =>
            {
                SugarUi.Click();
                onClick();
            }, 0.6f);
            var rect = UiFactory.Place((RectTransform)button.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, -660f), new Vector2(136f, 136f));
            UiLabel caption = SugarUi.Outlined("Caption", _root.transform, "", 30, Color.white, 3f);
            UiFactory.Place(caption.RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(x, -762f), new Vector2(220f, 50f));
            _quick.Add((rect, caption, captionKey));
            if (!badge) return null;
            Image dot = UiFactory.CreateImage("Badge", button.transform, SugarSprites.Circle, UiRoot.Theme.NegativeColor);
            UiFactory.Place(dot.rectTransform, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-16f, -16f), new Vector2(40f, 40f));
            dot.gameObject.SetActive(false);
            return dot.gameObject;
        }

        // ------------------------------------------------------------ Afișare

        public void Show()
        {
            SugarSaveData data = SugarSave.Data;
            int level = Mathf.Max(1, data.level);
            int chapter = ChapterTheme.ChapterOf(level);
            if (chapter != _backgroundChapter)
            {
                _backgroundChapter = chapter;
                _background.sprite = SugarSprites.Sky(chapter);
            }
            _tagline.Text = SugarLoc.T("Aliniază 3 capace la fel ca să eliberezi bomboanele!");
            _chapter.Text = SugarLoc.F("CAPITOLUL {0}", chapter + 1) + "  ·  " + SugarLoc.T(ChapterTheme.For(chapter).Name);
            _playLabel.Text = SugarLoc.T("JOACĂ") + "\n<size=34>" + SugarLoc.F("NIVELUL {0}", level) + "</size>";
            foreach (var (_, caption, key) in _quick) caption.Text = SugarLoc.T(key);
            RefreshCounters();
            _line = 0;
            Say(BoboLine(0));
            _nextLine = Time.unscaledTime + 4.5f;
            _nextBlink = Time.unscaledTime + 2f;
            _root.SetActive(true);
            _enterT = 0f;
        }

        public void Hide() => _root.SetActive(false);

        /// <summary>Stelele, monedele și punctele roșii (după cadou, misiuni sau restaurarea din cloud).</summary>
        public void RefreshCounters()
        {
            SugarSaveData data = SugarSave.Data;
            _stars.Text = SugarSave.TotalStars(data).ToString();
            _coins.Text = data.coins.ToString();
            _giftBadge.SetActive(SugarDaily.GiftAvailable);
            _missionsBadge.SetActive(SugarDaily.AnyClaimable);
        }

        /// <summary>Ce spune Bobo, pe rând: salutul, apoi ce te așteaptă azi.</summary>
        private string BoboLine(int index)
        {
            var lines = new List<string>();
            string nickname = SugarSave.Data.nickname;
            lines.Add(string.IsNullOrEmpty(nickname) ? SugarLoc.T("Bine ai venit!") : SugarLoc.F("Salut, {0}!", nickname));
            if (SugarDaily.GiftAvailable) lines.Add(SugarLoc.T("Te așteaptă un cadou!"));
            if (SugarDaily.AnyClaimable) lines.Add(SugarLoc.T("Ai misiuni de revendicat!"));
            lines.Add(SugarLoc.F("Nivelul {0} te așteaptă!", Mathf.Max(1, SugarSave.Data.level)));
            lines.Add(SugarLoc.T("Atinge-mă!"));
            return lines[index % lines.Count];
        }

        private void Say(string text)
        {
            _bubbleText.Text = text;
            _bubblePop = 0f;
        }

        private void PokeBobo()
        {
            _jumpVelocity = 1400f;
            _blinkT = 0f;
            Say(SugarLoc.T(TapLines[_tapLine++ % TapLines.Length]));
            _nextLine = Time.unscaledTime + 4f;
            if (SugarAudio.Instance != null) SugarAudio.Instance.Play(SugarSound.SpecialCreated, 0.8f, UnityEngine.Random.Range(1.1f, 1.4f));
            SugarAudio.Buzz(0);
            _bursts.BurstAt(_bobo, 16, SugarSprites.Sparkle, new[] { Color.white, Palette.TileLight(0), Palette.TileLight(3) });
        }

        // ------------------------------------------------------------ Animații

        private void Update()
        {
            if (!_root.activeSelf) return;
            float time = Time.unscaledTime;
            float dt = Time.unscaledDeltaTime;

            // Ploaia de bomboane.
            for (int i = 0; i < _floaters.Length; i++)
            {
                Vector2 p = _floaters[i].anchoredPosition;
                p.y -= _floaterSpeed[i] * dt * 0.35f;
                p.x += Mathf.Sin(time * 0.8f + _floaterPhase[i]) * 18f * dt;
                if (p.y < RainBottom)
                {
                    p.y = RainTop;
                    p.x = UnityEngine.Random.Range(-500f, 500f);
                }
                _floaters[i].anchoredPosition = p;
                _floaters[i].localRotation = Quaternion.Euler(0f, 0f, _floaterSpin[i] * time + _floaterPhase[i] * 30f);
            }

            _logo.RectTransform.localScale = Vector3.one * (1f + Mathf.Sin(time * 2f) * 0.03f);
            _logo.RectTransform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(time * 1.3f) * 2f);

            // Bobo: respiră, sare când e atins, clipește din când în când.
            _jumpVelocity -= 4200f * dt;
            _jump = Mathf.Max(0f, _jump + _jumpVelocity * dt);
            if (_jump <= 0f) _jumpVelocity = 0f;
            float breathe = Mathf.Sin(time * 2.4f);
            _bobo.anchoredPosition = new Vector2(0f, -40f + breathe * 8f + _jump);
            float squash = _jump > 0f ? 0.04f : breathe * 0.025f;
            _bobo.localScale = new Vector3(1f + squash, 1f - squash, 1f);
            _bobo.localRotation = Quaternion.Euler(0f, 0f, _jump > 0f ? Mathf.Sin(_jump * 0.03f) * 8f : Mathf.Sin(time * 1.1f) * 2f);
            if (_blinkT < 0f && time >= _nextBlink) _blinkT = 0f;
            float eyeY = 1f;
            if (_blinkT >= 0f)
            {
                _blinkT += dt;
                eyeY = Mathf.Abs(Mathf.Cos(Mathf.Clamp01(_blinkT / 0.16f) * Mathf.PI)) * 0.9f + 0.1f;
                if (_blinkT >= 0.16f)
                {
                    _blinkT = -1f;
                    _nextBlink = time + UnityEngine.Random.Range(2f, 4.5f);
                }
            }
            foreach (RectTransform eye in _eyes) eye.localScale = new Vector3(1f, eyeY, 1f);

            // Bula de vorbă: apare cu „pop” și își schimbă replica la câteva secunde.
            if (time >= _nextLine)
            {
                _nextLine = time + 4.5f;
                Say(BoboLine(++_line));
            }
            _bubblePop = Mathf.Min(1f, _bubblePop + dt / 0.3f);
            _bubble.localScale = Vector3.one * UiEase.OutBack(_bubblePop, 2.2f);

            // Butonul „Joacă”: pulsează, iar o dungă de lumină trece peste el.
            float pulse = 1f + Mathf.Sin(time * 3.2f) * 0.035f;
            _shine.anchoredPosition = new Vector2(Mathf.Lerp(-600f, 600f, Mathf.Repeat(time / 2.6f, 1f) * 1.6f - 0.3f), 0f);

            // Intrarea: logo-ul cade de sus, Bobo răsare, butoanele urcă pe rând.
            if (_enterT >= 0f)
            {
                _enterT += dt;
                float logoT = Mathf.Clamp01(_enterT / 0.6f);
                float playT = Mathf.Clamp01((_enterT - 0.25f) / 0.5f);
                _logo.RectTransform.anchoredPosition = new Vector2(0f, 520f + (1f - UiEase.OutBack(logoT, 2f)) * 600f);
                _playRect.anchoredPosition = new Vector2(0f, -420f - (1f - UiEase.OutBack(playT, 1.6f)) * 500f);
                for (int i = 0; i < _quick.Count; i++)
                {
                    float t = Mathf.Clamp01((_enterT - 0.4f - i * 0.07f) / 0.35f);
                    _quick[i].Rect.localScale = Vector3.one * UiEase.OutBack(t, 2f);
                }
                if (_enterT > 1.1f) _enterT = -1f;
            }
            _playRect.localScale = Vector3.one * pulse;
        }
    }
}
