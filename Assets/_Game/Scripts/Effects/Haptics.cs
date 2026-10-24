using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// Vibrații scurte pe Android (prin API-ul nativ Vibrator). În Editor nu face nimic.
    /// Respectă setarea „Vibrații” din salvare.
    /// </summary>
    public static class Haptics
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        private static AndroidJavaObject _vibrator;
        private static bool _initialized;
#endif

        public static void Light() => Vibrate(12);
        public static void Medium() => Vibrate(28);
        public static void Heavy() => Vibrate(60);

        private static bool Enabled => SaveSystem.Instance == null || SaveSystem.Instance.Data.vibrationOn;

        private static void Vibrate(long milliseconds)
        {
            if (!Enabled) return;
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                if (!_initialized)
                {
                    _initialized = true;
                    using var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                    using var activity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
                    _vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                }
                _vibrator?.Call("vibrate", milliseconds);
            }
            catch (System.Exception)
            {
                _vibrator = null; // dispozitivul nu suportă vibrații; nu mai încercăm
            }
#endif
        }

        // Unity adaugă permisiunea VIBRATE în manifest doar dacă vede Handheld.Vibrate în cod.
        // Metoda nu e apelată niciodată; există doar pentru asta.
        private static void EnsureVibratePermission()
        {
#if UNITY_ANDROID
            Handheld.Vibrate();
#endif
        }
    }
}
