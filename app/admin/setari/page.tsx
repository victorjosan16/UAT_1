"use client";

import { useEffect, useState } from "react";
import { doc, onSnapshot, setDoc } from "firebase/firestore";
import { db } from "@/lib/firebase";
import { Loader2, CheckCircle2, X, Plus, Trash2, Eye, EyeOff } from "lucide-react";

// ---------------------------------------------------------------------------
// Tipuri
// ---------------------------------------------------------------------------

interface LinkMeniu {
  id: string;
  eticheta: string;
  link: string;
  vizibil: boolean;
}

interface ConfigHeader {
  logoText: string;
  locatie: string;
  telefon: string;
  program: string;
  textButonLogin: string;
  linkuriMeniu: LinkMeniu[];
}

const CONFIG_IMPLICIT: ConfigHeader = {
  logoText: "PosterART",
  locatie: "",
  telefon: "",
  program: "",
  textButonLogin: "",
  linkuriMeniu: [],
};

function idNou(): string {
  return `${Date.now().toString(36)}${Math.random().toString(36).slice(2, 8)}`;
}

const stilInput =
  "w-full rounded-xl border border-gray-200 px-3 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-brand-primary/30 focus:border-brand-primary";

// ---------------------------------------------------------------------------
// Pagina
// ---------------------------------------------------------------------------

