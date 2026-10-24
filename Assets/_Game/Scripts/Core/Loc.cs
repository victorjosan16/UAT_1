using System.Collections.Generic;
using UnityEngine;

namespace QuarryEmpire
{
    /// <summary>
    /// Traduceri simple RO → EN. Textele din cod sunt scrise în română; Loc.T() întoarce traducerea
    /// în engleză dacă limba activă e engleza. Limba se alege după telefon sau din Pauză → LIMBA.
    /// Pentru o limbă nouă: adaugă încă un dicționar și o valoare în Language.
    /// </summary>
    public static class Loc
    {
        public enum Language { Romanian, English }

        private static Language? _current;

        public static Language Current
        {
            get
            {
                if (_current.HasValue) return _current.Value;
                string saved = SaveSystem.Instance != null ? SaveSystem.Instance.Data.language : "";
                if (saved == "ro") _current = Language.Romanian;
                else if (saved == "en") _current = Language.English;
                else _current = Application.systemLanguage == SystemLanguage.Romanian ? Language.Romanian : Language.English;
                return _current.Value;
            }
        }

        /// <summary>Schimbă limba și o salvează (UI-ul se reconstruiește la repornirea scenei).</summary>
        public static void Set(Language language)
        {
            _current = language;
            if (SaveSystem.Instance == null) return;
            SaveSystem.Instance.Data.language = language == Language.Romanian ? "ro" : "en";
            SaveSystem.Instance.MarkDirty();
        }

        /// <summary>Traduce un text scris în română (sau îl întoarce neschimbat dacă nu are traducere).</summary>
        public static string T(string ro)
        {
            if (string.IsNullOrEmpty(ro) || Current == Language.Romanian) return ro;
            if (English.TryGetValue(ro, out string en)) return en;

            // „Muncitor 2” → „Worker 2”: traducem partea de text și păstrăm numărul de la final.
            int space = ro.LastIndexOf(' ');
            if (space > 0 && int.TryParse(ro.Substring(space + 1), out _) && English.TryGetValue(ro.Substring(0, space), out string prefix))
                return prefix + ro.Substring(space);
            return ro;
        }

        /// <summary>Traduce un șablon și completează valorile: Loc.F("Strat {0}", 3).</summary>
        public static string F(string roFormat, params object[] args) => string.Format(T(roFormat), args);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics() => _current = null;

