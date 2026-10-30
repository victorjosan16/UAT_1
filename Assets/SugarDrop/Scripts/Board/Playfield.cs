using QuarryEmpire;
using UnityEngine;

namespace SugarDrop
{
    /// <summary>
    /// Construiește din cod „cutia” jocului: pereții laterali (bastoane de bomboane), pâlnia de sub tablă și borcanul.
    /// Bomboanele care ies pe jos din tablă alunecă pe pâlnie direct în borcan.
    /// </summary>
    public class Playfield : MonoBehaviour
    {
        [SerializeField] private Jar jar;
        [SerializeField] private PhysicsMaterial2D material;
        [SerializeField] private float wallThickness = 0.3f;
        [SerializeField] private float jarHalfWidth = 1.9f;
        [SerializeField] private float jarHeight = 2.9f;
        [SerializeField] private float funnelDrop = 2.1f;

        private Sprite _panel;
        private SpriteRenderer _boardBack;
        /// <summary>Cadrul tablei: o margine mai deschisă în jurul panoului închis (aspect de panou premium).</summary>
        private SpriteRenderer _boardFrame;
        private readonly System.Collections.Generic.List<SpriteRenderer> _canes = new();
        private readonly System.Collections.Generic.List<SpriteRenderer> _stripes = new();
        /// <summary>Ornamentele ținutului de pe marginea de sus a tablei (se leagănă ușor).</summary>
        private readonly System.Collections.Generic.List<SpriteRenderer> _ornaments = new();
        private readonly System.Collections.Generic.List<Vector3> _ornamentBase = new();
        private int _theme;

        /// <summary>Gura borcanului (pentru sclipiri și mascotă).</summary>
        public Vector3 JarMouth { get; private set; }
        public Vector3 JarCenter { get; private set; }
        public float JarHalfWidth => jarHalfWidth;

        /// <param name="halfWidth">Jumătate din lățimea tablei (pereții stau lipiți de ea).</param>
        public void Build(float halfWidth, float boardBottom, float boardTop, float topY)
        {
            _panel = ProceduralSprites.RoundedRect;
            float t = wallThickness;

            // Umbra moale a tablei (Soft Pop: tabla e o tavă albă care plutește puțin deasupra fundalului).
            Visual("BoardShadow", new Vector2(0f, (boardBottom + topY) * 0.5f - 0.12f), new Vector2(halfWidth * 2f + 0.5f, topY - boardBottom + 0.5f),
                QuestStyle.Shadow, -2, 0f);
            _boardFrame = Visual("BoardFrame", new Vector2(0f, (boardBottom + topY) * 0.5f), new Vector2(halfWidth * 2f + 0.55f, topY - boardBottom + 0.55f),
                Color.Lerp(QuestStyle.Board, Color.white, 0.3f), -1, 0f).GetComponent<SpriteRenderer>();
            // Fundalul tablei (ușor transparent, ca să se vadă degradeul).
            _boardBack = Visual("BoardBack", new Vector2(0f, (boardBottom + topY) * 0.5f), new Vector2(halfWidth * 2f + 0.3f, topY - boardBottom + 0.3f),
                new Color(Palette.BoardBack.r, Palette.BoardBack.g, Palette.BoardBack.b, 0.85f), 0, 0f).GetComponent<SpriteRenderer>();

            // Pereți laterali, de sub tablă până sus.
            float wallBottom = boardBottom - 0.2f;
            Cane("WallLeft", new Vector2(-halfWidth - t * 0.5f, (wallBottom + topY) * 0.5f), new Vector2(t, topY - wallBottom), 0f);
            Cane("WallRight", new Vector2(halfWidth + t * 0.5f, (wallBottom + topY) * 0.5f), new Vector2(t, topY - wallBottom), 0f);
            Knob(new Vector2(-halfWidth - t * 0.5f, topY));
            Knob(new Vector2(halfWidth + t * 0.5f, topY));
            Ornaments(halfWidth + t * 0.5f, topY);

            // Pâlnia: două pante care duc bomboanele spre gura borcanului.
            float jarTop = wallBottom - funnelDrop;
            Slope("FunnelLeft", new Vector2(-halfWidth, wallBottom), new Vector2(-jarHalfWidth, jarTop));
            Slope("FunnelRight", new Vector2(halfWidth, wallBottom), new Vector2(jarHalfWidth, jarTop));

            // Borcanul: doi pereți și fundul; în interior, zona care numără bomboanele.
            float jarBottom = jarTop - jarHeight;
            Solid("JarLeft", new Vector2(-jarHalfWidth - t * 0.5f, (jarBottom + jarTop) * 0.5f), new Vector2(t, jarTop - jarBottom), 0f,
                new Color(1f, 1f, 1f, 0.75f));
            Solid("JarRight", new Vector2(jarHalfWidth + t * 0.5f, (jarBottom + jarTop) * 0.5f), new Vector2(t, jarTop - jarBottom), 0f,
                new Color(1f, 1f, 1f, 0.75f));
            Solid("JarBottom", new Vector2(0f, jarBottom - t * 0.5f), new Vector2(jarHalfWidth * 2f + t * 2f, t), 0f,
                new Color(1f, 1f, 1f, 0.85f));
            Visual("JarGlass", new Vector2(0f, (jarBottom + jarTop) * 0.5f), new Vector2(jarHalfWidth * 2f, jarTop - jarBottom), Palette.Glass, 4, 0f);
            // Reflexii pe sticlă și un guler roz la gura borcanului.
            Visual("JarShine", new Vector2(-jarHalfWidth + 0.35f, (jarBottom + jarTop) * 0.5f + 0.2f), new Vector2(0.16f, (jarTop - jarBottom) * 0.7f),
                new Color(1f, 1f, 1f, 0.55f), 7, 0f);
            Visual("JarShine2", new Vector2(-jarHalfWidth + 0.62f, (jarBottom + jarTop) * 0.5f + 0.5f), new Vector2(0.08f, (jarTop - jarBottom) * 0.35f),
                new Color(1f, 1f, 1f, 0.45f), 7, 0f);
            _stripes.Add(Visual("RimLeft", new Vector2(-jarHalfWidth - t * 0.5f, jarTop + 0.05f), new Vector2(t + 0.25f, 0.28f), Palette.WallStripe, 9, 0f).GetComponent<SpriteRenderer>());
            _stripes.Add(Visual("RimRight", new Vector2(jarHalfWidth + t * 0.5f, jarTop + 0.05f), new Vector2(t + 0.25f, 0.28f), Palette.WallStripe, 9, 0f).GetComponent<SpriteRenderer>());
            Visual("Label", new Vector2(0f, jarBottom + (jarTop - jarBottom) * 0.3f), new Vector2(jarHalfWidth * 1.3f, 0.7f),
                new Color(1f, 0.56f, 0.69f, 0.35f), 3, 0f);

            JarMouth = new Vector3(0f, jarTop, 0f);
            JarCenter = new Vector3(0f, (jarBottom + jarTop) * 0.5f, 0f);
            jar.transform.position = JarCenter;
            var trigger = jar.gameObject.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(jarHalfWidth * 2f, jarTop - jarBottom);
        }

