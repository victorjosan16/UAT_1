using UnityEngine;

namespace TollRoadTycoon.Presentation
{
    /// <summary>Helpers to assemble world visuals from tinted procedural sprites.</summary>
    public static class Shapes
    {
        public static SpriteRenderer Sprite(Transform parent, string name, Sprite sprite, Color color, Vector2 pos, Vector2 size, int order, float rotation = 0)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(pos.x, pos.y, 0);
            go.transform.localRotation = Quaternion.Euler(0, 0, rotation);
            go.transform.localScale = new Vector3(size.x, size.y, 1);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.color = color;
            sr.sortingOrder = order;
            return sr;
        }

        public static SpriteRenderer Rect(Transform parent, string name, Color color, Vector2 pos, Vector2 size, int order, float rotation = 0)
            => Sprite(parent, name, SpriteFactory.Square, color, pos, size, order, rotation);

        public static SpriteRenderer Rounded(Transform parent, string name, Color color, Vector2 pos, Vector2 size, int order, float rotation = 0)
            => Sprite(parent, name, SpriteFactory.RoundedBody, color, pos, size, order, rotation);

        public static SpriteRenderer Circle(Transform parent, string name, Color color, Vector2 pos, float diameter, int order)
            => Sprite(parent, name, SpriteFactory.Circle, color, pos, new Vector2(diameter, diameter), order);

        /// <summary>
        /// Authored sprite from Resources/Art scaled to a world width (aspect kept), or null when the
        /// file does not exist so the caller can draw its placeholder instead.
        /// </summary>
        public static SpriteRenderer Art(Transform parent, string name, string path, long seed, Vector2 pos, float worldWidth, int order)
        {
            var sprite = ArtLibrary.Pick(path, seed);
            if (sprite == null) return null;
            float w = sprite.bounds.size.x;
            float scale = w > 0 ? worldWidth / w : 1f;
            var sr = Sprite(parent, name, sprite, Color.white, pos, new Vector2(scale, scale), order);
            return sr;
        }

        /// <summary>World-space label (legacy TextMesh, works without imported font assets).</summary>
        public static TextMesh Label(Transform parent, string name, string text, Color color, Vector2 pos, float size, int order, bool bold = true)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = new Vector3(pos.x, pos.y, 0);
            var tm = go.AddComponent<TextMesh>();
            tm.font = SpriteFactory.Font;
            tm.text = text;
            tm.color = color;
            tm.anchor = TextAnchor.MiddleCenter;
            tm.alignment = TextAlignment.Center;
            tm.fontSize = 64;
            tm.characterSize = size / 10f;
            tm.fontStyle = bold ? FontStyle.Bold : FontStyle.Normal;
            var mr = go.GetComponent<MeshRenderer>();
            mr.sharedMaterial = tm.font.material;
            mr.sortingOrder = order;
            return tm;
        }
    }

    /// <summary>Sorting orders for world layers.</summary>
    public static class Layers
    {
        public const int Ground = -100;
        public const int Road = -80;
        public const int Marking = -70;
        public const int Island = -60;
        public const int Decor = -50;
        public const int VehicleShadow = -10;
        public const int Vehicle = 0;
        public const int Barrier = 40;
        public const int Booth = 50;
        public const int Canopy = 60;
        public const int Tree = 70;
        public const int Popup = 100;
    }
}
