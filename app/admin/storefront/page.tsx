"use client";

import { useEffect, useRef, useState } from "react";
import {
  collection,
  onSnapshot,
  addDoc,
  updateDoc,
  deleteDoc,
  doc,
  writeBatch,
} from "firebase/firestore";
import { db } from "@/lib/firebase";
import { incarcaImagineCloudinary } from "@/lib/cloudinary";
import {
  Plus,
  ArrowUp,
  ArrowDown,
  Pencil,
  Trash2,
  Eye,
  EyeOff,
  Loader2,
  X,
  CheckCircle2,
  UploadCloud,
  Image as ImageIcon,
  Columns2,
  GalleryHorizontal,
  Megaphone,
} from "lucide-react";

// ---------------------------------------------------------------------------
// Tipuri
// ---------------------------------------------------------------------------

interface Grupa {
  id: string;
  nume: string;
}

type TipBloc = "hero" | "text_imagine" | "carusel_produse" | "banner_simplu";

interface ContinutHero {
  titlu: string;
  subtitlu: string;
  imagineBg: string;
  textButon: string;
  linkButon: string;
}

interface ContinutTextImagine {
  titlu: string;
  text: string;
  imagine: string;
  pozitieImagine: "stanga" | "dreapta";
}

interface ContinutCaruselProduse {
  titluSectiune: string;
  group_id: string;
}

interface ContinutBannerSimplu {
  text: string;
  textButon: string;
  linkButon: string;
}

interface BlocHeroData {
  id: string;
  tip: "hero";
  ordine: number;
  vizibil: boolean;
  continut: ContinutHero;
}
interface BlocTextImagineData {
  id: string;
  tip: "text_imagine";
  ordine: number;
  vizibil: boolean;
  continut: ContinutTextImagine;
}
interface BlocCaruselData {
  id: string;
  tip: "carusel_produse";
  ordine: number;
  vizibil: boolean;
  continut: ContinutCaruselProduse;
}
interface BlocBannerData {
  id: string;
  tip: "banner_simplu";
  ordine: number;
  vizibil: boolean;
  continut: ContinutBannerSimplu;
}

type BlocPagina = BlocHeroData | BlocTextImagineData | BlocCaruselData | BlocBannerData;

// ---------------------------------------------------------------------------
// Config tipuri de bloc
// ---------------------------------------------------------------------------

const ETICHETE_TIP: Record<TipBloc, string> = {
  hero: "Secțiune Hero",
  text_imagine: "Text + Imagine",
  carusel_produse: "Carusel Produse",
  banner_simplu: "Banner Simplu",
};

const ICOANE_TIP: Record<TipBloc, typeof ImageIcon> = {
  hero: ImageIcon,
  text_imagine: Columns2,
  carusel_produse: GalleryHorizontal,
  banner_simplu: Megaphone,
};

function continutImplicit(tip: TipBloc): BlocPagina["continut"] {
  switch (tip) {
    case "hero":
      return {
        titlu: "Titlu principal",
        subtitlu: "Un subtitlu care descrie oferta ta",
        imagineBg: "",
        textButon: "Vezi Produsele",
        linkButon: "#",
      };
    case "text_imagine":
      return {
        titlu: "Titlu secțiune",
        text: "Descrie aici povestea brandului sau un beneficiu cheie.",
        imagine: "",
        pozitieImagine: "dreapta",
      };
    case "carusel_produse":
      return { titluSectiune: "Produse Recomandate", group_id: "" };
    case "banner_simplu":
      return { text: "Reducere specială pentru tine!", textButon: "Cumpără Acum", linkButon: "#" };
  }
}

function rezumatBloc(bloc: BlocPagina): string {
  switch (bloc.tip) {
    case "hero":
      return bloc.continut.titlu || "(fără titlu)";
    case "text_imagine":
      return bloc.continut.titlu || "(fără titlu)";
    case "carusel_produse":
      return bloc.continut.titluSectiune || "(fără titlu)";
    case "banner_simplu":
      return bloc.continut.text || "(fără text)";
  }
}

