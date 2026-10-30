using System;
using QuarryEmpire;
using UnityEngine;
using UnityEngine.UI;

namespace SugarDrop
{
    /// <summary>
    /// „Capitolul terminat!”: trei stele mari care se aprind pe rând, panglica de lemn cu titlul, recompensele ca plăcuțe
    /// cu iconiță (monede, rachetă la start, ajutor), pancarta cu ținutul următor, Bobo și un singur buton mare.
    /// Folosește aceleași componente ca restul ferestrelor (SugarUi.Box / Ribbon / Chip) și animatorul de stele al
    /// ecranului de victorie. Nu schimbă recompensele: doar le arată.
    /// </summary>
    public class ChapterCompletePanel : MonoBehaviour
    {
        private const float Width = 840f;
        private const float Height = 1060f;
        private static readonly Color ChipGold = new(1f, 0.93f, 0.7f);

        private GameObject _root;
        private RectTransform _box;
        private UiLabel _title;
        private UiLabel _summary;
        private readonly Image[] _stars = new Image[3];
        private UiLabel _coinsLabel;
        private RectTransform _rocketChip, _boosterChip;
        private UiLabel _rocketLabel, _boosterLabel;
        private Image _banner;
        private Image _ornamentLeft, _ornamentRight;
        private UiLabel _nextCaption, _nextName;
        private Image _bobo;
        private Button _ok;
        private UiLabel _okLabel;
        private LevelCompleteAnimator _animator;
        private Action _onClose;
        private float _lockedUntil;

        public bool IsOpen => _root != null && _root.activeSelf;

        private void Awake()
        {
            UiTheme theme = UiRoot.Theme;
            _root = SugarUi.Dim(transform).gameObject;
            _box = SugarUi.Box(_root.transform, new Vector2(Width, Height));
            var holder = (RectTransform)_box.parent;

            // Stelele mari deasupra cutiei (cea din mijloc mai sus), ca pe ecranul de victorie.
            for (int i = 0; i < 3; i++)
            {
                _stars[i] = UiFactory.CreateImage("Star" + i, _box, ProceduralSprites.Star, Color.white);
                _stars[i].raycastTarget = false;
                float size = i == 1 ? 230f : 180f;
                UiFactory.Place(_stars[i].rectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), new Vector2((i - 1) * 200f, i == 1 ? 78f : 26f), new Vector2(size, size));
            }

            _title = SugarUi.Ribbon(_box, "Ribbon", -120f, Width + 70f, 126f, 62f);

            _summary = UiFactory.CreateText("Summary", _box, "", 44, TextAnchor.MiddleCenter, QuestStyle.Text);
            UiFactory.Place(_summary.RectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -268f), new Vector2(Width - 100f, 60f));
            _summary.Fit(26f, 44f);

