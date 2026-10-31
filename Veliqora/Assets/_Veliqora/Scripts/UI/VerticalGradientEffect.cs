using UnityEngine;
using UnityEngine.UI;

namespace Veliqora.UI
{
    /// <summary>Vertical two-color gradient applied to a Text/Graphic's vertices (used by the logo wordmark).</summary>
    public sealed class VerticalGradientEffect : BaseMeshEffect
    {
        public Color Top = Color.white;
        public Color Bottom = Color.gray;

        public override void ModifyMesh(VertexHelper vh)
        {
            if (!IsActive() || vh.currentVertCount == 0) return;
            float minY = float.MaxValue, maxY = float.MinValue;
            var v = new UIVertex();
            for (int i = 0; i < vh.currentVertCount; i++)
            {
                vh.PopulateUIVertex(ref v, i);
                minY = Mathf.Min(minY, v.position.y);
                maxY = Mathf.Max(maxY, v.position.y);
            }
            float h = Mathf.Max(1f, maxY - minY);
            for (int i = 0; i < vh.currentVertCount; i++)
            {
                vh.PopulateUIVertex(ref v, i);
                var c = Color.Lerp(Bottom, Top, (v.position.y - minY) / h);
                v.color = (Color32)(c * (Color)v.color);
                vh.SetUIVertex(v, i);
            }
        }
    }
}
