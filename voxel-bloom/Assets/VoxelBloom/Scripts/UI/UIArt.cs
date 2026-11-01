using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VoxelBloom
{
    /// <summary>
    /// Premium artwork extracted from the reference asset sheet (Resources/VBUI/...). Sprites are created at runtime
    /// from the textures, with nine-slice borders where a piece must stretch. Every lookup can fail (missing file):
    /// callers fall back to the procedural SpriteFactory look, so the game never breaks because of art.
    /// </summary>
    public static class UIArt
    {
        static readonly Dictionary<string, Sprite> cache = new Dictionary<string, Sprite>();

        public static Sprite Get(string path, Vector4 border = default, bool warnIfMissing = true)
        {
            string key = path + border;
            if (cache.TryGetValue(key, out var s)) return s;
            var tex = Resources.Load<Texture2D>("VBUI/" + path);
            if (tex != null)
            {
                tex.wrapMode = TextureWrapMode.Clamp;
                s = Sprite.Create(tex, new Rect(0, 0, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100f, 0, SpriteMeshType.FullRect, border);
                s.name = path;
            }
            else if (warnIfMissing) Debug.LogWarning("[UIArt] missing VBUI/" + path);
            cache[key] = s;
            return s;
        }

        public static bool Has(string path) => Get(path) != null;

        /// <summary>Simple (aspect-preserving) artwork image, sized by width; height follows the sprite aspect.</summary>
        public static Image Picture(Transform parent, string path, float width, bool raycast = false)
        {
            var sp = Get(path);
            var img = UI.Image(parent, System.IO.Path.GetFileName(path), sp, Color.white, false, raycast);
            img.preserveAspect = true;
            float aspect = sp != null ? sp.rect.height / sp.rect.width : 1f;
            img.rectTransform.sizeDelta = new Vector2(width, width * aspect);
            return img;
        }

        /// <summary>Nine-slice artwork image; pixelsPerUnitMultiplier scales the borders (art is drawn at sheet scale).</summary>
        public static Image Sliced(Transform parent, string path, Vector4 border, Color color, float borderScale = 1f)
        {
            var sp = Get(path, border);
            var img = UI.Image(parent, System.IO.Path.GetFileName(path), sp, color, true);
            img.type = Image.Type.Sliced;
            img.pixelsPerUnitMultiplier = 1f / Mathf.Max(0.01f, borderScale);
            return img;
        }

        /// <summary>
        /// Turns a procedural UIButton into an artwork button: hides the generated shadow/lip/face graphics and puts the
        /// sprite on the face (the press animation, sounds and hit area stay those of UIButton).
        /// </summary>
        public static Image Skin(UIButton button, string path)
        {
            var sp = Get(path);
            if (sp == null) return null;
            foreach (var n in new[] { "Shadow", "Lip" })
            {
                var t = button.transform.Find(n);
                if (t != null) t.gameObject.SetActive(false);
            }
            var face = button.Face.GetComponent<Image>();
            face.sprite = sp;
            face.type = Image.Type.Simple;
            face.preserveAspect = true;
            face.color = Color.white;
            button.Face.anchorMin = Vector2.zero; button.Face.anchorMax = Vector2.one;
            button.Face.offsetMin = Vector2.zero; button.Face.offsetMax = Vector2.zero;
            return face;
        }
    
        /// <summary>Glossy track + glossy green fill from the reference progress bar (nine-slice, live width).</summary>
        public static bool SkinProgress(ProgressBar pb)
        {
            var border = new Vector4(26, 20, 26, 20);
            var track = Get("HUD/progress_track", border);
            var fill = Get("HUD/progress_fill", border);
            if (track == null || fill == null) return false;
            var trackImg = pb.Fill.parent.GetComponent<Image>();
            trackImg.sprite = track; trackImg.type = Image.Type.Sliced; trackImg.color = Color.white;
            var fillImg = pb.Fill.GetComponent<Image>();
            fillImg.sprite = fill; fillImg.type = Image.Type.Sliced; fillImg.color = Color.white;
            pb.Fill.offsetMin = Vector2.zero; pb.Fill.offsetMax = Vector2.zero;
            pb.Inset = 0f; pb.MinWidth = 52f;
            return true;
        }
    }
}
