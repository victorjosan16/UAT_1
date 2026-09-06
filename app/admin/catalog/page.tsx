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
import { db } from "@/lib/firebase";
import { incarcaImagineCloudinary } from "@/lib/cloudinary";
import { ConfirmDialog, type ConfirmDialogState } from "@/components/ConfirmDialog";
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
  Layers,
  Pencil,
} from "lucide-react";

// ---------------------------------------------------------------------------
// Tipuri
// ---------------------------------------------------------------------------

interface Grupa {
  id: string;
  nume: string;
  descriere: string;
}

interface Produs {
  id: string;
  nume: string;
  descriere: string;
  pret: number;
  pretRedus: number | null;
  sku: string;
  stoc: number;
  group_id: string;
  imagini: string[];
  vizibil: boolean;
}

type Tab = "inventar" | "produs" | "grupe";

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
  const [grupe, setGrupe] = useState<Grupa[]>([]);
  const [produse, setProduse] = useState<Produs[]>([]);
  const [seIncarca, setSeIncarca] = useState(true);
  const [produsInEditare, setProdusInEditare] = useState<Produs | null>(null);

  useEffect(() => {
    const unsubGrupe = onSnapshot(collection(db, "groups"), (snapshot) => {
      const lista: Grupa[] = snapshot.docs
        .map((docSnap) => {
          const data = docSnap.data();
          return {
            id: docSnap.id,
            nume: data.nume ?? "Grupă",
            descriere: data.descriere ?? "",
          };
        })
        .sort((a, b) => a.nume.localeCompare(b.nume, "ro"));
      setGrupe(lista);
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
          group_id: data.group_id ?? "",
          imagini: Array.isArray(data.imagini) ? data.imagini : [],
          vizibil: data.vizibil !== false,
        };
      });
      setProduse(lista);
      setSeIncarca(false);
    });

    return () => {
      unsubGrupe();
      unsubProduse();
    };
  }, []);

  const hartaGrupe = useMemo(() => {
    const harta = new Map<string, string>();
    grupe.forEach((g) => harta.set(g.id, g.nume));
    return harta;
  }, [grupe]);

  function editeazaProdus(produs: Produs) {
    setProdusInEditare(produs);
    setTabActiv("produs");
  }

  function produsNou() {
    setProdusInEditare(null);
    setTabActiv("produs");
  }

  const TABURI: { id: Tab; label: string; icon: typeof Boxes }[] = [
    { id: "inventar", label: "Inventar Produse", icon: Boxes },
    { id: "produs", label: produsInEditare ? "Editează Produs" : "Adaugă Produs", icon: produsInEditare ? Pencil : Plus },
    { id: "grupe", label: "Gestiune Grupe", icon: Layers },
  ];

  return (
    <div className="animate-in fade-in">
      <div className="mb-6">
        <h1 className="text-2xl font-semibold text-gray-900">Catalog Produse</h1>
        <p className="text-sm text-gray-500 mt-1">Gestionează produsele, stocul și grupele magazinului.</p>
      </div>

      <div className="flex gap-1 border-b border-gray-200 mb-6">
        {TABURI.map((tab) => {
          const Icon = tab.icon;
          const activ = tabActiv === tab.id;
          return (
            <button
              key={tab.id}
              onClick={() => {
                if (tab.id === "produs" && !produsInEditare) produsNou();
                else setTabActiv(tab.id);
              }}
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
        <TabInventar
          produse={produse}
          hartaGrupe={hartaGrupe}
          seIncarca={seIncarca}
          onEditeaza={editeazaProdus}
        />
      )}
      {tabActiv === "produs" && (
        <TabProdusForm
          key={produsInEditare?.id ?? "produs-nou"}
          grupe={grupe}
          produsInEditare={produsInEditare}
          onFinalizat={() => {
            setProdusInEditare(null);
            setTabActiv("inventar");
          }}
        />
      )}
      {tabActiv === "grupe" && <TabGrupe grupe={grupe} />}
    </div>
  );
}

// ---------------------------------------------------------------------------
// Tab: Inventar Produse
// ---------------------------------------------------------------------------

function TabInventar({
  produse,
  hartaGrupe,
  seIncarca,
  onEditeaza,
}: {
  produse: Produs[];
  hartaGrupe: Map<string, string>;
  seIncarca: boolean;
  onEditeaza: (produs: Produs) => void;
}) {
  const [idInLucru, setIdInLucru] = useState<string | null>(null);
  const [confirmare, setConfirmare] = useState<ConfirmDialogState | null>(null);

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

  function stergeProdus(produs: Produs) {
    setConfirmare({
      titlu: "Șterge produsul",
      mesaj: `Ștergi produsul "${produs.nume}"? Această acțiune nu poate fi anulată.`,
      onConfirm: async () => {
        setIdInLucru(produs.id);
        try {
          await deleteDoc(doc(db, "products", produs.id));
        } catch (err) {
          console.error("Eroare la ștergerea produsului:", err);
        } finally {
          setIdInLucru(null);
        }
      },
    });
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

  // Grupare vizuală: produsele fără grupă apar sub eticheta „Negrupat”
  const grupuri = new Map<string, Produs[]>();
  for (const produs of produse) {
    const cheie = produs.group_id || "__negrupat__";
    if (!grupuri.has(cheie)) grupuri.set(cheie, []);
    grupuri.get(cheie)!.push(produs);
  }

  return (
    <div className="space-y-6">
      {Array.from(grupuri.entries()).map(([grupId, produseGrup]) => (
        <div key={grupId} className="bg-white rounded-2xl border border-gray-100 shadow-sm overflow-hidden">
          <div className="px-4 py-3 border-b border-gray-100 bg-gray-50/60 flex items-center justify-between">
            <span className="text-sm font-medium text-gray-700">
              {grupId === "__negrupat__" ? "Negrupat" : hartaGrupe.get(grupId) ?? "Grupă ștearsă"}
            </span>
            <span className="text-xs text-gray-400">{produseGrup.length} produse</span>
          </div>
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-gray-100 text-left text-gray-500">
                  <th className="px-4 py-3 font-medium">Produs</th>
                  <th className="px-4 py-3 font-medium">SKU</th>
                  <th className="px-4 py-3 font-medium">Preț</th>
                  <th className="px-4 py-3 font-medium">Stoc</th>
                  <th className="px-4 py-3 font-medium">Vizibil</th>
                  <th className="px-4 py-3 font-medium text-right">Acțiuni</th>
                </tr>
              </thead>
              <tbody>
                {produseGrup.map((produs) => {
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
                            produs.vizibil ? "bg-emerald-50 text-emerald-700" : "bg-gray-100 text-gray-500"
                          }`}
                        >
                          {produs.vizibil ? <Eye className="w-3.5 h-3.5" /> : <EyeOff className="w-3.5 h-3.5" />}
                          {produs.vizibil ? "Vizibil" : "Ascuns"}
                        </button>
                      </td>
                      <td className="px-4 py-3">
                        <div className="flex items-center justify-end gap-3">
                          <button
                            onClick={() => onEditeaza(produs)}
                            disabled={inLucru}
                            className="text-gray-400 hover:text-brand-primary transition-colors disabled:opacity-40"
                            aria-label="Editează produsul"
                          >
                            <Pencil className="w-4 h-4" />
                          </button>
                          <button
                            onClick={() => stergeProdus(produs)}
                            disabled={inLucru}
                            className="text-gray-300 hover:text-red-500 transition-colors disabled:opacity-40"
                            aria-label="Șterge produsul"
                          >
                            {inLucru ? <Loader2 className="w-4 h-4 animate-spin" /> : <Trash2 className="w-4 h-4" />}
                          </button>
                        </div>
                      </td>
                    </tr>
                  );
                })}
              </tbody>
            </table>
          </div>
        </div>
      ))}
      <ConfirmDialog stare={confirmare} onClose={() => setConfirmare(null)} />
    </div>
  );
}

// ---------------------------------------------------------------------------
// Tab: Adaugă / Editează Produs
// ---------------------------------------------------------------------------

function TabProdusForm({
  grupe,
  produsInEditare,
  onFinalizat,
}: {
  grupe: Grupa[];
  produsInEditare: Produs | null;
  onFinalizat: () => void;
}) {
  const esteEditare = produsInEditare !== null;

  const [nume, setNume] = useState(produsInEditare?.nume ?? "");
  const [descriere, setDescriere] = useState(produsInEditare?.descriere ?? "");
  const [pret, setPret] = useState(produsInEditare ? String(produsInEditare.pret) : "");
  const [pretRedus, setPretRedus] = useState(
    produsInEditare?.pretRedus != null ? String(produsInEditare.pretRedus) : ""
  );
  const [sku, setSku] = useState(produsInEditare?.sku ?? "");
  const [skuModificatManual, setSkuModificatManual] = useState(esteEditare);
  const [groupId, setGroupId] = useState(produsInEditare?.group_id ?? "");
  const [stocInitial, setStocInitial] = useState(produsInEditare ? String(produsInEditare.stoc) : "0");
  const [vizibil, setVizibil] = useState(produsInEditare?.vizibil ?? true);
  const [imaginiExistente, setImaginiExistente] = useState<string[]>(produsInEditare?.imagini ?? []);
  const [fisiereNoi, setFisiereNoi] = useState<File[]>([]);
  const [previewuriNoi, setPreviewuriNoi] = useState<string[]>([]);
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
    const fisiereNoiSelectate = Array.from(lista);
    setFisiereNoi(fisiereNoiSelectate);
    previewuriNoi.forEach((url) => URL.revokeObjectURL(url));
    setPreviewuriNoi(fisiereNoiSelectate.map((f) => URL.createObjectURL(f)));
  }

  function eliminaImagineExistenta(url: string) {
    setImaginiExistente((prev) => prev.filter((u) => u !== url));
  }

  async function salveazaProdus(e: React.FormEvent) {
    e.preventDefault();
    setEroare(null);
    setSucces(false);

    const pretNumeric = parseFloat(pret);
    const pretRedusNumeric = pretRedus.trim() === "" ? null : parseFloat(pretRedus);

    if (!nume.trim()) return setEroare("Numele produsului este obligatoriu.");
    if (!groupId) return setEroare("Selectează o grupă.");
    if (Number.isNaN(pretNumeric) || pretNumeric <= 0) return setEroare("Prețul trebuie să fie un număr pozitiv.");
    if (pretRedusNumeric !== null && (Number.isNaN(pretRedusNumeric) || pretRedusNumeric >= pretNumeric)) {
      return setEroare("Prețul redus trebuie să fie mai mic decât prețul normal.");
    }

    setSeSalveaza(true);
    try {
      const urlNoi: string[] = [];
      for (const fisier of fisiereNoi) {
        urlNoi.push(await incarcaImagineCloudinary(fisier));
      }

      const campuri = {
        nume: nume.trim(),
        descriere: descriere.trim(),
        pret: pretNumeric,
        pretRedus: pretRedusNumeric,
        sku: sku || genereazaSKU(nume),
        stoc: parseInt(stocInitial, 10) || 0,
        group_id: groupId,
        imagini: [...imaginiExistente, ...urlNoi],
        vizibil,
      };

      if (esteEditare && produsInEditare) {
        await updateDoc(doc(db, "products", produsInEditare.id), campuri);
      } else {
        await addDoc(collection(db, "products"), {
          ...campuri,
          data_adaugare: serverTimestamp(),
        });
      }

      setSucces(true);
      setTimeout(() => onFinalizat(), 800);
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
      <div className="flex items-center justify-between">
        <h3 className="font-medium text-gray-900">
          {esteEditare ? `Editează: ${produsInEditare!.nume}` : "Produs nou"}
        </h3>
        {esteEditare && (
          <button
            type="button"
            onClick={onFinalizat}
            className="text-xs text-gray-500 hover:text-gray-800 flex items-center gap-1"
          >
            <X className="w-3.5 h-3.5" />
            Anulează editarea
          </button>
        )}
      </div>

      {grupe.length === 0 && (
        <div className="bg-amber-50 text-amber-700 text-xs rounded-xl px-3 py-2.5">
          Nu ai nicio grupă încă. Creează una din tab-ul „Gestiune Grupe” înainte de a adăuga produse.
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
          <label className="block text-sm font-medium text-gray-700 mb-1.5">
            {esteEditare ? "Stoc" : "Stoc inițial"}
          </label>
          <input
            type="number"
            min="0"
            value={stocInitial}
            onChange={(e) => setStocInitial(e.target.value)}
            className="w-full rounded-xl border border-gray-200 px-3 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-brand-primary/30 focus:border-brand-primary"
          />
        </div>
      </div>

      <div className="grid grid-cols-2 gap-4 items-end">
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1.5">Grupă</label>
          <select
            value={groupId}
            onChange={(e) => setGroupId(e.target.value)}
            className="w-full rounded-xl border border-gray-200 px-3 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-brand-primary/30 focus:border-brand-primary bg-white"
          >
            <option value="">Selectează o grupă</option>
            {grupe.map((g) => (
              <option key={g.id} value={g.id}>
                {g.nume}
              </option>
            ))}
          </select>
        </div>
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1.5">Status</label>
          <button
            type="button"
            onClick={() => setVizibil((v) => !v)}
            className={`w-full flex items-center justify-center gap-2 rounded-xl px-3 py-2.5 text-sm font-medium transition-colors ${
              vizibil ? "bg-emerald-50 text-emerald-700" : "bg-gray-100 text-gray-500"
            }`}
          >
            {vizibil ? <Eye className="w-4 h-4" /> : <EyeOff className="w-4 h-4" />}
            {vizibil ? "Vizibil pe site" : "Ascuns"}
          </button>
        </div>
      </div>

      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1.5">Poze produs</label>

        {imaginiExistente.length > 0 && (
          <div className="flex gap-2 mb-3 flex-wrap">
            {imaginiExistente.map((url) => (
              <div key={url} className="relative">
                {/* eslint-disable-next-line @next/next/no-img-element */}
                <img src={url} alt="" className="w-16 h-16 rounded-xl object-cover border border-gray-100" />
                <button
                  type="button"
                  onClick={() => eliminaImagineExistenta(url)}
                  className="absolute -top-1.5 -right-1.5 w-5 h-5 rounded-full bg-white shadow-sm border border-gray-200 flex items-center justify-center text-gray-500 hover:text-red-500"
                  aria-label="Elimină poza"
                >
                  <X className="w-3 h-3" />
                </button>
              </div>
            ))}
          </div>
        )}

        <button
          type="button"
          onClick={() => inputFisiereRef.current?.click()}
          className="w-full flex flex-col items-center justify-center gap-2 border-2 border-dashed border-gray-200 rounded-2xl py-8 text-gray-400 hover:border-brand-primary hover:text-brand-primary transition-colors"
        >
          <UploadCloud className="w-6 h-6" />
          <span className="text-xs font-medium">Click pentru a încărca poze noi</span>
        </button>
        <input
          ref={inputFisiereRef}
          type="file"
          accept="image/*"
          multiple
          onChange={(e) => actualizeazaFisiere(e.target.files)}
          className="hidden"
        />
        {previewuriNoi.length > 0 && (
          <div className="flex gap-2 mt-3 flex-wrap">
            {previewuriNoi.map((url, i) => (
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
          {esteEditare ? "Modificări salvate cu succes!" : "Produs salvat cu succes!"}
        </div>
      )}

      <button
        type="submit"
        disabled={seSalveaza}
        className="flex items-center justify-center gap-2 bg-brand-primary text-white font-medium px-6 py-2.5 rounded-xl hover:bg-brand-primary-dark transition-colors disabled:opacity-60"
      >
        {seSalveaza ? <Loader2 className="w-4 h-4 animate-spin" /> : esteEditare ? "Salvează Modificările" : "Salvează Produs"}
      </button>
    </form>
  );
}

// ---------------------------------------------------------------------------
// Tab: Gestiune Grupe
// ---------------------------------------------------------------------------

function TabGrupe({ grupe }: { grupe: Grupa[] }) {
  const [numeNou, setNumeNou] = useState("");
  const [descriereNoua, setDescriereNoua] = useState("");
  const [seSalveaza, setSeSalveaza] = useState(false);
  const [eroare, setEroare] = useState<string | null>(null);

  const [idInEditare, setIdInEditare] = useState<string | null>(null);
  const [numeEditat, setNumeEditat] = useState("");
  const [descriereEditata, setDescriereEditata] = useState("");
  const [idInLucru, setIdInLucru] = useState<string | null>(null);
  const [confirmare, setConfirmare] = useState<ConfirmDialogState | null>(null);

  async function adaugaGrupa(e: React.FormEvent) {
    e.preventDefault();
    setEroare(null);
    if (!numeNou.trim()) return setEroare("Numele grupei este obligatoriu.");

    setSeSalveaza(true);
    try {
      await addDoc(collection(db, "groups"), {
        nume: numeNou.trim(),
        descriere: descriereNoua.trim(),
        data_creare: serverTimestamp(),
      });
      setNumeNou("");
      setDescriereNoua("");
    } catch (err) {
      console.error("Eroare la adăugarea grupei:", err);
      setEroare("A apărut o eroare la salvare.");
    } finally {
      setSeSalveaza(false);
    }
  }

  function incepeEditarea(grupa: Grupa) {
    setIdInEditare(grupa.id);
    setNumeEditat(grupa.nume);
    setDescriereEditata(grupa.descriere);
  }

  function anuleazaEditarea() {
    setIdInEditare(null);
  }

  async function salveazaEditarea(grupa: Grupa) {
    if (!numeEditat.trim()) return;
    setIdInLucru(grupa.id);
    try {
      await updateDoc(doc(db, "groups", grupa.id), {
        nume: numeEditat.trim(),
        descriere: descriereEditata.trim(),
      });
      setIdInEditare(null);
    } catch (err) {
      console.error("Eroare la actualizarea grupei:", err);
    } finally {
      setIdInLucru(null);
    }
  }

  function stergeGrupa(grupa: Grupa) {
    setConfirmare({
      titlu: "Șterge grupa",
      mesaj: `Ștergi grupa "${grupa.nume}"? Produsele existente rămân, dar fără grupă.`,
      onConfirm: async () => {
        setIdInLucru(grupa.id);
        try {
          await deleteDoc(doc(db, "groups", grupa.id));
        } catch (err) {
          console.error("Eroare la ștergerea grupei:", err);
        } finally {
          setIdInLucru(null);
        }
      },
    });
  }

  return (
    <div className="grid grid-cols-1 lg:grid-cols-2 gap-6 max-w-4xl">
      <form
        onSubmit={adaugaGrupa}
        className="bg-white rounded-2xl border border-gray-100 shadow-sm p-6 space-y-4 h-fit"
      >
        <h3 className="font-medium text-gray-900">Adaugă grupă</h3>
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1.5">Nume grupă</label>
          <input
            type="text"
            value={numeNou}
            onChange={(e) => setNumeNou(e.target.value)}
            placeholder="ex: Postere"
            className="w-full rounded-xl border border-gray-200 px-3 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-brand-primary/30 focus:border-brand-primary"
          />
        </div>
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1.5">Descriere (opțional)</label>
          <input
            type="text"
            value={descriereNoua}
            onChange={(e) => setDescriereNoua(e.target.value)}
            placeholder="ex: Postere printate pe hârtie mată"
            className="w-full rounded-xl border border-gray-200 px-3 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-brand-primary/30 focus:border-brand-primary"
          />
        </div>
        {eroare && <p className="text-xs text-red-600">{eroare}</p>}
        <button
          type="submit"
          disabled={seSalveaza}
          className="flex items-center justify-center gap-2 bg-brand-primary text-white font-medium px-5 py-2.5 rounded-xl hover:bg-brand-primary-dark transition-colors disabled:opacity-60 text-sm"
        >
          {seSalveaza ? <Loader2 className="w-4 h-4 animate-spin" /> : "Adaugă Grupă"}
        </button>
      </form>

      <div className="bg-white rounded-2xl border border-gray-100 shadow-sm p-6">
        <h3 className="font-medium text-gray-900 mb-4">Grupe existente</h3>
        {grupe.length === 0 ? (
          <p className="text-sm text-gray-400">Nicio grupă creată încă.</p>
        ) : (
          <ul className="space-y-2">
            {grupe.map((grupa) => (
              <li key={grupa.id} className="bg-gray-50 rounded-xl px-3.5 py-2.5">
                {idInEditare === grupa.id ? (
                  <div className="space-y-2">
                    <input
                      type="text"
                      value={numeEditat}
                      onChange={(e) => setNumeEditat(e.target.value)}
                      className="w-full rounded-lg border border-gray-200 px-2.5 py-1.5 text-sm focus:outline-none focus:ring-2 focus:ring-brand-primary/30 focus:border-brand-primary"
                    />
                    <input
                      type="text"
                      value={descriereEditata}
                      onChange={(e) => setDescriereEditata(e.target.value)}
                      placeholder="Descriere"
                      className="w-full rounded-lg border border-gray-200 px-2.5 py-1.5 text-xs focus:outline-none focus:ring-2 focus:ring-brand-primary/30 focus:border-brand-primary"
                    />
                    <div className="flex items-center gap-3 pt-1">
                      <button
                        onClick={() => salveazaEditarea(grupa)}
                        disabled={idInLucru === grupa.id}
                        className="flex items-center gap-1 text-xs font-medium text-brand-primary hover:underline"
                      >
                        {idInLucru === grupa.id ? (
                          <Loader2 className="w-3.5 h-3.5 animate-spin" />
                        ) : (
                          <CheckCircle2 className="w-3.5 h-3.5" />
                        )}
                        Salvează
                      </button>
                      <button
                        onClick={anuleazaEditarea}
                        className="text-xs font-medium text-gray-500 hover:text-gray-800"
                      >
                        Anulează
                      </button>
                    </div>
                  </div>
                ) : (
                  <div className="flex items-center justify-between">
                    <div className="min-w-0">
                      <p className="text-sm text-gray-800 truncate">{grupa.nume}</p>
                      {grupa.descriere && <p className="text-xs text-gray-400 truncate">{grupa.descriere}</p>}
                    </div>
                    <div className="flex items-center gap-3 flex-shrink-0 pl-3">
                      <button
                        onClick={() => incepeEditarea(grupa)}
                        className="text-gray-400 hover:text-brand-primary transition-colors"
                        aria-label="Editează grupa"
                      >
                        <Pencil className="w-3.5 h-3.5" />
                      </button>
                      <button
                        onClick={() => stergeGrupa(grupa)}
                        disabled={idInLucru === grupa.id}
                        className="text-gray-300 hover:text-red-500 transition-colors"
                        aria-label="Șterge grupa"
                      >
                        {idInLucru === grupa.id ? (
                          <Loader2 className="w-3.5 h-3.5 animate-spin" />
                        ) : (
                          <Trash2 className="w-3.5 h-3.5" />
                        )}
                      </button>
                    </div>
                  </div>
                )}
              </li>
            ))}
          </ul>
        )}
      </div>
      <ConfirmDialog stare={confirmare} onClose={() => setConfirmare(null)} />
    </div>
  );
}
