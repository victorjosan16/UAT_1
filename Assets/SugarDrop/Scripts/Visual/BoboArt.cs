using QuarryEmpire;
using UnityEngine;
using UnityEngine.UI;

namespace SugarDrop
{
    /// <summary>Ipostazele lui Bobo pictat (câte o imagine pentru fiecare moment din joc).</summary>
    public enum BoboPose
    {
        Idle, Happy, Scared, Sad, Wave, Thumbs, Think, Worried, Sleep, Love, Crystal, Gem, Star, Run,
    }

    /// <summary>
    /// Bobo pictat (Resources/Art/bobo*.png): blănosul alb cu urechi-frunze albastre și eșarfă cu stea. Imaginile
    /// sunt pătrate, cu personajul centrat și picioarele aproape de marginea de jos. O ipostază care lipsește
    /// folosește imaginea de bază (bobo.png); fără ea, jocul îl desenează pe Bobo din cod, ca înainte.
    /// </summary>
    public static class BoboArt
    {
        public static Sprite Idle => ArtLibrary.Find("bobo");
        public static Sprite Happy => Get(BoboPose.Happy);
        public static Sprite Scared => Get(BoboPose.Scared);
        public static bool Available => Idle != null;

        /// <summary>Numele imaginii fiecărei ipostaze (în Resources/Art).</summary>
        public static string Key(BoboPose pose) => pose switch
        {
            BoboPose.Happy => "bobohappy",
            BoboPose.Scared => "boboscared",
            BoboPose.Sad => "bobosad",
            BoboPose.Wave => "bobowave",
            BoboPose.Thumbs => "bobothumbs",
            BoboPose.Think => "bobothink",
            BoboPose.Worried => "boboworried",
            BoboPose.Sleep => "bobosleep",
            BoboPose.Love => "bobolove",
            BoboPose.Crystal => "bobocrystal",
            BoboPose.Gem => "bobogem",
            BoboPose.Star => "bobostar",
            BoboPose.Run => "boborun",
            _ => "bobo",
        };

        public static Sprite Get(BoboPose pose) => ArtLibrary.Find(Key(pose)) ?? Idle;

        /// <summary>Partea din imagine (de jos în sus) unde stau capul (pălăriile) și ochii (ochelarii).</summary>
        public const float HeadTop = 0.84f;
        public const float EyeLine = 0.55f;

        /// <summary>Bobo într-un dreptunghi de interfață: o singură imagine, picioarele jos, pe mijloc.</summary>
        public static Image AddTo(RectTransform parent, float size, Vector2 bottomCenter, BoboPose pose = BoboPose.Idle)
        {
            Image image = UiFactory.CreateImage("BoboArt", parent, Get(pose), Color.white);
            image.preserveAspect = true;
            image.raycastTarget = false;
            UiFactory.Place(image.rectTransform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), bottomCenter, new Vector2(size, size));
            return image;
        }
    }
}
