using UnityEngine;

namespace Veliqora.Infrastructure
{
    /// <summary>Marker placed in each generated scene so opening it directly in the Editor boots that screen.</summary>
    public sealed class SceneEntry : MonoBehaviour
    {
        public ScreenId Screen = ScreenId.Bootstrap;
    }
}
