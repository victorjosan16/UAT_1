using System;
using UnityEngine;

namespace Veliqora.UI
{
    /// <summary>Runs a callback when its GameObject is destroyed (used to unsubscribe UI from service events).</summary>
    public sealed class OnDestroyHook : MonoBehaviour
    {
        public Action Action;
        private void OnDestroy() => Action?.Invoke();
    }
}
