using System.Text;
using UnityEngine;

namespace SugarDrop
{
    /// <summary>
    /// Afișează pe ecran prima eroare apărută în joc (mesaj + primele rânduri din stivă) și versiunea build-ului,
    /// ca o problemă să poată fi raportată cu o captură de ecran, fără Console. Se creează singur la pornire.
    /// </summary>
    public class ErrorReporter : MonoBehaviour
    {
        private static ErrorReporter _instance;
        private static string _buildId;

        private string _message;
        private int _count;
        private GUIStyle _box, _text, _button;

        /// <summary>Commit-ul din care e făcut build-ul (din manifestul Unity Build Automation) sau „editor”.</summary>
        public static string BuildId
        {
            get
            {
                if (_buildId != null) return _buildId;
                _buildId = Application.isEditor ? "editor" : "local";
                var manifest = Resources.Load<TextAsset>("UnityCloudBuildManifest.json");
                if (manifest != null)
                {
                    string commit = JsonValue(manifest.text, "scmCommitId");
                    string number = JsonValue(manifest.text, "buildNumber");
                    if (!string.IsNullOrEmpty(commit)) _buildId = (commit.Length > 7 ? commit.Substring(0, 7) : commit) + (string.IsNullOrEmpty(number) ? "" : " #" + number);
                }
                return _buildId;
            }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            if (_instance != null) return;
            var go = new GameObject("ErrorReporter");
            DontDestroyOnLoad(go);
            _instance = go.AddComponent<ErrorReporter>();
        }

        private void OnEnable() => Application.logMessageReceived += OnLog;
        private void OnDisable() => Application.logMessageReceived -= OnLog;

        private void OnLog(string condition, string stackTrace, LogType type)
        {
            if (type != LogType.Exception && type != LogType.Error && type != LogType.Assert) return;
            _count++;
            if (_message != null) return; // păstrăm prima eroare: de obicei ea e cauza
            var builder = new StringBuilder(condition);
            string[] lines = (stackTrace ?? "").Split('\n');
            for (int i = 0, shown = 0; i < lines.Length && shown < 8; i++)
            {
                string line = lines[i].Trim();
                if (line.Length == 0) continue;
                builder.Append('\n').Append(line);
                shown++;
            }
            _message = builder.ToString();
        }

        private void OnGUI()
        {
            if (_message == null) return;
            float scale = Screen.width / 1080f;
            GUI.matrix = Matrix4x4.Scale(new Vector3(scale, scale, 1f));
            float height = Screen.height / scale;
            EnsureStyles();

            var area = new Rect(30f, height * 0.18f, 1020f, height * 0.6f);
            GUI.Box(area, GUIContent.none, _box);
            string header = "EROARE (build " + BuildId + ")" + (_count > 1 ? "  +" + (_count - 1) + " altele" : "");
            GUI.Label(new Rect(area.x + 30f, area.y + 24f, area.width - 60f, area.height - 170f), header + "\n\n" + _message, _text);
            if (GUI.Button(new Rect(area.x + area.width * 0.5f - 160f, area.yMax - 130f, 320f, 100f), "OK", _button))
            {
                _message = null;
                _count = 0;
            }
        }

        private void EnsureStyles()
        {
            if (_box != null) return;
            var background = new Texture2D(1, 1);
            background.SetPixel(0, 0, new Color(0.35f, 0.05f, 0.1f, 0.95f));
            background.Apply();
            _box = new GUIStyle { normal = { background = background } };
            _text = new GUIStyle { fontSize = 30, wordWrap = true, normal = { textColor = Color.white } };
            _button = new GUIStyle(GUI.skin.button) { fontSize = 44 };
        }

        /// <summary>Valoarea unui câmp text simplu dintr-un JSON („"cheie":"valoare"”), fără parser.</summary>
        private static string JsonValue(string json, string key)
        {
            int index = json.IndexOf("\"" + key + "\"", System.StringComparison.Ordinal);
            if (index < 0) return null;
            int colon = json.IndexOf(':', index);
            if (colon < 0) return null;
            int start = colon + 1;
            while (start < json.Length && (json[start] == ' ' || json[start] == '"')) start++;
            int end = start;
            while (end < json.Length && json[end] != '"' && json[end] != ',' && json[end] != '}') end++;
            return json.Substring(start, end - start).Trim();
        }
    }
}
