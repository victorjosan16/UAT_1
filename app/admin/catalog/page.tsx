"use client";

import { useEffect, useMemo, useRef, useState } from "react";
import {
  collection,
  onSnapshot,
  addDoc,
  updateDoc,
  deleteDoc,
  doc,
  increment,
  serverTimestamp,
} from "firebase/firestore";
import { ref, uploadBytes, getDownloadURL } from "firebase/storage";
import { db, storage } from "@/lib/firebase";
import {
  Plus,
  Minus,
  Trash2,
  Loader2,
  ImageOff,
  Eye,
  EyeOff,
  UploadCloud,
  CheckCircle2,
  X,
  Boxes,
  Tag,
} from "lucide-react";

// ---------------------------------------------------------------------------
// Tipuri
// ---------------------------------------------------------------------------

interface Categorie {
  id: string;
  nume: string;
  ordine: number;
}

interface Produs {
  id: string;
  nume: string;
  descriere: string;
  pret: number;
  pretRedus: number | null;
  sku: string;
  stoc: number;
  categorie_id: string;
  imagini: string[];
  vizibil: boolean;
}

type Tab = "inventar" | "adauga" | "categorii";

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

function formateazaPret(pret: number): string {
  return new Intl.NumberFormat("ro-RO", { minimumFractionDigits: 0 }).format(pret) + " lei";
}

function genereazaSKU(nume: string): string {
  const baza = nume
    .normalize("NFD")
    .replace(/[\u0300-\u036f]/g, "")
    .toUpperCase()
    .replace(/[^A-Z0-9]+/g, "-")
    .replace(/(^-+|-+$)/g, "")
    .slice(0, 14);
  const sufix = Math.floor(1000 + Math.random() * 9000);
  return `${baza || "PROD"}-${sufix}`;
}

// ---------------------------------------------------------------------------
// Pagina
// ---------------------------------------------------------------------------

export default function CatalogPage() {
  const [tabActiv, setTabActiv] = useState<Tab>("inventar");
  const [categorii, setCategorii] = useState<Categorie[]>([]);
  const [produse, setProduse] = useState<Produs[]>([]);
  const [seIncarca, setSeIncarca] = useState(true);

  useEffect(() => {
    const unsubCategorii = onSnapshot(collection(db, "categories"), (snapshot) => {
      const lista: Categorie[] = snapshot.docs
        .map((docSnap) => {
          const data = docSnap.data();
          return {
            id: docSnap.id,
            nume: data.nume ?? "Categorie",
            ordine: typeof data.ordine === "number" ? data.ordine : 0,
          };
        })
        .sort((a, b) => a.ordine - b.ordine);
      setCategorii(lista);
    });

    const unsubProduse = onSnapshot(collection(db, "products"), (snapshot) => {
      const lista: Produs[] = snapshot.docs.map((docSnap) => {
        const data = docSnap.data();
        return {
          id: docSnap.id,
          nume: data.nume ?? "Produs",
          descriere: data.descriere ?? "",
          pret: typeof data.pret === "number" ? data.pret : 0,
          pretRedus: typeof data.pretRedus === "number" ? data.pretRedus : null,
          sku: data.sku ?? "",
          stoc: typeof data.stoc === "number" ? data.stoc : 0,
          categorie_id: data.categorie_id ?? "",
          imagini: Array.isArray(data.imagini) ? data.imagini : [],
          vizibil: data.vizibil !== false,
        };
      });
      setProduse(lista);
      setSeIncarca(false);
    });

    return () => {
      unsubCategorii();
      unsubProduse();
    };
  }, []);

  const hartaCategorii = useMemo(() => {
    const harta = new Map<string, string>();
    categorii.forEach((c) => harta.set(c.id, c.nume));
    return harta;
  }, [categorii]);

  const TABURI: { id: Tab; label: string; icon: typeof Boxes }[] = [
    { id: "inventar", label: "Listă Inventar", icon: Boxes },
    { id: "adauga", label: "Adaugă Produs", icon: Plus },
    { id: "categorii", label: "Categorii", icon: Tag },
  ];

  return (
    <div className="animate-in fade-in">
      <div className="mb-6">
        <h1 className="text-2xl font-semibold text-gray-900">Catalog Produse</h1>
        <p className="text-sm text-gray-500 mt-1">Gestionează produsele, stocul și categoriile magazinului.</p>
      </div>

      <div className="flex gap-1 border-b border-gray-200 mb-6">
        {TABURI.map((tab) => {
          const Icon = tab.icon;
          const activ = tabActiv === tab.id;
          return (
            <button
              key={tab.id}
              onClick={() => setTabActiv(tab.id)}
              className={`flex items-center gap-2 px-4 py-2.5 text-sm font-medium border-b-2 -mb-px transition-colors ${
                activ
                  ? "border-brand-primary text-brand-primary"
                  : "border-transparent text-gray-500 hover:text-gray-800"
              }`}
            >
              <Icon className="w-4 h-4" />
              {tab.label}
            </button>
          );
        })}
      </div>

      {tabActiv === "inventar" && (
        <TabInventar produse={produse} hartaCategorii={hartaCategorii} seIncarca={seIncarca} />
      )}
      {tabActiv === "adauga" && (
        <TabAdaugaProdus categorii={categorii} onProdusAdaugat={() => setTabActiv("inventar")} />
      )}
      {tabActiv === "categorii" && <TabCategorii categorii={categorii} />}
    </div>
  );
}

