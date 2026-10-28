using System;
using QuarryEmpire;
using UnityEngine;
using UnityEngine.UI;

namespace SugarDrop
{
    /// <summary>
    /// Scena de poveste dinaintea unui nivel, ca un panou de bandă desenată: ilustrația mare (story{nivel}),
    /// tăblița de lemn cu „Nivelul N” și titlul, și balonul cu replica lui Bobo. Tăblița și balonul sunt desenate
    /// de joc exact peste locul lor din imagine, deci textul e tradus. Se închide la atingere, cu „Sari” sau singură.
    /// </summary>
    public class LevelStoryPanel : MonoBehaviour
    {
        private const float CardWidth = 1020f;
        private const float AutoClose = 9f;
        private const float MinShow = 0.6f;

        private GameObject _root;
        private CanvasGroup _group;
        private RectTransform _card;
        private RectTransform _artRect;
        private Image _art;
        private RectTransform _sign;
        private UiLabel _signText;
        private RectTransform _bubble;
        private UiLabel _bubbleText;
        private UiLabel _skipLabel;
        private UiLabel _hint;
        private Action _onDone;
        private float _time = -1f;
        private float _closing = -1f;
        private string _line = "";
        private int _lineSize;
        private int _shown = -1;

        public bool IsOpen => _root != null && _root.activeSelf;

        private void Awake()
        {
            Image dim = UiFactory.CreateImage("Dim", transform, UiFactory.WhiteSprite, new Color(0.08f, 0.1f, 0.18f, 0.9f));
            dim.preserveAspect = false;
            dim.raycastTarget = true;
            UiFactory.Stretch(dim.rectTransform, -300f);
            _root = dim.gameObject;
            _group = _root.AddComponent<CanvasGroup>();
            var tap = _root.AddComponent<Button>();
            tap.transition = Selectable.Transition.None;
            tap.onClick.AddListener(() =>
            {
                if (_time < MinShow) return;
                // Prima atingere cât timp textul încă apare: îl arată întreg; următoarea închide scena.
                if (_shown < _line.Length) _time = Mathf.Max(_time, 0.6f + _line.Length / QuestStyle.LettersPerSecond);
                else Close();
            });

            // Cardul: umbră, ramă albă și ilustrația (decupată la ramă, ca să se poată mări încet).
            _card = UiFactory.CreateRect("Card", _root.transform);
            Image shadow = UiFactory.CreatePanel("Shadow", _card, new Color(0f, 0f, 0f, 0.35f));
            UiFactory.Stretch((RectTransform)shadow.transform);
            ((RectTransform)shadow.transform).anchoredPosition = new Vector2(0f, -14f);
            Image frame = UiFactory.CreatePanel("Frame", _card, Color.white);
            UiFactory.Stretch((RectTransform)frame.transform);
            RectTransform window = UiFactory.CreateRect("Window", _card);
            UiFactory.Stretch(window, 12f);
            window.gameObject.AddComponent<RectMask2D>();
            _art = UiFactory.CreateImage("Art", window, UiFactory.WhiteSprite, Color.white);
            _art.preserveAspect = false;
            _art.raycastTarget = false;
            _artRect = _art.rectTransform;
            UiFactory.Stretch(_artRect);

            // Tăblița de lemn (sus-stânga) și balonul replicii: așezate prin ancore, în coordonatele imaginii.
            Image signEdge = UiFactory.CreatePanel("Sign", window, QuestStyle.WoodDeep);
            _sign = (RectTransform)signEdge.transform;
            Image signFace = UiFactory.CreatePanel("Face", _sign, QuestStyle.Wood);
            UiFactory.Stretch((RectTransform)signFace.transform, 5f);
            _signText = SugarUi.Outlined("Text", _sign, "", 32, Color.white, 2.5f);
            UiFactory.Stretch(_signText.RectTransform, 8f);

            Image bubbleEdge = UiFactory.CreatePanel("Bubble", window, new Color(0.18f, 0.16f, 0.22f));
            _bubble = (RectTransform)bubbleEdge.transform;
            Image bubbleFace = UiFactory.CreatePanel("Face", _bubble, Color.white);
            UiFactory.Stretch((RectTransform)bubbleFace.transform, 4f);
            _bubbleText = UiFactory.CreateText("Text", _bubble, "", 30, TextAnchor.MiddleCenter, new Color(0.16f, 0.12f, 0.2f));
            UiFactory.Stretch(_bubbleText.RectTransform, 12f);

            // „Sari” în zona sigură (nu sub notch): dreptunghiul ecranului, apoi zona sigură din el.
            RectTransform screen = UiFactory.CreateRect("Screen", _root.transform);
            UiFactory.Stretch(screen, 300f);
            RectTransform safe = UiFactory.CreateRect("Safe", screen);
            UiFactory.Stretch(safe);
            safe.gameObject.AddComponent<SafeAreaFitter>();
            Button skip = SugarUi.Button("Skip", safe, "", new Color(1f, 1f, 1f, 0.25f), () => Close(), 34);
            UiFactory.Place((RectTransform)skip.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-30f, -40f), new Vector2(200f, 86f));
            _skipLabel = UiFactory.GetButtonLabel(skip);
            _hint = UiFactory.CreateText("Hint", _root.transform, "", 64, TextAnchor.MiddleCenter, Color.white);
            UiFactory.Place(_hint.RectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(200f, 90f));

            _root.SetActive(false);
        }

