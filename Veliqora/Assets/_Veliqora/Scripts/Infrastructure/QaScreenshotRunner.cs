using System.Collections;
using System.IO;
using UnityEngine;

namespace Veliqora.Infrastructure
{
    /// <summary>
    /// Captures REAL in-game screenshots of the QA levels (1, 5, 11, 25, 50, 100) in Play Mode: unlocks the level,
    /// opens it, waits for the board to settle and writes Docs/Screenshots/real_level_NNN.png. Started from the editor
    /// menu Veliqora → Capture QA Level Screenshots (Play Mode). Progress is restored afterwards.
    /// </summary>
    public sealed class QaScreenshotRunner : MonoBehaviour
    {
        public static readonly int[] Levels = { 1, 5, 11, 25, 50, 100 };
        public string OutputDir = "Docs/Screenshots";

        public static void Run(string outputDir = null)
        {
            var go = new GameObject("QaScreenshotRunner");
            DontDestroyOnLoad(go);
            var r = go.AddComponent<QaScreenshotRunner>();
            if (!string.IsNullOrEmpty(outputDir)) r.OutputDir = outputDir;
            r.StartCoroutine(r.Capture());
        }

        private IEnumerator Capture()
        {
            var s = AppRoot.Instance != null ? AppRoot.Instance.Services : null;
            if (s == null) { Debug.LogError("[Veliqora] QA capture: AppRoot not booted."); Destroy(gameObject); yield break; }
            Directory.CreateDirectory(OutputDir);
            int previousUnlocked = s.Save.Progress.HighestUnlocked;
            UI.Overlays.CloseAll();
            foreach (int n in Levels)
            {
                if (n > s.Levels.Count) continue;
                s.Save.Progress.HighestUnlocked = Mathf.Max(previousUnlocked, n);
                s.Save.Session = null;
                while (s.Navigator.IsTransitioning) yield return null;
                s.Navigator.Go(ScreenId.Gameplay, n);
                while (s.Navigator.CurrentId != ScreenId.Gameplay || s.Navigator.IsTransitioning) yield return null;
                yield return new WaitForSecondsRealtime(1.2f);   // board built, intro tweens finished
                UI.Overlays.CloseAll();
                yield return new WaitForEndOfFrame();
                string path = Path.Combine(OutputDir, "real_level_" + n.ToString("000") + ".png");
                ScreenCapture.CaptureScreenshot(path);
                Debug.Log("[Veliqora] QA screenshot → " + path);
                yield return new WaitForSecondsRealtime(0.3f);
            }
            s.Save.Progress.HighestUnlocked = previousUnlocked;
            s.Save.Session = null;
            s.Navigator.Go(ScreenId.MainMenu);
            Destroy(gameObject);
        }
    }
}