        private static readonly string[] JarParts =
            { "FunnelLeft", "FunnelRight", "JarLeft", "JarRight", "JarBottom", "JarGlass", "JarShine", "JarShine2", "RimLeft", "RimRight", "Label" };

        /// <summary>
        /// Pâlnia și borcanul apar doar unde au rost (bomboane-ingredient, salvarea cu sirop); în rest, sub tablă
        /// e scena cu pericolul lui Bobo.
        /// </summary>
        public void SetJarVisible(bool visible)
        {
            foreach (Transform child in transform)
                if (System.Array.IndexOf(JarParts, child.name) >= 0) child.gameObject.SetActive(visible);
        }

        /// <summary>Culorile capitolului: fundalul tablei, bastoanele, gulerul borcanului.</summary>
        public void ApplyTheme(int chapter)
        {
            ChapterTheme theme = ChapterTheme.For(chapter);
            if (_boardBack != null) _boardBack.color = new Color(theme.BoardBack.r, theme.BoardBack.g, theme.BoardBack.b, 0.92f);
            if (_boardFrame != null) _boardFrame.color = Color.Lerp(theme.BoardBack, Color.white, 0.3f);
            Sprite cane = SugarSprites.Cane(chapter);
            foreach (SpriteRenderer renderer in _canes) renderer.sprite = cane;
            foreach (SpriteRenderer renderer in _stripes) renderer.color = theme.Stripe;
            _theme = ChapterTheme.IndexOf(chapter);
            Sprite ornament = LandStyle.Ornament(_theme);
            Color color = LandStyle.OrnamentColor(_theme);
            foreach (SpriteRenderer renderer in _ornaments)
            {
                renderer.sprite = ornament;
                renderer.color = color;
                renderer.transform.localScale = Vector3.one * (0.62f / ornament.bounds.size.x);
            }
        }

