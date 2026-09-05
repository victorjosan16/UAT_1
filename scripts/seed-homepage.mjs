// Populează homepage_blocks cu un set curat de blocuri (Hero cu Imagine,
// Carduri Beneficii, Pași, Carusel Produse), ca pagina principală să arate
// ca layout-ul de referință, cu text adaptat pentru un print shop (PosterART).
// Actualizează și meniul din site_config/header (nav + buton CTA), păstrând
// logo/locație/telefon/program existente neschimbate.
//
// Rulare: node scripts/seed-homepage.mjs
// (din rădăcina proiectului, cu .env.local completat)

import { readFileSync, existsSync } from "node:fs";
import { initializeApp } from "firebase/app";
import {
  getFirestore,
  collection,
  getDocs,
  deleteDoc,
  doc,
  addDoc,
  getDoc,
  setDoc,
} from "firebase/firestore";

function incarcaEnvLocal() {
  const cale = ".env.local";
  if (!existsSync(cale)) return;
  const continut = readFileSync(cale, "utf8");
  for (const linie of continut.split("\n")) {
    const linieCuratata = linie.trim();
    if (!linieCuratata || linieCuratata.startsWith("#")) continue;
    const index = linieCuratata.indexOf("=");
    if (index === -1) continue;
    const cheie = linieCuratata.slice(0, index).trim();
    const valoare = linieCuratata.slice(index + 1).trim();
    if (!(cheie in process.env)) process.env[cheie] = valoare;
  }
}

incarcaEnvLocal();

const firebaseConfig = {
  apiKey: process.env.NEXT_PUBLIC_FIREBASE_API_KEY,
  authDomain: process.env.NEXT_PUBLIC_FIREBASE_AUTH_DOMAIN,
  projectId: process.env.NEXT_PUBLIC_FIREBASE_PROJECT_ID,
  messagingSenderId: process.env.NEXT_PUBLIC_FIREBASE_MESSAGING_SENDER_ID,
  appId: process.env.NEXT_PUBLIC_FIREBASE_APP_ID,
};

if (!firebaseConfig.projectId) {
  console.error(
    "Lipsesc variabilele Firebase din .env.local. Rulează scriptul din rădăcina proiectului (unde e .env.local)."
  );
  process.exit(1);
}

const app = initializeApp(firebaseConfig);
const db = getFirestore(app);

function idNou(prefix) {
  return `${prefix}-${Date.now().toString(36)}${Math.random().toString(36).slice(2, 6)}`;
}

const MENIU_HEADER = ["Acasă", "Despre Noi", "Servicii", "Prețuri", "Contact"].map((eticheta) => ({
  id: idNou("link"),
  eticheta,
  link: "#",
  vizibil: true,
}));