            // Recompensele: monedele pe un rând, apoi rachetă și ajutor alături.
            Sprite coin = ProceduralSprites.Coin;
            RectTransform coinChip = SugarUi.Chip(_box, "CoinsChip", coin, "", new Vector2(420f, 104f), out _coinsLabel);
            UiFactory.Place(coinChip, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -346f), new Vector2(420f, 104f));
            _coinsLabel.RectTransform.GetComponentInParent<Image>().color = ChipGold;

            _rocketChip = SugarUi.Chip(_box, "RocketChip", SugarSprites.Rocket, "", new Vector2(370f, 104f), out _rocketLabel);
            UiFactory.Place(_rocketChip, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(-196f, -470f), new Vector2(370f, 104f));
            _boosterChip = SugarUi.Chip(_box, "BoosterChip", SugarSprites.Timer, "", new Vector2(370f, 104f), out _boosterLabel);
            UiFactory.Place(_boosterChip, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(196f, -470f), new Vector2(370f, 104f));

            // Pancarta ținutului următor: cerul lui, ornamente în colțuri, numele cu contur.
            RectTransform bannerHolder = UiFactory.CreateRect("NextBanner", _box);
            UiFactory.Place(bannerHolder, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -610f), new Vector2(Width - 120f, 250f));
            Image bannerEdge = UiFactory.CreatePanel("Edge", bannerHolder, QuestStyle.SurfaceTint);
            UiFactory.Stretch((RectTransform)bannerEdge.transform);
            _banner = UiFactory.CreateImage("Sky", bannerHolder, SugarSprites.Sky(0), Color.white);
            _banner.preserveAspect = false;
            _banner.raycastTarget = false;
            UiFactory.Stretch(_banner.rectTransform, 8f);
            _ornamentLeft = UiFactory.CreateImage("OrnamentLeft", bannerHolder, SugarSprites.Leaf, Color.white);
            UiFactory.Place(_ornamentLeft.rectTransform, new Vector2(0f, 1f), new Vector2(0.5f, 0.5f), new Vector2(62f, -50f), new Vector2(96f, 96f));
            _ornamentRight = UiFactory.CreateImage("OrnamentRight", bannerHolder, SugarSprites.Leaf, Color.white);
            UiFactory.Place(_ornamentRight.rectTransform, new Vector2(1f, 1f), new Vector2(0.5f, 0.5f), new Vector2(-62f, -50f), new Vector2(96f, 96f));
            _ornamentRight.rectTransform.localScale = new Vector3(-1f, 1f, 1f);
            _ornamentLeft.raycastTarget = _ornamentRight.raycastTarget = false;
            _nextCaption = SugarUi.Outlined("NextCaption", bannerHolder, "", 34, Color.white, 3f);
            UiFactory.Place(_nextCaption.RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(60f, 34f), new Vector2(Width - 320f, 50f));
            _nextName = SugarUi.Outlined("NextName", bannerHolder, "", 56, Color.white, 4f);
            UiFactory.Place(_nextName.RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(60f, -30f), new Vector2(Width - 320f, 80f));
            _nextName.Fit(30f, 56f);

            if (BoboArt.Available) _bobo = BoboArt.AddTo(_box, 280f, new Vector2(-Width * 0.5f + 56f, 36f), BoboPose.Star);

            _ok = SugarUi.Button("Ok", _box, "", theme.PositiveColor, Press, 56);
            _okLabel = UiFactory.GetButtonLabel(_ok);
            UiFactory.Place((RectTransform)_ok.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(110f, 54f), new Vector2(430f, 140f));

            UIParticleController particles = UiFactory.CreateRect("Sparkles", _box).gameObject.AddComponent<UIParticleController>();
            UiFactory.Stretch((RectTransform)particles.transform);
            particles.Initialize(48, SugarSprites.Sparkle, null, 0f);
            _animator = holder.gameObject.AddComponent<LevelCompleteAnimator>();
            _animator.Bind(holder, _stars, _coinsLabel, particles, QuestStyle.Gold, new Color(0.86f, 0.8f, 0.7f));

            _root.SetActive(false);
        }

        /// <param name="chapter">Capitolul terminat (0 = primul).</param>
        public void Show(int chapter, int stars, int maxStars, int coins, bool boosters, Action onClose)
        {
            _onClose = onClose;
            _title.Text = SugarLoc.F("Capitolul {0} terminat!", chapter + 1);
            _summary.Text = SugarLoc.F("Stele: {0}/30", stars);
            _coinsLabel.Text = "+" + coins;
            _rocketLabel.Text = SugarLoc.T("+1 rachetă la start");
            _boosterChip.gameObject.SetActive(boosters);
            _boosterLabel.Text = SugarLoc.T("+1 la fiecare ajutor");
            // Cu ajutor: două plăcuți alături; fără: rachetă pe mijloc.
            UiFactory.Place(_rocketChip, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(boosters ? -196f : 0f, -470f), new Vector2(370f, 104f));

            ChapterTheme next = ChapterTheme.For(chapter + 1);
            int themeIndex = ChapterTheme.IndexOf(chapter + 1);
            _banner.sprite = SugarSprites.Sky(chapter + 1);
            Sprite ornament = LandStyle.Ornament(themeIndex);
            Color ornamentColor = LandStyle.OrnamentColor(themeIndex);
            _ornamentLeft.sprite = _ornamentRight.sprite = ornament;
            _ornamentLeft.color = _ornamentRight.color = ornamentColor;
            _nextCaption.Text = SugarLoc.F("Urmează: {0}", "").Trim();
            _nextName.Text = SugarLoc.T(next.Name);
            _okLabel.Text = SugarLoc.T("SUPER!");

            _lockedUntil = Time.unscaledTime + 0.5f;
            _root.SetActive(true);
            // Câte stele se aprind: după cât din cele 30 de stele ale capitolului are jucătorul.
            int lit = maxStars <= 0 ? 3 : stars >= maxStars * 0.9f ? 3 : stars >= maxStars * 0.6f ? 2 : 1;
            _animator.PlayWin(lit, coins, "+{0}");
        }

        private void Press()
        {
            if (Time.unscaledTime < _lockedUntil) return;
            SugarUi.Click();
            Hide();
            Action close = _onClose;
            _onClose = null;
            close?.Invoke();
        }

        public void Hide()
        {
            if (_root != null) _root.SetActive(false);
            if (_animator != null) _animator.Stop();
        }

        /// <summary>Butonul „Înapoi” al telefonului închide fereastra ca butonul principal.</summary>
        public void Dismiss() => Press();
    }
}