// ---------------------------------------------------------------------------
// Tab: Listă Inventar
// ---------------------------------------------------------------------------

function TabInventar({
  produse,
  hartaCategorii,
  seIncarca,
}: {
  produse: Produs[];
  hartaCategorii: Map<string, string>;
  seIncarca: boolean;
}) {
  const [idInLucru, setIdInLucru] = useState<string | null>(null);

  async function modificaStoc(produs: Produs, delta: number) {
    if (produs.stoc + delta < 0) return;
    setIdInLucru(produs.id);
    try {
      await updateDoc(doc(db, "products", produs.id), { stoc: increment(delta) });
    } catch (err) {
      console.error("Eroare la actualizarea stocului:", err);
    } finally {
      setIdInLucru(null);
    }
  }

  async function comutaVizibilitate(produs: Produs) {
    setIdInLucru(produs.id);
    try {
      await updateDoc(doc(db, "products", produs.id), { vizibil: !produs.vizibil });
    } catch (err) {
      console.error("Eroare la actualizarea vizibilității:", err);
    } finally {
      setIdInLucru(null);
    }
  }

  async function stergeProdus(produs: Produs) {
    if (!confirm(`Ștergi produsul "${produs.nume}"? Această acțiune nu poate fi anulată.`)) return;
    setIdInLucru(produs.id);
    try {
      await deleteDoc(doc(db, "products", produs.id));
    } catch (err) {
      console.error("Eroare la ștergerea produsului:", err);
    } finally {
      setIdInLucru(null);
    }
  }

  if (seIncarca) {
    return (
      <div className="flex items-center justify-center h-64 text-gray-400">
        <Loader2 className="w-6 h-6 animate-spin mr-2" />
        Se încarcă produsele...
      </div>
    );
  }

  if (produse.length === 0) {
    return (
      <div className="bg-white rounded-2xl border border-gray-100 p-12 text-center text-gray-400">
        Nu ai încă niciun produs. Adaugă primul din tab-ul „Adaugă Produs”.
      </div>
    );
  }

  return (
    <div className="bg-white rounded-2xl border border-gray-100 shadow-sm overflow-hidden">
      <div className="overflow-x-auto">
        <table className="w-full text-sm">
          <thead>
            <tr className="border-b border-gray-100 text-left text-gray-500">
              <th className="px-4 py-3 font-medium">Produs</th>
              <th className="px-4 py-3 font-medium">SKU</th>
              <th className="px-4 py-3 font-medium">Categorie</th>
              <th className="px-4 py-3 font-medium">Preț</th>
              <th className="px-4 py-3 font-medium">Stoc</th>
              <th className="px-4 py-3 font-medium">Vizibil</th>
              <th className="px-4 py-3 font-medium text-right">Acțiuni</th>
            </tr>
          </thead>
          <tbody>
            {produse.map((produs) => {
              const areReducere = produs.pretRedus !== null && produs.pretRedus < produs.pret;
              const inLucru = idInLucru === produs.id;
              return (
                <tr key={produs.id} className="border-b border-gray-50 last:border-0 hover:bg-gray-50/60">
                  <td className="px-4 py-3">
                    <div className="flex items-center gap-3">
                      <div className="w-10 h-10 rounded-lg bg-gray-100 flex-shrink-0 overflow-hidden flex items-center justify-center">
                        {produs.imagini[0] ? (
                          // eslint-disable-next-line @next/next/no-img-element
                          <img src={produs.imagini[0]} alt={produs.nume} className="w-full h-full object-cover" />
                        ) : (
                          <ImageOff className="w-4 h-4 text-gray-300" />
                        )}
                      </div>
                      <span className="font-medium text-gray-900 max-w-[220px] truncate">{produs.nume}</span>
                    </div>
                  </td>
                  <td className="px-4 py-3 text-gray-500 font-mono text-xs">{produs.sku}</td>
                  <td className="px-4 py-3 text-gray-600">
                    {hartaCategorii.get(produs.categorie_id) ?? "—"}
                  </td>
                  <td className="px-4 py-3">
                    {areReducere ? (
                      <div className="flex items-center gap-1.5">
                        <span className="font-medium text-brand-primary">{formateazaPret(produs.pretRedus!)}</span>
                        <span className="text-xs text-gray-400 line-through">{formateazaPret(produs.pret)}</span>
                      </div>
                    ) : (
                      <span className="font-medium text-gray-900">{formateazaPret(produs.pret)}</span>
                    )}
                  </td>
                  <td className="px-4 py-3">
                    <div className="flex items-center gap-1.5">
                      <button
                        onClick={() => modificaStoc(produs, -1)}
                        disabled={inLucru || produs.stoc <= 0}
                        className="w-6 h-6 rounded-lg border border-gray-200 flex items-center justify-center hover:bg-gray-100 disabled:opacity-40"
                      >
                        <Minus className="w-3 h-3" />
                      </button>
                      <span
                        className={`w-6 text-center font-medium ${
                          produs.stoc === 0 ? "text-red-600" : "text-gray-900"
                        }`}
                      >
                        {produs.stoc}
                      </span>
                      <button
                        onClick={() => modificaStoc(produs, 1)}
                        disabled={inLucru}
                        className="w-6 h-6 rounded-lg border border-gray-200 flex items-center justify-center hover:bg-gray-100 disabled:opacity-40"
                      >
                        <Plus className="w-3 h-3" />
                      </button>
                    </div>
                  </td>
                  <td className="px-4 py-3">
                    <button
                      onClick={() => comutaVizibilitate(produs)}
                      disabled={inLucru}
                      className={`flex items-center gap-1.5 text-xs font-medium px-2.5 py-1.5 rounded-full ${
                        produs.vizibil
                          ? "bg-emerald-50 text-emerald-700"
                          : "bg-gray-100 text-gray-500"
                      }`}
                    >
                      {produs.vizibil ? <Eye className="w-3.5 h-3.5" /> : <EyeOff className="w-3.5 h-3.5" />}
                      {produs.vizibil ? "Vizibil" : "Ascuns"}
                    </button>
                  </td>
                  <td className="px-4 py-3 text-right">
                    <button
                      onClick={() => stergeProdus(produs)}
                      disabled={inLucru}
                      className="text-gray-300 hover:text-red-500 transition-colors disabled:opacity-40"
                      aria-label="Șterge produsul"
                    >
                      {inLucru ? <Loader2 className="w-4 h-4 animate-spin" /> : <Trash2 className="w-4 h-4" />}
                    </button>
                  </td>
                </tr>
              );
            })}
          </tbody>
        </table>
      </div>
    </div>
  );
}

