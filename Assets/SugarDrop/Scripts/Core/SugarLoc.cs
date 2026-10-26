using System.Collections.Generic;
using UnityEngine;

namespace SugarDrop
{
    /// <summary>Limbile jocului, în ordinea coloanelor din tabelul de traduceri.</summary>
    public enum SugarLanguage { Romanian, English, Spanish, Portuguese, French, German, Italian, Turkish, Indonesian }

    /// <summary>
    /// Traducerile Sugar Drop. Textele din cod sunt în română și servesc drept chei; fiecare rând din tabel are
    /// traducerea în toate limbile. Limba vine din Setări sau, la prima pornire, din limba telefonului
    /// (engleza pentru limbile pe care jocul nu le are).
    /// </summary>
    public static class SugarLoc
    {
        public static readonly SugarLanguage[] Languages =
        {
            SugarLanguage.English, SugarLanguage.Romanian, SugarLanguage.Spanish, SugarLanguage.Portuguese, SugarLanguage.French,
            SugarLanguage.German, SugarLanguage.Italian, SugarLanguage.Turkish, SugarLanguage.Indonesian,
        };

        private static readonly string[] Codes = { "ro", "en", "es", "pt", "fr", "de", "it", "tr", "id" };
        private static readonly string[] NativeNames = { "Română", "English", "Español", "Português", "Français", "Deutsch", "Italiano", "Türkçe", "Bahasa Indonesia" };

        /// <summary>Codul salvat în SugarSaveData.language („en”, „ro”, ...).</summary>
        public static string Code(SugarLanguage language) => Codes[(int)language];

        /// <summary>Numele limbii în limba ei (pentru lista din Setări).</summary>
        public static string NativeName(SugarLanguage language) => NativeNames[(int)language];

        public static SugarLanguage Current
        {
            get
            {
                string saved = SugarSave.Data.language;
                for (int i = 0; i < Codes.Length; i++)
                    if (Codes[i] == saved) return (SugarLanguage)i;
                return FromSystem(Application.systemLanguage);
            }
        }

        /// <summary>Limba jocului pentru limba telefonului; engleza dacă jocul nu o are.</summary>
        public static SugarLanguage FromSystem(SystemLanguage system) => system switch
        {
            SystemLanguage.Romanian => SugarLanguage.Romanian,
            SystemLanguage.Spanish => SugarLanguage.Spanish,
            SystemLanguage.Portuguese => SugarLanguage.Portuguese,
            SystemLanguage.French => SugarLanguage.French,
            SystemLanguage.German => SugarLanguage.German,
            SystemLanguage.Italian => SugarLanguage.Italian,
            SystemLanguage.Turkish => SugarLanguage.Turkish,
            SystemLanguage.Indonesian => SugarLanguage.Indonesian,
            _ => SugarLanguage.English,
        };

        public static string T(string ro) => Translate(ro, Current);
        public static string F(string roFormat, params object[] args) => string.Format(T(roFormat), args);

        /// <summary>Traducerea pură (pentru teste): textul în limba cerută, în engleză dacă lipsește, altfel cheia.</summary>
        public static string Translate(string ro, SugarLanguage language)
        {
            if (language == SugarLanguage.Romanian || ro == null || !Table.TryGetValue(ro, out string[] row)) return ro;
            string text = row[(int)language];
            return string.IsNullOrEmpty(text) ? row[(int)SugarLanguage.English] : text;
        }

        public static string Translate(string ro, bool english) => Translate(ro, english ? SugarLanguage.English : SugarLanguage.Romanian);

        /// <summary>Toate rândurile tabelului (pentru teste).</summary>
        public static IEnumerable<string[]> Rows => Table.Values;

