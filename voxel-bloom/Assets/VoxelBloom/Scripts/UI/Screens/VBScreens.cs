using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VoxelBloom.Core;

namespace VoxelBloom
{
    /// <summary>Shared look of the reference mockup: violet screens, lavender cards, purple title pills, green price buttons.</summary>
    public static class VB
    {
        public static readonly Color Bg1 = Theme.Hex("#5A3FC8"), Bg2 = Theme.Hex("#35278C"), Card = Theme.Hex("#EEE9FF"), CardRim = Theme.Hex("#C4B5FF"),
            Ink = Theme.Hex("#2B1D6B"), InkSoft = Theme.Hex("#5B4C93"), Pill = Theme.Hex("#6A4BE0"), PillRim = Theme.Hex("#A893FF"),
            Tab = Theme.Hex("#7A5CEB"), TabOn = Theme.Hex("#FFD23F"), PanelFace = Theme.Hex("#5B3FCB"), Plant = Theme.Hex("#3FBF4A");

        /// <summary>Violet gradient, a lighter floor band and leafy plants in the bottom corners.</summary>
        public static void Background(RectTransform root)
        {
            UI.Sky(root, Bg1, Bg2);
            var floor = UI.Image(root, "Floor", SpriteFactory.VerticalGradient(new Color(1, 1, 1, 0f), new Color(0.75f, 0.7f, 1f, 0.22f)), Color.white, false);
            floor.rectTransform.anchorMin = new Vector2(0, 0); floor.rectTransform.anchorMax = new Vector2(1, 0.22f);
            floor.rectTransform.offsetMin = floor.rectTransform.offsetMax = Vector2.zero;
            foreach (float side in new[] { 0f, 1f }) AddPlant(root, side);
        }

        static void AddPlant(RectTransform root, float side)
        {
            var pot = UI.Rect(root, "Plant");
            pot.anchorMin = pot.anchorMax = new Vector2(side, 0);
            pot.anchoredPosition = new Vector2(side == 0 ? 70 : -70, 90);
            pot.sizeDelta = new Vector2(220, 260);
            for (int i = 0; i < 7; i++)
            {
                var leaf = UI.Icon(pot, Icon.Leaf, 150);
                leaf.color = Color.Lerp(Plant, Theme.Hex("#8BE36A"), (i % 3) * 0.3f);
                leaf.rectTransform.anchoredPosition = new Vector2((i - 3) * 16f * (side == 0 ? 1 : -1), 40 + Mathf.Abs(i - 3) * -10f);
                leaf.rectTransform.localRotation = Quaternion.Euler(0, 0, (i - 3) * 24f + 90f);
            }
            var vase = UI.Image(pot, "Pot", SpriteFactory.Rounded(30), Theme.Hex("#C9773F"));
            vase.rectTransform.anchoredPosition = new Vector2(0, -70); vase.rectTransform.sizeDelta = new Vector2(150, 110);
        }

        /// <summary>Round purple back button and a purple title pill (like "Niveluri", "Magazin").</summary>
        public static RectTransform Header(RectTransform root, string title, Action onBack, float pillWidth = 520f)
        {
            var bar = UI.Rect(root, "Header").TopBand(24, 120, 30);
            var back = UI.IconButton(bar, Icon.ArrowLeft, 112, ButtonStyle.Purple);
            ((RectTransform)back.transform).Place(new Vector2(0, 0.5f), new Vector2(56, 0), new Vector2(112, 112));
            back.OnClick = onBack;
            var pill = TitlePill(bar, title, pillWidth, 104);
            pill.anchorMin = pill.anchorMax = new Vector2(0.5f, 0.5f);
            pill.anchoredPosition = new Vector2(30, 0);
            return bar;
        }

        public static RectTransform TitlePill(Transform parent, string title, float w, float h)
        {
            var rim = UI.Image(parent, "TitlePill", SpriteFactory.Rounded(56), PillRim);
            rim.rectTransform.sizeDelta = new Vector2(w, h);
            var face = UI.Image(rim.transform, "Face", SpriteFactory.Glossy(48), Pill);
            face.rectTransform.Stretch(6, 6, 6, 10);
            var t = UI.Text(face.transform, title, h * 0.5f, Color.white, TextAnchor.MiddleCenter, 0.2f, Theme.Hex("#2A1466"));
            t.fontStyle = FontStyle.Bold;
            t.rectTransform.Stretch(10, 10, 0, 0);
            return rim.rectTransform;
        }

        /// <summary>Lavender card with a lighter rim and a soft shadow.</summary>
        public static RectTransform Card(Transform parent, string name, Color? face = null)
        {
            var root = UI.Rect(parent, name);
            var sh = UI.Image(root, "Shadow", SpriteFactory.Shadow(40), new Color(0.1f, 0.05f, 0.3f, 0.35f));
            sh.rectTransform.Stretch(-26, -26, -18, -34);
            var rim = UI.Image(root, "Rim", SpriteFactory.Rounded(40), CardRim);
            rim.rectTransform.Stretch();
            var f = UI.Image(root, "Face", SpriteFactory.VerticalGradient(Color.white, face ?? Card), Color.white, false);
            var mask = UI.Image(root, "FaceMask", SpriteFactory.Rounded(36), face ?? Card);
            mask.rectTransform.Stretch(5, 5, 5, 5);
            f.transform.SetParent(mask.transform, false); f.rectTransform.Stretch(); f.color = new Color(1, 1, 1, 0.55f);
            return root;
        }

        /// <summary>Purple rounded tile with an icon (booster / event icons).</summary>
        public static Image IconTile(Transform parent, Icon icon, float size, Color? color = null)
        {
            var tile = UI.Image(parent, "IconTile", SpriteFactory.Glossy(36), color ?? Theme.Hex("#8E6BF2"));
            tile.rectTransform.sizeDelta = new Vector2(size, size);
            var ic = UI.Icon(tile.transform, icon, size * 0.72f);
            ic.rectTransform.anchorMin = ic.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            ic.rectTransform.anchoredPosition = Vector2.zero;
            return tile;
        }

        /// <summary>Green price button: currency icon + amount.</summary>
        public static UIButton PriceButton(Transform parent, string amount, Vector2 size, Action onClick, Icon currency = Icon.Coin, ButtonStyle style = ButtonStyle.Green)
        {
            var b = UI.Button(parent, null, style, size);
            var ic = UI.Icon(b.Face, currency, size.y * 0.62f);
            ic.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(size.y * 0.48f, 2), new Vector2(size.y * 0.62f, size.y * 0.62f));
            var t = UI.Text(b.Face, amount, size.y * 0.46f, Color.white, TextAnchor.MiddleCenter, 0.2f, Theme.Darker(Theme.GreenDark, 0.7f));
            t.fontStyle = FontStyle.Bold;
            t.rectTransform.Stretch(size.y * 0.75f, 10, 0, 0);
            b.OnClick = onClick;
            return b;
        }

        /// <summary>Gives a dialog the violet panel of the mockup.</summary>
        public static void VioletPanel(RectTransform panel)
        {
            var face = panel.Find("Face")?.GetComponent<Image>();
            if (face != null) face.color = PanelFace;
        }

