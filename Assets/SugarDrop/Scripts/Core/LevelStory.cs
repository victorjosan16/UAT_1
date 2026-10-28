using System.Collections.Generic;

namespace SugarDrop
{
    /// <summary>
    /// Povestea de dinaintea unui nivel: titlul de pe tăbliță, replica lui Bobo și unde stă balonul ei pe
    /// ilustrație (coordonate 0–1 din stânga-sus a imaginii Resources/Art/story{nivel}.png). Tăblița și balonul
    /// sunt desenate de joc peste imagine, deci textul apare tradus în limba aleasă.
    /// </summary>
    public readonly struct LevelStoryEntry
    {
        public readonly int Level;
        public readonly string TitleKey;
        public readonly string LineKey;
        public readonly float BubbleLeft, BubbleTop, BubbleRight, BubbleBottom;
        /// <summary>Colțul din dreapta-jos al tăbliței pictate (tăblița pornește din colțul stânga-sus).</summary>
        public readonly float SignRight, SignBottom;

        public LevelStoryEntry(int level, string title, string line, float left, float top, float right, float bottom, float signRight, float signBottom)
        {
            SignRight = signRight;
            SignBottom = signBottom;
            Level = level;
            TitleKey = title;
            LineKey = line;
            BubbleLeft = left;
            BubbleTop = top;
            BubbleRight = right;
            BubbleBottom = bottom;
        }

        public string ArtKey => "story" + Level;
    }

    /// <summary>Scenele de poveste ale nivelurilor (deocamdată din planșa „Aventurile lui Bobo”, nivelurile 1–30).</summary>
    public static class LevelStory
    {
        private static readonly LevelStoryEntry[] Entries =
        {
            new(1, "Trezirea lui Bobo", "Ce dimineață frumoasă! Hai să vedem ce se întâmplă în vale!", 0.604f, 0.277f, 0.979f, 0.644f, 0.482f, 0.270f),
            new(2, "Florile însetate", "Oh, nu! Florile sunt însetate!", 0.362f, 0.288f, 0.644f, 0.573f, 0.443f, 0.270f),
            new(3, "Cărarea blocată", "Calea este blocată! Trebuie să îndepărtăm aceste pietre!", 0.336f, 0.206f, 0.678f, 0.614f, 0.445f, 0.269f),
            new(4, "Podul rupt", "Podul s-a rupt! Să adunăm ce ne trebuie pentru a-l repara!", 0.421f, 0.169f, 0.844f, 0.554f, 0.413f, 0.266f),
            new(5, "Traversarea râului", "Uau! Acum putem traversa râul!", 0.574f, 0.295f, 0.831f, 0.635f, 0.490f, 0.270f),
            new(6, "Pădurea misterioasă", "Ce pădure misterioasă! Hai să vedem ce se ascunde aici!", 0.358f, 0.266f, 0.670f, 0.660f, 0.448f, 0.270f),
            new(7, "Prietenul pierdut", "Săracul! Trebuie să-l ajutăm să iasă!", 0.354f, 0.176f, 0.688f, 0.488f, 0.450f, 0.270f),
            new(8, "Peștera cristalelor", "Uau! Ce cristale minunate!", 0.451f, 0.279f, 0.755f, 0.586f, 0.455f, 0.266f),
            new(9, "Furtuna magică", "O furtună magică a cuprins valea! Trebuie s-o oprim!", 0.356f, 0.226f, 0.674f, 0.594f, 0.431f, 0.270f),
            new(10, "Gardianul cascadei", "Tu ești gardianul cascadei? Te voi ajuta să-ți regăsești energia!", 0.210f, 0.250f, 0.635f, 0.570f, 0.446f, 0.270f),
            new(11, "Cărarea secretă", "Am găsit o cărare secretă! Unde o să mă ducă oare?", 0.326f, 0.248f, 0.764f, 0.590f, 0.490f, 0.270f),
            new(15, "Un nou prieten", "Am găsit un nou prieten!", 0.490f, 0.171f, 0.801f, 0.419f, 0.448f, 0.270f),
            new(20, "Gardianul pădurii", "Ce gardian impresionant! Să-i redăm lumina pădurii!", 0.339f, 0.251f, 0.635f, 0.586f, 0.465f, 0.270f),
            new(25, "Mașina magică", "Această mașină antică nu funcționează! Hai să o reparăm!", 0.294f, 0.224f, 0.671f, 0.593f, 0.482f, 0.270f),
            new(29, "Trezirea cristalului", "Suntem atât de aproape! Cristalul începe să se trezească!", 0.599f, 0.110f, 0.985f, 0.475f, 0.462f, 0.252f),
            new(30, "Gardianul final", "Împreună putem readuce energia întregii văi! Hai, Bobo!", 0.199f, 0.259f, 0.590f, 0.605f, 0.440f, 0.250f),
        };

        private static Dictionary<int, LevelStoryEntry> _byLevel;

        public static IReadOnlyList<LevelStoryEntry> All => Entries;

        public static bool TryGet(int level, out LevelStoryEntry entry)
        {
            if (_byLevel == null)
            {
                _byLevel = new Dictionary<int, LevelStoryEntry>();
                foreach (LevelStoryEntry e in Entries) _byLevel[e.Level] = e;
            }
            return _byLevel.TryGetValue(level, out entry);
        }

        /// <summary>
        /// Scena se arată o singură dată: doar pentru nivelul nou (cel mai mare deblocat) și doar dacă n-a mai fost
        /// văzută. Rejucările nivelurilor vechi și reîncercările nu o mai arată.
        /// </summary>
        public static bool ShouldShow(int level, int currentLevel, int lastSeen) =>
            level == currentLevel && level > lastSeen && TryGet(level, out _);
    }
}
