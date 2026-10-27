using System;
using System.Collections.Generic;
using QuarryEmpire;
using UnityEngine;
using UnityEngine.UI;

namespace SugarDrop
{
    /// <summary>
    /// Cartea de povești de la pornire. Prima dată: povestea Tărâmului Dulce în patru pagini ilustrate și animate
    /// (textul apare literă cu literă, paginile se întorc ca într-o carte). La revenire: o singură pagină,
    /// „Bine ai revenit!”, cu Bobo bucuros și nivelul care te așteaptă.
    /// </summary>
    public class StoryPanel : MonoBehaviour
    {
        private const float BookWidth = 960f;
        private const float BookHeight = 1480f;
        private const float SceneHeight = 800f;
        private const float LettersPerSecond = 38f;
        private const float FlipSeconds = 0.32f;
        private const float WelcomeBackSeconds = 4.5f;

        private static readonly Color Paper = new(1f, 0.96f, 0.88f);
        private static readonly Color Ink = new(0.36f, 0.18f, 0.24f);

        private struct Page
        {
            public string Title;
            public string Text;
            public Func<RectTransform, Action<float>> Build;
        }

        private GameObject _root;
        private CanvasGroup _group;
        private RectTransform _book;
        private RectTransform _scene;
        private Image _sky;
        private UiLabel _title;
        private UiLabel _text;
        private UiLabel _hint;
        private UiLabel _skipLabel;
        private GameObject _skip;
        private readonly List<Image> _dots = new();
        private readonly List<RectTransform> _stars = new();

        private readonly List<Page> _pages = new();
        private int _page;
        private string _fullText;
        private float _typed;
        private float _pageTime;
        private Action<float> _animate;
        private Action _onDone;
        private float _flip = -1f;
        private bool _flipSwapped;
        private float _open;
        private bool _welcomeBack;

        public bool IsOpen => _root != null && _root.activeSelf;

        private void Awake()
        {
            UiTheme theme = UiRoot.Theme;
            _root = UiFactory.CreateRect("Story", transform).gameObject;
            UiFactory.Stretch((RectTransform)_root.transform);
            _group = _root.AddComponent<CanvasGroup>();

            // Noaptea din jurul cărții, cu stele care sclipesc.
            Image night = UiFactory.CreateImage("Night", _root.transform, UiFactory.WhiteSprite, new Color(0.16f, 0.07f, 0.2f, 0.92f));
            night.preserveAspect = false;
            night.raycastTarget = true;
            UiFactory.Stretch(night.rectTransform, -300f);
            var tap = night.gameObject.AddComponent<Button>();
            tap.transition = Selectable.Transition.None;
            tap.onClick.AddListener(Advance);
            for (int i = 0; i < 26; i++)
            {
                Image star = UiFactory.CreateImage("Star", _root.transform, SugarSprites.Sparkle, new Color(1f, 0.95f, 0.75f, 0.8f));
                float size = UnityEngine.Random.Range(18f, 42f);
                _stars.Add(UiFactory.Place(star.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                    new Vector2(UnityEngine.Random.Range(-540f, 540f), UnityEngine.Random.Range(-1250f, 1250f)), new Vector2(size, size)));
            }

            // Cartea: copertă roz, pagina crem, ilustrația sus și textul jos.
            Image cover = UiFactory.CreatePanel("Cover", _root.transform, Palette.WallStripe);
            _book = UiFactory.Place((RectTransform)cover.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -20f), new Vector2(BookWidth + 36f, BookHeight + 36f));
            Image paper = UiFactory.CreatePanel("Paper", _book, Paper);
            UiFactory.Place((RectTransform)paper.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(BookWidth, BookHeight));
            for (int i = 0; i < 4; i++)
            {
                Image corner = UiFactory.CreateImage("Corner", _book, SugarSprites.Heart, new Color(1f, 0.5f, 0.65f));
                Vector2 anchor = new(i % 2, i / 2);
                UiFactory.Place(corner.rectTransform, anchor, new Vector2(0.5f, 0.5f), new Vector2(anchor.x < 0.5f ? 30f : -30f, anchor.y < 0.5f ? 30f : -30f), new Vector2(54f, 54f));
            }