        private static readonly Dictionary<string, string[]> Table = Build(new[]
        {
            // ro, en, es, pt, fr, de, it, tr, id
            R("NIVELUL {0}", "LEVEL {0}", "NIVEL {0}", "NÍVEL {0}", "NIVEAU {0}", "LEVEL {0}", "LIVELLO {0}", "SEVİYE {0}", "LEVEL {0}"),
            R("mutări", "moves", "movs.", "jogadas", "coups", "Züge", "mosse", "hamle", "langkah"),
            R("JOACĂ", "PLAY", "JUGAR", "JOGAR", "JOUER", "SPIELEN", "GIOCA", "OYNA", "MAIN"),
            R("BOSS", "BOSS", "JEFE", "CHEFE", "BOSS", "BOSS", "BOSS", "BOSS", "BOS"),
            R("MEGA BOSS", "MEGA BOSS", "MEGAJEFE", "MEGACHEFE", "MÉGA BOSS", "MEGA-BOSS", "MEGA BOSS", "MEGA BOSS", "MEGA BOS"),
            R("Aliniază 3 capace la fel ca să eliberezi bomboanele!", "Match 3 caps to free the candies!",
                "¡Junta 3 tapas iguales para liberar los caramelos!", "Junte 3 tampinhas iguais para libertar os doces!",
                "Aligne 3 capsules identiques pour libérer les bonbons !", "Bringe 3 gleiche Kappen in eine Reihe, um die Bonbons zu befreien!",
                "Allinea 3 tappi uguali per liberare le caramelle!", "Şekerleri kurtarmak için 3 aynı kapağı yan yana getir!",
                "Sejajarkan 3 tutup yang sama untuk membebaskan permen!"),
            R("BRAVO!", "SWEET!", "¡GENIAL!", "DOCE!", "BRAVO !", "SÜSS!", "DOLCE!", "TATLI!", "MANIS!"),
            R("AULEU!", "OH NO!", "¡OH, NO!", "AH, NÃO!", "OH NON !", "OH NEIN!", "OH NO!", "EYVAH!", "OH TIDAK!"),
            R("Nivelul {0} terminat", "Level {0} complete", "Nivel {0} completado", "Nível {0} concluído", "Niveau {0} terminé",
                "Level {0} geschafft", "Livello {0} completato", "Seviye {0} tamamlandı", "Level {0} selesai"),
            R("+{0} monede", "+{0} coins", "+{0} monedas", "+{0} moedas", "+{0} pièces", "+{0} Münzen", "+{0} monete", "+{0} jeton", "+{0} koin"),
            R("CONTINUĂ", "CONTINUE", "CONTINUAR", "CONTINUAR", "CONTINUER", "WEITER", "CONTINUA", "DEVAM", "LANJUT"),
            R("REÎNCEARCĂ", "TRY AGAIN", "REINTENTAR", "TENTAR DE NOVO", "RÉESSAYER", "NOCHMAL", "RIPROVA", "TEKRAR DENE", "COBA LAGI"),
            R("+5 MUTĂRI (reclamă)", "+5 MOVES (ad)", "+5 MOVS. (anuncio)", "+5 JOGADAS (anúncio)", "+5 COUPS (pub)", "+5 ZÜGE (Werbung)",
                "+5 MOSSE (pubblicità)", "+5 HAMLE (reklam)", "+5 LANGKAH (iklan)"),
            R("HARTA", "MAP", "MAPA", "MAPA", "CARTE", "KARTE", "MAPPA", "HARİTA", "PETA"),
            R("PAUZĂ", "PAUSED", "PAUSA", "PAUSA", "PAUSE", "PAUSE", "PAUSA", "DURAKLATILDI", "JEDA"),
            R("RELUARE NIVEL", "RESTART LEVEL", "REINICIAR NIVEL", "REINICIAR NÍVEL", "RECOMMENCER", "LEVEL NEU STARTEN",
                "RICOMINCIA", "SEVİYEYİ YENİDEN BAŞLAT", "ULANGI LEVEL"),
            R("ÎNAPOI LA HARTĂ", "BACK TO MAP", "VOLVER AL MAPA", "VOLTAR AO MAPA", "RETOUR À LA CARTE", "ZURÜCK ZUR KARTE",
                "TORNA ALLA MAPPA", "HARİTAYA DÖN", "KEMBALI KE PETA"),
            R("SUNET: PORNIT", "SOUND: ON", "SONIDO: SÍ", "SOM: LIGADO", "SON : OUI", "TON: AN", "SUONO: SÌ", "SES: AÇIK", "SUARA: AKTIF"),
            R("SUNET: OPRIT", "SOUND: OFF", "SONIDO: NO", "SOM: DESLIGADO", "SON : NON", "TON: AUS", "SUONO: NO", "SES: KAPALI", "SUARA: MATI"),
            R("MUZICĂ: PORNITĂ", "MUSIC: ON", "MÚSICA: SÍ", "MÚSICA: LIGADA", "MUSIQUE : OUI", "MUSIK: AN", "MUSICA: SÌ", "MÜZİK: AÇIK", "MUSIK: AKTIF"),
            R("MUZICĂ: OPRITĂ", "MUSIC: OFF", "MÚSICA: NO", "MÚSICA: DESLIGADA", "MUSIQUE : NON", "MUSIK: AUS", "MUSICA: NO", "MÜZİK: KAPALI", "MUSIK: MATI"),
            R("VIBRAȚII: PORNITE", "VIBRATION: ON", "VIBRACIÓN: SÍ", "VIBRAÇÃO: LIGADA", "VIBRATIONS : OUI", "VIBRATION: AN",
                "VIBRAZIONE: SÌ", "TİTREŞİM: AÇIK", "GETAR: AKTIF"),
            R("VIBRAȚII: OPRITE", "VIBRATION: OFF", "VIBRACIÓN: NO", "VIBRAÇÃO: DESLIGADA", "VIBRATIONS : NON", "VIBRATION: AUS",
                "VIBRAZIONE: NO", "TİTREŞİM: KAPALI", "GETAR: MATI"),
            R("LIMBA", "LANGUAGE", "IDIOMA", "IDIOMA", "LANGUE", "SPRACHE", "LINGUA", "DİL", "BAHASA"),
            R("ÎNCHIDE", "CLOSE", "CERRAR", "FECHAR", "FERMER", "SCHLIESSEN", "CHIUDI", "KAPAT", "TUTUP"),
            R("ÎNAPOI", "BACK", "VOLVER", "VOLTAR", "RETOUR", "ZURÜCK", "INDIETRO", "GERİ", "KEMBALI"),
            R("Fără vieți!", "Out of lives!", "¡Sin vidas!", "Sem vidas!", "Plus de vies !", "Keine Leben mehr!", "Vite finite!", "Can kalmadı!", "Nyawa habis!"),
            R("Următoarea viață în {0}", "Next life in {0}", "Próxima vida en {0}", "Próxima vida em {0}", "Prochaine vie dans {0}",
                "Nächstes Leben in {0}", "Prossima vita tra {0}", "Sonraki can: {0}", "Nyawa berikutnya dalam {0}"),
            R("+1 VIAȚĂ (reclamă)", "+1 LIFE (ad)", "+1 VIDA (anuncio)", "+1 VIDA (anúncio)", "+1 VIE (pub)", "+1 LEBEN (Werbung)",
                "+1 VITA (pubblicità)", "+1 CAN (reklam)", "+1 NYAWA (iklan)"),
            R("Atinge un capac pentru bombă", "Tap a cap to drop the bomb", "Toca una tapa para lanzar la bomba", "Toque uma tampinha para soltar a bomba",
                "Touche une capsule pour lâcher la bombe", "Tippe auf eine Kappe für die Bombe", "Tocca un tappo per sganciare la bomba",
                "Bombayı atmak için bir kapağa dokun", "Ketuk tutup untuk menjatuhkan bom"),
            R("Cumpără cu {0} monede?", "Buy for {0} coins?", "¿Comprar por {0} monedas?", "Comprar por {0} moedas?", "Acheter pour {0} pièces ?",
                "Für {0} Münzen kaufen?", "Comprare per {0} monete?", "{0} jetona satın al?", "Beli dengan {0} koin?"),
            R("Nu ai destule monede", "Not enough coins", "No tienes suficientes monedas", "Moedas insuficientes", "Pas assez de pièces",
                "Nicht genug Münzen", "Monete insufficienti", "Yeterli jeton yok", "Koin tidak cukup"),
            R("Combo!", "Combo!", "¡Combo!", "Combo!", "Combo !", "Kombo!", "Combo!", "Kombo!", "Kombo!"),
            R("MIȘCARE REDUSĂ: DA", "REDUCED MOTION: ON", "MENOS MOVIMIENTO: SÍ", "MENOS MOVIMENTO: SIM", "MOUVEMENT RÉDUIT : OUI",
                "WENIGER BEWEGUNG: AN", "MOVIMENTO RIDOTTO: SÌ", "AZ HAREKET: AÇIK", "GERAK DIKURANGI: AKTIF"),
            R("MIȘCARE REDUSĂ: NU", "REDUCED MOTION: OFF", "MENOS MOVIMIENTO: NO", "MENOS MOVIMENTO: NÃO", "MOUVEMENT RÉDUIT : NON",
                "WENIGER BEWEGUNG: AUS", "MOVIMENTO RIDOTTO: NO", "AZ HAREKET: KAPALI", "GERAK DIKURANGI: MATI"),
            R("Dulce!", "Sweet!", "¡Dulce!", "Doce!", "Trop bon !", "Süß!", "Dolce!", "Tatlı!", "Manis!"),
            R("Delicios!", "Delicious!", "¡Delicioso!", "Delicioso!", "Délicieux !", "Köstlich!", "Delizioso!", "Nefis!", "Lezat!"),
            R("Super!", "Super!", "¡Súper!", "Super!", "Super !", "Super!", "Super!", "Süper!", "Super!"),
            R("Fabulos!", "Fabulous!", "¡Fabuloso!", "Fabuloso!", "Fabuleux !", "Fabelhaft!", "Favoloso!", "Muhteşem!", "Luar biasa!"),
            R("Ultimele mutări!", "Last moves!", "¡Últimos movimientos!", "Últimas jogadas!", "Derniers coups !", "Letzte Züge!",
                "Ultime mosse!", "Son hamleler!", "Langkah terakhir!"),
            R("SETĂRI", "SETTINGS", "AJUSTES", "CONFIGURAÇÕES", "PARAMÈTRES", "EINSTELLUNGEN", "IMPOSTAZIONI", "AYARLAR", "PENGATURAN"),
            R("CUMPĂRĂ", "BUY", "COMPRAR", "COMPRAR", "ACHETER", "KAUFEN", "COMPRA", "SATIN AL", "BELI"),
            R("GRATIS (reclamă)", "FREE (ad)", "GRATIS (anuncio)", "GRÁTIS (anúncio)", "GRATUIT (pub)", "GRATIS (Werbung)",
                "GRATIS (pubblicità)", "ÜCRETSİZ (reklam)", "GRATIS (iklan)"),
            R("Ai {0} monede", "You have {0} coins", "Tienes {0} monedas", "Você tem {0} moedas", "Tu as {0} pièces", "Du hast {0} Münzen",
                "Hai {0} monete", "{0} jetonun var", "Kamu punya {0} koin"),
            R("Bombă", "Bomb", "Bomba", "Bomba", "Bombe", "Bombe", "Bomba", "Bomba", "Bom"),
            R("+5 mutări", "+5 moves", "+5 movimientos", "+5 jogadas", "+5 coups", "+5 Züge", "+5 mosse", "+5 hamle", "+5 langkah"),
            R("Amestecă", "Shuffle", "Mezclar", "Embaralhar", "Mélanger", "Mischen", "Mescola", "Karıştır", "Acak"),
            R("Nivelul costă o viață.", "Leaving costs a life.", "Salir cuesta una vida.", "Sair custa uma vida.", "Quitter coûte une vie.",
                "Aufgeben kostet ein Leben.", "Uscire costa una vita.", "Çıkmak bir cana mal olur.", "Keluar akan memakai satu nyawa."),
            R("Boss învins! +1 la fiecare ajutor", "Boss beaten! +1 of each booster", "¡Jefe vencido! +1 de cada potenciador",
                "Chefe vencido! +1 de cada reforço", "Boss vaincu ! +1 de chaque bonus", "Boss besiegt! +1 von jedem Booster",
                "Boss sconfitto! +1 di ogni potenziamento", "Boss yenildi! Her güçlendiriciden +1", "Bos kalah! +1 setiap booster"),
            R("Final dulce!", "Sweet finale!", "¡Final dulce!", "Final doce!", "Final sucré !", "Süßes Finale!", "Finale dolce!", "Tatlı final!", "Akhir manis!"),
            R("Adu bomboanele jos, în borcan!", "Bring the candies down to the jar!", "¡Baja los caramelos hasta el frasco!",
                "Leve os doces até o pote!", "Fais descendre les bonbons dans le bocal !", "Bring die Bonbons hinunter ins Glas!",
                "Porta le caramelle giù nel barattolo!", "Şekerleri aşağıdaki kavanoza indir!", "Turunkan permen ke dalam stoples!"),
            R("Strânge piesele cerute!", "Collect the pieces!", "¡Reúne las piezas!", "Junte as peças!", "Récupère les pièces demandées !",
                "Sammle die Teile!", "Raccogli i pezzi!", "İstenen parçaları topla!", "Kumpulkan kepingannya!"),
            R("Topește toată glazura!", "Melt all the frosting!", "¡Derrite todo el glaseado!", "Derreta toda a cobertura!", "Fais fondre tout le glaçage !",
                "Schmilz die ganze Glasur!", "Sciogli tutta la glassa!", "Tüm kremayı erit!", "Lelehkan semua krimnya!"),
            R("Atinge scorul!", "Reach the score!", "¡Alcanza la puntuación!", "Alcance a pontuação!", "Atteins le score !", "Erreiche die Punktzahl!",
                "Raggiungi il punteggio!", "Puana ulaş!", "Capai skornya!"),
            R("Mai aveai puțin!", "So close!", "¡Por poco!", "Foi por pouco!", "Si près du but !", "So knapp!", "Per un pelo!", "Az kalmıştı!", "Hampir saja!"),
            R("BOSS!", "BOSS!", "¡JEFE!", "CHEFE!", "BOSS !", "BOSS!", "BOSS!", "BOSS!", "BOS!"),
            R("MEGA BOSS!", "MEGA BOSS!", "¡MEGAJEFE!", "MEGACHEFE!", "MÉGA BOSS !", "MEGA-BOSS!", "MEGA BOSS!", "MEGA BOSS!", "MEGA BOS!"),
            R("3 faze", "3 phases", "3 fases", "3 fases", "3 phases", "3 Phasen", "3 fasi", "3 aşama", "3 fase"),
            R("atac", "attack", "ataque", "ataque", "attaque", "Angriff", "attacco", "saldırı", "serangan"),
            R("Faza {0}/3", "Phase {0}/3", "Fase {0}/3", "Fase {0}/3", "Phase {0}/3", "Phase {0}/3", "Fase {0}/3", "Aşama {0}/3", "Fase {0}/3"),
            R("Faza {0}!", "Phase {0}!", "¡Fase {0}!", "Fase {0}!", "Phase {0} !", "Phase {0}!", "Fase {0}!", "Aşama {0}!", "Fase {0}!"),
            R("Învinge Boss-ul!", "Defeat the Boss!", "¡Derrota al jefe!", "Derrote o chefe!", "Bats le boss !", "Besiege den Boss!",
                "Sconfiggi il boss!", "Boss'u yen!", "Kalahkan bosnya!"),
            R("Atac la mutarea următoare!", "Attack next move!", "¡Ataque en el próximo movimiento!", "Ataque na próxima jogada!",
                "Attaque au prochain coup !", "Angriff im nächsten Zug!", "Attacco alla prossima mossa!", "Sonraki hamlede saldırı!",
                "Serangan di langkah berikutnya!"),
            R("Boss învins!", "Boss defeated!", "¡Jefe vencido!", "Chefe vencido!", "Boss vaincu !", "Boss besiegt!", "Boss sconfitto!",
                "Boss yenildi!", "Bos dikalahkan!"),
            R("Ploaie de ciocolată!", "Chocolate rain!", "¡Lluvia de chocolate!", "Chuva de chocolate!", "Pluie de chocolat !",
                "Schokoregen!", "Pioggia di cioccolato!", "Çikolata yağmuru!", "Hujan cokelat!"),
            R("Înghețare!", "Deep freeze!", "¡Congelación!", "Congelamento!", "Grand gel !", "Tiefkühlung!", "Gelata!", "Dondurucu!", "Pembekuan!"),
            R("Lanțuri!", "Chains!", "¡Cadenas!", "Correntes!", "Chaînes !", "Ketten!", "Catene!", "Zincirler!", "Rantai!"),
            R("Amestecă tabla!", "Board shuffle!", "¡Tablero mezclado!", "Tabuleiro embaralhado!", "Plateau mélangé !", "Brett gemischt!",
                "Tabellone mescolato!", "Tahta karıştı!", "Papan diacak!"),
            R("Mega-Boss învins! +2 la fiecare ajutor", "Mega-Boss beaten! +2 of each booster", "¡Megajefe vencido! +2 de cada potenciador",
                "Megachefe vencido! +2 de cada reforço", "Méga boss vaincu ! +2 de chaque bonus", "Mega-Boss besiegt! +2 von jedem Booster",
                "Mega boss sconfitto! +2 di ogni potenziamento", "Mega Boss yenildi! Her güçlendiriciden +2", "Mega bos kalah! +2 setiap booster"),
            R("Regele Ciocolată", "Chocolate King", "Rey Chocolate", "Rei Chocolate", "Roi Chocolat", "Schokoladenkönig", "Re Cioccolato",
                "Çikolata Kral", "Raja Cokelat"),
            R("Regina Gheții", "Ice Queen", "Reina del Hielo", "Rainha do Gelo", "Reine des Glaces", "Eiskönigin", "Regina dei Ghiacci",
                "Buz Kraliçesi", "Ratu Es"),
            R("Dragonul de Bomboane", "Candy Dragon", "Dragón de Caramelo", "Dragão de Doce", "Dragon Bonbon", "Bonbondrache", "Drago di Caramelle",
                "Şeker Ejderhası", "Naga Permen"),
            R("Monstrul de Jeleu", "Jelly Monster", "Monstruo de Gelatina", "Monstro de Geleia", "Monstre de Gelée", "Geleemonster",
                "Mostro di Gelatina", "Jöle Canavarı", "Monster Jeli"),
            R("Împăratul Zahărului", "Sugar Emperor", "Emperador del Azúcar", "Imperador do Açúcar", "Empereur du Sucre", "Zuckerkaiser",
                "Imperatore dello Zucchero", "Şeker İmparatoru", "Kaisar Gula"),
            R("Regina Dulciurilor", "Sweets Queen", "Reina de los Dulces", "Rainha dos Doces", "Reine des Douceurs", "Süßigkeitenkönigin",
                "Regina dei Dolci", "Tatlı Kraliçesi", "Ratu Manisan"),
            R("Titanul de Caramel", "Caramel Titan", "Titán de Caramelo", "Titã de Caramelo", "Titan Caramel", "Karamelltitan", "Titano di Caramello",
                "Karamel Titanı", "Titan Karamel"),
            R("Versiunea {0}", "Version {0}", "Versión {0}", "Versão {0}", "Version {0}", "Version {0}", "Versione {0}", "Sürüm {0}", "Versi {0}"),
            R("Reclame", "Ads", "Anuncios", "Anúncios", "Publicités", "Werbung", "Pubblicità", "Reklamlar", "Iklan"),
            R("DA, ACCEPT", "YES, I AGREE", "SÍ, ACEPTO", "SIM, ACEITO", "OUI, J'ACCEPTE", "JA, EINVERSTANDEN", "SÌ, ACCETTO", "EVET, KABUL", "YA, SETUJU"),
            R("NU, MULȚUMESC", "NO, THANKS", "NO, GRACIAS", "NÃO, OBRIGADO", "NON, MERCI", "NEIN, DANKE", "NO, GRAZIE", "HAYIR, TEŞEKKÜRLER", "TIDAK, TERIMA KASIH"),
            R("Reclamele din joc sunt opționale și îți dau recompense (vieți, mutări, ajutoare). Accepți reclame personalizate? Poți schimba oricând din Setări.",
                "Ads in this game are optional and give you rewards (lives, moves, boosters). Do you agree to personalized ads? You can change this anytime in Settings.",
                "Los anuncios del juego son opcionales y te dan recompensas (vidas, movimientos, potenciadores). ¿Aceptas anuncios personalizados? Puedes cambiarlo cuando quieras en Ajustes.",
                "Os anúncios do jogo são opcionais e dão recompensas (vidas, jogadas, reforços). Você aceita anúncios personalizados? Pode mudar isso a qualquer momento nas Configurações.",
                "Les publicités du jeu sont facultatives et te donnent des récompenses (vies, coups, bonus). Acceptes-tu les publicités personnalisées ? Tu peux changer d'avis à tout moment dans les Paramètres.",
                "Werbung ist in diesem Spiel freiwillig und bringt dir Belohnungen (Leben, Züge, Booster). Bist du mit personalisierter Werbung einverstanden? Du kannst das jederzeit in den Einstellungen ändern.",
                "La pubblicità nel gioco è facoltativa e ti dà ricompense (vite, mosse, potenziamenti). Accetti la pubblicità personalizzata? Puoi cambiare idea in qualsiasi momento nelle Impostazioni.",
                "Oyundaki reklamlar isteğe bağlıdır ve sana ödül verir (can, hamle, güçlendirici). Kişiselleştirilmiş reklamları kabul ediyor musun? Bunu istediğin zaman Ayarlar'dan değiştirebilirsin.",
                "Iklan di game ini opsional dan memberimu hadiah (nyawa, langkah, booster). Apakah kamu setuju dengan iklan yang dipersonalisasi? Kamu bisa mengubahnya kapan saja di Pengaturan."),
            R("RECLAME PERSONALIZATE: DA", "PERSONALIZED ADS: YES", "ANUNCIOS PERSONALIZADOS: SÍ", "ANÚNCIOS PERSONALIZADOS: SIM",
                "PUBS PERSONNALISÉES : OUI", "PERSONAL. WERBUNG: JA", "PUBBLICITÀ PERSONALIZZATA: SÌ", "KİŞİSEL REKLAMLAR: EVET",
                "IKLAN PERSONAL: YA"),
            R("RECLAME PERSONALIZATE: NU", "PERSONALIZED ADS: NO", "ANUNCIOS PERSONALIZADOS: NO", "ANÚNCIOS PERSONALIZADOS: NÃO",
                "PUBS PERSONNALISÉES : NON", "PERSONAL. WERBUNG: NEIN", "PUBBLICITÀ PERSONALIZZATA: NO", "KİŞİSEL REKLAMLAR: HAYIR",
                "IKLAN PERSONAL: TIDAK"),
            R("Nicio reclamă acum", "No ad right now", "No hay anuncios ahora", "Nenhum anúncio agora", "Aucune pub pour le moment",
                "Gerade keine Werbung", "Nessuna pubblicità ora", "Şu an reklam yok", "Tidak ada iklan sekarang"),
            R("Nu e nicio reclamă disponibilă. Verifică internetul și încearcă din nou puțin mai târziu.",
                "No ad is available. Check your internet connection and try again a bit later.",
                "No hay ningún anuncio disponible. Revisa tu conexión a internet e inténtalo de nuevo un poco más tarde.",
                "Nenhum anúncio disponível. Verifique sua conexão com a internet e tente novamente daqui a pouco.",
                "Aucune publicité n'est disponible. Vérifie ta connexion internet et réessaie un peu plus tard.",
                "Gerade ist keine Werbung verfügbar. Prüfe deine Internetverbindung und versuche es etwas später erneut.",
                "Nessuna pubblicità disponibile. Controlla la connessione a internet e riprova tra poco.",
                "Şu an uygun reklam yok. İnternet bağlantını kontrol et ve biraz sonra tekrar dene.",
                "Tidak ada iklan yang tersedia. Periksa koneksi internetmu dan coba lagi nanti."),
            R("Trage o bomboană ca să faci 3 la fel în linie!", "Swipe a candy to make 3 in a row!", "¡Desliza un caramelo para hacer 3 en línea!",
                "Deslize um doce para fazer 3 em linha!", "Fais glisser un bonbon pour en aligner 3 !", "Wische ein Bonbon, um 3 in eine Reihe zu bringen!",
                "Trascina una caramella per farne 3 in fila!", "3'lü sıra yapmak için bir şekeri kaydır!", "Geser permen untuk membuat 3 berjajar!"),
            R("Bine ai venit!", "Welcome!", "¡Bienvenido!", "Boas-vindas!", "Bienvenue !", "Willkommen!", "Benvenuto!", "Hoş geldin!", "Selamat datang!"),
            R("Nickname", "Nickname", "Apodo", "Apelido", "Pseudo", "Spitzname", "Nickname", "Takma ad", "Nama panggilan"),
            R("NICKNAME: {0}", "NICKNAME: {0}", "APODO: {0}", "APELIDO: {0}", "PSEUDO : {0}", "SPITZNAME: {0}", "NICKNAME: {0}", "TAKMA AD: {0}", "NAMA: {0}"),
            R("HAI SĂ JUCĂM!", "LET'S PLAY!", "¡A JUGAR!", "VAMOS JOGAR!", "C'EST PARTI !", "LOS GEHT'S!", "GIOCHIAMO!", "HADİ OYNAYALIM!", "AYO MAIN!"),
            R("SALVEAZĂ", "SAVE", "GUARDAR", "SALVAR", "ENREGISTRER", "SPEICHERN", "SALVA", "KAYDET", "SIMPAN"),
            R("ANULEAZĂ", "CANCEL", "CANCELAR", "CANCELAR", "ANNULER", "ABBRECHEN", "ANNULLA", "İPTAL", "BATAL"),
            R("Salut! Eu sunt Bobo, cupcake-ul care te însoțește prin Sugar Drop. Cum să-ți spun? Numele tău va apărea în clasament.",
                "Hi! I'm Bobo, the cupcake who'll keep you company in Sugar Drop. What should I call you? Your name will appear on the leaderboard.",
                "¡Hola! Soy Bobo, el cupcake que te acompañará en Sugar Drop. ¿Cómo te llamo? Tu nombre aparecerá en la clasificación.",
                "Oi! Eu sou o Bobo, o cupcake que vai te acompanhar no Sugar Drop. Como posso te chamar? Seu nome vai aparecer no ranking.",
                "Salut ! Je suis Bobo, le cupcake qui t'accompagne dans Sugar Drop. Comment je t'appelle ? Ton nom apparaîtra dans le classement.",
                "Hallo! Ich bin Bobo, der Cupcake, der dich durch Sugar Drop begleitet. Wie soll ich dich nennen? Dein Name erscheint in der Bestenliste.",
                "Ciao! Sono Bobo, il cupcake che ti accompagna in Sugar Drop. Come ti chiamo? Il tuo nome apparirà in classifica.",
                "Merhaba! Ben Bobo, Sugar Drop'ta sana eşlik edecek cupcake. Sana nasıl seslenelim? Adın sıralamada görünecek.",
                "Hai! Aku Bobo, cupcake yang akan menemanimu di Sugar Drop. Aku harus memanggilmu apa? Namamu akan muncul di papan peringkat."),
            R("Alege numele cu care apari în clasament.", "Choose the name shown on the leaderboard.", "Elige el nombre que aparecerá en la clasificación.",
                "Escolha o nome que aparece no ranking.", "Choisis le nom affiché dans le classement.", "Wähle den Namen für die Bestenliste.",
                "Scegli il nome che appare in classifica.", "Sıralamada görünecek adı seç.", "Pilih nama yang tampil di papan peringkat."),
            R("Minimum {0} caractere", "At least {0} characters", "Mínimo {0} caracteres", "Mínimo de {0} caracteres", "Au moins {0} caractères",
                "Mindestens {0} Zeichen", "Almeno {0} caratteri", "En az {0} karakter", "Minimal {0} karakter"),
            R("Litere, cifre și spații, maximum {0}", "Letters, digits and spaces, up to {0}", "Letras, números y espacios, máximo {0}",
                "Letras, números e espaços, até {0}", "Lettres, chiffres et espaces, {0} max.", "Buchstaben, Ziffern und Leerzeichen, max. {0}",
                "Lettere, numeri e spazi, massimo {0}", "Harf, rakam ve boşluk, en fazla {0}", "Huruf, angka, dan spasi, maks. {0}"),
            R("Salut, {0}!", "Hi, {0}!", "¡Hola, {0}!", "Oi, {0}!", "Salut, {0} !", "Hallo, {0}!", "Ciao, {0}!", "Merhaba, {0}!", "Hai, {0}!"),
            R("CLASAMENT", "LEADERBOARD", "CLASIFICACIÓN", "RANKING", "CLASSEMENT", "BESTENLISTE", "CLASSIFICA", "SIRALAMA", "PERINGKAT"),
            R("Punctele = suma celor mai bune scoruri de pe fiecare nivel", "Points = your best score on every level, added up",
                "Puntos = la suma de tu mejor puntuación en cada nivel", "Pontos = a soma da sua melhor pontuação em cada nível",
                "Points = la somme de ton meilleur score sur chaque niveau", "Punkte = Summe deiner Bestwerte aus jedem Level",
                "Punti = la somma del tuo record in ogni livello", "Puan = her seviyedeki en iyi skorlarının toplamı",
                "Poin = jumlah skor terbaikmu di setiap level"),
            R("Se încarcă…", "Loading…", "Cargando…", "Carregando…", "Chargement…", "Wird geladen…", "Caricamento…", "Yükleniyor…", "Memuat…"),
            R("Fii primul în clasament!", "Be the first on the leaderboard!", "¡Sé el primero de la clasificación!", "Seja o primeiro do ranking!",
                "Sois le premier du classement !", "Sei der Erste in der Bestenliste!", "Sii il primo in classifica!", "Sıralamada ilk sen ol!",
                "Jadilah yang pertama di papan peringkat!"),
            R("Clasamentul online nu e disponibil acum. Verifică internetul.", "The online leaderboard isn't available right now. Check your internet connection.",
                "La clasificación en línea no está disponible ahora. Revisa tu conexión a internet.",
                "O ranking online não está disponível agora. Verifique sua conexão com a internet.",
                "Le classement en ligne n'est pas disponible pour le moment. Vérifie ta connexion internet.",
                "Die Online-Bestenliste ist gerade nicht verfügbar. Prüfe deine Internetverbindung.",
                "La classifica online non è disponibile ora. Controlla la connessione a internet.",
                "Çevrimiçi sıralama şu an kullanılamıyor. İnternet bağlantını kontrol et.",
                "Papan peringkat online sedang tidak tersedia. Periksa koneksi internetmu."),
            R("Locul tău: {0} · {1} puncte", "Your rank: {0} · {1} points", "Tu puesto: {0} · {1} puntos", "Sua posição: {0} · {1} pontos",
                "Ton rang : {0} · {1} points", "Dein Platz: {0} · {1} Punkte", "La tua posizione: {0} · {1} punti", "Sıran: {0} · {1} puan",
                "Peringkatmu: {0} · {1} poin"),
            R("Punctele tale: {0}", "Your points: {0}", "Tus puntos: {0}", "Seus pontos: {0}", "Tes points : {0}", "Deine Punkte: {0}",
                "I tuoi punti: {0}", "Puanın: {0}", "Poinmu: {0}"),
            R(" (tu)", " (you)", " (tú)", " (você)", " (toi)", " (du)", " (tu)", " (sen)", " (kamu)"),
        });

        private static string[] R(params string[] row) => row;

        private static Dictionary<string, string[]> Build(string[][] rows)
        {
            var table = new Dictionary<string, string[]>(rows.Length);
            foreach (string[] row in rows) table[row[0]] = row;
            return table;
        }
    }
}
