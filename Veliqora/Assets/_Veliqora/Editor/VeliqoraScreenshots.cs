using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Veliqora.EditorTools
{
    /// <summary>Captures a Game-view screenshot while in Play Mode into Docs/Screenshots (for store/QA checklists).</summary>
    public static class VeliqoraScreenshots
    {
        [MenuItem("Veliqora/Capture Screenshot (Play Mode)", priority = 40)]
        public static void Capture()
        {
            if (!Application.isPlaying)
            {
                EditorUtility.DisplayDialog("Veliqora", "Enter Play Mode first (portrait Game view, e.g. 1080x1920).", "OK");
                return;
            }
            Directory.CreateDirectory("Docs/Screenshots");
            string path = "Docs/Screenshots/veliqora_" + DateTime.Now.ToString("yyyyMMdd_HHmmss") + ".png";
            ScreenCapture.CaptureScreenshot(path);
            Debug.Log("[Veliqora] Screenshot will be written to " + path);
        }
    }
}