// ---------------------------------------------------------------------------
// Pagina
// ---------------------------------------------------------------------------

export default function StorefrontPage() {
  const [blocuri, setBlocuri] = useState<BlocPagina[]>([]);
  const [grupe, setGrupe] = useState<Grupa[]>([]);
  const [seIncarca, setSeIncarca] = useState(true);
  const [blocInEditare, setBlocInEditare] = useState<BlocPagina | null>(null);
  const [menuTipDeschis, setMenuTipDeschis] = useState(false);
  const [idInLucru, setIdInLucru] = useState<string | null>(null);

  useEffect(() => {
    const unsubBlocuri = onSnapshot(collection(db, "homepage_blocks"), (snapshot) => {
      const lista: BlocPagina[] = snapshot.docs
        .map((docSnap) => {
          const data = docSnap.data();
          return {
            id: docSnap.id,
            tip: data.tip,
            ordine: typeof data.ordine === "number" ? data.ordine : 0,
            vizibil: data.vizibil !== false,
            continut: data.continut ?? continutImplicit(data.tip),
          } as BlocPagina;
        })
        .sort((a, b) => a.ordine - b.ordine);
      setBlocuri(lista);
      setSeIncarca(false);
    });

    const unsubGrupe = onSnapshot(collection(db, "groups"), (snapshot) => {
      const lista: Grupa[] = snapshot.docs
        .map((docSnap) => ({ id: docSnap.id, nume: docSnap.data().nume ?? "Grupă" }))
        .sort((a, b) => a.nume.localeCompare(b.nume, "ro"));
      setGrupe(lista);
    });

    return () => {
      unsubBlocuri();
      unsubGrupe();
    };
  }, []);

  async function adaugaBlocNou(tip: TipBloc) {
    const ordineNoua = blocuri.length > 0 ? Math.max(...blocuri.map((b) => b.ordine)) + 1 : 0;
    const continut = continutImplicit(tip);
    const docRef = await addDoc(collection(db, "homepage_blocks"), {
      tip,
      ordine: ordineNoua,
      vizibil: true,
      continut,
    });
    setBlocInEditare({ id: docRef.id, tip, ordine: ordineNoua, vizibil: true, continut } as BlocPagina);
    setMenuTipDeschis(false);
  }

  async function mutaBloc(index: number, directie: -1 | 1) {
    const altIndex = index + directie;
    if (altIndex < 0 || altIndex >= blocuri.length) return;
    const blocA = blocuri[index];
    const blocB = blocuri[altIndex];
    const batch = writeBatch(db);
    batch.update(doc(db, "homepage_blocks", blocA.id), { ordine: blocB.ordine });
    batch.update(doc(db, "homepage_blocks", blocB.id), { ordine: blocA.ordine });
    await batch.commit();
  }

  async function comutaVizibilitate(bloc: BlocPagina) {
    setIdInLucru(bloc.id);
    try {
      await updateDoc(doc(db, "homepage_blocks", bloc.id), { vizibil: !bloc.vizibil });
    } catch (err) {
      console.error("Eroare la actualizarea vizibilității blocului:", err);
    } finally {
      setIdInLucru(null);
    }
  }

  async function stergeBloc(bloc: BlocPagina) {
    if (!confirm(`Ștergi blocul „${ETICHETE_TIP[bloc.tip]}"? Această acțiune nu poate fi anulată.`)) return;
    setIdInLucru(bloc.id);
    try {
      await deleteDoc(doc(db, "homepage_blocks", bloc.id));
    } catch (err) {
      console.error("Eroare la ștergerea blocului:", err);
    } finally {
      setIdInLucru(null);
    }
  }

  return (
    <div className="animate-in fade-in">
      <div className="mb-6">
        <h1 className="text-2xl font-semibold text-gray-900">Constructor Pagină</h1>
        <p className="text-sm text-gray-500 mt-1">Compune pagina principală a magazinului din blocuri.</p>
      </div>

      {blocInEditare ? (
        <PanouEditareBloc
          bloc={blocInEditare}
          grupe={grupe}
          onSalvat={() => setBlocInEditare(null)}
          onAnulat={() => setBlocInEditare(null)}
        />
      ) : (
        <>
          <div className="relative mb-6">
            <button
              onClick={() => setMenuTipDeschis((v) => !v)}
              className="flex items-center gap-2 bg-brand-primary text-white font-medium px-5 py-2.5 rounded-xl hover:bg-brand-primary-dark transition-colors text-sm"
            >
              <Plus className="w-4 h-4" />
              Adaugă Bloc Nou
            </button>
            {menuTipDeschis && (
              <div className="mt-3 flex flex-wrap gap-2 bg-white rounded-2xl border border-gray-100 shadow-sm p-3 max-w-xl animate-in fade-in slide-in-from-top-2">
                {(Object.keys(ETICHETE_TIP) as TipBloc[]).map((tip) => {
                  const Icon = ICOANE_TIP[tip];
                  return (
                    <button
                      key={tip}
                      onClick={() => adaugaBlocNou(tip)}
                      className="flex items-center gap-2 px-3.5 py-2 rounded-xl border border-gray-200 text-sm text-gray-700 hover:border-brand-primary hover:text-brand-primary transition-colors"
                    >
                      <Icon className="w-4 h-4" />
                      {ETICHETE_TIP[tip]}
                    </button>
                  );
                })}
              </div>
            )}
          </div>

          {seIncarca ? (
            <div className="flex items-center justify-center h-64 text-gray-400">
              <Loader2 className="w-6 h-6 animate-spin mr-2" />
              Se încarcă blocurile...
            </div>
          ) : blocuri.length === 0 ? (
            <div className="bg-white rounded-2xl border border-gray-100 p-12 text-center text-gray-400">
              Pagina principală nu are încă niciun bloc. Adaugă primul cu butonul de mai sus.
            </div>
          ) : (
            <div className="space-y-3 max-w-2xl">
              {blocuri.map((bloc, index) => {
                const Icon = ICOANE_TIP[bloc.tip];
                const inLucru = idInLucru === bloc.id;
                return (
                  <div
                    key={bloc.id}
                    className="bg-white rounded-2xl border border-gray-100 shadow-sm hover:shadow-xl transition-shadow p-4 flex items-center gap-3"
                  >
                    <div className="flex flex-col gap-1 flex-shrink-0">
                      <button
                        onClick={() => mutaBloc(index, -1)}
                        disabled={index === 0}
                        className="w-6 h-6 rounded-lg border border-gray-200 flex items-center justify-center hover:bg-gray-100 disabled:opacity-30"
                        aria-label="Mută sus"
                      >
                        <ArrowUp className="w-3 h-3" />
                      </button>
                      <button
                        onClick={() => mutaBloc(index, 1)}
                        disabled={index === blocuri.length - 1}
                        className="w-6 h-6 rounded-lg border border-gray-200 flex items-center justify-center hover:bg-gray-100 disabled:opacity-30"
                        aria-label="Mută jos"
                      >
                        <ArrowDown className="w-3 h-3" />
                      </button>
                    </div>

                    <div className="w-10 h-10 rounded-xl bg-brand-primary/10 text-brand-primary flex items-center justify-center flex-shrink-0">
                      <Icon className="w-4.5 h-4.5" />
                    </div>

                    <div className="flex-1 min-w-0">
                      <p className="text-xs text-gray-400">{ETICHETE_TIP[bloc.tip]}</p>
                      <p className="text-sm font-medium text-gray-900 truncate">{rezumatBloc(bloc)}</p>
                    </div>

                    <button
                      onClick={() => comutaVizibilitate(bloc)}
                      disabled={inLucru}
                      className={`flex-shrink-0 flex items-center gap-1.5 text-xs font-medium px-2.5 py-1.5 rounded-full ${
                        bloc.vizibil ? "bg-emerald-50 text-emerald-700" : "bg-gray-100 text-gray-500"
                      }`}
                    >
                      {bloc.vizibil ? <Eye className="w-3.5 h-3.5" /> : <EyeOff className="w-3.5 h-3.5" />}
                      {bloc.vizibil ? "Vizibil" : "Ascuns"}
                    </button>

                    <button
                      onClick={() => setBlocInEditare(bloc)}
                      disabled={inLucru}
                      className="flex-shrink-0 text-gray-400 hover:text-brand-primary transition-colors disabled:opacity-40"
                      aria-label="Editează blocul"
                    >
                      <Pencil className="w-4 h-4" />
                    </button>
                    <button
                      onClick={() => stergeBloc(bloc)}
                      disabled={inLucru}
                      className="flex-shrink-0 text-gray-300 hover:text-red-500 transition-colors disabled:opacity-40"
                      aria-label="Șterge blocul"
                    >
                      {inLucru ? <Loader2 className="w-4 h-4 animate-spin" /> : <Trash2 className="w-4 h-4" />}
                    </button>
                  </div>
                );
              })}
            </div>
          )}
        </>
      )}
    </div>
  );
}

