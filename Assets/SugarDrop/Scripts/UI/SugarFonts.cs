using QuarryEmpire;
using UnityEngine;
using UnityEngine.UI;

namespace SugarDrop
{
    /// <summary>
    /// Fontul potrivit limbii: Baloo 2 (din temă) pentru limbile cu litere latine, Nunito Black pentru rusă și ucraineană
    /// (Baloo nu are litere chirilice). La schimbarea limbii, toate textele deja create primesc fontul nou.
    /// </summary>
    public static class SugarFonts
    {
        private const string CyrillicPath = "Fonts/Nunito-Black";
        private static Font _cyrillic;

        private static Font Cyrillic => _cyrillic != null ? _cyrillic : _cyrillic = Resources.Load<Font>(CyrillicPath);

        public static void Apply()
        {
            UiTheme theme = UiRoot.Theme;
            Font latin = theme != null ? theme.Font : null;
            Font wanted = SugarLoc.NeedsCyrillic && Cyrillic != null ? Cyrillic : null;
            UiLabel.FontOverride = wanted;
            Font target = wanted != null ? wanted : latin;
            if (target == null) return;
            foreach (Text text in Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                // Doar textele jocului (fontul temei sau cel chirilic), nu alte fonturi alese anume.
                if (text.font == target || (text.font != latin && text.font != Cyrillic)) continue;
                text.font = target;
            }
        }
    }
}
