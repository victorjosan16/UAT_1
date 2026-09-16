import type { Language } from "@/types";

/**
 * Every UI string in the app, keyed by dotted path then by language — see
 * docs/GAME_DESIGN.md §Localization. Question/category/country content
 * lives next to its own data instead (data/questions/*, data/categories.ts,
 * data/countries.ts) since each entry needs its own translation, not a
 * shared template.
 */
const STRINGS = {
  "nav.home": { en: "Home", ro: "Acasă", es: "Inicio", pt: "Início", hi: "होम", id: "Beranda", ru: "Главная" },
  "nav.discover": { en: "Discover", ro: "Descoperă", es: "Descubrir", pt: "Descobrir", hi: "खोजें", id: "Jelajahi", ru: "Обзор" },
  "nav.play": { en: "Play", ro: "Joacă", es: "Jugar", pt: "Jogar", hi: "खेलें", id: "Main", ru: "Играть" },
  "nav.ranking": { en: "Ranking", ro: "Clasament", es: "Clasificación", pt: "Classificação", hi: "रैंकिंग", id: "Peringkat", ru: "Рейтинг" },
  "nav.profile": { en: "Profile", ro: "Profil", es: "Perfil", pt: "Perfil", hi: "प्रोफ़ाइल", id: "Profil", ru: "Профиль" },

  "home.greeting": { en: "Hi, {name}", ro: "Salut, {name}", es: "Hola, {name}", pt: "Olá, {name}", hi: "नमस्ते, {name}", id: "Hai, {name}", ru: "Привет, {name}" },
  "home.dailyStreakDays": { en: "{count} days", ro: "{count} zile", es: "{count} días", pt: "{count} dias", hi: "{count} दिन", id: "{count} hari", ru: "{count} дней" },
  "home.dailyStreakDay": { en: "{count} day", ro: "{count} zi", es: "{count} día", pt: "{count} dia", hi: "{count} दिन", id: "{count} hari", ru: "{count} день" },
  "home.hero.eyebrow": { en: "DAILY CHALLENGE", ro: "PROVOCAREA ZILEI", es: "DESAFÍO DIARIO", pt: "DESAFIO DIÁRIO", hi: "दैनिक चुनौती", id: "TANTANGAN HARIAN", ru: "ЕЖЕДНЕВНЫЙ ВЫЗОВ" },
  "home.hero.title": {
    en: "10 Questions, Once a Day",
    ro: "10 Întrebări, O Dată pe Zi",
    es: "10 Preguntas, Una Vez al Día",
    pt: "10 Perguntas, Uma Vez por Dia",
    hi: "10 प्रश्न, दिन में एक बार",
    id: "10 Pertanyaan, Sekali Sehari",
    ru: "10 вопросов раз в день",
  },
  "home.hero.meta": {
    en: "Same set for everyone today — see how you stack up.",
    ro: "Același set pentru toți azi — vezi cum te descurci.",
    es: "El mismo set para todos hoy — mira cómo te va.",
    pt: "O mesmo conjunto para todos hoje — veja como você se sai.",
    hi: "आज सभी के लिए एक जैसे प्रश्न — देखें आप कैसा करते हैं।",
    id: "Set yang sama untuk semua orang hari ini — lihat performamu.",
    ru: "Один и тот же набор для всех сегодня — посмотри, как ты справишься.",
  },
  "home.hero.cta": { en: "PLAY NOW", ro: "JOACĂ ACUM", es: "JUGAR AHORA", pt: "JOGAR AGORA", hi: "अभी खेलें", id: "MAIN SEKARANG", ru: "ИГРАТЬ СЕЙЧАС" },
  "home.quickPlay": { en: "Quick Play", ro: "Joc Rapid", es: "Juego Rápido", pt: "Jogo Rápido", hi: "क्विक प्ले", id: "Main Cepat", ru: "Быстрая игра" },
  "home.levelJourney": { en: "Level Journey", ro: "Traseul Nivelurilor", es: "Ruta de Niveles", pt: "Jornada de Níveis", hi: "लेवल यात्रा", id: "Perjalanan Level", ru: "Путь уровней" },
  "home.categories": { en: "Categories", ro: "Categorii", es: "Categorías", pt: "Categorias", hi: "श्रेणियाँ", id: "Kategori", ru: "Категории" },
  "home.seeAll": { en: "See all", ro: "Vezi tot", es: "Ver todo", pt: "Ver tudo", hi: "सभी देखें", id: "Lihat semua", ru: "Смотреть все" },
  "home.continue": { en: "Continue", ro: "Continuă", es: "Continuar", pt: "Continuar", hi: "जारी रखें", id: "Lanjutkan", ru: "Продолжить" },
  "home.continueCta": { en: "CONTINUE", ro: "CONTINUĂ", es: "CONTINUAR", pt: "CONTINUAR", hi: "जारी रखें", id: "LANJUTKAN", ru: "ПРОДОЛЖИТЬ" },
  "home.yourBest": { en: "Your Best", ro: "Recordurile Tale", es: "Tu Mejor Marca", pt: "Seu Melhor Resultado", hi: "आपका सर्वश्रेष्ठ", id: "Rekor Terbaikmu", ru: "Твой лучший результат" },
  "home.bestScore": { en: "Best Score", ro: "Scor Maxim", es: "Mejor Puntuación", pt: "Melhor Pontuação", hi: "सर्वश्रेष्ठ स्कोर", id: "Skor Terbaik", ru: "Лучший счёт" },
  "home.bestIQ": { en: "Best Q5 IQ", ro: "Q5 IQ Maxim", es: "Mejor Q5 IQ", pt: "Melhor Q5 IQ", hi: "सर्वश्रेष्ठ Q5 IQ", id: "Q5 IQ Terbaik", ru: "Лучший Q5 IQ" },

  "discover.title": { en: "Discover", ro: "Descoperă", es: "Descubrir", pt: "Descobrir", hi: "खोजें", id: "Jelajahi", ru: "Обзор" },
  "discover.subtitle": {
    en: "Pick a category and start a 10-question run.",
    ro: "Alege o categorie și începe un set de 10 întrebări.",
    es: "Elige una categoría y comienza una ronda de 10 preguntas.",
    pt: "Escolha uma categoria e comece uma rodada de 10 perguntas.",
    hi: "एक श्रेणी चुनें और 10 प्रश्नों की एक श्रृंखला शुरू करें।",
    id: "Pilih kategori dan mulai 10 pertanyaan.",
    ru: "Выбери категорию и начни раунд из 10 вопросов.",
  },
  "discover.play": { en: "PLAY", ro: "JOACĂ", es: "JUGAR", pt: "JOGAR", hi: "खेलें", id: "MAIN", ru: "ИГРАТЬ" },
  "discover.comingSoon": { en: "SOON", ro: "ÎN CURÂND", es: "PRONTO", pt: "EM BREVE", hi: "जल्द आ रहा है", id: "SEGERA", ru: "СКОРО" },
  "discover.comingSoonBadge": { en: "Coming soon", ro: "În curând", es: "Próximamente", pt: "Em breve", hi: "जल्द आ रहा है", id: "Segera hadir", ru: "Скоро" },

  "play.title": { en: "Play", ro: "Joacă", es: "Jugar", pt: "Jogar", hi: "खेलें", id: "Main", ru: "Играть" },
  "play.subtitle": {
    en: "Choose how you want to play.",
    ro: "Alege cum vrei să joci.",
    es: "Elige cómo quieres jugar.",
    pt: "Escolha como você quer jogar.",
    hi: "चुनें कि आप कैसे खेलना चाहते हैं।",
    id: "Pilih cara bermainmu.",
    ru: "Выбери, как ты хочешь играть.",
  },
  "play.quickPlay.title": { en: "Quick Play", ro: "Joc Rapid", es: "Juego Rápido", pt: "Jogo Rápido", hi: "क्विक प्ले", id: "Main Cepat", ru: "Быстрая игра" },
  "play.quickPlay.desc": {
    en: "10 mixed questions, straight in.",
    ro: "10 întrebări variate, direct la joc.",
    es: "10 preguntas variadas, directo al juego.",
    pt: "10 perguntas variadas, direto ao jogo.",
    hi: "10 मिश्रित प्रश्न, सीधे खेल में।",
    id: "10 pertanyaan campuran, langsung main.",
    ru: "10 разных вопросов, сразу в игру.",
  },
  "play.category.title": { en: "Category Quiz", ro: "Quiz pe Categorii", es: "Quiz por Categoría", pt: "Quiz por Categoria", hi: "श्रेणी क्विज़", id: "Kuis Kategori", ru: "Викторина по категориям" },
  "play.category.desc": {
    en: "Pick a topic and test it in depth.",
    ro: "Alege un subiect și testează-l în profunzime.",
    es: "Elige un tema y ponlo a prueba a fondo.",
    pt: "Escolha um tema e teste-o a fundo.",
    hi: "एक विषय चुनें और उसे गहराई से परखें।",
    id: "Pilih topik dan uji secara mendalam.",
    ru: "Выбери тему и проверь себя основательно.",
  },
  "play.daily.title": { en: "Daily Challenge", ro: "Provocarea Zilei", es: "Desafío Diario", pt: "Desafio Diário", hi: "दैनिक चुनौती", id: "Tantangan Harian", ru: "Ежедневный вызов" },
  "play.daily.desc": {
    en: "Same 10 questions for everyone today.",
    ro: "Aceleași 10 întrebări pentru toți azi.",
    es: "Las mismas 10 preguntas para todos hoy.",
    pt: "As mesmas 10 perguntas para todos hoje.",
    hi: "आज सभी के लिए वही 10 प्रश्न।",
    id: "10 pertanyaan yang sama untuk semua hari ini.",
    ru: "Одни и те же 10 вопросов для всех сегодня.",
  },
  "play.level.title": { en: "Level Journey", ro: "Traseul Nivelurilor", es: "Ruta de Niveles", pt: "Jornada de Níveis", hi: "लेवल यात्रा", id: "Perjalanan Level", ru: "Путь уровней" },
  "play.level.desc": {
    en: "20 levels, Rookie to Genius, then Endless.",
    ro: "20 de niveluri, de la Începător la Geniu, apoi Infinit.",
    es: "20 niveles, de Novato a Genio, luego Infinito.",
    pt: "20 níveis, de Novato a Gênio, depois Infinito.",
    hi: "20 स्तर, नौसिखिया से जीनियस तक, फिर एंडलेस।",
    id: "20 level, dari Pemula ke Jenius, lalu Tanpa Batas.",
    ru: "20 уровней, от Новичка до Гения, затем Бесконечный режим.",
  },
  "play.championship.title": {
    en: "5-Player Championship",
    ro: "Campionat pe 5 Jucători",
    es: "Campeonato de 5 Jugadores",
    pt: "Campeonato de 5 Jogadores",
    hi: "5-खिलाड़ी चैंपियनशिप",
    id: "Kejuaraan 5 Pemain",
    ru: "Чемпионат на 5 игроков",
  },
  "play.championship.desc": {
    en: "Live multiplayer — coming soon.",
    ro: "Multiplayer live — în curând.",
    es: "Multijugador en vivo — próximamente.",
    pt: "Multijogador ao vivo — em breve.",
    hi: "लाइव मल्टीप्लेयर — जल्द आ रहा है।",
    id: "Multipemain langsung — segera hadir.",
    ru: "Live-мультиплеер — скоро.",
  },
  "play.friend.title": { en: "Challenge a Friend", ro: "Provoacă un Prieten", es: "Desafía a un Amigo", pt: "Desafie um Amigo", hi: "किसी मित्र को चुनौती दें", id: "Tantang Teman", ru: "Бросить вызов другу" },
  "play.friend.desc": {
    en: "Async 1v1 — coming soon.",
    ro: "1v1 asincron — în curând.",
    es: "1v1 asíncrono — próximamente.",
    pt: "1x1 assíncrono — em breve.",
    hi: "असिंक्रोनस 1v1 — जल्द आ रहा है।",
    id: "1v1 asinkron — segera hadir.",
    ru: "Асинхронный 1 на 1 — скоро.",
  },

  "ranking.title": { en: "Ranking", ro: "Clasament", es: "Clasificación", pt: "Classificação", hi: "रैंकिंग", id: "Peringkat", ru: "Рейтинг" },
  "ranking.subtitle": {
    en: "Online leaderboards are coming soon.",
    ro: "Clasamentele online vin în curând.",
    es: "Las clasificaciones en línea llegarán pronto.",
    pt: "Os rankings online chegarão em breve.",
    hi: "ऑनलाइन लीडरबोर्ड जल्द आ रहे हैं।",
    id: "Papan peringkat online segera hadir.",
    ru: "Онлайн-рейтинги скоро появятся.",
  },
  "ranking.emptyText": {
    en: "Play a run to start tracking your best score.",
    ro: "Joacă un set ca să-ți urmărești scorul maxim.",
    es: "Juega una ronda para empezar a registrar tu mejor puntuación.",
    pt: "Jogue uma rodada para começar a registrar sua melhor pontuação.",
    hi: "अपना सर्वश्रेष्ठ स्कोर ट्रैक करने के लिए एक गेम खेलें।",
    id: "Mainkan satu putaran untuk mulai melacak skor terbaikmu.",
    ru: "Сыграй раунд, чтобы начать отслеживать свой лучший результат.",
  },
  "ranking.bestScore": { en: "Best Score", ro: "Scor Maxim", es: "Mejor Puntuación", pt: "Melhor Pontuação", hi: "सर्वश्रेष्ठ स्कोर", id: "Skor Terbaik", ru: "Лучший счёт" },
  "ranking.bestIQ": { en: "Best Q5 IQ", ro: "Q5 IQ Maxim", es: "Mejor Q5 IQ", pt: "Melhor Q5 IQ", hi: "सर्वश्रेष्ठ Q5 IQ", id: "Q5 IQ Terbaik", ru: "Лучший Q5 IQ" },
  "ranking.bestStreak": { en: "Best Streak", ro: "Serie Maximă", es: "Mejor Racha", pt: "Melhor Sequência", hi: "सर्वश्रेष्ठ स्ट्रीक", id: "Rentetan Terbaik", ru: "Лучшая серия" },
  "ranking.runsPlayed": { en: "Runs Played", ro: "Jocuri Jucate", es: "Partidas Jugadas", pt: "Partidas Jogadas", hi: "खेले गए गेम", id: "Permainan Dimainkan", ru: "Сыграно игр" },

  "profile.guestPlayer": { en: "Guest player", ro: "Jucător invitat", es: "Jugador invitado", pt: "Jogador convidado", hi: "अतिथि खिलाड़ी", id: "Pemain tamu", ru: "Гость" },
  "profile.stats": { en: "Stats", ro: "Statistici", es: "Estadísticas", pt: "Estatísticas", hi: "आँकड़े", id: "Statistik", ru: "Статистика" },
  "profile.badges": { en: "Badges", ro: "Insigne", es: "Insignias", pt: "Emblemas", hi: "बैज", id: "Lencana", ru: "Значки" },
  "profile.runsPlayed": { en: "Runs Played", ro: "Jocuri Jucate", es: "Partidas Jugadas", pt: "Partidas Jogadas", hi: "खेले गए गेम", id: "Permainan Dimainkan", ru: "Сыграно игр" },
  "profile.bestScore": { en: "Best Score", ro: "Scor Maxim", es: "Mejor Puntuación", pt: "Melhor Pontuação", hi: "सर्वश्रेष्ठ स्कोर", id: "Skor Terbaik", ru: "Лучший счёт" },
  "profile.bestStreak": { en: "Best Streak", ro: "Serie Maximă", es: "Mejor Racha", pt: "Melhor Sequência", hi: "सर्वश्रेष्ठ स्ट्रीक", id: "Rentetan Terbaik", ru: "Лучшая серия" },
  "profile.bestIQ": { en: "Best Q5 IQ", ro: "Q5 IQ Maxim", es: "Mejor Q5 IQ", pt: "Melhor Q5 IQ", hi: "सर्वश्रेष्ठ Q5 IQ", id: "Q5 IQ Terbaik", ru: "Лучший Q5 IQ" },
  "profile.dailyStreak": { en: "Daily Streak", ro: "Serie Zilnică", es: "Racha Diaria", pt: "Sequência Diária", hi: "दैनिक स्ट्रीक", id: "Rentetan Harian", ru: "Ежедневная серия" },
  "profile.locked": { en: "Locked", ro: "Blocată", es: "Bloqueado", pt: "Bloqueado", hi: "लॉक्ड", id: "Terkunci", ru: "Заблокировано" },
  "profile.language": { en: "Language", ro: "Limbă", es: "Idioma", pt: "Idioma", hi: "भाषा", id: "Bahasa", ru: "Язык" },

  "quiz.questionOf": {
    en: "Question {index} of {total}",
    ro: "Întrebarea {index} din {total}",
    es: "Pregunta {index} de {total}",
    pt: "Pergunta {index} de {total}",
    hi: "प्रश्न {index} / {total}",
    id: "Pertanyaan {index} dari {total}",
    ru: "Вопрос {index} из {total}",
  },
  "quiz.correct": { en: "CORRECT!", ro: "CORECT!", es: "¡CORRECTO!", pt: "CORRETO!", hi: "सही!", id: "BENAR!", ru: "ВЕРНО!" },
  "quiz.timesUp": { en: "TIME'S UP!", ro: "TIMPUL A EXPIRAT!", es: "¡SE ACABÓ EL TIEMPO!", pt: "TEMPO ESGOTADO!", hi: "समय समाप्त!", id: "WAKTU HABIS!", ru: "ВРЕМЯ ВЫШЛО!" },
  "quiz.quit": { en: "Quit", ro: "Ieșire", es: "Salir", pt: "Sair", hi: "बाहर निकलें", id: "Keluar", ru: "Выйти" },
  "quiz.correctAnswers": { en: "Correct answers", ro: "Răspunsuri corecte", es: "Respuestas correctas", pt: "Respostas corretas", hi: "सही उत्तर", id: "Jawaban benar", ru: "Правильные ответы" },
  "quiz.streak": { en: "Streak", ro: "Serie", es: "Racha", pt: "Sequência", hi: "स्ट्रीक", id: "Rentetan", ru: "Серия" },
  "quiz.score": { en: "Score", ro: "Scor", es: "Puntuación", pt: "Pontuação", hi: "स्कोर", id: "Skor", ru: "Счёт" },
  "quiz.level": { en: "LEVEL", ro: "NIVEL", es: "NIVEL", pt: "NÍVEL", hi: "स्तर", id: "LEVEL", ru: "УРОВЕНЬ" },

  "results.perfectGame": { en: "PERFECT GAME", ro: "JOC PERFECT", es: "JUEGO PERFECTO", pt: "JOGO PERFEITO", hi: "परफेक्ट गेम", id: "PERMAINAN SEMPURNA", ru: "ИДЕАЛЬНАЯ ИГРА" },
  "results.newRecord": { en: "NEW RECORD!", ro: "RECORD NOU!", es: "¡NUEVO RÉCORD!", pt: "NOVO RECORDE!", hi: "नया रिकॉर्ड!", id: "REKOR BARU!", ru: "НОВЫЙ РЕКОРД!" },
  "results.correct": { en: "CORRECT", ro: "CORECTE", es: "CORRECTAS", pt: "CORRETAS", hi: "सही", id: "BENAR", ru: "ВЕРНО" },
  "results.points": { en: "POINTS", ro: "PUNCTE", es: "PUNTOS", pt: "PONTOS", hi: "अंक", id: "POIN", ru: "ОЧКИ" },
  "results.bestStreak": { en: "BEST STREAK", ro: "SERIE MAXIMĂ", es: "MEJOR RACHA", pt: "MELHOR SEQUÊNCIA", hi: "सर्वश्रेष्ठ स्ट्रीक", id: "RENTETAN TERBAIK", ru: "ЛУЧШАЯ СЕРИЯ" },
  "results.avgResponse": { en: "AVG RESPONSE", ro: "TIMP MEDIU", es: "TIEMPO MEDIO", pt: "TEMPO MÉDIO", hi: "औसत समय", id: "RATA-RATA WAKTU", ru: "СРЕДНЕЕ ВРЕМЯ" },
  "results.playAgain": { en: "PLAY AGAIN", ro: "JOACĂ DIN NOU", es: "JUGAR DE NUEVO", pt: "JOGAR NOVAMENTE", hi: "फिर से खेलें", id: "MAIN LAGI", ru: "ИГРАТЬ СНОВА" },
  "results.backToHome": { en: "BACK TO HOME", ro: "ÎNAPOI ACASĂ", es: "VOLVER AL INICIO", pt: "VOLTAR AO INÍCIO", hi: "होम पर वापस जाएं", id: "KEMBALI KE BERANDA", ru: "НА ГЛАВНУЮ" },

  "badge.firstWin": { en: "FIRST WIN", ro: "PRIMA VICTORIE", es: "PRIMERA VICTORIA", pt: "PRIMEIRA VITÓRIA", hi: "पहली जीत", id: "KEMENANGAN PERTAMA", ru: "ПЕРВАЯ ПОБЕДА" },
  "badge.perfect10": { en: "PERFECT 10", ro: "10 PERFECT", es: "10 PERFECTO", pt: "10 PERFEITO", hi: "परफेक्ट 10", id: "10 SEMPURNA", ru: "ИДЕАЛЬНЫЕ 10" },
  "badge.onFire": { en: "ON FIRE", ro: "ÎN FLĂCĂRI", es: "EN LLAMAS", pt: "PEGANDO FOGO", hi: "ऑन फायर", id: "SEDANG PANAS", ru: "В УДАРЕ" },
  "badge.sevenDayStreak": { en: "7 DAY STREAK", ro: "SERIE DE 7 ZILE", es: "RACHA DE 7 DÍAS", pt: "SEQUÊNCIA DE 7 DIAS", hi: "7 दिन की स्ट्रीक", id: "RENTETAN 7 HARI", ru: "СЕРИЯ 7 ДНЕЙ" },
  "badge.thousandQuestions": { en: "1000 QUESTIONS", ro: "1000 DE ÎNTREBĂRI", es: "1000 PREGUNTAS", pt: "1000 PERGUNTAS", hi: "1000 प्रश्न", id: "1000 PERTANYAAN", ru: "1000 ВОПРОСОВ" },
  "badge.champion": { en: "CHAMPION", ro: "CAMPION", es: "CAMPEÓN", pt: "CAMPEÃO", hi: "चैंपियन", id: "JUARA", ru: "ЧЕМПИОН" },
} as const satisfies Record<string, Record<Language, string>>;

export type StringKey = keyof typeof STRINGS;

/** Exposed for tests only — verifying every key has non-empty text in every supported language. */
export const STRING_KEYS = Object.keys(STRINGS) as StringKey[];

/** Resolves a UI string in the given language, substituting any `{placeholder}` values. */
export function translate(key: StringKey, language: Language, vars?: Record<string, string | number>): string {
  const template = STRINGS[key][language];
  if (!vars) return template;
  return template.replace(/\{(\w+)\}/g, (match: string, name: string) => (name in vars ? String(vars[name]) : match));
}