// ---------------------------------------------------------------------------
// Panou editare bloc — comută pe tipul de formular potrivit
// ---------------------------------------------------------------------------

function PanouEditareBloc({
  bloc,
  grupe,
  onSalvat,
  onAnulat,
}: {
  bloc: BlocPagina;
  grupe: Grupa[];
  onSalvat: () => void;
  onAnulat: () => void;
}) {
  return (
    <div className="bg-white rounded-2xl border border-gray-100 shadow-sm p-6 max-w-2xl">
      <div className="flex items-center justify-between mb-5">
        <h3 className="font-medium text-gray-900">Editează: {ETICHETE_TIP[bloc.tip]}</h3>
        <button
          onClick={onAnulat}
          className="text-xs text-gray-500 hover:text-gray-800 flex items-center gap-1"
        >
          <X className="w-3.5 h-3.5" />
          Închide
        </button>
      </div>

      {bloc.tip === "hero" && (
        <FormHero blocId={bloc.id} continutInitial={bloc.continut} vizibilInitial={bloc.vizibil} onSalvat={onSalvat} />
      )}
      {bloc.tip === "text_imagine" && (
        <FormTextImagine
          blocId={bloc.id}
          continutInitial={bloc.continut}
          vizibilInitial={bloc.vizibil}
          onSalvat={onSalvat}
        />
      )}
      {bloc.tip === "carusel_produse" && (
        <FormCaruselProduse
          blocId={bloc.id}
          continutInitial={bloc.continut}
          vizibilInitial={bloc.vizibil}
          grupe={grupe}
          onSalvat={onSalvat}
        />
      )}
      {bloc.tip === "banner_simplu" && (
        <FormBannerSimplu
          blocId={bloc.id}
          continutInitial={bloc.continut}
          vizibilInitial={bloc.vizibil}
          onSalvat={onSalvat}
        />
      )}
    </div>
  );
}

