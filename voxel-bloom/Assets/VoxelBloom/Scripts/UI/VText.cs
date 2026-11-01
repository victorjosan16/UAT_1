using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VoxelBloom
{
    /// <summary>
    /// UI text built on UnityEngine.UI.Text with the bundled Baloo 2 font. It needs no imported
    /// TextMeshPro resources (TMP Essentials are not available in cloud builds). It adds a float font size,
    /// an outline, a vertical gradient and a wrap toggle.
    /// </summary>
    public class VText : Text
    {
        Outline outline;
        VerticalGradient gradient;

        public new float fontSize
        {
            get => base.fontSize;
            set => base.fontSize = Mathf.Max(1, Mathf.RoundToInt(value));
        }

        public bool Wrap
        {
            get => horizontalOverflow == HorizontalWrapMode.Wrap;
            set => horizontalOverflow = value ? HorizontalWrapMode.Wrap : HorizontalWrapMode.Overflow;
        }

        /// <summary>Outline thickness relative to the font size (0.25 ≈ bold sticker outline).</summary>
        public float outlineWidth
        {
            get => outline != null ? outline.effectDistance.x / Mathf.Max(1, base.fontSize) : 0f;
            set
            {
                if (value <= 0f) { if (outline != null) outline.enabled = false; return; }
                EnsureOutline();
                float d = Mathf.Clamp(value * base.fontSize * 0.12f, 1.5f, 10f);
                outline.effectDistance = new Vector2(d, -d);
                outline.enabled = true;
            }
        }

        public Color outlineColor
        {
            get => outline != null ? outline.effectColor : Color.clear;
            set { EnsureOutline(); outline.effectColor = value; }
        }

        public void SetGradient(Color top, Color bottom)
        {
            if (gradient == null) gradient = gameObject.AddComponent<VerticalGradient>();
            gradient.Top = top;
            gradient.Bottom = bottom;
            color = Color.white;
            SetVerticesDirty();
        }

        void EnsureOutline()
        {
            if (outline != null) return;
            outline = gameObject.GetComponent<Outline>();
            if (outline == null) { outline = gameObject.AddComponent<Outline>(); outline.useGraphicAlpha = true; }
        }
    }

    /// <summary>Tints a graphic's vertices from Top to Bottom.</summary>
    public sealed class VerticalGradient : BaseMeshEffect
    {
        public Color Top = Color.white, Bottom = Color.white;
        readonly List<UIVertex> verts = new List<UIVertex>();

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || vh.currentVertCount == 0) return;
            verts.Clear();
            vh.GetUIVertexStream(verts);
            float min = float.MaxValue, max = float.MinValue;
            foreach (var v in verts) { min = Mathf.Min(min, v.position.y); max = Mathf.Max(max, v.position.y); }
            float h = Mathf.Max(0.001f, max - min);
            for (int i = 0; i < verts.Count; i++)
            {
                var v = verts[i];
                Color c = Color.Lerp(Bottom, Top, (v.position.y - min) / h);
                v.color = (Color32)(c * (Color)v.color);
                verts[i] = v;
            }
            vh.Clear();
            vh.AddUIVertexTriangleStream(verts);
        }
    }
}