        private static readonly Dictionary<string, string> English = new()
        {
            // Resurse, blocuri, straturi, utilaje
            { "Piatră", "Stone" }, { "Cupru", "Copper" }, { "Fier", "Iron" }, { "Aur", "Gold" }, { "Diamant", "Diamond" },
            { "Cărămidă", "Brick" }, { "Lingou de cupru", "Copper Ingot" }, { "Lingou de fier", "Iron Ingot" },
            { "Lingou de aur", "Gold Ingot" }, { "Diamant șlefuit", "Cut Diamond" },
            { "Suprafață", "Surface" }, { "Lut roșu", "Red Clay" }, { "Rocă", "Bedrock" }, { "Filonul de aur", "Gold Vein" },
            { "Peșteri adânci", "Deep Caves" }, { "Miezul de cristal", "Crystal Core" },
            { "Buldozer", "Bulldozer" }, { "Burghiu", "Drill" }, { "Camion", "Truck" }, { "Muncitor", "Worker" },
            { "Rafinărie", "Refinery" },
            // Upgrade-uri
            { "Forță", "Power" }, { "Viteză lovire", "Hit Speed" }, { "Viteză mers", "Move Speed" },
            { "Capacitate", "Capacity" }, { "Târnăcop", "Pickaxe" },
            // Etichete în lume
            { "VINDE", "SELL" }, { "MINEREU", "ORE" }, { "LINGOURI", "INGOTS" }, { "COMENZI", "ORDERS" },
            { "URCĂ", "GET IN" }, { "ÎNCARCĂ\nCAMIONUL", "LOAD\nTRUCK" }, { "RAFINĂRIE", "REFINERY" }, { "CAMION", "TRUCK" },
            { "COMENZI\nAșteaptă un camion…", "ORDERS\nWaiting for a truck…" },
            { "RAFINĂRIE\nÎn lucru: {0} · Gata: {1}", "REFINERY\nProcessing: {0} · Ready: {1}" },
            { "CAMION {0}/{1}\n{2}", "TRUCK {0}/{1}\n{2}" },
            { "Încarcă-mă!", "Load me!" }, { "Se încarcă", "Loading" }, { "Spre vânzare", "Going to sell" },
            { "Vinde", "Selling" }, { "Se întoarce", "Returning" },
            { "COMANDĂ: {0} × {1}\n{2}/{3} · {4}s · {5}", "ORDER: {0} × {1}\n{2}/{3} · {4}s · {5}" },
            { "{0} · Nv.{1}", "{0} · Lv.{1}" }, { "PLIN", "FULL" }, { "MAX", "MAX" },
            // Mesaje
            { "Nu ai destui bani", "Not enough money" }, { "{0} → nivel {1}", "{0} → level {1}" },
            { "Deblocat: {0}!", "Unlocked: {0}!" }, { "Strat curățat! Coborâm la {0} m", "Layer cleared! Going down to {0} m" },
            { "Comanda a expirat. Camionul a plecat.", "Order expired. The truck left." },
            { "Comandă livrată! +{0}", "Order delivered! +{0}" }, { "Comandă nouă: {0} × {1}!", "New order: {0} × {1}!" },
            { "Prea dur! Îmbunătățește târnăcopul sau folosește burghiul.", "Too hard! Upgrade your pickaxe or use the drill." },
            { "Stivă plină! Mergi la zona VINDE.", "Backpack full! Go to the SELL zone." },
            { "Conduci: {0}", "Driving: {0}" }, { "{0} NV.{1}!", "{0} LV.{1}!" },
            // HUD
            { "STRAT", "LAYER" }, { "BANI x2 · {0}s", "CASH x2 · {0}s" }, { "BANI x2", "CASH x2" },
            { "Livrează {0} × {1}", "Deliver {0} × {1}" }, { "{0}/{1} · {2}s · bonus {3}", "{0}/{1} · {2}s · bonus {3}" },
            { "Curăță stratul", "Clear the layer" }, { "Mai sunt {0} blocuri până coborâm", "{0} blocks left before going deeper" },
            { "COBOARĂ", "GET OUT" },
            // Meniu, setări, ferestre
            { "Sapă. Vinde. Construiește-ți imperiul.", "Dig. Sell. Build your empire." },
            { "JOACĂ", "PLAY" }, { "Mișcă-te cu joystick-ul. Minerul lovește singur blocurile din jur.",
                "Drag anywhere to move. Your miner hits nearby blocks automatically." },
            { "PAUZĂ", "PAUSED" }, { "CONTINUĂ", "RESUME" }, { "SUNET: PORNIT", "SOUND: ON" }, { "SUNET: OPRIT", "SOUND: OFF" },
            { "MUZICĂ: PORNITĂ", "MUSIC: ON" }, { "MUZICĂ: OPRITĂ", "MUSIC: OFF" },
            { "VIBRAȚII: PORNITE", "VIBRATION: ON" }, { "VIBRAȚII: OPRITE", "VIBRATION: OFF" },
            { "GRAFICĂ: {0}", "GRAPHICS: {0}" }, { "ÎNALTĂ", "HIGH" }, { "ECHILIBRATĂ", "BALANCED" }, { "ECONOMIC (30 FPS)", "BATTERY (30 FPS)" },
            { "LIMBA: ROMÂNĂ", "LANGUAGE: ENGLISH" },
            { "SIGUR? APASĂ IAR", "SURE? TAP AGAIN" }, { "RESETEAZĂ PROGRESUL", "RESET PROGRESS" },
            { "Bine ai revenit!\nCât ai lipsit, muncitorii au câștigat\n{0}", "Welcome back!\nWhile you were away, your workers earned\n{0}" },
            { "IA {0}", "CLAIM {0}" }, { "IA {0} (reclamă)", "CLAIM {0} (ad)" },
            { "RECLAMĂ DE TEST\n\nRecompensa vine în {0}…", "TEST AD\n\nReward in {0}…" },
            { "ÎNCHIDE (fără recompensă)", "CLOSE (no reward)" },
            { "STRATUL {0}", "LAYER {0}" }, { "Coborâm…", "Going deeper…" },
            // Tutorial
            { "Sparge blocuri în carieră", "Break blocks in the quarry" },
            { "Mergi lângă blocuri: minerul lovește singur", "Walk next to blocks: your miner hits them automatically" },
            { "Vinde resursele", "Sell your resources" }, { "Du stiva la placa verde VINDE", "Bring your stack to the green SELL pad" },
            { "Cumpără un upgrade", "Buy an upgrade" }, { "Stai pe o placă mov ca să plătești", "Stand on a purple pad to pay" },
        };
    }
}