// ---------------------------------------------------------------------------
// Sub-componente reutilizabile pentru formulare
// ---------------------------------------------------------------------------

function ToggleVizibil({ vizibil, onChange }: { vizibil: boolean; onChange: (v: boolean) => void }) {
  return (
    <button
      type="button"
      onClick={() => onChange(!vizibil)}
      className={`w-full flex items-center justify-center gap-2 rounded-xl px-3 py-2.5 text-sm font-medium transition-colors ${
        vizibil ? "bg-emerald-50 text-emerald-700" : "bg-gray-100 text-gray-500"
      }`}
    >
      {vizibil ? <Eye className="w-4 h-4" /> : <EyeOff className="w-4 h-4" />}
      {vizibil ? "Vizibil pe site" : "Ascuns"}
    </button>
  );
}

function CampImagine({
  eticheta,
  imagineCurenta,
  onImagineNoua,
}: {
  eticheta: string;
  imagineCurenta: string;
  onImagineNoua: (fisier: File, preview: string) => void;
}) {
  const inputRef = useRef<HTMLInputElement>(null);
  return (
    <div>
      <label className="block text-sm font-medium text-gray-700 mb-1.5">{eticheta}</label>
      {imagineCurenta && (
        <div className="mb-3">
          {/* eslint-disable-next-line @next/next/no-img-element */}
          <img src={imagineCurenta} alt="" className="w-full h-32 rounded-xl object-cover border border-gray-100" />
        </div>
      )}
      <button
        type="button"
        onClick={() => inputRef.current?.click()}
        className="w-full flex flex-col items-center justify-center gap-2 border-2 border-dashed border-gray-200 rounded-2xl py-6 text-gray-400 hover:border-brand-primary hover:text-brand-primary transition-colors"
      >
        <UploadCloud className="w-5 h-5" />
        <span className="text-xs font-medium">{imagineCurenta ? "Înlocuiește poza" : "Click pentru a încărca o poză"}</span>
      </button>
      <input
        ref={inputRef}
        type="file"
        accept="image/*"
        onChange={(e) => {
          const fisier = e.target.files?.[0];
          if (fisier) onImagineNoua(fisier, URL.createObjectURL(fisier));
        }}
        className="hidden"
      />
    </div>
  );
}

