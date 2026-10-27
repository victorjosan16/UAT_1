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
            Image dim = UiFactory.CreateImage("Dim", parent, UiFactory.WhiteSprite, new Color(SoftPop.Scrim.r, SoftPop.Scrim.g, SoftPop.Scrim.b, alpha * 0.8f));
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
            Image shadow = UiFactory.CreatePanel("BoxShadow", holder, SoftPop.Shadow);
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
            effect.effectColor = new Color(0.3f, 0.24f, 0.42f, 0.8f);
            effect.effectDistance = new Vector2(outline, -outline);
            return label;
        }

        /// <summary>„Sugar Drop” cu fiecare literă în altă culoare de bomboană.</summary>
        public static string Logo(string text)
        {
            var builder = new System.Text.StringBuilder();
            int index = 0;
            foreach (char c in text)
            {
                if (c == ' ')
                {
                    builder.Append(' ');
                    continue;
                }
                Color color = Palette.Tile(index++ % Palette.TileColorCount);
                builder.Append("<color=#").Append(ColorUtility.ToHtmlStringRGB(color)).Append('>').Append(c).Append("</color>");
            }
            return builder.ToString();
        }

        /// <summary>Buton „3D” al jocului, cu animația de apăsare Sugar Drop (în locul celei generice).</summary>
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
