using System;

namespace Veliqora.UI
{
    /// <summary>Global UI feedback hooks (audio/haptics subscribe; UI code stays decoupled from services).</summary>
    public static class UIFeedback
    {
        public static event Action ButtonPressed;
        public static void RaiseButtonPressed() => ButtonPressed?.Invoke();
    }
}