function MesajeForm({ eroare, succes }: { eroare: string | null; succes: boolean }) {
  return (
    <>
      {eroare && (
        <div className="flex items-center gap-2 text-red-600 text-xs bg-red-50 rounded-xl px-3 py-2.5">
          <X className="w-3.5 h-3.5 flex-shrink-0" />
          {eroare}
        </div>
      )}
      {succes && (
        <div className="flex items-center gap-2 text-emerald-700 text-xs bg-emerald-50 rounded-xl px-3 py-2.5">
          <CheckCircle2 className="w-3.5 h-3.5 flex-shrink-0" />
          Modificări salvate cu succes!
        </div>
      )}
    </>
  );
}

const stilInput =
  "w-full rounded-xl border border-gray-200 px-3 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-brand-primary/30 focus:border-brand-primary";

// ---------------------------------------------------------------------------
// Formular: Hero
// ---------------------------------------------------------------------------

function FormHero({
  blocId,
  continutInitial,
  vizibilInitial,
  onSalvat,
}: {
  blocId: string;
  continutInitial: ContinutHero;
  vizibilInitial: boolean;
  onSalvat: () => void;
}) {
  const [titlu, setTitlu] = useState(continutInitial.titlu);
  const [subtitlu, setSubtitlu] = useState(continutInitial.subtitlu);
  const [textButon, setTextButon] = useState(continutInitial.textButon);
  const [linkButon, setLinkButon] = useState(continutInitial.linkButon);
  const [imagineBg, setImagineBg] = useState(continutInitial.imagineBg);
  const [fisierNou, setFisierNou] = useState<File | null>(null);
  const [previewNou, setPreviewNou] = useState<string | null>(null);
  const [vizibil, setVizibil] = useState(vizibilInitial);
  const [seSalveaza, setSeSalveaza] = useState(false);
  const [eroare, setEroare] = useState<string | null>(null);
  const [succes, setSucces] = useState(false);

  async function salveaza(e: React.FormEvent) {
    e.preventDefault();
    setEroare(null);
    setSucces(false);
    if (!titlu.trim()) return setEroare("Titlul este obligatoriu.");

    setSeSalveaza(true);
    try {
      const urlImagine = fisierNou ? await incarcaImagineCloudinary(fisierNou) : imagineBg;
      const continut: ContinutHero = {
        titlu: titlu.trim(),
        subtitlu: subtitlu.trim(),
        imagineBg: urlImagine,
        textButon: textButon.trim(),
        linkButon: linkButon.trim(),
      };
      await updateDoc(doc(db, "homepage_blocks", blocId), { continut, vizibil });
      setSucces(true);
      setTimeout(onSalvat, 700);
    } catch (err) {
      console.error("Eroare la salvarea blocului hero:", err);
      setEroare("A apărut o eroare la salvare.");
    } finally {
      setSeSalveaza(false);
    }
  }

  return (
    <form onSubmit={salveaza} className="space-y-4">
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1.5">Titlu</label>
        <input type="text" value={titlu} onChange={(e) => setTitlu(e.target.value)} className={stilInput} />
      </div>
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1.5">Subtitlu</label>
        <input type="text" value={subtitlu} onChange={(e) => setSubtitlu(e.target.value)} className={stilInput} />
      </div>
      <div className="grid grid-cols-2 gap-4">
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1.5">Text buton</label>
          <input type="text" value={textButon} onChange={(e) => setTextButon(e.target.value)} className={stilInput} />
        </div>
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1.5">Link buton</label>
          <input type="text" value={linkButon} onChange={(e) => setLinkButon(e.target.value)} className={stilInput} />
        </div>
      </div>
      <CampImagine
        eticheta="Imagine de fundal"
        imagineCurenta={previewNou ?? imagineBg}
        onImagineNoua={(fisier, preview) => {
          setFisierNou(fisier);
          setPreviewNou(preview);
        }}
      />
      <ToggleVizibil vizibil={vizibil} onChange={setVizibil} />
      <MesajeForm eroare={eroare} succes={succes} />
      <button
        type="submit"
        disabled={seSalveaza}
        className="flex items-center justify-center gap-2 bg-brand-primary text-white font-medium px-6 py-2.5 rounded-xl hover:bg-brand-primary-dark transition-colors disabled:opacity-60"
      >
        {seSalveaza ? <Loader2 className="w-4 h-4 animate-spin" /> : "Salvează Modificările"}
      </button>
    </form>
  );
}

