// Populează homepage_blocks cu un set curat de blocuri (Hero cu Imagine,
// Carduri Beneficii, Pași, Carusel Produse), ca pagina principală să arate
// ca layout-ul de referință, cu text adaptat pentru un print shop.
//
// Rulare: node scripts/seed-homepage.mjs
// (din rădăcina proiectului, cu .env.local completat)

import { readFileSync, existsSync } from "node:fs";
import { initializeApp } from "firebase/app";
import { getFirestore, collection, getDocs, deleteDoc, doc, addDoc } from "firebase/firestore";

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

const BLOCURI = (groupIdCarusel) => [
  {
    tip: "hero_imagine",
    ordine: 0,
    vizibil: true,
    continut: {
      eyebrow: "★★★★★ 4.9 (peste 500 recenzii)",
      titlu: "Printuri Profesionale, Fără Bătăi de Cap",
      subtitlu:
        "De la cărți de vizită la bannere de format mare — calitate premium, comandă online în câteva minute.",
      imagine: "",
      textButonPrimar: "Vezi Produsele",
      linkButonPrimar: "#produse",
      textButonSecundar: "Cum Funcționează",
      linkButonSecundar: "#pasi",
      statistici: [
        { id: "stat1", valoare: "15.000+", eticheta: "Comenzi Livrate" },
        { id: "stat2", valoare: "250+", eticheta: "Clienți Fideli" },
        { id: "stat3", valoare: "24h", eticheta: "Producție Rapidă" },
      ],
    },
  },
  {
    tip: "carduri_beneficii",
    ordine: 1,
    vizibil: true,
    continut: {
      titluSectiune: "De ce să ne alegi",
      carduri: [
        {
          id: "card1",
          titlu: "Suport 24/7",
          text: "Echipa noastră îți răspunde rapid la orice întrebare despre comandă sau fișiere.",
          icon: "headphones",
          link: "",
        },
        {
          id: "card2",
          titlu: "Fișiere Verificate",
          text: "Verificăm fiecare fișier înainte de print, ca rezultatul să fie exact cum ți-l dorești.",
          icon: "shield",
          link: "",
        },
        {
          id: "card3",
          titlu: "Producție Rapidă",
          text: "Majoritatea comenzilor sunt gata în 24-48 de ore, fără compromisuri de calitate.",
          icon: "clock",
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
      titluSectiune: "Cum Comanzi în 3 Pași Simpli",
      imagine: "",
      pasi: [
        { id: "pas1", titlu: "Alege Produsul", text: "Selectezi tipul de print și specificațiile din catalog." },
        {
          id: "pas2",
          titlu: "Încarci Fișierul",
          text: "Trimiți designul tău sau ceri ajutor de la echipa de grafică.",
        },
        {
          id: "pas3",
          titlu: "Primești Comanda",
          text: "Printăm, împachetăm și livrăm sau pregătim pentru ridicare.",
        },
      ],
      statistici: [
        { id: "stat1", valoare: "98%", eticheta: "Clienți Mulțumiți" },
        { id: "stat2", valoare: "15.000+", eticheta: "Comenzi" },
        { id: "stat3", valoare: "4.9/5", eticheta: "Rating Mediu" },
      ],
    },
  },
  {
    tip: "carusel_produse",
    ordine: 3,
    vizibil: true,
    continut: {
      titluSectiune: "Produsele Noastre Populare",
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

  console.log("\nGata! Deschide pagina principală și fă refresh.");
  console.log("Nu uita: încarcă pozele pentru Hero și Pași din Admin → Constructor Pagină (Editează).");
  process.exit(0);
}

main().catch((err) => {
  console.error("Eroare la seed:", err);
  process.exit(1);
});
