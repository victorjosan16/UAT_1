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
        private readonly System.Collections.Generic.List<SpriteRenderer> _canes = new();
        private readonly System.Collections.Generic.List<SpriteRenderer> _stripes = new();

        /// <summary>Gura borcanului (pentru sclipiri și mascotă).</summary>
        public Vector3 JarMouth { get; private set; }
        public Vector3 JarCenter { get; private set; }
        public float JarHalfWidth => jarHalfWidth;

        /// <param name="halfWidth">Jumătate din lățimea tablei (pereții stau lipiți de ea).</param>
        public void Build(float halfWidth, float boardBottom, float boardTop, float topY)
        {
            _panel = ProceduralSprites.RoundedRect;
            float t = wallThickness;

            // Fundalul tablei (ușor transparent, ca să se vadă degradeul).
            _boardBack = Visual("BoardBack", new Vector2(0f, (boardBottom + topY) * 0.5f), new Vector2(halfWidth * 2f + 0.3f, topY - boardBottom + 0.3f),
                new Color(Palette.BoardBack.r, Palette.BoardBack.g, Palette.BoardBack.b, 0.85f), 0, 0f).GetComponent<SpriteRenderer>();

            // Pereți laterali, de sub tablă până sus.
            float wallBottom = boardBottom - 0.2f;
            Cane("WallLeft", new Vector2(-halfWidth - t * 0.5f, (wallBottom + topY) * 0.5f), new Vector2(t, topY - wallBottom), 0f);
            Cane("WallRight", new Vector2(halfWidth + t * 0.5f, (wallBottom + topY) * 0.5f), new Vector2(t, topY - wallBottom), 0f);
            Knob(new Vector2(-halfWidth - t * 0.5f, topY));
            Knob(new Vector2(halfWidth + t * 0.5f, topY));

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

        /// <summary>Culorile capitolului: fundalul tablei, bastoanele, gulerul borcanului.</summary>
        public void ApplyTheme(int chapter)
        {
            ChapterTheme theme = ChapterTheme.For(chapter);
            if (_boardBack != null) _boardBack.color = new Color(theme.BoardBack.r, theme.BoardBack.g, theme.BoardBack.b, 0.85f);
            Sprite cane = SugarSprites.Cane(chapter);
            foreach (SpriteRenderer renderer in _canes) renderer.sprite = cane;
            foreach (SpriteRenderer renderer in _stripes) renderer.color = theme.Stripe;
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