// ---------------------------------------------------------------------------
// Formular: Text + Imagine
// ---------------------------------------------------------------------------

function FormTextImagine({
  blocId,
  continutInitial,
  vizibilInitial,
  onSalvat,
}: {
  blocId: string;
  continutInitial: ContinutTextImagine;
  vizibilInitial: boolean;
  onSalvat: () => void;
}) {
  const [titlu, setTitlu] = useState(continutInitial.titlu);
  const [text, setText] = useState(continutInitial.text);
  const [pozitieImagine, setPozitieImagine] = useState(continutInitial.pozitieImagine);
  const [imagine, setImagine] = useState(continutInitial.imagine);
  const [fisierNou, setFisierNou] = useState<File | null>(null);
  const [previewNou, setPreviewNou] = useState<string | null>(null);
  const [vizibil, setVizibil] = useState(vizibilInitial);
  const [seSalveaza, setSeSalveaza] = useState(false);
  const [eroare, setEroare] = useState<string | null>(null);
  const [succes, setSucces] = useState(false);

  async function salveaza(e: React.FormEvent) {
    e.preventDefault();
    setEroare(null);
    setSucces(false);
    if (!titlu.trim()) return setEroare("Titlul este obligatoriu.");

    setSeSalveaza(true);
    try {
      const urlImagine = fisierNou ? await incarcaImagineCloudinary(fisierNou) : imagine;
      const continut: ContinutTextImagine = {
        titlu: titlu.trim(),
        text: text.trim(),
        imagine: urlImagine,
        pozitieImagine,
      };
      await updateDoc(doc(db, "homepage_blocks", blocId), { continut, vizibil });
      setSucces(true);
      setTimeout(onSalvat, 700);
    } catch (err) {
      console.error("Eroare la salvarea blocului text + imagine:", err);
      setEroare("A apărut o eroare la salvare.");
    } finally {
      setSeSalveaza(false);
    }
  }

  return (
    <form onSubmit={salveaza} className="space-y-4">
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1.5">Titlu</label>
        <input type="text" value={titlu} onChange={(e) => setTitlu(e.target.value)} className={stilInput} />
      </div>
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1.5">Text</label>
        <textarea value={text} onChange={(e) => setText(e.target.value)} rows={4} className={`${stilInput} resize-none`} />
      </div>
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1.5">Poziție imagine</label>
        <div className="flex gap-2">
          <button
            type="button"
            onClick={() => setPozitieImagine("stanga")}
            className={`flex-1 px-3.5 py-2 rounded-xl text-sm font-medium border transition-colors ${
              pozitieImagine === "stanga"
                ? "border-brand-primary text-brand-primary bg-brand-primary/5"
                : "border-gray-200 text-gray-600"
            }`}
          >
            Stânga
          </button>
          <button
            type="button"
            onClick={() => setPozitieImagine("dreapta")}
            className={`flex-1 px-3.5 py-2 rounded-xl text-sm font-medium border transition-colors ${
              pozitieImagine === "dreapta"
                ? "border-brand-primary text-brand-primary bg-brand-primary/5"
                : "border-gray-200 text-gray-600"
            }`}
          >
            Dreapta
          </button>
        </div>
      </div>
      <CampImagine
        eticheta="Imagine"
        imagineCurenta={previewNou ?? imagine}
        onImagineNoua={(fisier, preview) => {
          setFisierNou(fisier);
          setPreviewNou(preview);
        }}
      />
      <ToggleVizibil vizibil={vizibil} onChange={setVizibil} />
      <MesajeForm eroare={eroare} succes={succes} />
      <button
        type="submit"
        disabled={seSalveaza}
        className="flex items-center justify-center gap-2 bg-brand-primary text-white font-medium px-6 py-2.5 rounded-xl hover:bg-brand-primary-dark transition-colors disabled:opacity-60"
      >
        {seSalveaza ? <Loader2 className="w-4 h-4 animate-spin" /> : "Salvează Modificările"}
      </button>
    </form>
  );
}