        /// <summary>Arată scena nivelului; fără scenă sau fără ilustrație, onDone se cheamă imediat.</summary>
        public void Show(int level, Action onDone)
        {
            Sprite art = null;
            if (_root == null || !LevelStory.TryGet(level, out LevelStoryEntry entry) || (art = ArtLibrary.Find(entry.ArtKey)) == null)
            {
                onDone?.Invoke();
                return;
            }
            _onDone = onDone;
            _art.sprite = art;
            float aspect = art.rect.height / Mathf.Max(1f, art.rect.width);
            float height = CardWidth * aspect;
            UiFactory.Place(_card, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, 40f), new Vector2(CardWidth, height));
            _hint.RectTransform.anchoredPosition = new Vector2(0f, 40f - height * 0.5f - 90f);

            // Tăblița și balonul acoperă textul pictat în imagine (puțin mai mari decât el).
            PlaceNormalized(_sign, 0.004f, 0.004f, entry.SignRight, entry.SignBottom);
            PlaceNormalized(_bubble, entry.BubbleLeft - 0.018f, entry.BubbleTop - 0.022f, entry.BubbleRight + 0.018f, entry.BubbleBottom + 0.022f);
            Vector2 window = new(CardWidth - 24f, height - 24f);
            Vector2 signSize = new((entry.SignRight - 0.004f) * window.x, (entry.SignBottom - 0.004f) * window.y);
            string title = SugarLoc.T(entry.TitleKey);
            int titleSize = FitSize(title, signSize.x - 20f, signSize.y * 0.55f, 1, 34);
            _signText.Text = "<size=" + Mathf.RoundToInt(titleSize * 0.85f) + ">" + SugarLoc.F("NIVELUL {0}", level) + "</size>\n<size=" + titleSize + ">" + title + "</size>";
            string line = SugarLoc.T(entry.LineKey);
            Vector2 bubbleSize = new((entry.BubbleRight - entry.BubbleLeft + 0.036f) * window.x - 28f, (entry.BubbleBottom - entry.BubbleTop + 0.044f) * window.y - 28f);
            int lineSize = FitSize(line, bubbleSize.x, bubbleSize.y, 4, 36);
            // Replica apare literă cu literă, după ce balonul „pocnește”.
            _line = line;
            _lineSize = lineSize;
            _shown = -1;
            _bubbleText.Text = "";
            _skipLabel.Text = SugarLoc.T("SARI") + " ›";

            _time = 0f;
            _closing = -1f;
            _group.alpha = 1f;
            _root.transform.SetAsLastSibling();
            _root.SetActive(true);
            Step();
        }

        private static void PlaceNormalized(RectTransform rect, float left, float top, float right, float bottom)
        {
            rect.anchorMin = new Vector2(Mathf.Clamp01(left), 1f - Mathf.Clamp01(bottom));
            rect.anchorMax = new Vector2(Mathf.Clamp01(right), 1f - Mathf.Clamp01(top));
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        /// <summary>Cea mai mare mărime de literă la care textul încape în dreptunghi (cu rupere pe rânduri).</summary>
        private static int FitSize(string text, float width, float height, int maxLines, int maxSize)
        {
            const float charWidth = 0.52f, lineHeight = 1.18f;
            for (int size = maxSize; size > 14; size--)
            {
                int perLine = Mathf.Max(1, Mathf.FloorToInt(width / (size * charWidth)));
                int lines = CountLines(text, perLine);
                if (lines <= maxLines && lines * size * lineHeight <= height) return size;
            }
            return 14;
        }

        private static int CountLines(string text, int perLine)
        {
            int lines = 1, used = 0;
            foreach (string word in text.Split(' '))
            {
                int length = word.Length;
                if (used == 0) used = length;
                else if (used + 1 + length <= perLine) used += 1 + length;
                else
                {
                    lines++;
                    used = length;
                }
            }
            return lines;
        }

        public void Close()
        {
            if (_closing < 0f && IsOpen) _closing = 0f;
        }

        private void Update()
        {
            if (!IsOpen) return;
            float dt = Time.unscaledDeltaTime;
            _time += dt;
            if (_time >= AutoClose) Close();
            if (_closing >= 0f)
            {
                _closing += dt;
                _group.alpha = 1f - Mathf.Clamp01(_closing / 0.25f);
                if (_closing >= 0.25f)
                {
                    _root.SetActive(false);
                    Action done = _onDone;
                    _onDone = null;
                    done?.Invoke();
                    return;
                }
            }
            Step();
        }

        private void Step()
        {
            // Cardul apare cu o săritură, ilustrația se mărește încet (ca o cameră), balonul „pocnește” după el.
            float pop = Mathf.Clamp01(_time / 0.45f);
            _card.localScale = Vector3.one * Mathf.LerpUnclamped(0.85f, 1f, UiEase.OutBack(pop, 1.6f));
            _artRect.localScale = Vector3.one * (1f + Mathf.Min(_time, AutoClose) * 0.0025f);
            float bubble = Mathf.Clamp01((_time - 0.35f) / 0.3f);
            _bubble.localScale = Vector3.one * UiEase.OutBack(bubble, 2.2f);
            int letters = Mathf.Clamp(Mathf.FloorToInt((_time - 0.6f) * QuestStyle.LettersPerSecond), 0, _line.Length);
            if (letters != _shown)
            {
                _shown = letters;
                _bubbleText.Text = letters == 0 ? "" : "<size=" + _lineSize + ">" + _line.Substring(0, letters) + "</size>";
            }
            _hint.Text = _shown >= _line.Length ? "<size=64>›</size>" : "";
            _hint.Color = new Color(1f, 1f, 1f, 0.45f + 0.4f * Mathf.Abs(Mathf.Sin(_time * 2.5f)));
        }
    }
}
