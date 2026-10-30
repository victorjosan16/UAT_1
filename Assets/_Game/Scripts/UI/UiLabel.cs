using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace QuarryEmpire
{
    /// <summary>
    /// Text de UI care folosește TextMeshPro când tema are un font TMP valid și textul clasic
    /// (cu fontul TTF al temei) altfel. Restul codului lucrează doar cu UiLabel, deci jocul
    /// arată bine și dacă TMP nu e disponibil în build.
    /// </summary>
    public class UiLabel : MonoBehaviour
    {
        /// <summary>Font folosit în locul celui din temă (ex. un font cu litere chirilice pentru rusă); null = fontul temei.</summary>
        public static Font FontOverride { get; set; }

        private TMP_Text _tmp;
        private Text _legacy;

        public RectTransform RectTransform => (RectTransform)transform;

        public string Text
        {
            get => _tmp != null ? _tmp.text : _legacy != null ? _legacy.text : "";
            set
            {
                if (_tmp != null) { if (_tmp.text != value) _tmp.text = value; }
                else if (_legacy != null && _legacy.text != value) _legacy.text = value;
            }
        }

        public Color Color
        {
            get => _tmp != null ? _tmp.color : _legacy != null ? _legacy.color : Color.white;
            set
            {
                if (_tmp != null) _tmp.color = value;
                else if (_legacy != null) _legacy.color = value;
            }
        }

        /// <summary>
        /// Textul se micșorează singur (între min și max) ca să încapă în dreptunghiul etichetei, în loc să iasă din el
        /// și să se suprapună peste elementele vecine (traduceri mai lungi, texte pe două sau trei rânduri).
        /// </summary>
        public void Fit(float minSize, float maxSize)
        {
            if (_tmp != null)
            {
                _tmp.enableAutoSizing = true;
                _tmp.fontSizeMin = minSize;
                _tmp.fontSizeMax = maxSize;
            }
            else if (_legacy != null)
            {
                _legacy.resizeTextForBestFit = true;
                _legacy.resizeTextMinSize = Mathf.RoundToInt(minSize);
                _legacy.resizeTextMaxSize = Mathf.RoundToInt(maxSize);
            }
        }

        /// <summary>Înălțimea de care are nevoie textul curent la lățimea dată (pentru ferestre care se adaptează la text).</summary>
        public float MeasureHeight(float width)
        {
            if (_tmp != null) return _tmp.GetPreferredValues(_tmp.text, width, 0f).y;
            if (_legacy != null) return _legacy.cachedTextGeneratorForLayout.GetPreferredHeight(_legacy.text, _legacy.GetGenerationSettings(new Vector2(width, 0f))) / Mathf.Max(0.01f, _legacy.pixelsPerUnit);
            return 0f;
        }

        /// <summary>Creează o etichetă nouă sub „parent”.</summary>
        public static UiLabel Create(string name, Transform parent, string text, float fontSize,
            TextAnchor alignment, Color color)
        {
            RectTransform rect = UiFactory.CreateRect(name, parent);
            var label = rect.gameObject.AddComponent<UiLabel>();
            UiTheme theme = UiRoot.Theme;

            if (theme != null && theme.TmpFont != null && TryCreateTmp(label, theme.TmpFont, text, fontSize, alignment, color))
                return label;

            var legacy = rect.gameObject.AddComponent<Text>();
            legacy.font = FontOverride != null ? FontOverride : theme != null && theme.Font != null ? theme.Font : UiFactory.BuiltinFont;
            legacy.text = text;
            legacy.fontSize = Mathf.RoundToInt(fontSize);
            legacy.alignment = alignment;
            legacy.color = color;
            legacy.horizontalOverflow = HorizontalWrapMode.Wrap;
            legacy.verticalOverflow = VerticalWrapMode.Overflow;
            legacy.raycastTarget = false;
            legacy.supportRichText = true;
            label._legacy = legacy;
            return label;
        }

        private static bool TryCreateTmp(UiLabel label, TMP_FontAsset font, string text, float fontSize,
            TextAnchor alignment, Color color)
        {
            try
            {
                var tmp = label.gameObject.AddComponent<TextMeshProUGUI>();
                tmp.font = font;
                tmp.text = text;
                tmp.fontSize = fontSize;
                tmp.alignment = ToTmpAlignment(alignment);
                tmp.color = color;
                tmp.raycastTarget = false;
                tmp.richText = true;
                label._tmp = tmp;
                return true;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("TextMeshPro indisponibil, folosim textul clasic: " + e.Message);
                var broken = label.GetComponent<TextMeshProUGUI>();
                if (broken != null) Destroy(broken);
                return false;
            }
        }

        private static TextAlignmentOptions ToTmpAlignment(TextAnchor anchor) => anchor switch
        {
            TextAnchor.UpperLeft => TextAlignmentOptions.TopLeft,
            TextAnchor.UpperCenter => TextAlignmentOptions.Top,
            TextAnchor.UpperRight => TextAlignmentOptions.TopRight,
            TextAnchor.MiddleLeft => TextAlignmentOptions.Left,
            TextAnchor.MiddleRight => TextAlignmentOptions.Right,
            TextAnchor.LowerLeft => TextAlignmentOptions.BottomLeft,
            TextAnchor.LowerCenter => TextAlignmentOptions.Bottom,
            TextAnchor.LowerRight => TextAlignmentOptions.BottomRight,
            _ => TextAlignmentOptions.Center,
        };
    }
}