// ---------------------------------------------------------------------------
// Formular: Carusel Produse
// ---------------------------------------------------------------------------

function FormCaruselProduse({
  blocId,
  continutInitial,
  vizibilInitial,
  grupe,
  onSalvat,
}: {
  blocId: string;
  continutInitial: ContinutCaruselProduse;
  vizibilInitial: boolean;
  grupe: Grupa[];
  onSalvat: () => void;
}) {
  const [titluSectiune, setTitluSectiune] = useState(continutInitial.titluSectiune);
  const [groupId, setGroupId] = useState(continutInitial.group_id);
  const [vizibil, setVizibil] = useState(vizibilInitial);
  const [seSalveaza, setSeSalveaza] = useState(false);
  const [eroare, setEroare] = useState<string | null>(null);
  const [succes, setSucces] = useState(false);

  async function salveaza(e: React.FormEvent) {
    e.preventDefault();
    setEroare(null);
    setSucces(false);
    if (!titluSectiune.trim()) return setEroare("Titlul secțiunii este obligatoriu.");
    if (!groupId) return setEroare("Selectează o grupă de produse.");

    setSeSalveaza(true);
    try {
      const continut: ContinutCaruselProduse = { titluSectiune: titluSectiune.trim(), group_id: groupId };
      await updateDoc(doc(db, "homepage_blocks", blocId), { continut, vizibil });
      setSucces(true);
      setTimeout(onSalvat, 700);
    } catch (err) {
      console.error("Eroare la salvarea blocului carusel:", err);
      setEroare("A apărut o eroare la salvare.");
    } finally {
      setSeSalveaza(false);
    }
  }

  return (
    <form onSubmit={salveaza} className="space-y-4">
      {grupe.length === 0 && (
        <div className="bg-amber-50 text-amber-700 text-xs rounded-xl px-3 py-2.5">
          Nu ai nicio grupă creată încă — creează una din Catalog → Gestiune Grupe.
        </div>
      )}
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1.5">Titlu secțiune</label>
        <input
          type="text"
          value={titluSectiune}
          onChange={(e) => setTitluSectiune(e.target.value)}
          className={stilInput}
        />
      </div>
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1.5">Grupă de produse</label>
        <select value={groupId} onChange={(e) => setGroupId(e.target.value)} className={`${stilInput} bg-white`}>
          <option value="">Selectează o grupă</option>
          {grupe.map((g) => (
            <option key={g.id} value={g.id}>
              {g.nume}
            </option>
          ))}
        </select>
      </div>
      <ToggleVizibil vizibil={vizibil} onChange={setVizibil} />
      <MesajeForm eroare={eroare} succes={succes} />
      <button
        type="submit"
        disabled={seSalveaza}
        className="flex items-center justify-center gap-2 bg-brand-primary text-white font-medium px-6 py-2.5 rounded-xl hover:bg-brand-primary-dark transition-colors disabled:opacity-60"
      >
        {seSalveaza ? <Loader2 className="w-4 h-4 animate-spin" /> : "Salvează Modificările"}
      </button>
    </form>
  );
}

