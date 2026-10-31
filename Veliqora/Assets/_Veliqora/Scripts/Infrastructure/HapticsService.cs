using UnityEngine;
using Veliqora.Core.Save;

namespace Veliqora.Infrastructure
{
    /// <summary>Short, optional vibrations. Android uses the system Vibrator with millisecond durations.</summary>
    public sealed class HapticsService
    {
        private readonly SettingsData _settings;
#if UNITY_ANDROID && !UNITY_EDITOR
        private AndroidJavaObject _vibrator;
        private bool _resolved;
#endif

        public HapticsService(SettingsData settings) { _settings = settings; }

        public void Light() => Vibrate(12);
        public void Medium() => Vibrate(24);
        public void Success() => Vibrate(40);

        private void Vibrate(long ms)
        {
            if (_settings == null || !_settings.Haptics) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                if (!_resolved)
                {
                    _resolved = true;
                    using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                    {
                        var activity = player.GetStatic<AndroidJavaObject>("currentActivity");
                        _vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                    }
                }
                if (_vibrator != null) { _vibrator.Call("vibrate", ms); return; }
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[Veliqora] Vibrator unavailable: " + e.Message);
                _vibrator = null;
            }
            if (ms >= 40) Handheld.Vibrate(); // fallback (also makes Unity add the VIBRATE permission)
#endif
        }
    }
}