export default function SetariPage() {
  const [config, setConfig] = useState<ConfigHeader>(CONFIG_IMPLICIT);
  const [seIncarca, setSeIncarca] = useState(true);
  const [seSalveaza, setSeSalveaza] = useState(false);
  const [eroare, setEroare] = useState<string | null>(null);
  const [succes, setSucces] = useState(false);

  useEffect(() => {
    const unsubscribe = onSnapshot(doc(db, "site_config", "header"), (snap) => {
      const data = snap.data();
      if (data) {
        setConfig({
          logoText: data.logoText ?? CONFIG_IMPLICIT.logoText,
          locatie: data.locatie ?? "",
          telefon: data.telefon ?? "",
          program: data.program ?? "",
          textButonLogin: data.textButonLogin ?? "",
          linkuriMeniu: Array.isArray(data.linkuriMeniu) ? data.linkuriMeniu : [],
        });
      }
      setSeIncarca(false);
    });
    return () => unsubscribe();
  }, []);

  function actualizeazaCamp<K extends keyof ConfigHeader>(camp: K, valoare: ConfigHeader[K]) {
    setConfig((prev) => ({ ...prev, [camp]: valoare }));
  }

  function adaugaLink() {
    setConfig((prev) => ({
      ...prev,
      linkuriMeniu: [...prev.linkuriMeniu, { id: idNou(), eticheta: "", link: "#", vizibil: true }],
    }));
  }

  function actualizeazaLink(id: string, campuri: Partial<Omit<LinkMeniu, "id">>) {
    setConfig((prev) => ({
      ...prev,
      linkuriMeniu: prev.linkuriMeniu.map((l) => (l.id === id ? { ...l, ...campuri } : l)),
    }));
  }

  function stergeLink(id: string) {
    setConfig((prev) => ({
      ...prev,
      linkuriMeniu: prev.linkuriMeniu.filter((l) => l.id !== id),
    }));
  }

  async function salveaza(e: React.FormEvent) {
    e.preventDefault();
    setEroare(null);
    setSucces(false);
    setSeSalveaza(true);
    try {
      await setDoc(doc(db, "site_config", "header"), config);
      setSucces(true);
      setTimeout(() => setSucces(false), 2000);
    } catch (err) {
      console.error("Eroare la salvarea setărilor:", err);
      setEroare("A apărut o eroare la salvare. Încearcă din nou.");
    } finally {
      setSeSalveaza(false);
    }
  }

  if (seIncarca) {
    return (
      <div className="flex items-center justify-center h-64 text-gray-400">
        <Loader2 className="w-6 h-6 animate-spin mr-2" />
        Se încarcă setările...
      </div>
    );
  }

  return (
    <div className="animate-in fade-in">
      <div className="mb-6">
        <h1 className="text-2xl font-semibold text-gray-900">Setări</h1>
        <p className="text-sm text-gray-500 mt-1">Configurează antetul (header) magazinului public.</p>
      </div>

      <form onSubmit={salveaza} className="space-y-6 max-w-2xl">
        <div className="bg-white rounded-2xl border border-gray-100 shadow-sm p-6 space-y-4">
          <h3 className="font-medium text-gray-900">Informații antet</h3>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1.5">Nume magazin (logo)</label>
            <input
              type="text"
              value={config.logoText}
              onChange={(e) => actualizeazaCamp("logoText", e.target.value)}
              className={stilInput}
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1.5">Locație (opțional)</label>
            <input
              type="text"
              value={config.locatie}
              onChange={(e) => actualizeazaCamp("locatie", e.target.value)}
              placeholder="ex: Chișinău, Moldova"
              className={stilInput}
            />
          </div>

          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1.5">Telefon</label>
              <input
                type="text"
                value={config.telefon}
                onChange={(e) => actualizeazaCamp("telefon", e.target.value)}
                placeholder="ex: 022 22 11 90"
                className={stilInput}
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1.5">Program</label>
              <input
                type="text"
                value={config.program}
                onChange={(e) => actualizeazaCamp("program", e.target.value)}
                placeholder="ex: L - V de la 09:30 la 18:00"
                className={stilInput}
              />
            </div>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1.5">Text buton login (opțional)</label>
            <input
              type="text"
              value={config.textButonLogin}
              onChange={(e) => actualizeazaCamp("textButonLogin", e.target.value)}
              placeholder="ex: Intrați"
              className={stilInput}
            />
          </div>
        </div>

        <div className="bg-white rounded-2xl border border-gray-100 shadow-sm p-6 space-y-4">
          <div className="flex items-center justify-between">
            <h3 className="font-medium text-gray-900">Meniu principal</h3>
            <button
              type="button"
              onClick={adaugaLink}
              className="flex items-center gap-1.5 text-xs font-medium text-brand-primary hover:underline"
            >
              <Plus className="w-3.5 h-3.5" />
              Adaugă Link
            </button>
          </div>

          {config.linkuriMeniu.length === 0 ? (
            <p className="text-sm text-gray-400">Niciun link în meniu încă.</p>
          ) : (
            <div className="space-y-2">
              {config.linkuriMeniu.map((linkItem) => (
                <div key={linkItem.id} className="flex items-center gap-2 bg-gray-50 rounded-xl p-2.5">
                  <input
                    type="text"
                    value={linkItem.eticheta}
                    onChange={(e) => actualizeazaLink(linkItem.id, { eticheta: e.target.value })}
                    placeholder="Etichetă (ex: Produse)"
                    className="flex-1 min-w-0 rounded-lg border border-gray-200 px-2.5 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand-primary/30 focus:border-brand-primary"
                  />
                  <input
                    type="text"
                    value={linkItem.link}
                    onChange={(e) => actualizeazaLink(linkItem.id, { link: e.target.value })}
                    placeholder="Link (ex: #)"
                    className="flex-1 min-w-0 rounded-lg border border-gray-200 px-2.5 py-2 text-sm font-mono focus:outline-none focus:ring-2 focus:ring-brand-primary/30 focus:border-brand-primary"
                  />
                  <button
                    type="button"
                    onClick={() => actualizeazaLink(linkItem.id, { vizibil: !linkItem.vizibil })}
                    className={`flex-shrink-0 p-2 rounded-lg transition-colors ${
                      linkItem.vizibil ? "text-emerald-600 hover:bg-emerald-50" : "text-gray-400 hover:bg-gray-100"
                    }`}
                    aria-label={linkItem.vizibil ? "Ascunde linkul" : "Arată linkul"}
                    title={linkItem.vizibil ? "Vizibil" : "Ascuns"}
                  >
                    {linkItem.vizibil ? <Eye className="w-4 h-4" /> : <EyeOff className="w-4 h-4" />}
                  </button>
                  <button
                    type="button"
                    onClick={() => stergeLink(linkItem.id)}
                    className="flex-shrink-0 p-2 rounded-lg text-gray-300 hover:text-red-500 hover:bg-red-50 transition-colors"
                    aria-label="Șterge linkul"
                  >
                    <Trash2 className="w-4 h-4" />
                  </button>
                </div>
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
            Setări salvate cu succes!
          </div>
        )}

        <button
          type="submit"
          disabled={seSalveaza}
          className="flex items-center justify-center gap-2 bg-brand-primary text-white font-medium px-6 py-2.5 rounded-xl hover:bg-brand-primary-dark transition-colors disabled:opacity-60"
        >
          {seSalveaza ? <Loader2 className="w-4 h-4 animate-spin" /> : "Salvează Setările"}
        </button>
      </form>
    </div>
  );
}