// ---------------------------------------------------------------------------
// Formular: Banner Simplu
// ---------------------------------------------------------------------------

function FormBannerSimplu({
  blocId,
  continutInitial,
  vizibilInitial,
  onSalvat,
}: {
  blocId: string;
  continutInitial: ContinutBannerSimplu;
  vizibilInitial: boolean;
  onSalvat: () => void;
}) {
  const [text, setText] = useState(continutInitial.text);
  const [textButon, setTextButon] = useState(continutInitial.textButon);
  const [linkButon, setLinkButon] = useState(continutInitial.linkButon);
  const [vizibil, setVizibil] = useState(vizibilInitial);
  const [seSalveaza, setSeSalveaza] = useState(false);
  const [eroare, setEroare] = useState<string | null>(null);
  const [succes, setSucces] = useState(false);

  async function salveaza(e: React.FormEvent) {
    e.preventDefault();
    setEroare(null);
    setSucces(false);
    if (!text.trim()) return setEroare("Textul este obligatoriu.");

    setSeSalveaza(true);
    try {
      const continut: ContinutBannerSimplu = {
        text: text.trim(),
        textButon: textButon.trim(),
        linkButon: linkButon.trim(),
      };
      await updateDoc(doc(db, "homepage_blocks", blocId), { continut, vizibil });
      setSucces(true);
      setTimeout(onSalvat, 700);
    } catch (err) {
      console.error("Eroare la salvarea blocului banner:", err);
      setEroare("A apărut o eroare la salvare.");
    } finally {
      setSeSalveaza(false);
    }
  }

  return (
    <form onSubmit={salveaza} className="space-y-4">
      <div>
        <label className="block text-sm font-medium text-gray-700 mb-1.5">Text banner</label>
        <textarea value={text} onChange={(e) => setText(e.target.value)} rows={2} className={`${stilInput} resize-none`} />
      </div>
      <div className="grid grid-cols-2 gap-4">
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1.5">Text buton (opțional)</label>
          <input type="text" value={textButon} onChange={(e) => setTextButon(e.target.value)} className={stilInput} />
        </div>
        <div>
          <label className="block text-sm font-medium text-gray-700 mb-1.5">Link buton</label>
          <input type="text" value={linkButon} onChange={(e) => setLinkButon(e.target.value)} className={stilInput} />
        </div>
      </div>
      <ToggleVizibil vizibil={vizibil} onChange={setVizibil} />
      <MesajeForm eroare={eroare} succes={succes} />
      <button
        type="submit"
        disabled={seSalveaza}
        className="flex items-center justify-center gap-2 bg-brand-primary text-white font-medium px-6 py-2.5 rounded-xl hover:bg-brand-primary-dark transition-colors disabled:opacity-60"
      >
        {seSalveaza ? <Loader2 className="w-4 h-4 animate-spin" /> : "Salvează Modificările"}
      </button>
    </form>
  );
}