const BLOCURI = (groupIdCarusel) => [
  {
    tip: "hero_imagine",
    ordine: 0,
    vizibil: true,
    continut: {
      eyebrow: "",
      titlu: "Pentru că printarea profesională nu ar trebui să fie complicată.",
      subtitlu:
        "Soluții de tipar rapide, de înaltă calitate și accesibile pentru afacerea ta. Încărcare simplă a fișierelor, finisaje premium.",
      imagine: "",
      textButonPrimar: "Explorează Serviciile",
      linkButonPrimar: "#produse",
      textButonSecundar: "Cere Ofertă",
      linkButonSecundar: "#contact",
      statistici: [
        { id: "stat1", valoare: "15.000+", eticheta: "Proiecte Finalizate" },
        { id: "stat2", valoare: "800+", eticheta: "Clienți Activi" },
        { id: "stat3", valoare: "24/7", eticheta: "Suport Expert" },
      ],
    },
  },
  {
    tip: "carduri_beneficii",
    ordine: 1,
    vizibil: true,
    continut: {
      titluSectiune: "3 Motive Pentru a Alege PosterART",
      carduri: [
        {
          id: "card1",
          titlu: "Calitate Garantată",
          text: "Fiecare comandă este verificată riguros pentru a asigura un finisaj perfect.",
          icon: "shield",
          link: "",
        },
        {
          id: "card2",
          titlu: "Livrare Rapidă",
          text: "Termene limită strânse? Oferim opțiuni de printare expres pentru a livra la timp.",
          icon: "clock",
          link: "",
        },
        {
          id: "card3",
          titlu: "Suport Personalizat",
          text: "Echipa noastră de experți vă ghidează de la pregătirea fișierului până la livrare.",
          icon: "handshake",
          link: "",
        },
      ],
    },
  },
  {
    tip: "pasi",
    ordine: 2,
    vizibil: true,
    continut: {
      titluSectiune: "Urmează Acești 3 Pași Simpli Pentru a Comanda!",
      imagine: "",
      pasi: [
        {
          id: "pas1",
          titlu: "Încarcă Fișierele Gata de Print",
          text: "Trimiți fișierele (PDF, AI, PSD) direct din platformă.",
        },
        {
          id: "pas2",
          titlu: "Aprobă Bunul de Tipar Digital",
          text: "Verifici previzualizarea digitală și confirmi înainte de producție.",
        },
        {
          id: "pas3",
          titlu: "Primește Comanda",
          text: "Ridici comanda sau o primești prin livrare, gata de utilizare.",
        },
      ],
      statistici: [
        { id: "stat1", valoare: "99,8%", eticheta: "Satisfacție Clienți" },
        { id: "stat2", valoare: "2.500+", eticheta: "Recenzii Pozitive" },
        { id: "stat3", valoare: "5★", eticheta: "Calificativ Mediu" },
      ],
    },
  },
  {
    tip: "carusel_produse",
    ordine: 3,
    vizibil: true,
    continut: {
      titluSectiune: "Serviciile Noastre de Top",
      group_id: groupIdCarusel,
    },
  },
];

async function main() {
  console.log("Șterg blocurile existente din homepage_blocks...");
  const snapshotVechi = await getDocs(collection(db, "homepage_blocks"));
  for (const docSnap of snapshotVechi.docs) {
    await deleteDoc(doc(db, "homepage_blocks", docSnap.id));
  }
  console.log(`Șterse ${snapshotVechi.size} blocuri vechi.`);

  console.log("Caut o grupă existentă pentru caruselul de produse...");
  const grupeSnap = await getDocs(collection(db, "groups"));
  let groupIdCarusel = "";
  if (!grupeSnap.empty) {
    const grupaAleasa = grupeSnap.docs[0];
    groupIdCarusel = grupaAleasa.id;
    console.log(`Folosesc grupa „${grupaAleasa.data().nume ?? ""}" pentru carusel.`);
  } else {
    console.warn(
      "Nu există nicio grupă în Firestore — blocul de carusel va fi creat fără grupă selectată. Alege una manual din Constructor Pagină după rulare."
    );
  }

  for (const bloc of BLOCURI(groupIdCarusel)) {
    await addDoc(collection(db, "homepage_blocks"), bloc);
    console.log(`Adăugat bloc: ${bloc.tip}`);
  }

  console.log("Actualizez meniul din antet (păstrez logo/locație/telefon/program existente)...");
  const headerRef = doc(db, "site_config", "header");
  const headerSnap = await getDoc(headerRef);
  const headerExistent = headerSnap.exists() ? headerSnap.data() : {};
  await setDoc(
    headerRef,
    {
      ...headerExistent,
      textButonLogin: "Înregistrare",
      linkuriMeniu: MENIU_HEADER,
    },
    { merge: true }
  );
  console.log("Meniu actualizat: Acasă, Despre Noi, Servicii, Prețuri, Contact.");

  console.log("\nGata! Deschide pagina principală și fă refresh.");
  console.log("Nu uita: încarcă pozele pentru Hero și Pași din Admin → Constructor Pagină (Editează).");
  process.exit(0);
}

main().catch((err) => {
  console.error("Eroare la seed:", err);
  process.exit(1);
});
