using QuarryEmpire;
using UnityEngine;
using UnityEngine.UI;

namespace SugarDrop
{
    /// <summary>
    /// Bobo pictat (Resources/Art/bobo.png, bobohappy.png, boboscared.png): blănosul alb cu urechi-frunze albastre
    /// și eșarfă cu stea. Imaginile sunt pătrate, cu personajul centrat și picioarele aproape de marginea de jos.
    /// Când lipsesc, jocul îl desenează pe Bobo din cod, ca înainte.
    /// </summary>
    public static class BoboArt
    {
        public static Sprite Idle => ArtLibrary.Find("bobo");
        public static Sprite Happy => ArtLibrary.Find("bobohappy") ?? Idle;
        public static Sprite Scared => ArtLibrary.Find("boboscared") ?? Idle;
        public static bool Available => Idle != null;

        /// <summary>Partea din imagine (de jos în sus) unde stau capul (pălăriile) și ochii (ochelarii).</summary>
        public const float HeadTop = 0.84f;
        public const float EyeLine = 0.55f;

        /// <summary>Bobo într-un dreptunghi de interfață: o singură imagine, picioarele jos, pe mijloc.</summary>
        public static Image AddTo(RectTransform parent, float size, Vector2 bottomCenter)
        {
            Image image = UiFactory.CreateImage("BoboArt", parent, Idle, Color.white);
            image.preserveAspect = true;
            image.raycastTarget = false;
            UiFactory.Place(image.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), bottomCenter, new Vector2(size, size));
            return image;
        }
    }
}
