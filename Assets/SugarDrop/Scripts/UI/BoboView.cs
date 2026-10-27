using QuarryEmpire;
using UnityEngine;
using UnityEngine.UI;

namespace SugarDrop
{
    /// <summary>
    /// Bobo desenat în interfață (meniu, garderobă, antrenament): folie, glazură, cireașă, ochi care clipesc,
    /// zâmbet, obraji și costumul ales. Dimensiunile sunt pentru un Bobo de 380×420 px, înmulțite cu „scale”.
    /// </summary>
    public sealed class BoboView
    {
        public RectTransform Root { get; private set; }
        public RectTransform[] Eyes { get; private set; }
        public float Scale { get; private set; }
        private Image _cherry;
        private Image _costume;

        public static BoboView Build(Transform parent, float scale, bool shadow = true)
        {
            var view = new BoboView { Scale = scale };
            RectTransform root = UiFactory.CreateRect("Bobo", parent);
            root.sizeDelta = new Vector2(380f, 420f) * scale;
            view.Root = root;
            Image Part(string name, Sprite sprite, Color color, Vector2 position, Vector2 size)
            {
                Image image = UiFactory.CreateImage(name, root, sprite, color);
                UiFactory.Place(image.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), position * scale, size * scale);
                return image;
            }
            if (shadow) Part("Shadow", SugarSprites.SoftDot, SoftPop.Shadow, new Vector2(0f, -200f), new Vector2(300f, 70f)).preserveAspect = false;
            Part("Cup", SugarSprites.CupWrapper, Color.white, new Vector2(0f, -80f), new Vector2(340f, 340f));
            Part("Frosting", SugarSprites.Frosting, Color.white, new Vector2(0f, 50f), new Vector2(360f, 360f));
            view._cherry = Part("Cherry", SugarSprites.Circle, new Color(0.93f, 0.2f, 0.32f), new Vector2(10f, 205f), new Vector2(80f, 80f));
            Part("Smile", SugarSprites.Smile, new Color(0.35f, 0.12f, 0.22f), new Vector2(0f, -120f), new Vector2(110f, 110f));
            view.Eyes = new RectTransform[2];
            for (int e = 0; e < 2; e++)
            {
                Image eye = Part("Eye", SugarSprites.Circle, new Color(0.25f, 0.12f, 0.2f), new Vector2((e * 2 - 1) * 60f, -60f), new Vector2(40f, 40f));
                Image glint = UiFactory.CreateImage("Glint", eye.transform, SugarSprites.Circle, Color.white);
                UiFactory.Place(glint.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(-7f, 8f) * scale, new Vector2(12f, 12f) * scale);
                view.Eyes[e] = eye.rectTransform;
            }
            for (int e = -1; e <= 1; e += 2)
                Part("Cheek", SugarSprites.SoftDot, new Color(1f, 0.45f, 0.6f, 0.55f), new Vector2(e * 105f, -105f), new Vector2(70f, 46f)).preserveAspect = false;
            view._costume = Part("Costume", SugarSprites.Circle, Color.white, Vector2.zero, Vector2.one * 10f);
            view.SetCostume(CostumeRules.Current, scale);
            return view;
        }

        public void SetCostume(Costume costume) => SetCostume(costume, Scale);

        /// <summary>Pune costumul: pălăriile și fundița pe cap (în locul cireșei), ochelarii pe ochi.</summary>
        public void SetCostume(Costume costume, float scale)
        {
            Sprite sprite = SugarSprites.CostumeSprite(costume);
            _costume.gameObject.SetActive(sprite != null);
            _cherry.gameObject.SetActive(sprite == null || CostumeRules.OnEyes(costume));
            if (sprite == null) return;
            _costume.sprite = sprite;
            bool eyes = CostumeRules.OnEyes(costume);
            _costume.rectTransform.anchoredPosition = (eyes ? new Vector2(0f, -58f) : new Vector2(0f, 270f)) * scale;
            _costume.rectTransform.sizeDelta = new Vector2(230f, 230f) * scale;
        }
    }
}
