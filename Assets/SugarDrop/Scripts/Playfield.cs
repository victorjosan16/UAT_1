using QuarryEmpire;
using UnityEngine;

namespace SugarDrop
{
    /// <summary>
    /// Construiește din cod „cutia” jocului: pereții laterali, pâlnia de sub tablă și borcanul.
    /// Bomboanele care cad prin găurile tablei alunecă pe pâlnie direct în borcan.
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

        /// <param name="halfWidth">Jumătate din lățimea tablei (pereții stau lipiți de ea).</param>
        public void Build(float halfWidth, float boardBottom, float boardTop, float topY)
        {
            _panel = ProceduralSprites.RoundedRect;
            float t = wallThickness;

            // Fundalul tablei și al zonei cu bomboane.
            Visual("BoardBack", new Vector2(0f, (boardBottom + topY) * 0.5f), new Vector2(halfWidth * 2f + 0.3f, topY - boardBottom + 0.3f),
                Palette.BoardBack, 0, 0f);

            // Pereți laterali, de sub tablă până sus.
            float wallBottom = boardBottom - 0.2f;
            Solid("WallLeft", new Vector2(-halfWidth - t * 0.5f, (wallBottom + topY) * 0.5f), new Vector2(t, topY - wallBottom), 0f);
            Solid("WallRight", new Vector2(halfWidth + t * 0.5f, (wallBottom + topY) * 0.5f), new Vector2(t, topY - wallBottom), 0f);

            // Pâlnia: două pante care duc bomboanele spre gura borcanului.
            float jarTop = wallBottom - funnelDrop;
            Slope("FunnelLeft", new Vector2(-halfWidth, wallBottom), new Vector2(-jarHalfWidth, jarTop));
            Slope("FunnelRight", new Vector2(halfWidth, wallBottom), new Vector2(jarHalfWidth, jarTop));

            // Borcanul: doi pereți și fundul; în interior, zona care numără bomboanele.
            float jarBottom = jarTop - jarHeight;
            Solid("JarLeft", new Vector2(-jarHalfWidth - t * 0.5f, (jarBottom + jarTop) * 0.5f), new Vector2(t, jarTop - jarBottom), 0f);
            Solid("JarRight", new Vector2(jarHalfWidth + t * 0.5f, (jarBottom + jarTop) * 0.5f), new Vector2(t, jarTop - jarBottom), 0f);
            Solid("JarBottom", new Vector2(0f, jarBottom - t * 0.5f), new Vector2(jarHalfWidth * 2f + t * 2f, t), 0f);
            Visual("JarGlass", new Vector2(0f, (jarBottom + jarTop) * 0.5f), new Vector2(jarHalfWidth * 2f, jarTop - jarBottom), Palette.Glass, 4, 0f);

            jar.transform.position = new Vector3(0f, (jarBottom + jarTop) * 0.5f, 0f);
            var trigger = jar.gameObject.AddComponent<BoxCollider2D>();
            trigger.isTrigger = true;
            trigger.size = new Vector2(jarHalfWidth * 2f, jarTop - jarBottom);
        }

        private void Solid(string name, Vector2 center, Vector2 size, float angle)
        {
            GameObject go = Visual(name, center, size, Palette.Wall, 8, angle);
            var collider = go.AddComponent<BoxCollider2D>();
            collider.size = size;
            if (material != null) collider.sharedMaterial = material;
        }

        private void Slope(string name, Vector2 from, Vector2 to)
        {
            Vector2 middle = (from + to) * 0.5f;
            Vector2 delta = to - from;
            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg;
            Solid(name, middle, new Vector2(delta.magnitude + wallThickness, wallThickness), angle);
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