// ---------------------------------------------------------------------------
// Tab: Adaugă Produs
// ---------------------------------------------------------------------------

function TabAdaugaProdus({
  categorii,
  onProdusAdaugat,
}: {
  categorii: Categorie[];
  onProdusAdaugat: () => void;
}) {
  const [nume, setNume] = useState("");
  const [descriere, setDescriere] = useState("");
  const [pret, setPret] = useState("");
  const [pretRedus, setPretRedus] = useState("");
  const [sku, setSku] = useState("");
  const [skuModificatManual, setSkuModificatManual] = useState(false);
  const [categorieId, setCategorieId] = useState("");
  const [stocInitial, setStocInitial] = useState("0");
  const [fisiere, setFisiere] = useState<File[]>([]);
  const [previewuri, setPreviewuri] = useState<string[]>([]);
  const [seSalveaza, setSeSalveaza] = useState(false);
  const [eroare, setEroare] = useState<string | null>(null);
  const [succes, setSucces] = useState(false);
  const inputFisiereRef = useRef<HTMLInputElement>(null);

  function actualizeazaNume(valoare: string) {
    setNume(valoare);
    if (!skuModificatManual) setSku(genereazaSKU(valoare));
  }

  function actualizeazaFisiere(lista: FileList | null) {
    if (!lista) return;
    const fisiereNoi = Array.from(lista);
    setFisiere(fisiereNoi);
    previewuri.forEach((url) => URL.revokeObjectURL(url));
    setPreviewuri(fisiereNoi.map((f) => URL.createObjectURL(f)));
  }

  function resetForm() {
    setNume("");
    setDescriere("");
    setPret("");
    setPretRedus("");
    setSku("");
    setSkuModificatManual(false);
    setCategorieId("");
    setStocInitial("0");
    setFisiere([]);
    previewuri.forEach((url) => URL.revokeObjectURL(url));
    setPreviewuri([]);
    if (inputFisiereRef.current) inputFisiereRef.current.value = "";
  }

  async function salveazaProdus(e: React.FormEvent) {
    e.preventDefault();
    setEroare(null);
    setSucces(false);

    const pretNumeric = parseFloat(pret);
    const pretRedusNumeric = pretRedus.trim() === "" ? null : parseFloat(pretRedus);

    if (!nume.trim()) return setEroare("Numele produsului este obligatoriu.");
    if (!categorieId) return setEroare("Selectează o categorie.");
    if (Number.isNaN(pretNumeric) || pretNumeric <= 0) return setEroare("Prețul trebuie să fie un număr pozitiv.");
    if (pretRedusNumeric !== null && (Number.isNaN(pretRedusNumeric) || pretRedusNumeric >= pretNumeric)) {
      return setEroare("Prețul redus trebuie să fie mai mic decât prețul normal.");
    }

    setSeSalveaza(true);
    try {
      const urlImagini: string[] = [];
      for (const fisier of fisiere) {
        const cale = `products/${Date.now()}-${fisier.name}`;
        const storageRef = ref(storage, cale);
        await uploadBytes(storageRef, fisier);
        const url = await getDownloadURL(storageRef);
        urlImagini.push(url);
      }

      await addDoc(collection(db, "products"), {
        nume: nume.trim(),
        descriere: descriere.trim(),
        pret: pretNumeric,
        pretRedus: pretRedusNumeric,
        sku: sku || genereazaSKU(nume),
        stoc: parseInt(stocInitial, 10) || 0,
        categorie_id: categorieId,
        imagini: urlImagini,
        vizibil: true,
        data_adaugare: serverTimestamp(),
      });

      setSucces(true);
      resetForm();
      setTimeout(() => onProdusAdaugat(), 900);
    } catch (err) {
      console.error("Eroare la salvarea produsului:", err);
      setEroare("A apărut o eroare la salvare. Încearcă din nou.");
    } finally {
      setSeSalveaza(false);
    }
  }

  return (
    <form
      onSubmit={salveazaProdus}
      className="bg-white rounded-2xl border border-gray-100 shadow-sm p-6 max-w-2xl space-y-5"
    >
      {categorii.length === 0 && (
        <div className="bg-amber-50 text-amber-700 text-xs rounded-xl px-3 py-2.5">
          Nu ai nicio categorie încă. Creează una din tab-ul „Categorii” înainte de a adăuga produse.
        </div>
      )}

      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1.5">Nume produs</label>
        <input
          type="text"
          value={nume}
          onChange={(e) => actualizeazaNume(e.target.value)}
          placeholder="ex: Poster Vintage A3"
          className="w-full rounded-xl border border-gray-200 px-3 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-brand-primary/30 focus:border-brand-primary"
        />
      </div>

      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1.5">Descriere</label>
        <textarea
          value={descriere}
          onChange={(e) => setDescriere(e.target.value)}
          rows={3}
          placeholder="Detalii despre produs, material, dimensiuni..."
          className="w-full rounded-xl border border-gray-200 px-3 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-brand-primary/30 focus:border-brand-primary resize-none"
        />
      </div>

      <div className="grid grid-cols-2 gap-4">
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1.5">Preț (lei)</label>
          <input
            type="number"
            step="0.01"
            min="0"
            value={pret}
            onChange={(e) => setPret(e.target.value)}
            placeholder="99"
            className="w-full rounded-xl border border-gray-200 px-3 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-brand-primary/30 focus:border-brand-primary"
          />
        </div>
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1.5">Preț redus (opțional)</label>
          <input
            type="number"
            step="0.01"
            min="0"
            value={pretRedus}
            onChange={(e) => setPretRedus(e.target.value)}
            placeholder="79"
            className="w-full rounded-xl border border-gray-200 px-3 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-brand-primary/30 focus:border-brand-primary"
          />
        </div>
      </div>

      <div className="grid grid-cols-2 gap-4">
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1.5">SKU</label>
          <input
            type="text"
            value={sku}
            onChange={(e) => {
              setSku(e.target.value);
              setSkuModificatManual(true);
            }}
            placeholder="Generat automat"
            className="w-full rounded-xl border border-gray-200 px-3 py-2.5 text-sm font-mono focus:outline-none focus:ring-2 focus:ring-brand-primary/30 focus:border-brand-primary"
          />
        </div>
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1.5">Stoc inițial</label>
          <input
            type="number"
            min="0"
            value={stocInitial}
            onChange={(e) => setStocInitial(e.target.value)}
            className="w-full rounded-xl border border-gray-200 px-3 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-brand-primary/30 focus:border-brand-primary"
          />
        </div>
      </div>

      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1.5">Categorie</label>
        <select
          value={categorieId}
          onChange={(e) => setCategorieId(e.target.value)}
          className="w-full rounded-xl border border-gray-200 px-3 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-brand-primary/30 focus:border-brand-primary bg-white"
        >
          <option value="">Selectează o categorie</option>
          {categorii.map((c) => (
            <option key={c.id} value={c.id}>
              {c.nume}
            </option>
          ))}
        </select>
      </div>

      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1.5">Poze produs</label>
        <button
          type="button"
          onClick={() => inputFisiereRef.current?.click()}
          className="w-full flex flex-col items-center justify-center gap-2 border-2 border-dashed border-gray-200 rounded-2xl py-8 text-gray-400 hover:border-brand-primary hover:text-brand-primary transition-colors"
        >
          <UploadCloud className="w-6 h-6" />
          <span className="text-xs font-medium">Click pentru a încărca una sau mai multe poze</span>
        </button>
        <input
          ref={inputFisiereRef}
          type="file"
          accept="image/*"
          multiple
          onChange={(e) => actualizeazaFisiere(e.target.files)}
          className="hidden"
        />
        {previewuri.length > 0 && (
          <div className="flex gap-2 mt-3 flex-wrap">
            {previewuri.map((url, i) => (
              // eslint-disable-next-line @next/next/no-img-element
              <img key={i} src={url} alt="" className="w-16 h-16 rounded-xl object-cover border border-gray-100" />
            ))}
          </div>
        )}
      </div>

      {eroare && (
        <div className="flex items-center gap-2 text-red-600 text-xs bg-red-50 rounded-xl px-3 py-2.5">
          <X className="w-3.5 h-3.5 flex-shrink-0" />
          {eroare}
        </div>
      )}
      {succes && (
        <div className="flex items-center gap-2 text-emerald-700 text-xs bg-emerald-50 rounded-xl px-3 py-2.5">
          <CheckCircle2 className="w-3.5 h-3.5 flex-shrink-0" />
          Produs salvat cu succes!
        </div>
      )}

      <button
        type="submit"
        disabled={seSalveaza}
        className="flex items-center justify-center gap-2 bg-brand-primary text-white font-medium px-6 py-2.5 rounded-xl hover:bg-brand-primary-dark transition-colors disabled:opacity-60"
      >
        {seSalveaza ? <Loader2 className="w-4 h-4 animate-spin" /> : "Salvează Produs"}
      </button>
    </form>
  );
}

