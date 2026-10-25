using System.Collections.Generic;
using UnityEngine;

namespace SugarDrop
{
    /// <summary>Traduceri RO → EN pentru Sugar Drop (textele din cod sunt în română).</summary>
    public static class SugarLoc
    {
        public static bool IsEnglish
        {
            get
            {
                string saved = SugarSave.Data.language;
                if (saved == "en") return true;
                if (saved == "ro") return false;
                return Application.systemLanguage != SystemLanguage.Romanian;
            }
        }

        public static string T(string ro) => Translate(ro, IsEnglish);

        /// <summary>Traducerea pură (pentru teste): textul românesc sau echivalentul englezesc, dacă există.</summary>
        public static string Translate(string ro, bool english) => english && English.TryGetValue(ro, out string en) ? en : ro;
        public static string F(string roFormat, params object[] args) => string.Format(T(roFormat), args);

        private static readonly Dictionary<string, string> English = new()
        {
            { "NIVELUL {0}", "LEVEL {0}" }, { "mutări", "moves" }, { "JOACĂ", "PLAY" }, { "BOSS", "BOSS" },
            { "Aliniază 3 capace la fel ca să eliberezi bomboanele!", "Match 3 caps to free the candies!" },
            { "BRAVO!", "SWEET!" }, { "AULEU!", "OH NO!" }, { "Nivelul {0} terminat", "Level {0} complete" },
            { "+{0} monede", "+{0} coins" }, { "CONTINUĂ", "CONTINUE" }, { "REÎNCEARCĂ", "TRY AGAIN" },
            { "Ai strâns {0} din {1} bomboane.", "You collected {0} of {1} candies." },
            { "+5 MUTĂRI (reclamă)", "+5 MOVES (ad)" }, { "HARTA", "MAP" },
            { "PAUZĂ", "PAUSED" }, { "RELUARE NIVEL", "RESTART LEVEL" }, { "ÎNAPOI LA HARTĂ", "BACK TO MAP" },
            { "SUNET: PORNIT", "SOUND: ON" }, { "SUNET: OPRIT", "SOUND: OFF" },
            { "MUZICĂ: PORNITĂ", "MUSIC: ON" }, { "MUZICĂ: OPRITĂ", "MUSIC: OFF" },
            { "VIBRAȚII: PORNITE", "VIBRATION: ON" }, { "VIBRAȚII: OPRITE", "VIBRATION: OFF" },
            { "LIMBA: ROMÂNĂ", "LANGUAGE: ENGLISH" }, { "ÎNCHIDE", "CLOSE" },
            { "Fără vieți!", "Out of lives!" }, { "Următoarea viață în {0}", "Next life in {0}" },
            { "+1 VIAȚĂ (reclamă)", "+1 LIFE (ad)" },
            { "Atinge un capac pentru bombă", "Tap a cap to drop the bomb" },
            { "Cumpără cu {0} monede?", "Buy for {0} coins?" }, { "Nu ai destule monede", "Not enough coins" },
            { "Glisează un capac ca să aliniezi 3 la fel!", "Swipe a cap to line up 3 of a kind!" },
            { "Nivel Boss!", "Boss Level!" }, { "Dulce!", "Sweet!" }, { "Delicios!", "Delicious!" },
            { "Super!", "Super!" }, { "Fabulos!", "Fabulous!" }, { "Ultimele mutări!", "Last moves!" },
            { "Stele: {0}", "Stars: {0}" }, { "SETĂRI", "SETTINGS" }, { "CUMPĂRĂ", "BUY" },
            { "GRATIS (reclamă)", "FREE (ad)" }, { "Ai {0} monede", "You have {0} coins" },
            { "Bombă", "Bomb" }, { "+5 mutări", "+5 moves" }, { "Amestecă", "Shuffle" },
            { "Nivelul costă o viață.", "Leaving costs a life." }, { "Boss învins! +1 la fiecare ajutor", "Boss beaten! +1 of each booster" },
            { "Final dulce!", "Sweet finale!" },
            { "Adu bomboanele jos, în borcan!", "Bring the candies down to the jar!" },
            { "Strânge piesele cerute!", "Collect the pieces!" },
            { "Topește toată glazura!", "Melt all the frosting!" },
            { "Atinge scorul!", "Reach the score!" },
            { "Mai aveai puțin!", "So close!" },
            { "Mai rămân: {0}", "Still needed: {0}" },
        };
    }
}