            RectTransform window = UiFactory.CreateRect("Window", _book);
            UiFactory.Place(window, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -58f), new Vector2(BookWidth - 80f, SceneHeight));
            window.gameObject.AddComponent<RectMask2D>();
            _sky = UiFactory.CreateImage("Sky", window, SugarSprites.Sky(0), Color.white);
            _sky.preserveAspect = false;
            UiFactory.Stretch(_sky.rectTransform);
            _scene = UiFactory.CreateRect("Scene", window);
            UiFactory.Stretch(_scene);
            Image frame = UiFactory.CreatePanel("Frame", _book, new Color(0.85f, 0.55f, 0.65f, 0.35f));
            UiFactory.Place((RectTransform)frame.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -58f + 6f), new Vector2(BookWidth - 68f, 12f));

            _title = SugarUi.Outlined("Title", _book, "", 60, theme.AccentColor, 4f);
            UiFactory.Place(_title.RectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -SceneHeight - 90f), new Vector2(BookWidth - 80f, 90f));
            _text = UiFactory.CreateText("Text", _book, "", 44, TextAnchor.UpperCenter, Ink);
            UiFactory.Place(_text.RectTransform, new Vector2(0.5f, 1f), new Vector2(0.5f, 1f), new Vector2(0f, -SceneHeight - 150f), new Vector2(BookWidth - 110f, 330f));
            _hint = UiFactory.CreateText("Hint", _book, "", 30, TextAnchor.MiddleCenter, Color.Lerp(Ink, Paper, 0.45f));
            UiFactory.Place(_hint.RectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 76f), new Vector2(BookWidth - 80f, 44f));
            for (int i = 0; i < 4; i++)
            {
                Image dot = UiFactory.CreateImage("Dot", _book, SugarSprites.Circle, Color.white);
                UiFactory.Place(dot.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0.5f), new Vector2((i - 1.5f) * 44f, 40f), new Vector2(22f, 22f));
                _dots.Add(dot);
            }

            Button skip = SugarUi.Button("Skip", _root.transform, "", new Color(1f, 1f, 1f, 0.25f), Skip, 34);
            UiFactory.Place((RectTransform)skip.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -40f), new Vector2(200f, 86f));
            _skip = skip.gameObject;
            _skipLabel = UiFactory.GetButtonLabel(skip);

            _root.SetActive(false);
        }

        // ------------------------------------------------------------ Afișare

        /// <summary>Povestea de la prima pornire; la final (sau la „Sari”) se cheamă onDone.</summary>
        public void ShowStory(Action onDone)
        {
            _welcomeBack = false;
            _pages.Clear();
            _pages.Add(new Page { Title = "A fost odată...", Text = "A fost odată un Tărâm Dulce, unde copacii purtau acadele, iar râurile curgeau cu sirop de căpșuni.", Build = BuildLand });
            _pages.Add(new Page { Title = "Vrăjitorul Acrișor", Text = "Dar într-o noapte, Vrăjitorul Acrișor a furat Borcanul Fermecat și a împrăștiat bomboanele prin toate cele zece ținuturi.", Build = BuildWizard });
            _pages.Add(new Page { Title = "Bobo, eroul dulce", Text = "Atunci, un cupcake mic și curajos, pe nume Bobo, a pornit la drum să le adune înapoi...", Build = BuildJourney });
            _pages.Add(new Page { Title = "Povestea ta începe", Text = "...dar nu poate reuși singur. Vrei să fii eroul care îl ajută?", Build = BuildInvitation });
            Open(onDone);
        }

        /// <summary>Pagina „Bine ai revenit”: se închide singură după câteva secunde sau la atingere.</summary>
        /// <param name="away">Cât a lipsit jucătorul (de la o zi în sus, Bobo spune că i-a fost dor).</param>
        public void ShowWelcomeBack(string nickname, int level, TimeSpan away, Action onDone)
        {
            _welcomeBack = true;
            _pages.Clear();
            int chapter = ChapterTheme.ChapterOf(Mathf.Max(1, level));
            string line = (away.TotalHours >= 24 ? SugarLoc.T("Ne-a fost dor de tine!") + "\n" : "")
                + SugarLoc.T(ChapterTheme.For(chapter).Name) + "\n" + SugarLoc.F("Nivelul {0} te așteaptă!", Mathf.Max(1, level));
            _pages.Add(new Page
            {
                Title = SugarLoc.F("Bine ai revenit, {0}!", nickname),
                Text = line,
                Build = scene =>
                {
                    _sky.sprite = SugarSprites.Sky(chapter);
                    return BuildReunion(scene, chapter);
                },
            });
            Open(onDone);
        }

        private void Open(Action onDone)
        {
            _onDone = onDone;
            _page = 0;
            _open = 0f;
            _flip = -1f;
            _skip.SetActive(!_welcomeBack);
            _skipLabel.Text = SugarLoc.T("SARI") + " ›";
            foreach (Image dot in _dots) dot.gameObject.SetActive(!_welcomeBack);
            _root.SetActive(true);
            ShowPage();
        }

        /// <summary>Butonul „Înapoi” al telefonului: sare peste poveste.</summary>
        public void Skip()
        {
            if (!IsOpen) return;
            SugarUi.Click();
            Close();
        }

        private void Close()
        {
            _root.SetActive(false);
            ClearScene();
            Action done = _onDone;
            _onDone = null;
            done?.Invoke();
        }

        /// <summary>Atingere: întâi termină textul, apoi întoarce pagina (sau închide cartea).</summary>
        private void Advance()
        {
            if (_flip >= 0f || _open < 0.6f) return;
            if (_typed < _fullText.Length)
            {
                _typed = _fullText.Length;
                return;
            }
            if (_page >= _pages.Count - 1)
            {
                SugarUi.Click();
                Close();
                return;
            }
            if (SugarAudio.Instance != null) SugarAudio.Instance.Play(SugarSound.Swap, 0.7f, 1.2f);
            _flip = 0f;
            _flipSwapped = false;
        }

        private void ShowPage()
        {
            ClearScene();
            Page page = _pages[_page];
            _sky.color = Color.white;
            _sky.sprite = SugarSprites.Sky(0);
            _animate = page.Build(_scene);
            _title.Text = _welcomeBack ? page.Title : SugarLoc.T(page.Title);
            _fullText = _welcomeBack ? page.Text : SugarLoc.T(page.Text);
            _typed = _welcomeBack ? _fullText.Length : 0f;
            _text.Text = _welcomeBack ? _fullText : "";
            _pageTime = 0f;
            for (int i = 0; i < _dots.Count; i++)
                _dots[i].color = i == _page ? UiRoot.Theme.AccentColor : new Color(0.85f, 0.78f, 0.8f);
        }

        private void ClearScene()
        {
            for (int i = _scene.childCount - 1; i >= 0; i--) Destroy(_scene.GetChild(i).gameObject);
            _animate = null;
        }

        private void Update()
        {
            if (!_root.activeSelf) return;
            float dt = Time.unscaledDeltaTime;
            float time = Time.unscaledTime;

            // Cartea se deschide: apare din adânc și se îndreaptă.
            _open = Mathf.Min(1f, _open + dt / 0.6f);
            _group.alpha = Mathf.Clamp01(_open * 2f);
            float scale = Mathf.Lerp(0.7f, 1f, UiEase.OutBack(_open, 1.6f));

            // Întoarcerea paginii: cartea se strânge pe orizontală, se schimbă pagina, apoi se deschide la loc.
            float flipX = 1f;
            if (_flip >= 0f)
            {
                _flip += dt / FlipSeconds;
                if (_flip >= 1f && !_flipSwapped)
                {
                    _flipSwapped = true;
                    _page++;
                    ShowPage();
                }
                flipX = _flip < 1f ? Mathf.Cos(_flip * Mathf.PI * 0.5f) : Mathf.Sin(Mathf.Min(1f, _flip - 1f) * Mathf.PI * 0.5f);
                if (_flip >= 2f) _flip = -1f;
            }
            _book.localScale = new Vector3(scale * Mathf.Max(0.02f, flipX), scale, 1f);
            _book.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(time * 0.7f) * 0.6f);

            for (int i = 0; i < _stars.Count; i++)
            {
                float twinkle = 0.55f + 0.45f * Mathf.Sin(time * (1.3f + i * 0.07f) + i);
                _stars[i].localScale = Vector3.one * twinkle;
                _stars[i].localRotation = Quaternion.Euler(0f, 0f, time * 20f + i * 13f);
            }

            if (_flip < 0f)
            {
                _pageTime += dt;
                if (_typed < _fullText.Length)
                {
                    int before = (int)_typed;
                    _typed = Mathf.Min(_fullText.Length, _typed + dt * LettersPerSecond);
                    if ((int)_typed != before) _text.Text = _fullText.Substring(0, (int)_typed);
                }
                else if (_text.Text.Length != _fullText.Length) _text.Text = _fullText;
            }
            _animate?.Invoke(_pageTime);

            bool done = _typed >= _fullText.Length;
            _hint.Text = done ? SugarLoc.T("Atinge pentru a continua") : "";
            _hint.Color = new Color(_hint.Color.r, _hint.Color.g, _hint.Color.b, 0.45f + 0.4f * Mathf.Abs(Mathf.Sin(time * 2.5f)));

            if (_welcomeBack && _pageTime >= WelcomeBackSeconds && _flip < 0f) Close();
        }

        // ------------------------------------------------------------ Ilustrațiile paginilor

        private static RectTransform Actor(RectTransform scene, Sprite sprite, Color color, Vector2 position, float size)
        {
            Image image = UiFactory.CreateImage("Actor", scene, sprite, color);
            return UiFactory.Place(image.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position, new Vector2(size, size));
        }

        private static RectTransform Block(RectTransform scene, Color color, Vector2 position, Vector2 size, Vector2 pivot)
        {
            Image image = UiFactory.CreatePanel("Block", scene, color);
            return UiFactory.Place(image.rectTransform, new Vector2(0.5f, 0.5f), pivot, position, size);
        }

        /// <summary>Particule care urcă sau cad în buclă prin scenă (inimioare, sclipiri, confetti).</summary>
        private static Action<float> Floaters(RectTransform scene, Sprite sprite, Color[] colors, int count, float speed, float size)
        {
            var items = new RectTransform[count];
            var phases = new float[count];
            for (int i = 0; i < count; i++)
            {
                phases[i] = UnityEngine.Random.value;
                items[i] = Actor(scene, sprite, colors[i % colors.Length], Vector2.zero, size * UnityEngine.Random.Range(0.6f, 1.2f));
            }
            return t =>
            {
                for (int i = 0; i < count; i++)
                {
                    float k = Mathf.Repeat(phases[i] + t * Mathf.Abs(speed) * 0.12f, 1f);
                    float y = speed > 0f ? Mathf.Lerp(-SceneHeight * 0.55f, SceneHeight * 0.55f, k) : Mathf.Lerp(SceneHeight * 0.55f, -SceneHeight * 0.55f, k);
                    float x = (phases[i] - 0.5f) * 820f + Mathf.Sin(t * 1.5f + i) * 30f;
                    items[i].anchoredPosition = new Vector2(x, y);
                    items[i].localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 2f + i) * 25f);
                }
            };
        }

        /// <summary>Pagina 1: soarele, dealuri, copaci-acadea care se leagănă și inimioare care urcă.</summary>
        private Action<float> BuildLand(RectTransform scene)
        {
            _sky.sprite = SugarSprites.Sky(0);
            RectTransform sun = Actor(scene, SugarSprites.Circle, new Color(1f, 0.88f, 0.4f), new Vector2(280f, 250f), 150f);
            RectTransform rays = Actor(scene, SugarSprites.Sparkle, new Color(1f, 0.95f, 0.6f, 0.8f), new Vector2(280f, 250f), 260f);
            rays.SetSiblingIndex(0);
            Block(scene, new Color(0.62f, 0.86f, 0.55f), new Vector2(-250f, -380f), new Vector2(700f, 260f), new Vector2(0.5f, 0.5f));
            Block(scene, new Color(1f, 0.72f, 0.8f), new Vector2(260f, -400f), new Vector2(760f, 260f), new Vector2(0.5f, 0.5f));
            var trees = new List<RectTransform>();
            Color[] crowns = { Palette.Tile(0), Palette.Tile(3), Palette.Tile(4), Palette.Tile(2) };
            for (int i = 0; i < 4; i++)
            {
                float x = -330f + i * 220f;
                RectTransform stick = Block(scene, Color.white, new Vector2(x, -330f), new Vector2(22f, 230f), new Vector2(0.5f, 0f));
                RectTransform crown = Actor(stick, SugarSprites.Circle, crowns[i], new Vector2(0f, 0f), 150f);
                crown.anchorMin = crown.anchorMax = new Vector2(0.5f, 1f);
                crown.anchoredPosition = new Vector2(0f, 40f);
                RectTransform swirl = Actor(crown, SugarSprites.CandySwirl, new Color(1f, 1f, 1f, 0.75f), Vector2.zero, 120f);
                trees.Add(stick);
                trees.Add(swirl);
            }
            Action<float> hearts = Floaters(scene, SugarSprites.Heart, new[] { new Color(1f, 0.5f, 0.65f), Color.white }, 8, 0.8f, 40f);
            return t =>
            {
                sun.localScale = Vector3.one * (1f + Mathf.Sin(t * 2f) * 0.05f);
                rays.localRotation = Quaternion.Euler(0f, 0f, t * 25f);
                for (int i = 0; i < trees.Count; i += 2)
                {
                    trees[i].localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 1.4f + i) * 6f);
                    trees[i + 1].localRotation = Quaternion.Euler(0f, 0f, t * 60f);
                }
                hearts(t);
            };
        }

        /// <summary>Pagina 2: noapte, fulgere, vrăjitorul coboară și bomboanele zboară din borcan.</summary>
        private Action<float> BuildWizard(RectTransform scene)
        {
            _sky.sprite = SugarSprites.Sky(9);
            _sky.color = new Color(0.35f, 0.28f, 0.55f);
            Action<float> stars = Floaters(scene, SugarSprites.Sparkle, new[] { new Color(1f, 0.95f, 0.7f) }, 10, 0.15f, 26f);
            RectTransform jar = Block(scene, new Color(0.85f, 0.95f, 1f, 0.75f), new Vector2(0f, -250f), new Vector2(210f, 250f), new Vector2(0.5f, 0.5f));
            Block(jar, Palette.WallStripe, new Vector2(0f, 135f), new Vector2(240f, 40f), new Vector2(0.5f, 0.5f));
            var candies = new List<RectTransform>();
            for (int i = 0; i < 10; i++)
                candies.Add(Actor(scene, i % 2 == 0 ? PieceSprites.Piece(i % 5) : SugarSprites.CandyWrapped, i % 2 == 0 ? Color.white : Palette.Candy(i % 5), new Vector2(0f, -250f), 70f));
            RectTransform wizard = Actor(scene, SugarSprites.BossFace(0), Color.white, new Vector2(0f, 600f), 270f);
            Image flash = UiFactory.CreateImage("Flash", scene, UiFactory.WhiteSprite, new Color(1f, 1f, 1f, 0f));
            flash.preserveAspect = false;
            UiFactory.Stretch(flash.rectTransform);
            return t =>
            {
                stars(t);
                float enter = Mathf.Clamp01(t / 1.2f);
                wizard.anchoredPosition = new Vector2(Mathf.Sin(t * 1.6f) * 60f, Mathf.Lerp(600f, 170f, UiEase.OutBack(enter, 1.4f)));
                wizard.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 3f) * 8f);
                // Fulger la fiecare 2,5 secunde (dublu, ca o furtună).
                float cycle = Mathf.Repeat(t, 2.5f);
                flash.color = new Color(1f, 1f, 1f, cycle < 0.08f || (cycle > 0.16f && cycle < 0.22f) ? 0.55f : 0f);
                for (int i = 0; i < candies.Count; i++)
                {
                    float k = Mathf.Repeat(t * 0.45f + i / (float)candies.Count, 1f);
                    float angle = (i / (float)candies.Count) * Mathf.PI * 2f + t * 0.5f;
                    float radius = k * 520f;
                    candies[i].anchoredPosition = new Vector2(Mathf.Cos(angle) * radius, -250f + Mathf.Sin(angle) * radius * 0.6f + k * 200f);
                    candies[i].localRotation = Quaternion.Euler(0f, 0f, t * 200f + i * 40f);
                    candies[i].localScale = Vector3.one * (1f - k * 0.5f);
                }
            };
        }

        /// <summary>Pagina 3: Bobo pornește la drum, sărind pe o cărare de bomboane.</summary>
        private Action<float> BuildJourney(RectTransform scene)
        {
            _sky.sprite = SugarSprites.Sky(1);
            Block(scene, new Color(0.62f, 0.86f, 0.62f), new Vector2(0f, -390f), new Vector2(1000f, 240f), new Vector2(0.5f, 0.5f));
            var path = new List<RectTransform>();
            for (int i = 0; i < 8; i++)
                path.Add(Actor(scene, PieceSprites.Piece(i % 5), Color.white, new Vector2(-420f + i * 120f, -300f + Mathf.Sin(i) * 14f), 64f));
            Actor(scene, SugarSprites.Leaf, new Color(0.3f, 0.7f, 0.4f), new Vector2(-360f, 250f), 120f);
            Actor(scene, SugarSprites.Leaf, new Color(0.4f, 0.8f, 0.5f), new Vector2(380f, 200f), 100f).localRotation = Quaternion.Euler(0f, 0f, 140f);
            BoboView bobo = BoboView.Build(scene, 0.55f);
            Action<float> sparkles = Floaters(scene, SugarSprites.Sparkle, new[] { Color.white, new Color(1f, 0.9f, 0.5f) }, 6, 0.5f, 34f);
            return t =>
            {
                float walk = Mathf.Clamp01(t / 2.4f);
                float x = Mathf.Lerp(-560f, 0f, walk);
                float hop = Mathf.Abs(Mathf.Sin(t * 5f)) * (walk < 1f ? 70f : 25f);
                bobo.Root.anchoredPosition = new Vector2(x, -150f + hop);
                bobo.Root.localRotation = Quaternion.Euler(0f, 0f, walk < 1f ? Mathf.Sin(t * 10f) * 6f : 0f);
                for (int i = 0; i < path.Count; i++)
                {
                    bool collected = path[i].anchoredPosition.x < x + 40f && walk < 1f;
                    path[i].localScale = Vector3.one * (1f + Mathf.Sin(t * 4f + i) * 0.08f + (collected ? 0.15f : 0f));
                }
                sparkles(t);
            };
        }

        /// <summary>Pagina 4: Bobo face cu mâna, o inimă mare pulsează, stele în cerc și confetti.</summary>
        private Action<float> BuildInvitation(RectTransform scene)
        {
            _sky.sprite = SugarSprites.Sky(4);
            RectTransform heart = Actor(scene, SugarSprites.Heart, new Color(1f, 0.55f, 0.7f, 0.85f), new Vector2(0f, 10f), 520f);
            var ring = new List<RectTransform>();
            for (int i = 0; i < 8; i++) ring.Add(Actor(scene, ProceduralSprites.Star, new Color(1f, 0.85f, 0.35f), Vector2.zero, 60f));
            BoboView bobo = BoboView.Build(scene, 0.62f);
            Action<float> confetti = Floaters(scene, SugarSprites.Sparkle,
                new[] { Palette.TileLight(0), Palette.TileLight(1), Palette.TileLight(2), Palette.TileLight(3), Palette.TileLight(4) }, 14, -0.9f, 34f);
            return t =>
            {
                heart.localScale = Vector3.one * (1f + Mathf.Abs(Mathf.Sin(t * 3f)) * 0.08f);
                for (int i = 0; i < ring.Count; i++)
                {
                    float angle = i / (float)ring.Count * Mathf.PI * 2f + t * 0.8f;
                    ring[i].anchoredPosition = new Vector2(Mathf.Cos(angle) * 330f, Mathf.Sin(angle) * 250f);
                    ring[i].localRotation = Quaternion.Euler(0f, 0f, t * 90f);
                }
                bobo.Root.anchoredPosition = new Vector2(0f, -40f + Mathf.Abs(Mathf.Sin(t * 3f)) * 30f);
                bobo.Root.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 4f) * 10f);
                confetti(t);
            };
        }

        /// <summary>„Bine ai revenit”: Bobo sare de bucurie în ținutul curent, cu ornamentele lui.</summary>
        private static Action<float> BuildReunion(RectTransform scene, int chapter)
        {
            int theme = ChapterTheme.IndexOf(chapter);
            Action<float> ambient = Floaters(scene, LandStyle.Ambient(theme, 0), new[] { LandStyle.OrnamentColor(theme), Color.white }, 10, 0.6f, 44f);
            Actor(scene, SugarSprites.Heart, new Color(1f, 0.6f, 0.75f, 0.6f), new Vector2(0f, 0f), 420f);
            BoboView bobo = BoboView.Build(scene, 0.65f);
            RectTransform left = Actor(scene, LandStyle.Ornament(theme), LandStyle.OrnamentColor(theme), new Vector2(-320f, -200f), 120f);
            RectTransform right = Actor(scene, LandStyle.Ornament(theme), LandStyle.OrnamentColor(theme), new Vector2(320f, -200f), 120f);
            return t =>
            {
                ambient(t);
                float jump = Mathf.Abs(Mathf.Sin(t * 4f));
                bobo.Root.anchoredPosition = new Vector2(0f, -60f + jump * 60f);
                bobo.Root.localScale = new Vector3(1f + (1f - jump) * 0.05f, 1f - (1f - jump) * 0.05f, 1f);
                left.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(t * 2f) * 15f);
                right.localRotation = Quaternion.Euler(0f, 0f, -Mathf.Sin(t * 2f) * 15f);
            };
        }
    }
}