// ---------------------------------------------------------------------------
// Tab: Categorii
// ---------------------------------------------------------------------------

function TabCategorii({ categorii }: { categorii: Categorie[] }) {
  const [numeCategorie, setNumeCategorie] = useState("");
  const [ordine, setOrdine] = useState("0");
  const [seSalveaza, setSeSalveaza] = useState(false);
  const [idInLucru, setIdInLucru] = useState<string | null>(null);
  const [eroare, setEroare] = useState<string | null>(null);

  async function adaugaCategorie(e: React.FormEvent) {
    e.preventDefault();
    setEroare(null);
    if (!numeCategorie.trim()) return setEroare("Numele categoriei este obligatoriu.");

    setSeSalveaza(true);
    try {
      await addDoc(collection(db, "categories"), {
        nume: numeCategorie.trim(),
        ordine: parseInt(ordine, 10) || 0,
        data_creare: serverTimestamp(),
      });
      setNumeCategorie("");
      setOrdine("0");
    } catch (err) {
      console.error("Eroare la adăugarea categoriei:", err);
      setEroare("A apărut o eroare la salvare.");
    } finally {
      setSeSalveaza(false);
    }
  }

  async function stergeCategorie(categorie: Categorie) {
    if (!confirm(`Ștergi categoria "${categorie.nume}"?`)) return;
    setIdInLucru(categorie.id);
    try {
      await deleteDoc(doc(db, "categories", categorie.id));
    } catch (err) {
      console.error("Eroare la ștergerea categoriei:", err);
    } finally {
      setIdInLucru(null);
    }
  }

  return (
    <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 max-w-4xl">
      <form
        onSubmit={adaugaCategorie}
        className="bg-white rounded-2xl border border-gray-100 shadow-sm p-6 space-y-4 h-fit"
      >
        <h3 className="font-medium text-gray-900">Adaugă categorie</h3>
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1.5">Nume categorie</label>
          <input
            type="text"
            value={numeCategorie}
            onChange={(e) => setNumeCategorie(e.target.value)}
            placeholder="ex: Postere"
            className="w-full rounded-xl border border-gray-200 px-3 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-brand-primary/30 focus:border-brand-primary"
          />
        </div>
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1.5">Ordine afișare</label>
          <input
            type="number"
            value={ordine}
            onChange={(e) => setOrdine(e.target.value)}
            className="w-full rounded-xl border border-gray-200 px-3 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-brand-primary/30 focus:border-brand-primary"
          />
        </div>
        {eroare && <p className="text-xs text-red-600">{eroare}</p>}
        <button
          type="submit"
          disabled={seSalveaza}
          className="flex items-center justify-center gap-2 bg-brand-primary text-white font-medium px-5 py-2.5 rounded-xl hover:bg-brand-primary-dark transition-colors disabled:opacity-60 text-sm"
        >
          {seSalveaza ? <Loader2 className="w-4 h-4 animate-spin" /> : "Adaugă Categorie"}
        </button>
      </form>

      <div className="bg-white rounded-2xl border border-gray-100 shadow-sm p-6">
        <h3 className="font-medium text-gray-900 mb-4">Categorii existente</h3>
        {categorii.length === 0 ? (
          <p className="text-sm text-gray-400">Nicio categorie creată încă.</p>
        ) : (
          <ul className="space-y-2">
            {categorii.map((categorie) => (
              <li
                key={categorie.id}
                className="flex items-center justify-between bg-gray-50 rounded-xl px-3.5 py-2.5"
              >
                <span className="text-sm text-gray-800">{categorie.nume}</span>
                <div className="flex items-center gap-3">
                  <span className="text-xs text-gray-400">ordine: {categorie.ordine}</span>
                  <button
                    onClick={() => stergeCategorie(categorie)}
                    disabled={idInLucru === categorie.id}
                    className="text-gray-300 hover:text-red-500 transition-colors"
                    aria-label="Șterge categoria"
                  >
                    {idInLucru === categorie.id ? (
                      <Loader2 className="w-4 h-4 animate-spin" />
                    ) : (
                      <Trash2 className="w-3.5 h-3.5" />
                    )}
                  </button>
                </div>
              </li>
            ))}
          </ul>
        )}
      </div>
    </div>
  );
}