        public static void Toast(string m, Icon icon = Icon.Info) => Router.Instance.Toast(m, icon);
    }

    /// <summary>Keeps a map piece sized as a fraction of the map's width (the map scales with the screen).</summary>
    public sealed class MapPiece : MonoBehaviour
    {
        public float WidthFraction, Aspect = 1f;
        float last;
        void LateUpdate()
        {
            var p = transform.parent as RectTransform; if (p == null) return;
            float w = p.rect.width; if (Mathf.Approximately(w, last)) return; last = w;
            ((RectTransform)transform).sizeDelta = new Vector2(w * WidthFraction, w * WidthFraction * Aspect);
        }
    }

    /// <summary>On/off switch of the Setări screen (green track when on).</summary>
    public sealed class VBToggle : MonoBehaviour
    {
        Image track; RectTransform knob; bool value;
        public Action<bool> Changed;

        public static VBToggle Create(Transform parent, bool initial, Action<bool> changed)
        {
            var rt = UI.Rect(parent, "Toggle"); rt.sizeDelta = new Vector2(130, 70);
            var t = rt.gameObject.AddComponent<VBToggle>();
            t.track = UI.Image(rt, "Track", SpriteFactory.Rounded(35), Color.white, true, true);
            t.track.rectTransform.Stretch();
            var k = UI.Image(rt, "Knob", SpriteFactory.Circle(), Color.white, false);
            t.knob = k.rectTransform; t.knob.sizeDelta = new Vector2(58, 58);
            var btn = t.track.gameObject.AddComponent<UIButton>(); btn.Face = rt; btn.Sound = Sfx.Select;
            btn.OnClick = () => t.Set(!t.value, true);
            t.Set(initial, false);
            t.Changed = changed;
            return t;
        }

        public void Set(bool v, bool notify)
        {
            value = v;
            track.color = v ? Theme.Hex("#45D85A") : Theme.Hex("#B9B3D6");
            knob.anchorMin = knob.anchorMax = new Vector2(v ? 1 : 0, 0.5f);
            knob.anchoredPosition = new Vector2(v ? -35 : 35, 0);
            if (notify) Changed?.Invoke(v);
        }
    }

    /// <summary>The landscape (3D diorama without pedestal) shared by the title and home screens.</summary>
    public static class HomeBackdrop
    {
        static VoxelStage stage;

        public static RawImage Attach(Transform parent)
        {
            if (stage == null)
            {
                stage = VoxelStage.Create("HomeBackdrop", 64, false);
                stage.UseTexture(900, 1950);
                stage.Floating = true;
                stage.Model.gameObject.SetActive(false);
                var scene = new GameObject("Scenery").transform;
                scene.SetParent(stage.transform, false);
                if (Props.AddDiorama(scene, out var pos, out var look, out var fov, true)) stage.SetCamera(pos, look, fov);
            }
            var raw = UI.Raw(parent, "Landscape", stage.Texture);
            raw.rectTransform.Stretch();
            var fit = raw.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = 900f / 1950f;
            return raw;
        }

        public static void SetActive(bool on) { if (stage != null) stage.SetActive(on); }
    }

    // ====================================================================== 1. title
    /// <summary>
    /// Title (first page): the painted landscape (Backgrounds/title_background, envelope-fitted so tall phones only crop
    /// the sides), light rays, the Voxel Bloom 3D logo and the green "Joacă" button, placed at the positions of the
    /// reference (fractions of the picture). Falls back to the 3D landscape if the picture is missing.
    /// </summary>
    public sealed class TitleScreen : ScreenBase
    {
        const float LogoY = 0.215f, LogoW = 0.74f, PlayY = 0.706f, PlayW = 0.67f;
        RectTransform bgRect, logo, play;
        Vector2 lastSize;

        protected override void OnBuild()
        {
            UI.Sky(Root, Theme.Hex("#2F93F0"), Theme.Hex("#A6DCFF"));
            var painted = UIArt.Get("Backgrounds/title_background", default, false);
            if (painted != null)
            {
                var bg = UI.Image(Root, "TitleBackground", painted, Color.white, false);
                bg.rectTransform.Stretch();
                var fit = bg.gameObject.AddComponent<AspectRatioFitter>();
                fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
                fit.aspectRatio = painted.rect.width / painted.rect.height;
                bgRect = bg.rectTransform;
            }
            else bgRect = (RectTransform)HomeBackdrop.Attach(Root).transform;

            var rays = UI.Image(bgRect, "Rays", SpriteFactory.Rays(), new Color(1f, 1f, 0.9f, 0.55f), false);
            rays.rectTransform.anchorMin = rays.rectTransform.anchorMax = new Vector2(0.5f, 1f - LogoY);
            rays.rectTransform.sizeDelta = new Vector2(1500, 1500);
            var logoArt = UIArt.Has("Logo/logo_title") ? "Logo/logo_title" : "Logo/logo";
            var l = UIArt.Picture(bgRect, logoArt, 800);
            logo = l.rectTransform;
            logo.anchorMin = logo.anchorMax = new Vector2(0.5f, 1f - LogoY);
            logo.anchoredPosition = Vector2.zero;

            var playArt = UIArt.Has("Buttons/play_title") ? "Buttons/play_title" : "Buttons/play";
            var b = UI.Button(bgRect, null, ButtonStyle.Green, new Vector2(700, 236));
            play = (RectTransform)b.transform;
            play.anchorMin = play.anchorMax = new Vector2(0.5f, 1f - PlayY);
            play.anchoredPosition = Vector2.zero;
            if (UIArt.Skin(b, playArt) == null) { Destroy(b.gameObject); b = UI.Button(bgRect, "Joacă", ButtonStyle.Green, new Vector2(700, 176), Icon.Play, 110); play = (RectTransform)b.transform; play.anchorMin = play.anchorMax = new Vector2(0.5f, 1f - PlayY); }
            b.Sound = Sfx.Whoosh;
            b.OnClick = () => Router.Show<HomeScreen>();
        }

        /// <summary>Sizes follow the picture (like the reference) but never get wider than the screen.</summary>
        void LateUpdate()
        {
            var size = new Vector2(bgRect.rect.width, Root.rect.width);
            if (size == lastSize || size.x <= 0) return;
            lastSize = size;
            Fit(logo, Mathf.Min(LogoW * size.x, 0.94f * size.y));
            Fit(play, Mathf.Min(PlayW * size.x, 0.86f * size.y));
        }

        static void Fit(RectTransform rt, float w)
        {
            var img = rt.GetComponent<Image>();
            var sp = img != null && img.sprite != null ? img.sprite : rt.GetComponent<UIButton>()?.Face.GetComponent<Image>().sprite;
            float aspect = sp != null ? sp.rect.height / sp.rect.width : 0.34f;
            rt.sizeDelta = new Vector2(w, w * aspect);
        }

        public override bool FadeIn => false;
        public override void OnShow() { HomeBackdrop.SetActive(!(bgRect != null && bgRect.GetComponent<Image>() != null)); App.State.ForceReset(GameState.Home); }
        public override void OnBack() { Application.Quit(); }
    }

    // ====================================================================== 5. level map
    /// <summary>"Niveluri": a winding golden path over a meadow with a river; 8 levels per page, stars, locks, page dots.</summary>
    public sealed class MapScreen : ScreenBase
    {
        const int PerPage = 8;
        // node positions (x fraction, y fraction from the top of the map area), level 1 at the top
        static readonly Vector2[] Nodes = { new Vector2(0.2f, 0.14f), new Vector2(0.5f, 0.09f), new Vector2(0.8f, 0.15f), new Vector2(0.3f, 0.33f),
            new Vector2(0.62f, 0.42f), new Vector2(0.84f, 0.58f), new Vector2(0.52f, 0.7f), new Vector2(0.2f, 0.8f) };
        RectTransform map, dots;
        int page;

        // supplied map art (1024x1536): node centres in picture pixels, level 1 first
        static readonly Vector2[] ArtNodes = { new Vector2(212, 452), new Vector2(472, 352), new Vector2(748, 322), new Vector2(320, 685),
            new Vector2(619, 740), new Vector2(843, 908), new Vector2(524, 996), new Vector2(232, 1066) };
        static readonly Vector2[] ArtDots = { new Vector2(390, 1438), new Vector2(473, 1440), new Vector2(554, 1439), new Vector2(635, 1439) };
        RectTransform art, artNodes;

        void BuildArt()
        {
            var bg = UIArt.Get("Backgrounds/home_background", default, false);
            if (bg != null)
            {
                var b = UI.Image(Root, "Backdrop", bg, Color.white, false); b.rectTransform.Stretch();
                var f = b.gameObject.AddComponent<AspectRatioFitter>(); f.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent; f.aspectRatio = bg.rect.width / bg.rect.height;
                UI.Image(Root, "Dim", null, new Color(0.15f, 0.08f, 0.35f, 0.35f), false).rectTransform.Stretch();
            }
            var sp = UIArt.Get("Map/map_full");
            var img = UI.Image(Root, "Map", sp, Color.white, false);
            art = img.rectTransform;
            art.anchorMin = new Vector2(0, 0.5f); art.anchorMax = new Vector2(1, 0.5f);
            var fit = img.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.WidthControlsHeight; fit.aspectRatio = sp.rect.width / sp.rect.height;
            artNodes = UI.Rect(art, "Nodes").Stretch();
            Hit(new Rect(23, 11, 165, 170), OnBack);
            Hit(new Rect(45, 1330, 190, 190), () => Show(page - 1));
            Hit(new Rect(785, 1330, 190, 190), () => Show(page + 1));
        }

        Vector2 Frac(Vector2 px) => new Vector2(px.x / 1024f, 1f - px.y / 1536f);

        void Hit(Rect px, Action onClick)
        {
            var r = UI.Rect(art, "Hit"); r.anchorMin = Frac(new Vector2(px.xMin, px.yMax)); r.anchorMax = Frac(new Vector2(px.xMax, px.yMin));
            r.offsetMin = r.offsetMax = Vector2.zero;
            var i = r.gameObject.AddComponent<Image>(); i.color = new Color(1, 1, 1, 0);
            var b = r.gameObject.AddComponent<UIButton>(); b.Face = r; b.OnClick = onClick; b.Squash = false;
        }

        RectTransform Piece(string path, Vector2 px, float wPx)
        {
            var sp = UIArt.Get(path);
            var img = UI.Image(artNodes, System.IO.Path.GetFileName(path), sp, Color.white, false, true);
            img.rectTransform.anchorMin = img.rectTransform.anchorMax = Frac(px);
            float wf = wPx / 1024f;
            img.rectTransform.sizeDelta = Vector2.zero;
            var le = img.gameObject.AddComponent<MapPiece>(); le.WidthFraction = wf; le.Aspect = sp.rect.height / sp.rect.width;
            return img.rectTransform;
        }

        void ShowArt()
        {
            for (int i = artNodes.childCount - 1; i >= 0; i--) Destroy(artNodes.GetChild(i).gameObject);
            int total = ConveyorController.TotalLevels, current = Services.Progress.LatestConveyorLevel(total);
            for (int i = 0; i < PerPage; i++)
            {
                int level = page * PerPage + i + 1;
                var at = ArtNodes[i];
                if (level > total) { Piece("Map/node_locked", at, 212).GetComponent<Image>().color = new Color(1, 1, 1, 0.5f); continue; }
                bool unlocked = Services.Progress.IsConveyorUnlocked(level, total);
                int stars = Services.Progress.ConveyorStarsFor(level);
                bool isCurrent = unlocked && level == current && stars == 0;
                if (!unlocked)
                {
                    var lk = Piece("Map/node_locked", at, i == 4 ? 270 : 212); // slot 5 of the art is the big glowing node
                    var b0 = lk.gameObject.AddComponent<UIButton>(); b0.Face = lk; b0.Squash = false;
                    b0.OnClick = () => Router.Toast("Termină întâi nivelul " + (level - 1), Icon.Lock);
                    continue;
                }
                var n = Piece(isCurrent ? "Map/node_current_blank" : "Map/node_open_blank", at, isCurrent || i == 4 ? 262 : 230);
                var t = UI.Text(n, level.ToString(), 90, Color.white, TextAnchor.MiddleCenter, 0.22f, Theme.Hex("#2A1466"));
                t.fontStyle = FontStyle.Bold; t.resizeTextForBestFit = true; t.resizeTextMinSize = 20; t.resizeTextMaxSize = 110;
                t.rectTransform.anchorMin = new Vector2(0.2f, 0.36f); t.rectTransform.anchorMax = new Vector2(0.8f, 0.92f);
                t.rectTransform.offsetMin = t.rectTransform.offsetMax = Vector2.zero;
                Vector2[] sp3 = { new Vector2(0.27f, 0.25f), new Vector2(0.5f, 0.22f), new Vector2(0.73f, 0.25f) };
                for (int k = stars; k < 3; k++) // the painted stars are gold: grey out the ones not earned
                {
                    var e = UI.Image(n, "StarEmpty", UIArt.Get("Icons/star_empty") ?? SpriteFactory.Get(Icon.StarEmpty), Color.white, false);
                    e.preserveAspect = true; e.rectTransform.anchorMin = e.rectTransform.anchorMax = sp3[k];
                    e.rectTransform.sizeDelta = Vector2.zero;
                    var mp = e.gameObject.AddComponent<MapPiece>(); mp.WidthFraction = 0.24f; mp.Aspect = 1f; // fraction of the node width
                }
                var b = n.gameObject.AddComponent<UIButton>(); b.Face = n; b.Squash = false; b.Sound = Sfx.Select;
                b.OnClick = () => ConveyorController.StartLevel(level);
            }
            int pages = Pages, first = Mathf.Clamp(page - 1, 0, Mathf.Max(0, pages - 4));
            for (int i = 0; i < 4; i++)
            {
                bool exists = first + i < pages, on = first + i == page;
                var d = UI.Image(artNodes, "Dot", SpriteFactory.Circle(), !exists ? new Color(0, 0, 0, 0) : on ? Theme.Hex("#FFC531") : Theme.Hex("#4FA8F0"), false);
                d.rectTransform.anchorMin = d.rectTransform.anchorMax = Frac(ArtDots[i]);
                var mp = d.gameObject.AddComponent<MapPiece>(); mp.WidthFraction = 66f / 1024f; mp.Aspect = 1f;
            }
        }

        protected override void OnBuild()
        {
            if (UIArt.Has("Map/map_full")) { BuildArt(); return; }
            UI.Sky(Root, Theme.Hex("#8FD9FF"), Theme.Hex("#5CC0F5"));
            map = UI.Rect(Root, "Map").Stretch(0, 0, 170, 240);
            VB.Header(Root, "Niveluri", OnBack, 460);
            var prev = UI.IconButton(Root, Icon.ArrowLeft, 120, ButtonStyle.Purple);
            ((RectTransform)prev.transform).Place(new Vector2(0, 0), new Vector2(110, 120), new Vector2(120, 120));
            prev.OnClick = () => Show(page - 1);
            var next = UI.IconButton(Root, Icon.ArrowRight, 120, ButtonStyle.Purple);
            ((RectTransform)next.transform).Place(new Vector2(1, 0), new Vector2(-110, 120), new Vector2(120, 120));
            next.OnClick = () => Show(page + 1);
            dots = UI.Rect(Root, "Dots"); dots.anchorMin = dots.anchorMax = new Vector2(0.5f, 0); dots.anchoredPosition = new Vector2(0, 120); dots.sizeDelta = new Vector2(500, 50);
        }

        int Pages => Mathf.Max(1, Mathf.CeilToInt(ConveyorController.TotalLevels / (float)PerPage));

        public override void OnShow()
        {
            int current = Services.Progress.LatestConveyorLevel(ConveyorController.TotalLevels);
            Show((current - 1) / PerPage);
        }

        void Show(int p)
        {
            page = Mathf.Clamp(p, 0, Pages - 1);
            if (art != null) { ShowArt(); return; }
            for (int i = map.childCount - 1; i >= 0; i--) Destroy(map.GetChild(i).gameObject);
            for (int i = dots.childCount - 1; i >= 0; i--) Destroy(dots.GetChild(i).gameObject);
            var rect = map.rect;
            float W = rect.width > 0 ? rect.width : 1080f, H = rect.height > 0 ? rect.height : 1500f;
            Vector2 Pos(Vector2 f) => new Vector2((f.x - 0.5f) * W, (0.5f - f.y) * H);

            // meadow, river and trees
            var grass = UI.Image(map, "Meadow", SpriteFactory.VerticalGradient(Theme.Hex("#8EDC6A"), Theme.Hex("#52B947")), Color.white, false);
            grass.rectTransform.Stretch();
            var river = new List<Vector2>();
            for (int i = 0; i <= 24; i++) { float t = i / 24f; river.Add(Pos(new Vector2(0.5f + 0.38f * Mathf.Sin(t * 5.2f + page), t))); }
            Polyline(river, 150, Theme.Hex("#BDEBFF"));
            Polyline(river, 120, Theme.Hex("#69C6F5"));
            var rng = new DeterministicRandom(page * 31 + 7);
            for (int i = 0; i < 16; i++)
            {
                var f = new Vector2(i % 2 == 0 ? 0.04f + (float)rng.NextDouble() * 0.12f : 0.84f + (float)rng.NextDouble() * 0.12f, 0.04f + i * 0.06f);
                Tree(Pos(f), 90 + (float)rng.NextDouble() * 60);
            }
            // golden path through the nodes (Catmull-Rom)
            var pts = new List<Vector2>();
            for (int i = 0; i < Nodes.Length - 1; i++)
            {
                Vector2 p0 = Nodes[Mathf.Max(0, i - 1)], p1 = Nodes[i], p2 = Nodes[i + 1], p3 = Nodes[Mathf.Min(Nodes.Length - 1, i + 2)];
                for (int k = 0; k < 10; k++)
                {
                    float t = k / 10f, t2 = t * t, t3 = t2 * t;
                    var q = 0.5f * (2 * p1 + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t2 + (-p0 + 3 * p1 - 3 * p2 + p3) * t3);
                    pts.Add(Pos(q));
                }
            }
            pts.Add(Pos(Nodes[Nodes.Length - 1]));
            Polyline(pts, 92, Theme.Hex("#FFC23C"));
            Polyline(pts, 70, Theme.Hex("#C9C2DD"));

            int total = ConveyorController.TotalLevels, current = Services.Progress.LatestConveyorLevel(total);
            for (int i = 0; i < PerPage; i++)
            {
                int level = page * PerPage + i + 1;
                if (level > total) break;
                Node(level, Pos(Nodes[i]), current, total);
            }
            int shown = Mathf.Min(Pages, 8), first = Mathf.Clamp(page - shown / 2, 0, Pages - shown);
            for (int i = 0; i < shown; i++)
            {
                var d = UI.Image(dots, "Dot", SpriteFactory.Circle(), first + i == page ? Theme.Gold : Theme.Hex("#C9C2F0"), false);
                d.rectTransform.anchorMin = d.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                d.rectTransform.anchoredPosition = new Vector2((i - (shown - 1) / 2f) * 46, 0);
                d.rectTransform.sizeDelta = Vector2.one * (first + i == page ? 34 : 28);
            }
        }

        void Polyline(List<Vector2> pts, float width, Color color)
        {
            for (int i = 0; i + 1 < pts.Count; i++)
            {
                var a = pts[i]; var b = pts[i + 1]; var d = b - a;
                var seg = UI.Image(map, "Seg", SpriteFactory.Rounded(32), color);
                seg.rectTransform.anchorMin = seg.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                seg.rectTransform.anchoredPosition = (a + b) / 2f;
                seg.rectTransform.sizeDelta = new Vector2(d.magnitude + width, width);
                seg.pixelsPerUnitMultiplier = 64f / width;
                seg.rectTransform.localRotation = Quaternion.Euler(0, 0, Mathf.Atan2(d.y, d.x) * Mathf.Rad2Deg);
            }
        }

        void Tree(Vector2 at, float size)
        {
            var trunk = UI.Image(map, "Trunk", SpriteFactory.Rounded(10), Theme.Hex("#8E5A34"));
            trunk.rectTransform.anchorMin = trunk.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
            trunk.rectTransform.anchoredPosition = at + new Vector2(0, -size * 0.35f); trunk.rectTransform.sizeDelta = new Vector2(size * 0.18f, size * 0.5f);
            for (int i = 0; i < 3; i++)
            {
                var c = UI.Image(map, "Leaves", SpriteFactory.Circle(), Color.Lerp(Theme.Hex("#2F9A3E"), Theme.Hex("#6BD35A"), i * 0.4f), false);
                c.rectTransform.anchorMin = c.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                c.rectTransform.anchoredPosition = at + new Vector2((i - 1) * size * 0.22f, i == 1 ? size * 0.12f : 0);
                c.rectTransform.sizeDelta = Vector2.one * size * (i == 1 ? 0.75f : 0.6f);
            }
        }

        void Node(int level, Vector2 at, int current, int total)
        {
            bool unlocked = Services.Progress.IsConveyorUnlocked(level, total);
            int stars = Services.Progress.ConveyorStarsFor(level);
            var def = Services.Data.GetConveyorLevel(level);
            if (!unlocked)
            {
                var ped = UI.Image(map, "Locked", SpriteFactory.Glossy(40), Theme.Hex("#9AA0B8"));
                ped.rectTransform.anchorMin = ped.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                ped.rectTransform.anchoredPosition = at; ped.rectTransform.sizeDelta = new Vector2(150, 130);
                var lk = UI.Icon(ped.transform, Icon.Lock, 80);
                lk.rectTransform.anchorMin = lk.rectTransform.anchorMax = new Vector2(0.5f, 0.5f); lk.rectTransform.anchoredPosition = new Vector2(0, 8);
                var hit = ped.gameObject.AddComponent<UIButton>(); ped.raycastTarget = true; hit.Face = ped.rectTransform;
                hit.OnClick = () => Router.Toast("Termină întâi nivelul " + (level - 1), Icon.Lock);
                return;
            }
            bool isCurrent = level == current && stars == 0;
            float size = isCurrent ? 170 : 140;
            if (isCurrent)
            {
                var glow = UI.Image(map, "Glow", SpriteFactory.Glow(), new Color(1f, 0.85f, 0.3f, 0.8f), false);
                glow.rectTransform.anchorMin = glow.rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
                glow.rectTransform.anchoredPosition = at; glow.rectTransform.sizeDelta = Vector2.one * 300;
            }
            var b = UI.Button(map, null, ButtonStyle.Purple, new Vector2(size, size));
            var r = (RectTransform)b.transform; r.anchorMin = r.anchorMax = new Vector2(0.5f, 0.5f); r.anchoredPosition = at;
            var face = b.Face.GetComponent<Image>(); face.sprite = SpriteFactory.Circle(); face.type = Image.Type.Simple;
            face.color = def != null && def.Tier >= 2 ? ConveyorScreen.TierColors[def.Tier] : Theme.Hex("#7B4DFF");
            var lip = b.transform.Find("Lip")?.GetComponent<Image>(); if (lip != null) { lip.sprite = SpriteFactory.Circle(); lip.type = Image.Type.Simple; }
            var sh = b.transform.Find("Shadow"); if (sh != null) sh.gameObject.SetActive(false);
            var t = UI.Text(b.Face, level.ToString(), size * 0.48f, Color.white, TextAnchor.MiddleCenter, 0.2f, Theme.Hex("#2A1466"));
            t.fontStyle = FontStyle.Bold; t.rectTransform.Stretch(0, 0, 0, 12);
            for (int i = 0; i < 3; i++)
            {
                var sp = UIArt.Get(i < stars ? "Icons/star_gold" : "Icons/star_empty") ?? SpriteFactory.Get(i < stars ? Icon.Star : Icon.StarEmpty);
                var s = UI.Image(r, "Star", sp, Color.white, false); s.preserveAspect = true;
                s.rectTransform.anchorMin = s.rectTransform.anchorMax = new Vector2(0.5f, 0);
                s.rectTransform.anchoredPosition = new Vector2((i - 1) * 48, i == 1 ? -4 : 2); s.rectTransform.sizeDelta = new Vector2(52, 52);
            }
            b.Sound = Sfx.Select;
            b.OnClick = () => ConveyorController.StartLevel(level);
        }

        public override void OnBack() => Router.Show<HomeScreen>();
    }

    // ====================================================================== 6. how to play
    public sealed class HowToPlayDialog : DialogBase
    {
        public Action Done;
        protected override string Title => "Cum se joacă?";
        protected override Vector2 Size => new Vector2(900, 1260);

        protected override void OnBuild()
        {
            VB.VioletPanel(Panel);
            Step(0, "Alege o tavă\ndin coadă.", (r) => { MiniTray(r, new[] { "#FFD93B", "#D9259C", "#A47CF7" }, new Vector2(60, 0)); var h = UI.Icon(r, Icon.Play, 70); h.rectTransform.anchoredPosition = new Vector2(150, -40); h.rectTransform.localRotation = Quaternion.Euler(0, 0, 120); });
            Step(1, "Cuburile circulă\npe bandă.", (r) => { var belt = UI.Image(r, "Belt", SpriteFactory.Rounded(20), Theme.Hex("#5A5396")); belt.rectTransform.sizeDelta = new Vector2(300, 60); belt.rectTransform.anchoredPosition = new Vector2(40, -30); string[] c = { "#D9259C", "#FFD93B", "#3ED6F0", "#4CD964" }; for (int i = 0; i < 4; i++) Cube(r, c[i], new Vector2(-50 + i * 60, -20), 50); var a = UI.Icon(r, Icon.ArrowRight, 50); a.rectTransform.anchoredPosition = new Vector2(150, -30); });
            Step(2, "Cuburile de aceeași culoare\numplu tava și eliberează loc.", (r) => { MiniTray(r, new[] { "#3ED6F0", "#FFD93B", "#4CD964" }, new Vector2(40, 0)); var ok = UI.Icon(r, Icon.Check2, 70); ok.rectTransform.anchoredPosition = new Vector2(150, -30); });
            var b = UI.Button(Body, "Înțeles!", ButtonStyle.Green, new Vector2(560, 140), null, 64);
            ((RectTransform)b.transform).Place(new Vector2(0.5f, 0), new Vector2(0, 70), new Vector2(560, 140));
            b.OnClick = () => { Services.Save.HowToPlaySeen = true; Services.Commit(); Close(); Done?.Invoke(); };
        }

        void Step(int i, string text, Action<RectTransform> art)
        {
            var card = VB.Card(Body, "Step" + i);
            card.Place(new Vector2(0.5f, 1), new Vector2(0, -100 - i * 270), new Vector2(780, 240));
            var num = UI.Image(card, "Num", SpriteFactory.Circle(), Theme.Hex("#5B3FC9"), false);
            num.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(60, 0), new Vector2(76, 76));
            UI.Text(num.transform, (i + 1).ToString(), 46, Color.white).rectTransform.Stretch();
            var t = UI.Text(card, text, 38, VB.Ink, TextAnchor.MiddleLeft);
            t.Wrap = true; t.fontStyle = FontStyle.Bold;
            t.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(115, 0), new Vector2(330, 200), new Vector2(0, 0.5f));
            var r = UI.Rect(card, "Art"); r.anchorMin = r.anchorMax = new Vector2(1, 0.5f); r.anchoredPosition = new Vector2(-190, 10);
            art(r);
        }

        static void Cube(RectTransform r, string hex, Vector2 at, float size)
        {
            var c = UI.Image(r, "Cube", SpriteFactory.Glossy(14), Theme.Hex(hex));
            c.rectTransform.anchoredPosition = at; c.rectTransform.sizeDelta = Vector2.one * size;
        }

        static void MiniTray(RectTransform r, string[] cols, Vector2 at)
        {
            var tray = UI.Image(r, "Tray", SpriteFactory.Rounded(18), ConveyorScreen.TrayRim);
            tray.rectTransform.anchoredPosition = at; tray.rectTransform.sizeDelta = new Vector2(cols.Length * 66 + 24, 84);
            var inner = UI.Image(tray.transform, "Inner", SpriteFactory.Rounded(14), ConveyorScreen.TrayInner); inner.rectTransform.Stretch(7, 7, 7, 7);
            for (int i = 0; i < cols.Length; i++) Cube(r, cols[i], at + new Vector2((i - (cols.Length - 1) / 2f) * 66, 0), 58);
        }
    }

    // ====================================================================== 7. boosters (buy one)
    public sealed class BoostersInfoDialog : DialogBase
    {
        public Action Changed;
        protected override string Title => "Boostere";
        protected override Vector2 Size => new Vector2(940, 1240);

        public static readonly (BoosterType t, Icon icon, string name, string desc)[] All =
        {
            (BoosterType.Shuffle, Icon.Refresh, "Reîmprospătează", "Schimbă ordinea tăvilor din coadă."),
            (BoosterType.ColorShuffle, Icon.ColorSwap, "Amestecă culorile", "Rearanjează culorile cuburilor de pe bandă."),
            (BoosterType.ExtraStation, Icon.CubesPlus, "Adaugă o tavă", "Deschide o stație în plus (slotul verde +)."),
        };

        protected override void OnBuild()
        {
            VB.VioletPanel(Panel);
            for (int i = 0; i < All.Length; i++)
            {
                var (t, icon, name, desc) = All[i];
                var card = VB.Card(Body, name);
                card.Place(new Vector2(0.5f, 1), new Vector2(0, -110 - i * 300), new Vector2(820, 260));
                var tile = VB.IconTile(card, icon, 170);
                tile.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(115, 0), new Vector2(170, 170));
                var n = UI.Text(card, name, 44, VB.Ink, TextAnchor.UpperLeft); n.fontStyle = FontStyle.Bold;
                n.rectTransform.Place(new Vector2(0, 1), new Vector2(225, -30), new Vector2(560, 56), new Vector2(0, 1));
                var d = UI.Text(card, desc, 32, VB.InkSoft, TextAnchor.UpperLeft); d.Wrap = true;
                d.rectTransform.Place(new Vector2(0, 1), new Vector2(225, -90), new Vector2(360, 110), new Vector2(0, 1));
                var have = UI.Text(card, "Ai: " + Services.Boosters.Count(t), 30, VB.InkSoft, TextAnchor.MiddleRight);
                have.rectTransform.Place(new Vector2(1, 1), new Vector2(-30, -40), new Vector2(200, 40), new Vector2(1, 0.5f));
                int price = Services.Config.Booster(t).Price;
                var bt = t;
                var buy = VB.PriceButton(card, price.ToString(), new Vector2(230, 96), () =>
                {
                    if (Services.Boosters.Buy(bt, Services.Config, Services.Wallet) != PurchaseResult.Success) { VB.Toast("Nu ai destule monede", Icon.Coin); return; }
                    Services.Commit();
                    Services.Audio.Play(Sfx.Coin);
                    have.text = "Ai: " + Services.Boosters.Count(bt);
                    Changed?.Invoke();
                });
                ((RectTransform)buy.transform).Place(new Vector2(1, 0), new Vector2(-140, 70), new Vector2(230, 96));
            }
        }
    }

    // ====================================================================== 8. shop
    /// <summary>"Magazin": tabs Boostere / Monede / Gems / Pachete. Real-money purchases are not configured (no fake buttons).</summary>
    public sealed class StoreScreen : ScreenBase
    {
        RectTransform content; readonly List<Image> tabs = new List<Image>(); VText coinsText, gemsText; int tab;

        protected override void OnBuild()
        {
            VB.Background(Root);
            VB.Header(Root, "Magazin", OnBack, 380);
            var wallet = UI.Rect(Root, "Wallet"); wallet.anchorMin = wallet.anchorMax = new Vector2(1, 1); wallet.pivot = new Vector2(1, 1);
            wallet.anchoredPosition = new Vector2(-24, -20); wallet.sizeDelta = new Vector2(260, 130);
            coinsText = Balance(wallet, Icon.Coin, 0); gemsText = Balance(wallet, Icon.Gem, 1);
            var bar = UI.Rect(Root, "Tabs").TopBand(170, 96, 30);
            string[] names = { "Boostere", "Monede", "Gems", "Pachete" };
            for (int i = 0; i < 4; i++)
            {
                int idx = i;
                var b = UI.Button(bar, null, ButtonStyle.Purple, new Vector2(240, 90));
                ((RectTransform)b.transform).Place(new Vector2((i + 0.5f) / 4f, 0.5f), Vector2.zero, new Vector2(240, 90));
                var t = UI.Text(b.Face, names[i], 38, Color.white, TextAnchor.MiddleCenter, 0.18f, Theme.Hex("#2A1466")); t.fontStyle = FontStyle.Bold; t.rectTransform.Stretch();
                tabs.Add(b.Face.GetComponent<Image>());
                b.OnClick = () => ShowTab(idx);
            }
            content = UI.Rect(Root, "Content").Stretch(30, 30, 300, 300);
        }

        VText Balance(RectTransform parent, Icon icon, int row)
        {
            var pill = UI.Image(parent, "Balance", SpriteFactory.Rounded(30), new Color(0.1f, 0.08f, 0.3f, 0.75f));
            pill.rectTransform.anchorMin = pill.rectTransform.anchorMax = new Vector2(1, 1); pill.rectTransform.pivot = new Vector2(1, 1);
            pill.rectTransform.anchoredPosition = new Vector2(0, -row * 66); pill.rectTransform.sizeDelta = new Vector2(250, 58);
            UI.Icon(pill.transform, icon, 52).rectTransform.Place(new Vector2(0, 0.5f), new Vector2(28, 0), new Vector2(52, 52));
            var t = UI.Text(pill.transform, "0", 36, Color.white, TextAnchor.MiddleRight); t.rectTransform.Stretch(60, 18, 0, 0);
            return t;
        }

        public override void OnShow() { ShowTab(tab); }
        void Update() { if (Services.Initialized) { coinsText.text = Services.Wallet.Coins.ToString("N0"); gemsText.text = Services.Save.Gems.ToString(); } }

        void ShowTab(int i)
        {
            tab = i;
            for (int k = 0; k < tabs.Count; k++) tabs[k].color = k == i ? VB.TabOn : VB.Tab;
            for (int k = content.childCount - 1; k >= 0; k--) Destroy(content.GetChild(k).gameObject);
            switch (i)
            {
                case 0:
                    for (int k = 0; k < 3; k++)
                    {
                        var (t, icon, name, _) = BoostersInfoDialog.All[k];
                        int price = new[] { 500, 750, 1000 }[k];
                        var bt = t;
                        Item(k, icon, name, "×3", price.ToString(), Icon.Coin, () => BuyCoins(price, () => Services.Boosters.Add(bt, 3)));
                    }
                    break;
                case 1:
                    Item(0, Icon.Coin, "1.000 monede", "", "50", Icon.Gem, () => BuyGems(50, () => Services.Wallet.Add(1000)));
                    Item(1, Icon.Coin, "3.000 monede", "", "130", Icon.Gem, () => BuyGems(130, () => Services.Wallet.Add(3000)));
                    Item(2, Icon.Coin, "8.000 monede", "", "300", Icon.Gem, () => BuyGems(300, () => Services.Wallet.Add(8000)));
                    Note("Monedele se câștigă și terminând niveluri.", 3);
                    break;
                case 2:
                    Item(0, Icon.Gem, "100 gems", "", "—", Icon.Gem, () => VB.Toast("Plățile reale nu sunt configurate încă", Icon.Gem), ButtonStyle.White);
                    Item(1, Icon.Gem, "550 gems", "", "—", Icon.Gem, () => VB.Toast("Plățile reale nu sunt configurate încă", Icon.Gem), ButtonStyle.White);
                    Item(2, Icon.Gem, "1.200 gems", "", "—", Icon.Gem, () => VB.Toast("Plățile reale nu sunt configurate încă", Icon.Gem), ButtonStyle.White);
                    Note("Gems: din nivelurile Greu (+5), Foarte greu (+10) și din evenimente.\nCumpărarea cu bani reali nu este configurată.", 3);
                    break;
                default:
                    Item(0, Icon.Gift, "Pachet Start", "3 din fiecare\n+1.000 monede", "120", Icon.Gem, () => BuyGems(120, () => { AddAll(3); Services.Wallet.Add(1000); }));
                    Item(1, Icon.Trophy, "Pachet Mare", "10 din fiecare\n+5.000 monede", "350", Icon.Gem, () => BuyGems(350, () => { AddAll(10); Services.Wallet.Add(5000); }));
                    Item(2, Icon.Heart, "Vieți pline", "Toate viețile", "30", Icon.Gem, () => BuyGems(30, () => Services.Lives.Refill()));
                    break;
            }
        }

        static void AddAll(int n) { foreach (var b in BoostersInfoDialog.All) Services.Boosters.Add(b.t, n); }

        void Item(int i, Icon icon, string name, string sub, string price, Icon currency, Action buy, ButtonStyle style = ButtonStyle.Green)
        {
            var card = VB.Card(content, name, Theme.Hex("#D9CCFF"));
            card.Place(new Vector2((i + 0.5f) / 3f, 1), new Vector2(0, -20), new Vector2(316, 560), new Vector2(0.5f, 1));
            var ic = UI.Icon(card, icon, 200); ic.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, -140), new Vector2(200, 200));
            if (!string.IsNullOrEmpty(sub))
            {
                var s = UI.Text(card, sub, sub.Length <= 3 ? 64 : 30, Color.white, TextAnchor.MiddleRight, 0.25f, VB.Ink); s.Wrap = true; s.fontStyle = FontStyle.Bold;
                s.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, -275), new Vector2(280, 90));
            }
            var n = UI.Text(card, name, 36, VB.Ink); n.fontStyle = FontStyle.Bold; n.Wrap = true;
            n.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, -350), new Vector2(290, 60));
            var b = VB.PriceButton(card, price, new Vector2(250, 104), buy, currency, style);
            ((RectTransform)b.transform).Place(new Vector2(0.5f, 0), new Vector2(0, 80), new Vector2(250, 104));
        }

        void Note(string text, int row)
        {
            var t = UI.Text(content, text, 34, new Color(1, 1, 1, 0.85f)); t.Wrap = true;
            t.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, -640), new Vector2(980, 140));
        }

        static void BuyCoins(int price, Action grant)
        {
            if (!Services.Wallet.TrySpend(price)) { VB.Toast("Nu ai destule monede", Icon.Coin); return; }
            grant(); Services.Commit(); Services.Audio.Play(Sfx.Coin); VB.Toast("Cumpărat!", Icon.Check2);
        }

        static void BuyGems(int price, Action grant)
        {
            if (Services.Save.Gems < price) { VB.Toast("Nu ai destule gems", Icon.Gem); return; }
            Services.Save.Gems -= price;
            grant(); Services.Commit(); Services.Audio.Play(Sfx.Coin); VB.Toast("Cumpărat!", Icon.Check2);
        }

        public override void OnBack() => Router.Show<HomeScreen>();
    }

    // ====================================================================== 9. events
    public sealed class EventsHubScreen : ScreenBase
    {
        RectTransform list;

        protected override void OnBuild()
        {
            VB.Background(Root);
            VB.Header(Root, "Evenimente", OnBack, 520);
            list = UI.Rect(Root, "List").Stretch(40, 40, 190, 300);
        }

        public override void OnShow() => Rebuild();

        void Rebuild()
        {
            for (int i = list.childCount - 1; i >= 0; i--) Destroy(list.GetChild(i).gameObject);
            var ev = Services.Events; long now = Services.Now; var s = Services.Save;
            ev.Refresh(now);
            Row(0, Icon.CalendarStar, "Provocarea Zilnică", "Finalizează " + ConveyorEvents.DailyGoal + " niveluri!", s.DailyChallengeCount, ConveyorEvents.DailyGoal,
                s.DailyChallengeClaimed ? "Revino mâine" : null, ev.CanClaimDaily(now), () => Show(ev.ClaimDaily(now)));
            Row(1, Icon.Target, "Misiune Specială", "Colectează " + ConveyorEvents.MissionGoal + " de cuburi!", s.MissionCubes, ConveyorEvents.MissionGoal,
                null, ev.CanClaimMission, () => Show(ev.ClaimMission()));
            long left = Math.Max(0, ConveyorEvents.SeasonEndsAt(now) - now);
            Row(2, Icon.Gem, "Sezonul Curcubeu", "Câștigă " + ConveyorEvents.SeasonGoal + " stele: recompense unice!", s.SeasonStars, ConveyorEvents.SeasonGoal,
                (left / 86400) + "z " + (left % 86400 / 3600) + "h", ev.CanClaimSeason(now), () => Show(ev.ClaimSeason(now)));
        }

        void Show(EventReward r)
        {
            Services.Commit();
            if (r.Coins + r.Gems + r.Boosters > 0)
            {
                Services.Audio.Play(Sfx.Win);
                var parts = new List<string>();
                if (r.Coins > 0) parts.Add("+" + r.Coins + " monede"); if (r.Gems > 0) parts.Add("+" + r.Gems + " gems"); if (r.Boosters > 0) parts.Add("+" + r.Boosters + " din fiecare booster");
                VB.Toast(string.Join(", ", parts), Icon.Gift);
            }
            Rebuild();
        }

        void Row(int i, Icon icon, string title, string sub, int have, int goal, string extra, bool claimable, Action claim)
        {
            var card = VB.Card(list, title);
            card.Place(new Vector2(0.5f, 1), new Vector2(0, -20 - i * 330), new Vector2(980, 290), new Vector2(0.5f, 1));
            var tile = VB.IconTile(card, icon, 170, Color.white);
            tile.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(120, 0), new Vector2(170, 170));
            var t = UI.Text(card, title, 46, VB.Ink, TextAnchor.UpperLeft); t.fontStyle = FontStyle.Bold;
            t.rectTransform.Place(new Vector2(0, 1), new Vector2(230, -34), new Vector2(560, 60), new Vector2(0, 1));
            var st = UI.Text(card, sub, 32, VB.InkSoft, TextAnchor.UpperLeft); st.Wrap = true;
            st.rectTransform.Place(new Vector2(0, 1), new Vector2(230, -96), new Vector2(500, 80), new Vector2(0, 1));
            var bar = UI.Progress(card, Theme.Hex("#45D85A"), 46);
            ((RectTransform)bar.transform).Place(new Vector2(0, 0), new Vector2(230, 56), new Vector2(430, 46), new Vector2(0, 0.5f));
            UIArt.SkinProgress(bar);
            bar.Set(goal == 0 ? 1 : Mathf.Clamp01(have / (float)goal), true);
            var bt = UI.Text((RectTransform)bar.transform, extra ?? (Mathf.Min(have, goal) + "/" + goal), 30, Color.white, TextAnchor.MiddleCenter, 0.2f, VB.Ink);
            bt.rectTransform.Stretch();
            var gift = UI.Button(card, null, claimable ? ButtonStyle.Gold : ButtonStyle.White, new Vector2(170, 170), Icon.Gift);
            ((RectTransform)gift.transform).Place(new Vector2(1, 0.5f), new Vector2(-110, 0), new Vector2(170, 170));
            gift.SetInteractable(claimable);
            gift.OnClick = () => { if (claimable) claim(); };
            if (claimable) Tween.Pop(gift.transform, 0.12f, 0.6f);
        }

        public override void OnBack() => Router.Show<HomeScreen>();
    }

    // ====================================================================== 10. settings
    public sealed class SettingsScreen : ScreenBase
    {
        protected override void OnBuild()
        {
            VB.Background(Root);
            VB.Header(Root, "Setări", OnBack, 420);
            Build(Root, 190f);
        }

        /// <summary>The settings rows (also used by the in-game settings dialog).</summary>
        public static void Build(RectTransform parent, float top)
        {
            var st = Services.Save.Settings;
            void Row(int i, Icon icon, string label, Action<RectTransform> right) => SettingsRow(parent, top, i, icon, label, right);
            Row(0, Icon.Sound, "Sunet", r => VBToggle.Create(r, st.Sound, v => { st.Sound = v; Apply(); }));
            Row(1, Icon.Music, "Muzică", r => VBToggle.Create(r, st.Music, v => { st.Music = v; Apply(); }));
            Row(2, Icon.Vibrate, "Vibrații", r => VBToggle.Create(r, st.Vibration, v => { st.Vibration = v; Apply(); if (v) Haptics.Light(); }));
            Row(3, Icon.Globe, "Limbă", r => Chevron(r, "Română", () => VB.Toast("Momentan jocul este doar în limba română", Icon.Globe)));
            Row(4, Icon.Cloud, "Salvare în cloud", r => VBToggle.Create(r, Services.Save.CloudSave, v =>
            {
                Services.Save.CloudSave = v; Services.Commit();
                if (v) VB.Toast("Sincronizarea are nevoie de Google Play Games (neconfigurat încă). Progresul rămâne salvat pe telefon.", Icon.Cloud);
            }));
            Row(5, Icon.Info, "Despre joc", r => Chevron(r, "", () => Router.Instance.Open<AboutDialog>()));
        }

        static void Apply() { Services.NotifySettingsChanged(); Services.Commit(); }

        static void SettingsRow(RectTransform parent, float top, int i, Icon icon, string label, Action<RectTransform> right)
        {
            var card = VB.Card(parent, label);
            card.anchorMin = card.anchorMax = new Vector2(0.5f, 1); card.pivot = new Vector2(0.5f, 1);
            card.anchoredPosition = new Vector2(0, -top - i * 150); card.sizeDelta = new Vector2(Mathf.Min(980, parent.rect.width > 0 ? parent.rect.width - 20 : 980), 128);
            var ic = UI.Icon(card, icon, 84); ic.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(70, 0), new Vector2(84, 84));
            var t = UI.Text(card, label, 44, VB.Ink, TextAnchor.MiddleLeft); t.fontStyle = FontStyle.Bold;
            t.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(140, 0), new Vector2(560, 70), new Vector2(0, 0.5f));
            var slot = UI.Rect(card, "Right"); slot.anchorMin = slot.anchorMax = new Vector2(1, 0.5f); slot.anchoredPosition = new Vector2(-100, 0);
            right(slot);
        }

        static void Chevron(RectTransform r, string value, Action onClick)
        {
            var b = UI.Rect(r, "Chevron"); b.sizeDelta = new Vector2(320, 100); b.anchoredPosition = new Vector2(-60, 0);
            var img = b.gameObject.AddComponent<Image>(); img.color = new Color(1, 1, 1, 0);
            var btn = b.gameObject.AddComponent<UIButton>(); btn.Face = b; btn.OnClick = onClick;
            var t = UI.Text(b, value, 38, VB.InkSoft, TextAnchor.MiddleRight); t.rectTransform.Stretch(0, 70, 0, 0);
            var a = UI.Icon(b, Icon.ArrowRight, 56); a.rectTransform.Place(new Vector2(1, 0.5f), new Vector2(-28, 0), new Vector2(56, 56));
        }

        public override void OnBack() => Router.Show<HomeScreen>();
    }

    /// <summary>In-game "Setări" (same rows as the settings screen, keeps the level running underneath).</summary>
    public sealed class SettingsVioletDialog : DialogBase
    {
        protected override string Title => "Setări";
        protected override Vector2 Size => new Vector2(980, 1160);
        protected override void OnBuild() { VB.VioletPanel(Panel); SettingsScreen.Build(Body, 30f); }
    }

    public sealed class AboutDialog : DialogBase
    {
        protected override string Title => "Despre joc";
        protected override Vector2 Size => new Vector2(860, 900);

        protected override void OnBuild()
        {
            VB.VioletPanel(Panel);
            var t = UI.Text(Body, "Voxel Bloom 3D\nVersiunea " + Application.version +
                "\n\nSortează cuburile colorate de pe bandă\nîn tăvile potrivite.\n\nCod, niveluri, grafică procedurală și sunete:\noriginale. Font: Baloo 2 (SIL Open Font License).", 36, Color.white);
            t.Wrap = true; t.rectTransform.Stretch(20, 20, 40, 60);
        }
    }

    // ====================================================================== leaderboard (local records)
    /// <summary>"Clasament": the player's own records. An online leaderboard needs a server, which is not configured.</summary>
    public sealed class LeaderboardScreen : ScreenBase
    {
        RectTransform list;

        protected override void OnBuild()
        {
            VB.Background(Root);
            VB.Header(Root, "Clasament", OnBack, 480);
            list = UI.Rect(Root, "List").Stretch(40, 40, 190, 300);
        }

        public override void OnShow()
        {
            for (int i = list.childCount - 1; i >= 0; i--) Destroy(list.GetChild(i).gameObject);
            var s = Services.Save; int total = ConveyorController.TotalLevels;
            int done = s.ConveyorStars.Count, best = 0; foreach (var k in s.ConveyorStars.Keys) best = Mathf.Max(best, k);
            Row(0, Icon.Star, "Stele câștigate", Services.Progress.ConveyorTotalStars + " / " + total * 3);
            Row(1, Icon.Trophy, "Niveluri terminate", done + " / " + total);
            Row(2, Icon.Crown, "Cel mai mare nivel", best > 0 ? best.ToString() : "—");
            Row(3, Icon.Cube, "Cuburi sortate", s.CubesDelivered.ToString("N0"));
            Row(4, Icon.Target, "Misiuni finalizate", s.MissionsDone.ToString());
            var note = UI.Text(list, "Clasamentul online cu alți jucători are nevoie de un server, care nu este configurat încă.", 32, new Color(1, 1, 1, 0.85f));
            note.Wrap = true; note.rectTransform.Place(new Vector2(0.5f, 1), new Vector2(0, -5 * 160 - 60), new Vector2(940, 120));
        }

        void Row(int i, Icon icon, string label, string value)
        {
            var card = VB.Card(list, label);
            card.Place(new Vector2(0.5f, 1), new Vector2(0, -10 - i * 160), new Vector2(980, 136), new Vector2(0.5f, 1));
            var ic = UI.Icon(card, icon, 90); ic.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(75, 0), new Vector2(90, 90));
            var t = UI.Text(card, label, 42, VB.Ink, TextAnchor.MiddleLeft); t.fontStyle = FontStyle.Bold;
            t.rectTransform.Place(new Vector2(0, 0.5f), new Vector2(150, 0), new Vector2(520, 70), new Vector2(0, 0.5f));
            var v = UI.Text(card, value, 46, Theme.Hex("#5B3FC9"), TextAnchor.MiddleRight); v.fontStyle = FontStyle.Bold;
            v.rectTransform.Place(new Vector2(1, 0.5f), new Vector2(-40, 0), new Vector2(300, 70), new Vector2(1, 0.5f));
        }

        public override void OnBack() => Router.Show<HomeScreen>();
    }
}
