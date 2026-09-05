"use client";

import { useEffect, useMemo, useState } from "react";
import {
  collection,
  doc,
  onSnapshot,
  addDoc,
  serverTimestamp,
} from "firebase/firestore";
import { db } from "@/lib/firebase";
import {
  ShoppingCart,
  X,
  Plus,
  Minus,
  Trash2,
  Loader2,
  CheckCircle2,
  ImageOff,
  ArrowRight,
  MapPin,
  Search,
  Sparkles,
} from "lucide-react";

// ---------------------------------------------------------------------------
// Tipuri — blocuri de pagină
// ---------------------------------------------------------------------------

type TipBloc =
  | "hero"
  | "hero_imagine"
  | "text_imagine"
  | "carusel_produse"
  | "banner_simplu"
  | "carduri_beneficii"
  | "pasi";

interface ContinutHero {
  titlu: string;
  subtitlu: string;
  imagineBg: string;
  textButon: string;
  linkButon: string;
}

interface StatisticaHero {
  id: string;
  valoare: string;
  eticheta: string;
}

interface ContinutHeroImagine {
  eyebrow: string;
  titlu: string;
  subtitlu: string;
  imagine: string;
  textButonPrimar: string;
  linkButonPrimar: string;
  textButonSecundar: string;
  linkButonSecundar: string;
  statistici: StatisticaHero[];
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

interface CardBeneficiu {
  id: string;
  titlu: string;
  text: string;
}

interface ContinutCarduriBeneficii {
  titluSectiune: string;
  carduri: CardBeneficiu[];
}

interface PasItem {
  id: string;
  titlu: string;
  text: string;
}

interface ContinutPasi {
  titluSectiune: string;
  imagine: string;
  pasi: PasItem[];
}

interface BlocHeroData {
  id: string;
  tip: "hero";
  ordine: number;
  vizibil: boolean;
  continut: ContinutHero;
}
interface BlocHeroImagineData {
  id: string;
  tip: "hero_imagine";
  ordine: number;
  vizibil: boolean;
  continut: ContinutHeroImagine;
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
interface BlocCarduriBeneficiiData {
  id: string;
  tip: "carduri_beneficii";
  ordine: number;
  vizibil: boolean;
  continut: ContinutCarduriBeneficii;
}
interface BlocPasiData {
  id: string;
  tip: "pasi";
  ordine: number;
  vizibil: boolean;
  continut: ContinutPasi;
}

type BlocPagina =
  | BlocHeroData
  | BlocHeroImagineData
  | BlocTextImagineData
  | BlocCaruselData
  | BlocBannerData
  | BlocCarduriBeneficiiData
  | BlocPasiData;

// ---------------------------------------------------------------------------
// Tipuri — catalog & coș
// ---------------------------------------------------------------------------

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

interface ItemCos {
  produsId: string;
  nume: string;
  pretUnitar: number;
  imagine: string | null;
  cantitate: number;
}

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

const CONFIG_HEADER_IMPLICIT: ConfigHeader = {
  logoText: "PosterART",
  locatie: "",
  telefon: "",
  program: "",
  textButonLogin: "",
  linkuriMeniu: [],
};

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

function formateazaPret(pret: number): string {
  return new Intl.NumberFormat("ro-RO", { minimumFractionDigits: 0 }).format(pret) + " lei";
}

function pretEfectiv(produs: Produs): number {
  return produs.pretRedus !== null && produs.pretRedus !== undefined && produs.pretRedus < produs.pret
    ? produs.pretRedus
    : produs.pret;
}

// ---------------------------------------------------------------------------
// Pagina
// ---------------------------------------------------------------------------

export default function MagazinPage() {
  const [configHeader, setConfigHeader] = useState<ConfigHeader>(CONFIG_HEADER_IMPLICIT);
  const [blocuri, setBlocuri] = useState<BlocPagina[]>([]);
  const [produse, setProduse] = useState<Produs[]>([]);
  const [seIncarca, setSeIncarca] = useState(true);

  const [cosDeschis, setCosDeschis] = useState(false);
  const [cos, setCos] = useState<ItemCos[]>([]);
  const [comandaTrimisa, setComandaTrimisa] = useState(false);
  const [seTrimite, setSeTrimite] = useState(false);
  const [eroareComanda, setEroareComanda] = useState<string | null>(null);
  const [numeClient, setNumeClient] = useState("");
  const [telefonClient, setTelefonClient] = useState("");

  // Configurarea antetului — live din Firestore
  useEffect(() => {
    const unsubscribe = onSnapshot(doc(db, "site_config", "header"), (snap) => {
      const data = snap.data();
      if (data) {
        setConfigHeader({
          logoText: data.logoText ?? CONFIG_HEADER_IMPLICIT.logoText,
          locatie: data.locatie ?? "",
          telefon: data.telefon ?? "",
          program: data.program ?? "",
          textButonLogin: data.textButonLogin ?? "",
          linkuriMeniu: Array.isArray(data.linkuriMeniu) ? data.linkuriMeniu : [],
        });
      }
    });
    return () => unsubscribe();
  }, []);

  // Blocurile paginii principale — live din Firestore
  useEffect(() => {
    const unsubscribe = onSnapshot(collection(db, "homepage_blocks"), (snapshot) => {
      const lista: BlocPagina[] = snapshot.docs
        .map((docSnap) => {
          const data = docSnap.data();
          return {
            id: docSnap.id,
            tip: data.tip,
            ordine: typeof data.ordine === "number" ? data.ordine : 0,
            vizibil: data.vizibil !== false,
            continut: data.continut ?? {},
          } as BlocPagina;
        })
        .filter((bloc) => bloc.vizibil)
        .sort((a, b) => a.ordine - b.ordine);
      setBlocuri(lista);
      setSeIncarca(false);
    });
    return () => unsubscribe();
  }, []);

  // Produse — live din Firestore
  useEffect(() => {
    const unsubscribe = onSnapshot(
      collection(db, "products"),
      (snapshot) => {
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
      },
      () => {}
    );
    return () => unsubscribe();
  }, []);

  // Ascunde automat produsele fără stoc sau marcate ca invizibile
  const produseDisponibile = useMemo(
    () => produse.filter((p) => p.vizibil && p.stoc > 0),
    [produse]
  );

  // -------------------------------------------------------------------------
  // Coș
  // -------------------------------------------------------------------------

  function adaugaInCos(produs: Produs) {
    setCos((prev) => {
      const existent = prev.find((item) => item.produsId === produs.id);
      if (existent) {
        return prev.map((item) =>
          item.produsId === produs.id
            ? { ...item, cantitate: Math.min(item.cantitate + 1, produs.stoc) }
            : item
        );
      }
      return [
        ...prev,
        {
          produsId: produs.id,
          nume: produs.nume,
          pretUnitar: pretEfectiv(produs),
          imagine: produs.imagini[0] ?? null,
          cantitate: 1,
        },
      ];
    });
    setCosDeschis(true);
  }

  function modificaCantitate(produsId: string, delta: number) {
    setCos((prev) =>
      prev
        .map((item) =>
          item.produsId === produsId
            ? { ...item, cantitate: item.cantitate + delta }
            : item
        )
        .filter((item) => item.cantitate > 0)
    );
  }

  function eliminaDinCos(produsId: string) {
    setCos((prev) => prev.filter((item) => item.produsId !== produsId));
  }

  const totalCos = useMemo(
    () => cos.reduce((suma, item) => suma + item.pretUnitar * item.cantitate, 0),
    [cos]
  );
  const numarItemiCos = useMemo(
    () => cos.reduce((suma, item) => suma + item.cantitate, 0),
    [cos]
  );

  async function trimiteComanda() {
    setEroareComanda(null);

    if (cos.length === 0) return;
    if (!numeClient.trim() || !telefonClient.trim()) {
      setEroareComanda("Te rugăm să completezi numele și telefonul.");
      return;
    }

    setSeTrimite(true);
    try {
      const produseText = cos.map((item) => `${item.cantitate}x ${item.nume}`).join(" + ");
      await addDoc(collection(db, "orders"), {
        client: numeClient.trim(),
        telefon: telefonClient.trim(),
        suma: totalCos,
        produse: produseText,
        status: "noua",
        termen: "",
        data_creare: serverTimestamp(),
      });
      setComandaTrimisa(true);
      setCos([]);
      setNumeClient("");
      setTelefonClient("");
    } catch (err) {
      console.error("Eroare la trimiterea comenzii:", err);
      setEroareComanda("A apărut o eroare. Te rugăm să încerci din nou.");
    } finally {
      setSeTrimite(false);
    }
  }

  function inchideCos() {
    setCosDeschis(false);
    setComandaTrimisa(false);
    setEroareComanda(null);
  }

  return (
    <main className="min-h-screen bg-gray-50">
      {/* Header */}
      <header className="sticky top-0 z-30 bg-white/95 backdrop-blur border-b border-gray-100">
        <div className="h-1 bg-brand-primary" />
        <div className="max-w-6xl mx-auto px-4 sm:px-6">
          <div className="flex flex-wrap items-center gap-3 py-3">
            <div className="flex items-center gap-2 flex-shrink-0">
              <div className="w-8 h-8 rounded-xl bg-brand-primary flex items-center justify-center text-white font-bold text-sm">
                P
              </div>
              <span className="font-semibold text-gray-900 text-lg">{configHeader.logoText}</span>
            </div>

            {configHeader.locatie && (
              <div className="hidden md:flex items-center gap-1.5 text-xs text-gray-500 flex-shrink-0">
                <MapPin className="w-3.5 h-3.5" />
                {configHeader.locatie}
              </div>
            )}

            {(configHeader.telefon || configHeader.program) && (
              <div className="hidden md:flex flex-col flex-shrink-0 leading-tight">
                {configHeader.telefon && (
                  <span className="text-sm font-semibold text-gray-900">{configHeader.telefon}</span>
                )}
                {configHeader.program && <span className="text-xs text-gray-400">{configHeader.program}</span>}
              </div>
            )}

            <div className="hidden sm:block flex-1 min-w-[140px] max-w-xs">
              <div className="relative">
                <input
                  type="text"
                  placeholder="Căutare"
                  className="w-full rounded-xl border border-gray-200 pl-3 pr-9 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand-primary/30 focus:border-brand-primary"
                />
                <Search className="w-4 h-4 absolute right-3 top-1/2 -translate-y-1/2 text-gray-400" />
              </div>
            </div>

            <div className="flex items-center gap-2 ml-auto flex-shrink-0">
              {configHeader.textButonLogin && (
                <button className="hidden sm:inline-flex bg-gray-100 text-gray-700 text-sm font-medium px-4 py-2 rounded-xl hover:bg-gray-200 transition-colors">
                  {configHeader.textButonLogin}
                </button>
              )}
              <button
                onClick={() => setCosDeschis(true)}
                className="relative flex items-center gap-2 bg-gray-900 text-white text-sm font-medium px-4 py-2 rounded-xl hover:bg-gray-800 transition-colors"
              >
                <ShoppingCart className="w-4 h-4" />
                Coș
                {numarItemiCos > 0 && (
                  <span className="absolute -top-2 -right-2 bg-brand-accent text-white text-xs font-semibold w-5 h-5 rounded-full flex items-center justify-center">
                    {numarItemiCos}
                  </span>
                )}
              </button>
            </div>
          </div>

          {configHeader.linkuriMeniu.some((l) => l.vizibil) && (
            <nav className="flex gap-5 overflow-x-auto pb-3 text-sm border-t border-gray-50 pt-2.5">
              {configHeader.linkuriMeniu
                .filter((l) => l.vizibil)
                .map((linkItem) => (
                  <a
                    key={linkItem.id}
                    href={linkItem.link || "#"}
                    className="flex-shrink-0 text-gray-600 hover:text-brand-primary font-medium transition-colors"
                  >
                    {linkItem.eticheta}
                  </a>
                ))}
            </nav>
          )}
        </div>
      </header>

      {/* Blocuri dinamice ale paginii */}
      {seIncarca ? (
        <div className="flex items-center justify-center h-64 text-gray-400">
          <Loader2 className="w-6 h-6 animate-spin mr-2" />
          Se încarcă pagina...
        </div>
      ) : blocuri.length === 0 ? (
        <div className="flex flex-col items-center justify-center h-64 text-gray-400 gap-2">
          <ImageOff className="w-8 h-8" />
          Magazinul este în curs de configurare.
        </div>
      ) : (
        <div className="pb-16">
          {blocuri.map((bloc) => (
            <RandeazaBloc key={bloc.id} bloc={bloc} produseDisponibile={produseDisponibile} onAdaugaInCos={adaugaInCos} />
          ))}
        </div>
      )}

      {/* Coș slide-out */}
      {cosDeschis && (
        <div className="fixed inset-0 z-50">
          <div
            className="absolute inset-0 bg-black/40 animate-in fade-in"
            onClick={inchideCos}
          />
          <div className="absolute right-0 top-0 h-full w-full max-w-md bg-white shadow-xl flex flex-col animate-in slide-in-from-right">
            <div className="flex items-center justify-between px-5 h-16 border-b border-gray-100 flex-shrink-0">
              <h2 className="font-semibold text-gray-900">Coșul tău</h2>
              <button
                onClick={inchideCos}
                className="p-2 rounded-lg hover:bg-gray-100"
                aria-label="Închide"
              >
                <X className="w-5 h-5" />
              </button>
            </div>

            {comandaTrimisa ? (
              <div className="flex-1 flex flex-col items-center justify-center text-center px-8 gap-3 animate-in fade-in">
                <div className="w-14 h-14 rounded-full bg-emerald-50 text-emerald-600 flex items-center justify-center">
                  <CheckCircle2 className="w-7 h-7" />
                </div>
                <p className="font-semibold text-gray-900">Comandă trimisă cu succes!</p>
                <p className="text-sm text-gray-500">Te contactăm în curând pentru confirmare.</p>
                <button
                  onClick={inchideCos}
                  className="mt-4 bg-brand-primary text-white text-sm font-medium px-5 py-2.5 rounded-xl hover:bg-brand-primary-dark transition-colors"
                >
                  Închide
                </button>
              </div>
            ) : (
              <>
                <div className="flex-1 overflow-y-auto px-5 py-4 space-y-3">
                  {cos.length === 0 ? (
                    <div className="flex flex-col items-center justify-center h-full text-gray-400 gap-2">
                      <ShoppingCart className="w-8 h-8" />
                      Coșul este gol.
                    </div>
                  ) : (
                    cos.map((item) => (
                      <div
                        key={item.produsId}
                        className="flex items-center gap-3 bg-gray-50 rounded-2xl p-3"
                      >
                        <div className="w-14 h-14 rounded-xl bg-gray-200 flex-shrink-0 overflow-hidden">
                          {item.imagine ? (
                            // eslint-disable-next-line @next/next/no-img-element
                            <img src={item.imagine} alt={item.nume} className="w-full h-full object-cover" />
                          ) : (
                            <div className="w-full h-full flex items-center justify-center text-gray-400">
                              <ImageOff className="w-5 h-5" />
                            </div>
                          )}
                        </div>
                        <div className="flex-1 min-w-0">
                          <p className="text-sm font-medium text-gray-900 truncate">{item.nume}</p>
                          <p className="text-xs text-gray-500">{formateazaPret(item.pretUnitar)} / buc</p>
                          <div className="flex items-center gap-2 mt-1.5">
                            <button
                              onClick={() => modificaCantitate(item.produsId, -1)}
                              className="w-6 h-6 rounded-lg bg-white border border-gray-200 flex items-center justify-center hover:bg-gray-100"
                              aria-label="Scade cantitatea"
                            >
                              <Minus className="w-3 h-3" />
                            </button>
                            <span className="text-sm font-medium w-4 text-center">{item.cantitate}</span>
                            <button
                              onClick={() => modificaCantitate(item.produsId, 1)}
                              className="w-6 h-6 rounded-lg bg-white border border-gray-200 flex items-center justify-center hover:bg-gray-100"
                              aria-label="Crește cantitatea"
                            >
                              <Plus className="w-3 h-3" />
                            </button>
                          </div>
                        </div>
                        <div className="flex flex-col items-end justify-between self-stretch">
                          <button
                            onClick={() => eliminaDinCos(item.produsId)}
                            className="text-gray-300 hover:text-red-500 transition-colors"
                            aria-label="Elimină produsul"
                          >
                            <Trash2 className="w-4 h-4" />
                          </button>
                          <span className="text-sm font-semibold text-gray-900 whitespace-nowrap">
                            {formateazaPret(item.pretUnitar * item.cantitate)}
                          </span>
                        </div>
                      </div>
                    ))
                  )}
                </div>

                {cos.length > 0 && (
                  <div className="flex-shrink-0 border-t border-gray-100 p-5 space-y-3">
                    <div className="flex items-center justify-between">
                      <span className="text-sm text-gray-500">Total</span>
                      <span className="text-lg font-semibold text-gray-900">{formateazaPret(totalCos)}</span>
                    </div>

                    <input
                      type="text"
                      placeholder="Nume complet"
                      value={numeClient}
                      onChange={(e) => setNumeClient(e.target.value)}
                      className="w-full rounded-xl border border-gray-200 px-3 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-brand-primary/30 focus:border-brand-primary"
                    />
                    <input
                      type="tel"
                      placeholder="Telefon"
                      value={telefonClient}
                      onChange={(e) => setTelefonClient(e.target.value)}
                      className="w-full rounded-xl border border-gray-200 px-3 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-brand-primary/30 focus:border-brand-primary"
                    />

                    {eroareComanda && (
                      <p className="text-xs text-red-600">{eroareComanda}</p>
                    )}

                    <button
                      onClick={trimiteComanda}
                      disabled={seTrimite}
                      className="w-full flex items-center justify-center gap-2 bg-brand-primary text-white font-medium py-3 rounded-xl hover:bg-brand-primary-dark transition-colors disabled:opacity-60"
                    >
                      {seTrimite ? (
                        <Loader2 className="w-4 h-4 animate-spin" />
                      ) : (
                        "Trimite Comanda"
                      )}
                    </button>
                  </div>
                )}
              </>
            )}
          </div>
        </div>
      )}
    </main>
  );
}

// ---------------------------------------------------------------------------
// Randare bloc — dispatch pe tip
// ---------------------------------------------------------------------------

function RandeazaBloc({
  bloc,
  produseDisponibile,
  onAdaugaInCos,
}: {
  bloc: BlocPagina;
  produseDisponibile: Produs[];
  onAdaugaInCos: (produs: Produs) => void;
}) {
  switch (bloc.tip) {
    case "hero":
      return <BlocHero continut={bloc.continut} />;
    case "hero_imagine":
      return <BlocHeroImagine continut={bloc.continut} />;
    case "text_imagine":
      return <BlocTextImagine continut={bloc.continut} />;
    case "carusel_produse":
      return (
        <BlocCaruselProduse
          continut={bloc.continut}
          produseDisponibile={produseDisponibile}
          onAdaugaInCos={onAdaugaInCos}
        />
      );
    case "banner_simplu":
      return <BlocBannerSimplu continut={bloc.continut} />;
    case "carduri_beneficii":
      return <BlocCarduriBeneficii continut={bloc.continut} />;
    case "pasi":
      return <BlocPasi continut={bloc.continut} />;
    default:
      return null;
  }
}

function BlocHero({ continut }: { continut: ContinutHero }) {
  return (
    <section
      className="relative min-h-[420px] flex items-center justify-center text-center px-6 py-20 bg-gray-900 bg-cover bg-center"
      style={continut.imagineBg ? { backgroundImage: `url(${continut.imagineBg})` } : undefined}
    >
      <div className="absolute inset-0 bg-black/40" />
      <div className="relative z-10 max-w-2xl">
        <h1 className="text-3xl sm:text-4xl font-semibold text-white">{continut.titlu}</h1>
        {continut.subtitlu && <p className="mt-3 text-white/80">{continut.subtitlu}</p>}
        {continut.textButon && (
          <a
            href={continut.linkButon || "#"}
            className="inline-flex items-center gap-2 mt-6 bg-brand-accent text-white font-medium px-6 py-3 rounded-2xl hover:brightness-95 transition"
          >
            {continut.textButon}
            <ArrowRight className="w-4 h-4" />
          </a>
        )}
      </div>
    </section>
  );
}

function BlocHeroImagine({ continut }: { continut: ContinutHeroImagine }) {
  return (
    <section className="bg-brand-primary relative overflow-hidden">
      <div className="max-w-6xl mx-auto px-4 sm:px-6 py-16 md:py-24 grid md:grid-cols-2 gap-10 items-center">
        <div className="text-white">
          {continut.eyebrow && (
            <div className="inline-flex items-center gap-2 bg-white/10 rounded-full px-4 py-1.5 text-sm mb-4">
              {continut.eyebrow}
            </div>
          )}
          <h1 className="text-3xl sm:text-5xl font-bold leading-tight">{continut.titlu}</h1>
          {continut.subtitlu && <p className="mt-4 text-white/80 max-w-md">{continut.subtitlu}</p>}
          <div className="mt-6 flex flex-wrap gap-3">
            {continut.textButonPrimar && (
              <a
                href={continut.linkButonPrimar || "#"}
                className="bg-gray-900 text-white px-5 py-3 rounded-2xl font-medium hover:bg-gray-800 transition-colors"
              >
                {continut.textButonPrimar}
              </a>
            )}
            {continut.textButonSecundar && (
              <a
                href={continut.linkButonSecundar || "#"}
                className="bg-brand-accent text-white px-5 py-3 rounded-2xl font-medium hover:brightness-95 transition"
              >
                {continut.textButonSecundar}
              </a>
            )}
          </div>
          {continut.statistici.length > 0 && (
            <div className="mt-10 flex flex-wrap gap-8">
              {continut.statistici.map((stat) => (
                <div key={stat.id}>
                  <p className="text-2xl font-bold text-white">{stat.valoare}</p>
                  <p className="text-xs text-white/60">{stat.eticheta}</p>
                </div>
              ))}
            </div>
          )}
        </div>
        <div className="relative">
          {continut.imagine ? (
            // eslint-disable-next-line @next/next/no-img-element
            <img
              src={continut.imagine}
              alt={continut.titlu}
              className="rounded-[2.5rem] w-full object-cover aspect-[4/5]"
            />
          ) : (
            <div className="rounded-[2.5rem] w-full aspect-[4/5] bg-white/10 flex items-center justify-center text-white/40">
              <ImageOff className="w-10 h-10" />
            </div>
          )}
        </div>
      </div>
    </section>
  );
}

function BlocTextImagine({ continut }: { continut: ContinutTextImagine }) {
  const imagineStanga = continut.pozitieImagine === "stanga";
  return (
    <section className="max-w-6xl mx-auto px-4 sm:px-6 py-12">
      <div className={`flex flex-col ${imagineStanga ? "md:flex-row" : "md:flex-row-reverse"} gap-8 items-center`}>
        <div className="flex-1 w-full aspect-video rounded-2xl overflow-hidden bg-gray-100">
          {continut.imagine ? (
            // eslint-disable-next-line @next/next/no-img-element
            <img src={continut.imagine} alt={continut.titlu} className="w-full h-full object-cover" />
          ) : (
            <div className="w-full h-full flex items-center justify-center text-gray-300">
              <ImageOff className="w-8 h-8" />
            </div>
          )}
        </div>
        <div className="flex-1">
          <h2 className="text-2xl font-semibold text-gray-900">{continut.titlu}</h2>
          <p className="mt-3 text-gray-600 whitespace-pre-line">{continut.text}</p>
        </div>
      </div>
    </section>
  );
}

function BlocCaruselProduse({
  continut,
  produseDisponibile,
  onAdaugaInCos,
}: {
  continut: ContinutCaruselProduse;
  produseDisponibile: Produs[];
  onAdaugaInCos: (produs: Produs) => void;
}) {
  const produseGrup = useMemo(
    () => produseDisponibile.filter((p) => p.group_id === continut.group_id),
    [produseDisponibile, continut.group_id]
  );

  if (produseGrup.length === 0) return null;

  return (
    <section className="max-w-6xl mx-auto px-4 sm:px-6 py-12">
      <h2 className="text-2xl font-semibold text-gray-900 mb-6">{continut.titluSectiune}</h2>
      <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-4 sm:gap-5">
        {produseGrup.map((produs) => (
          <ProdusCard key={produs.id} produs={produs} onAdauga={() => onAdaugaInCos(produs)} />
        ))}
      </div>
    </section>
  );
}

function BlocBannerSimplu({ continut }: { continut: ContinutBannerSimplu }) {
  return (
    <section className="max-w-6xl mx-auto px-4 sm:px-6 py-6">
      <div className="bg-brand-primary rounded-2xl px-6 py-8 flex flex-col sm:flex-row items-center justify-between gap-4 text-white text-center sm:text-left">
        <p className="font-medium text-lg">{continut.text}</p>
        {continut.textButon && (
          <a
            href={continut.linkButon || "#"}
            className="flex-shrink-0 bg-white text-brand-primary font-medium px-5 py-2.5 rounded-xl hover:bg-gray-50 transition-colors"
          >
            {continut.textButon}
          </a>
        )}
      </div>
    </section>
  );
}

function BlocCarduriBeneficii({ continut }: { continut: ContinutCarduriBeneficii }) {
  if (continut.carduri.length === 0) return null;
  return (
    <section className="max-w-6xl mx-auto px-4 sm:px-6 py-16">
      <h2 className="text-2xl sm:text-3xl font-semibold text-center text-gray-900 mb-10">
        {continut.titluSectiune}
      </h2>
      <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-6">
        {continut.carduri.map((card) => (
          <div
            key={card.id}
            className="bg-white rounded-2xl border border-gray-100 shadow-sm hover:shadow-xl transition-shadow p-6"
          >
            <div className="w-11 h-11 rounded-xl bg-brand-primary/10 text-brand-primary flex items-center justify-center mb-4">
              <Sparkles className="w-5 h-5" />
            </div>
            <h3 className="font-semibold text-gray-900">{card.titlu}</h3>
            {card.text && <p className="text-sm text-gray-500 mt-2">{card.text}</p>}
          </div>
        ))}
      </div>
    </section>
  );
}

function BlocPasi({ continut }: { continut: ContinutPasi }) {
  if (continut.pasi.length === 0) return null;
  return (
    <section className="max-w-6xl mx-auto px-4 sm:px-6 py-16">
      <h2 className="text-2xl sm:text-3xl font-semibold text-center text-gray-900 mb-12">
        {continut.titluSectiune}
      </h2>
      <div className={`grid gap-10 items-center ${continut.imagine ? "md:grid-cols-2" : ""}`}>
        <div className="space-y-6">
          {continut.pasi.map((pas, index) => (
            <div key={pas.id} className="flex gap-4">
              <div className="w-9 h-9 rounded-full bg-brand-primary text-white flex items-center justify-center font-semibold flex-shrink-0">
                {index + 1}
              </div>
              <div>
                <h3 className="font-medium text-gray-900">{pas.titlu}</h3>
                {pas.text && <p className="text-sm text-gray-500 mt-1">{pas.text}</p>}
              </div>
            </div>
          ))}
        </div>
        {continut.imagine && (
          <div className="relative">
            {/* eslint-disable-next-line @next/next/no-img-element */}
            <img
              src={continut.imagine}
              alt={continut.titluSectiune}
              className="rounded-[2.5rem] w-full object-cover aspect-square"
            />
          </div>
        )}
      </div>
    </section>
  );
}

// ---------------------------------------------------------------------------
// Card produs
// ---------------------------------------------------------------------------

function ProdusCard({ produs, onAdauga }: { produs: Produs; onAdauga: () => void }) {
  const areReducere = produs.pretRedus !== null && produs.pretRedus < produs.pret;
  const imagine = produs.imagini[0] ?? null;

  return (
    <div className="bg-white rounded-2xl overflow-hidden shadow-sm hover:shadow-xl transition-shadow border border-gray-100 flex flex-col">
      <div className="relative aspect-square bg-gray-100">
        {imagine ? (
          // eslint-disable-next-line @next/next/no-img-element
          <img src={imagine} alt={produs.nume} className="w-full h-full object-cover" />
        ) : (
          <div className="w-full h-full flex items-center justify-center text-gray-300">
            <ImageOff className="w-8 h-8" />
          </div>
        )}
        {areReducere && (
          <span className="absolute top-2 left-2 bg-brand-accent text-white text-xs font-semibold px-2.5 py-1 rounded-full">
            Reducere
          </span>
        )}
      </div>

      <div className="p-3.5 flex flex-col flex-1">
        <p className="text-sm font-medium text-gray-900 line-clamp-2">{produs.nume}</p>

        <div className="mt-auto pt-3 flex items-center justify-between">
          <div className="flex items-baseline gap-1.5">
            <span className="text-sm font-semibold text-brand-primary">
              {formateazaPret(pretEfectiv(produs))}
            </span>
            {areReducere && (
              <span className="text-xs text-gray-400 line-through">
                {formateazaPret(produs.pret)}
              </span>
            )}
          </div>
        </div>

        <button
          onClick={onAdauga}
          className="mt-3 w-full bg-gray-900 text-white text-xs font-medium py-2.5 rounded-xl hover:bg-brand-primary transition-colors"
        >
          Adaugă în coș
        </button>
      </div>
    </div>
  );
}
