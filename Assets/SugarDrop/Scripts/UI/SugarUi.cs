using QuarryEmpire;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace SugarDrop
{
    /// <summary>Bucăți de interfață comune pentru ecranele Sugar Drop (fundal întunecat, cutii, contur pe text).</summary>
    public static class SugarUi
    {
        /// <summary>Strat semi-transparent pe tot ecranul care blochează atingerile.</summary>
        public static Image Dim(Transform parent, float alpha = 0.55f)
        {
            Image dim = UiFactory.CreateImage("Dim", parent, UiFactory.WhiteSprite, new Color(QuestStyle.Scrim.r, QuestStyle.Scrim.g, QuestStyle.Scrim.b, alpha * 0.8f));
            dim.preserveAspect = false;
            dim.raycastTarget = true;
            UiFactory.Stretch(dim.rectTransform, -300f);
            return dim;
        }

        /// <summary>Cutie deschisă la culoare, cu o „panglică” roz sus și animație de apariție.</summary>
        public static RectTransform Box(Transform parent, Vector2 size)
        {
            // Containerul animat (apare cu „pop”), cu umbra în spate și cutia deasupra.
            RectTransform holder = UiFactory.CreateRect("BoxHolder", parent);
            UiFactory.Place(holder, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
            holder.gameObject.AddComponent<CanvasGroup>();
            holder.gameObject.AddComponent<PopIn>();
            Image shadow = UiFactory.CreatePanel("BoxShadow", holder, QuestStyle.Shadow);
            UiFactory.Place((RectTransform)shadow.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0f, -14f), size);
            Image box = UiFactory.CreatePanel("Box", holder, UiRoot.Theme.PanelLightColor);
            var rect = UiFactory.Place((RectTransform)box.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, size);
            box.raycastTarget = true;
            Image ribbon = UiFactory.CreatePanel("Ribbon", rect, Palette.WallStripe);
            UiFactory.Place((RectTransform)ribbon.transform, new Vector2(0.5f, 1f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(size.x * 0.7f, 26f));
            return rect;
        }

        /// <summary>Schimbă mărimea unei cutii create cu Box (cutie, umbră, panglică).</summary>
        public static void ResizeBox(RectTransform box, Vector2 size)
        {
            box.sizeDelta = size;
            var holder = (RectTransform)box.parent;
            holder.sizeDelta = size;
            ((RectTransform)holder.Find("BoxShadow")).sizeDelta = size;
            ((RectTransform)box.Find("Ribbon")).sizeDelta = new Vector2(size.x * 0.7f, 26f);
        }

        /// <summary>Text cu contur închis (se citește pe orice fundal).</summary>
        public static UiLabel Outlined(string name, Transform parent, string text, float size, Color color, float outline = 3f)
        {
            UiLabel label = UiFactory.CreateText(name, parent, text, size, TextAnchor.MiddleCenter, color);
            var effect = label.gameObject.AddComponent<Outline>();
            effect.effectColor = QuestStyle.Outline;
            effect.effectDistance = new Vector2(outline, -outline);
            return label;
        }

        /// <summary>Logo-ul scris: primul cuvânt albastru ca apa, al doilea auriu („DROP QUEST”).</summary>
        public static string Logo(string text)
        {
            var builder = new System.Text.StringBuilder();
            string[] words = text.ToUpperInvariant().Split(' ');
            for (int i = 0; i < words.Length; i++)
            {
                Color color = i == 0 ? Palette.Tile(1) : Palette.Tile(3);
                if (i > 0) builder.Append(i == 1 ? "\n" : " ");
                builder.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(color)).Append('>').Append(words[i]).Append("</color>");
            }
            return builder.ToString();
        }

        /// <summary>
        /// Card crem „în relief” (stilul Drop Quest): o muchie maro dedesubt și fața deasupra. Întoarce rădăcina
        /// (de așezat) și fața (unde se pune conținutul).
        /// </summary>
        public static RectTransform Card(string name, Transform parent, out Image face, Color? color = null)
        {
            RectTransform root = UiFactory.CreateRect(name, parent);
            Image edge = UiFactory.CreatePanel("Edge", root, CardEdge);
            UiFactory.Stretch((RectTransform)edge.transform);
            face = UiFactory.CreatePanel("Face", root, color ?? QuestStyle.Surface);
            UiFactory.Stretch((RectTransform)face.transform);
            ((RectTransform)face.transform).offsetMin = new Vector2(0f, 8f);
            return root;
        }

        /// <summary>Muchia de sub cardurile crem.</summary>
        public static readonly Color CardEdge = new(0.79f, 0.66f, 0.47f);

        /// <summary>Buton „3D” al jocului, cu animația de apăsare a jocului (în locul celei generice).</summary>
        public static Button Button(string name, Transform parent, string text, Color color, System.Action onClick, float fontSize = 40f)
        {
            Button button = UiFactory.CreateButton(name, parent, text, color, onClick, fontSize);
            SwapPressAnimation(button.gameObject);
            return button;
        }

        public static Button IconButton(string name, Transform parent, Sprite icon, Color color, System.Action onClick, float iconScale = 0.5f)
        {
            Button button = UiFactory.CreateIconButton(name, parent, icon, color, onClick, iconScale);
            SwapPressAnimation(button.gameObject);
            return button;
        }

        private static void SwapPressAnimation(GameObject go)
        {
            if (go.TryGetComponent(out PressScale generic)) Object.Destroy(generic);
            go.AddComponent<UIButtonPressAnimator>();
        }

        public static void Click()
        {
            if (SugarAudio.Instance != null) SugarAudio.Instance.Play(SugarSound.Click);
            SugarAudio.Buzz(0);
        }

        /// <summary>Arată o reclamă cu recompensă (LevelPlay sau reclama de test); recompensa doar dacă e văzută până la capăt.</summary>
        public static void RewardedAd(string placement, System.Action onReward)
        {
            System.Action<bool> done = ok =>
            {
                if (ok) onReward?.Invoke();
            };
            if (SugarAds.Instance != null) SugarAds.Instance.ShowRewarded(placement, done);
            else if (AdService.Instance != null) AdService.Instance.ShowRewarded(placement, done);
            else onReward?.Invoke();
        }

        public static string Time(int seconds) => $"{seconds / 60:00}:{seconds % 60:00}";
    }
}
