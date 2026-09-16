import type { Language, LocalizedText, QuizQuestionSource } from "@/types";

const TRUE: LocalizedText = { en: "True", ro: "Adevărat", es: "Verdadero", pt: "Verdadeiro", hi: "सत्य", id: "Benar", ru: "Правда" };
const FALSE: LocalizedText = { en: "False", ro: "Fals", es: "Falso", pt: "Falso", hi: "असत्य", id: "Salah", ru: "Ложь" };
const LANGUAGES = Object.keys(TRUE) as Language[];

function boolAnswers(isTrue: boolean): Pick<QuizQuestionSource, "correctAnswer" | "distractors"> {
  const correctAnswer = isTrue ? TRUE : FALSE;
  const distractor = isTrue ? FALSE : TRUE;
  const distractors = Object.fromEntries(LANGUAGES.map((lang) => [lang, [distractor[lang]]])) as Record<Language, string[]>;
  return { correctAnswer, distractors };
}

/**
 * True/False — a simple statement with exactly two options (the correct
 * boolean and its opposite as the sole distractor). The answer-pill list
 * that renders every other renderKind already handles any option count
 * generically, so this needed no dedicated UI — just content and the
 * looser 1-distractor rule questionBank.test.ts allows for this renderKind.
 */
export const TRUE_FALSE_QUESTIONS: readonly QuizQuestionSource[] = [
  {
    id: "tf-1",
    categoryId: "GENERAL_KNOWLEDGE",
    difficulty: 2,
    renderKind: "TRUE_FALSE",
    prompt: {
      en: "The Great Wall of China is visible from space with the naked eye.",
      ro: "Marele Zid Chinezesc este vizibil din spațiu cu ochiul liber.",
      es: "La Gran Muralla China es visible desde el espacio a simple vista.",
      pt: "A Grande Muralha da China é visível do espaço a olho nu.",
      hi: "चीन की महान दीवार अंतरिक्ष से नग्न आंखों से दिखाई देती है।",
      id: "Tembok Besar Tiongkok terlihat dari luar angkasa dengan mata telanjang.",
      ru: "Великую Китайскую стену видно из космоса невооружённым глазом.",
    },
    ...boolAnswers(false),
  },
  {
    id: "tf-2",
    categoryId: "GENERAL_KNOWLEDGE",
    difficulty: 1,
    renderKind: "TRUE_FALSE",
    prompt: {
      en: "The Pacific Ocean is the largest ocean on Earth.",
      ro: "Oceanul Pacific este cel mai mare ocean de pe Pământ.",
      es: "El océano Pacífico es el océano más grande de la Tierra.",
      pt: "O oceano Pacífico é o maior oceano da Terra.",
      hi: "प्रशांत महासागर पृथ्वी का सबसे बड़ा महासागर है।",
      id: "Samudra Pasifik adalah samudra terbesar di Bumi.",
      ru: "Тихий океан — самый большой океан на Земле.",
    },
    ...boolAnswers(true),
  },
  {
    id: "tf-3",
    categoryId: "GENERAL_KNOWLEDGE",
    difficulty: 1,
    renderKind: "TRUE_FALSE",
    prompt: {
      en: "A group of lions is called a pride.",
      ro: "Un grup de lei se numește haită.",
      es: "Un grupo de leones se llama manada.",
      pt: "Um grupo de leões é chamado de manada.",
      hi: "शेरों के समूह को 'प्राइड' कहा जाता है।",
      id: "Sekelompok singa disebut 'pride'.",
      ru: "Группу львов называют прайдом.",
    },
    ...boolAnswers(true),
  },
  {
    id: "tf-4",
    categoryId: "GENERAL_KNOWLEDGE",
    difficulty: 1,
    renderKind: "TRUE_FALSE",
    prompt: {
      en: "Sharks are mammals.",
      ro: "Rechinii sunt mamifere.",
      es: "Los tiburones son mamíferos.",
      pt: "Os tubarões são mamíferos.",
      hi: "शार्क स्तनधारी होते हैं।",
      id: "Hiu adalah mamalia.",
      ru: "Акулы — это млекопитающие.",
    },
    ...boolAnswers(false),
  },
  {
    id: "tf-5",
    categoryId: "GENERAL_KNOWLEDGE",
    difficulty: 1,
    renderKind: "TRUE_FALSE",
    prompt: {
      en: "The Eiffel Tower is located in London.",
      ro: "Turnul Eiffel se află la Londra.",
      es: "La Torre Eiffel está ubicada en Londres.",
      pt: "A Torre Eiffel está localizada em Londres.",
      hi: "एफिल टॉवर लंदन में स्थित है।",
      id: "Menara Eiffel terletak di London.",
      ru: "Эйфелева башня находится в Лондоне.",
    },
    ...boolAnswers(false),
  },
  {
    id: "tf-6",
    categoryId: "GENERAL_KNOWLEDGE",
    difficulty: 2,
    renderKind: "TRUE_FALSE",
    prompt: {
      en: "Water boils at 100°C at sea level.",
      ro: "Apa fierbe la 100°C la nivelul mării.",
      es: "El agua hierve a 100 °C al nivel del mar.",
      pt: "A água ferve a 100 °C ao nível do mar.",
      hi: "समुद्र तल पर पानी 100°C पर उबलता है।",
      id: "Air mendidih pada 100°C di permukaan laut.",
      ru: "Вода закипает при 100°C на уровне моря.",
    },
    ...boolAnswers(true),
  },
  {
    id: "tf-7",
    categoryId: "GENERAL_KNOWLEDGE",
    difficulty: 2,
    renderKind: "TRUE_FALSE",
    prompt: {
      en: "Bats are blind.",
      ro: "Liliecii sunt orbi.",
      es: "Los murciélagos son ciegos.",
      pt: "Os morcegos são cegos.",
      hi: "चमगादड़ अंधे होते हैं।",
      id: "Kelelawar itu buta.",
      ru: "Летучие мыши слепы.",
    },
    ...boolAnswers(false),
  },
  {
    id: "tf-8",
    categoryId: "GENERAL_KNOWLEDGE",
    difficulty: 1,
    renderKind: "TRUE_FALSE",
    prompt: {
      en: "The Sun rises in the west.",
      ro: "Soarele răsare la vest.",
      es: "El sol sale por el oeste.",
      pt: "O sol nasce no oeste.",
      hi: "सूरज पश्चिम में उगता है।",
      id: "Matahari terbit di barat.",
      ru: "Солнце встаёт на западе.",
    },
    ...boolAnswers(false),
  },
  {
    id: "tf-9",
    categoryId: "GENERAL_KNOWLEDGE",
    difficulty: 1,
    renderKind: "TRUE_FALSE",
    prompt: {
      en: "Mount Everest is the tallest mountain on Earth.",
      ro: "Muntele Everest este cel mai înalt munte de pe Pământ.",
      es: "El monte Everest es la montaña más alta de la Tierra.",
      pt: "O monte Everest é a montanha mais alta da Terra.",
      hi: "माउंट एवरेस्ट पृथ्वी का सबसे ऊँचा पर्वत है।",
      id: "Gunung Everest adalah gunung tertinggi di Bumi.",
      ru: "Эверест — самая высокая гора на Земле.",
    },
    ...boolAnswers(true),
  },
  {
    id: "tf-10",
    categoryId: "GENERAL_KNOWLEDGE",
    difficulty: 3,
    renderKind: "TRUE_FALSE",
    prompt: {
      en: "Octopuses have three hearts.",
      ro: "Caracatițele au trei inimi.",
      es: "Los pulpos tienen tres corazones.",
      pt: "Os polvos têm três corações.",
      hi: "ऑक्टोपस के तीन दिल होते हैं।",
      id: "Gurita memiliki tiga jantung.",
      ru: "У осьминогов три сердца.",
    },
    ...boolAnswers(true),
  },
  {
    id: "tf-11",
    categoryId: "GENERAL_KNOWLEDGE",
    difficulty: 2,
    renderKind: "TRUE_FALSE",
    prompt: {
      en: "Lightning never strikes the same place twice.",
      ro: "Fulgerul nu lovește niciodată de două ori în același loc.",
      es: "Un rayo nunca cae dos veces en el mismo lugar.",
      pt: "Um raio nunca cai duas vezes no mesmo lugar.",
      hi: "बिजली कभी भी एक ही स्थान पर दो बार नहीं गिरती।",
      id: "Petir tidak pernah menyambar tempat yang sama dua kali.",
      ru: "Молния никогда не бьёт в одно и то же место дважды.",
    },
    ...boolAnswers(false),
  },
  {
    id: "tf-12",
    categoryId: "GENERAL_KNOWLEDGE",
    difficulty: 2,
    renderKind: "TRUE_FALSE",
    prompt: {
      en: "The Sahara is the largest hot desert in the world.",
      ro: "Sahara este cel mai mare deșert cald din lume.",
      es: "El Sahara es el desierto cálido más grande del mundo.",
      pt: "O Saara é o maior deserto quente do mundo.",
      hi: "सहारा दुनिया का सबसे बड़ा गर्म रेगिस्तान है।",
      id: "Sahara adalah gurun panas terbesar di dunia.",
      ru: "Сахара — крупнейшая жаркая пустыня в мире.",
    },
    ...boolAnswers(true),
  },
];