        /// <summary>Câte un ornament deasupra fiecărui stâlp, plus doi mai mici alături.</summary>
        private void Ornaments(float x, float topY)
        {
            Vector2[] offsets = { new(0f, 0.45f), new(-0.35f, 0.25f), new(0.35f, 0.25f) };
            for (int side = -1; side <= 1; side += 2)
                foreach (Vector2 offset in offsets)
                {
                    var go = new GameObject("Ornament");
                    go.transform.SetParent(transform, false);
                    Vector3 position = new Vector3(side * x + offset.x * side, topY + offset.y, 0f);
                    go.transform.position = position;
                    var renderer = go.AddComponent<SpriteRenderer>();
                    renderer.sprite = SugarSprites.Heart;
                    renderer.sortingOrder = 10;
                    _ornaments.Add(renderer);
                    _ornamentBase.Add(position);
                }
        }

        /// <summary>Animația ornamentelor după ținut: plutesc, se rotesc sau sclipesc.</summary>
        private void Update()
        {
            float time = Time.time;
            AmbientMotion motion = LandStyle.Motion(_theme);
            for (int i = 0; i < _ornaments.Count; i++)
            {
                Transform ornament = _ornaments[i].transform;
                float phase = i * 1.3f;
                float small = i % 3 == 0 ? 1f : 0.65f;
                Sprite sprite = _ornaments[i].sprite;
                float scale = 0.62f * small / sprite.bounds.size.x;
                switch (motion)
                {
                    case AmbientMotion.Twinkle:
                        ornament.localScale = Vector3.one * scale * (0.8f + 0.25f * Mathf.Abs(Mathf.Sin(time * 2.2f + phase)));
                        ornament.localRotation = Quaternion.Euler(0f, 0f, time * 30f + phase * 40f);
                        break;
                    case AmbientMotion.Flutter:
                        ornament.localScale = Vector3.one * scale;
                        ornament.localRotation = Quaternion.Euler(0f, 0f, Mathf.Sin(time * 2f + phase) * 18f);
                        break;
                    default:
                        ornament.localScale = Vector3.one * scale * (1f + 0.06f * Mathf.Sin(time * 3f + phase));
                        ornament.localRotation = Quaternion.identity;
                        break;
                }
                ornament.position = _ornamentBase[i] + Vector3.up * (Mathf.Sin(time * 1.6f + phase) * 0.07f);
            }
        }

        private void Solid(string name, Vector2 center, Vector2 size, float angle, Color? color = null)
        {
            GameObject go = Visual(name, center, size, color ?? Palette.Wall, 8, angle);
            AddCollider(go, size);
        }

        /// <summary>Perete cu dungi diagonale roz-albe (baston de bomboană).</summary>
        private void Cane(string name, Vector2 center, Vector2 size, float angle)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.SetPositionAndRotation(center, Quaternion.Euler(0f, 0f, angle));
            var spriteRenderer = go.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = SugarSprites.CandyCane;
            _canes.Add(spriteRenderer);
            spriteRenderer.drawMode = SpriteDrawMode.Tiled;
            spriteRenderer.tileMode = SpriteTileMode.Continuous;
            spriteRenderer.size = size;
            spriteRenderer.sortingOrder = 8;
            AddCollider(go, size);
        }

        private void Knob(Vector2 center)
        {
            var go = new GameObject("Knob");
            go.transform.SetParent(transform, false);
            go.transform.position = center;
            var spriteRenderer = go.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = SugarSprites.Circle;
            spriteRenderer.color = Palette.WallStripe;
            spriteRenderer.sortingOrder = 9;
            _stripes.Add(spriteRenderer);
            go.transform.localScale = Vector3.one * (wallThickness * 1.6f / spriteRenderer.sprite.bounds.size.x);
        }

        private void AddCollider(GameObject go, Vector2 size)
        {
            var collider = go.AddComponent<BoxCollider2D>();
            collider.size = size;
            if (material != null) collider.sharedMaterial = material;
        }

        private void Slope(string name, Vector2 from, Vector2 to)
        {
            Vector2 middle = (from + to) * 0.5f;
            Vector2 delta = to - from;
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            Cane(name, middle, new Vector2(delta.magnitude + wallThickness, wallThickness), angle);
        }

        private GameObject Visual(string name, Vector2 center, Vector2 size, Color color, int order, float angle)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.SetPositionAndRotation(center, Quaternion.Euler(0f, 0f, angle));
            var spriteRenderer = go.AddComponent<SpriteRenderer>();
            spriteRenderer.sprite = _panel;
            spriteRenderer.drawMode = SpriteDrawMode.Sliced;
            spriteRenderer.size = size;
            spriteRenderer.color = color;
            spriteRenderer.sortingOrder = order;
            return go;
        }
    }
}
