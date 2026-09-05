"use client";

import { useEffect, useMemo, useState } from "react";
import Image from "next/image";
import { collection, onSnapshot } from "firebase/firestore";
import { db } from "@/lib/firebase";
import { useSite, type Produs } from "@/lib/site-context";
import { ProdusCard } from "@/components/ProdusCard";
import {
  Loader2,
  ImageOff,
  ArrowRight,
  Sparkles,
  Headphones,
  BookOpen,
  Monitor,
  Truck,
  ShieldCheck,
  Clock,
  PlayCircle,
  Star,
  Handshake,
} from "lucide-react";

const HARTA_ICOANE_CARD: Record<string, typeof Sparkles> = {
  sparkles: Sparkles,
  headphones: Headphones,
  book: BookOpen,
  monitor: Monitor,
  truck: Truck,
  shield: ShieldCheck,
  clock: Clock,
  handshake: Handshake,
};

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
  icon: string;
  link: string;
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
  statistici: StatisticaHero[];
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
// Completează câmpurile lipsă din documente vechi (create înainte ca un tip
// de bloc să primească un câmp nou), ca randarea să nu crape pe date vechi.
// ---------------------------------------------------------------------------

function normalizeazaContinut(tip: TipBloc, continut: Record<string, unknown> | undefined): BlocPagina["continut"] {
  const c = continut ?? {};
  switch (tip) {
    case "hero":
      return {
        titlu: (c.titlu as string) ?? "",
        subtitlu: (c.subtitlu as string) ?? "",
        imagineBg: (c.imagineBg as string) ?? "",
        textButon: (c.textButon as string) ?? "",
        linkButon: (c.linkButon as string) ?? "",
      };
    case "hero_imagine":
      return {
        eyebrow: (c.eyebrow as string) ?? "",
        titlu: (c.titlu as string) ?? "",
        subtitlu: (c.subtitlu as string) ?? "",
        imagine: (c.imagine as string) ?? "",
        textButonPrimar: (c.textButonPrimar as string) ?? "",
        linkButonPrimar: (c.linkButonPrimar as string) ?? "",
        textButonSecundar: (c.textButonSecundar as string) ?? "",
        linkButonSecundar: (c.linkButonSecundar as string) ?? "",
        statistici: Array.isArray(c.statistici) ? (c.statistici as StatisticaHero[]) : [],
      };
    case "text_imagine":
      return {
        titlu: (c.titlu as string) ?? "",
        text: (c.text as string) ?? "",
        imagine: (c.imagine as string) ?? "",
        pozitieImagine: c.pozitieImagine === "stanga" ? "stanga" : "dreapta",
      };
    case "carusel_produse":
      return {
        titluSectiune: (c.titluSectiune as string) ?? "",
        group_id: (c.group_id as string) ?? "",
      };
    case "banner_simplu":
      return {
        text: (c.text as string) ?? "",
        textButon: (c.textButon as string) ?? "",
        linkButon: (c.linkButon as string) ?? "",
      };
    case "carduri_beneficii":
      return {
        titluSectiune: (c.titluSectiune as string) ?? "",
        carduri: Array.isArray(c.carduri) ? (c.carduri as CardBeneficiu[]) : [],
      };
    case "pasi":
      return {
        titluSectiune: (c.titluSectiune as string) ?? "",
        imagine: (c.imagine as string) ?? "",
        pasi: Array.isArray(c.pasi) ? (c.pasi as PasItem[]) : [],
        statistici: Array.isArray(c.statistici) ? (c.statistici as StatisticaHero[]) : [],
      };
  }
}

const TIPURI_VALIDE: TipBloc[] = [
  "hero",
  "hero_imagine",
  "text_imagine",
  "carusel_produse",
  "banner_simplu",
  "carduri_beneficii",
  "pasi",
];

// ---------------------------------------------------------------------------
// Pagina
// ---------------------------------------------------------------------------

export default function AcasaPage() {
  const [blocuri, setBlocuri] = useState<BlocPagina[]>([]);
  const [seIncarca, setSeIncarca] = useState(true);

  useEffect(() => {
    const unsubscribe = onSnapshot(collection(db, "homepage_blocks"), (snapshot) => {
      const lista: BlocPagina[] = snapshot.docs
        .filter((docSnap) => TIPURI_VALIDE.includes(docSnap.data().tip))
        .map((docSnap) => {
          const data = docSnap.data();
          return {
            id: docSnap.id,
            tip: data.tip,
            ordine: typeof data.ordine === "number" ? data.ordine : 0,
            vizibil: data.vizibil !== false,
            continut: normalizeazaContinut(data.tip, data.continut),
          } as BlocPagina;
        })
        .filter((bloc) => bloc.vizibil)
        .sort((a, b) => a.ordine - b.ordine);
      setBlocuri(lista);
      setSeIncarca(false);
    });
    return () => unsubscribe();
  }, []);

  if (seIncarca) {
    return (
      <div className="flex items-center justify-center h-64 text-gray-400">
        <Loader2 className="w-6 h-6 animate-spin mr-2" />
        Se încarcă pagina...
      </div>
    );
  }

  if (blocuri.length === 0) {
    return (
      <div className="flex flex-col items-center justify-center h-64 text-gray-400 gap-2">
        <ImageOff className="w-8 h-8" />
        Magazinul este în curs de configurare.
      </div>
    );
  }

  return (
    <div>
      {blocuri.map((bloc) => (
        <RandeazaBloc key={bloc.id} bloc={bloc} />
      ))}
    </div>
  );
}

// ---------------------------------------------------------------------------
// Randare bloc — dispatch pe tip
// ---------------------------------------------------------------------------

function RandeazaBloc({ bloc }: { bloc: BlocPagina }) {
  switch (bloc.tip) {
    case "hero":
      return <BlocHero continut={bloc.continut} />;
    case "hero_imagine":
      return <BlocHeroImagine continut={bloc.continut} />;
    case "text_imagine":
      return <BlocTextImagine continut={bloc.continut} />;
    case "carusel_produse":
      return <BlocCaruselProduse continut={bloc.continut} />;
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
    <section className="bg-brand-primary relative overflow-hidden rounded-b-[2.5rem] sm:rounded-b-[4rem]">
      <Star className="hidden sm:block absolute top-10 right-[38%] w-5 h-5 text-brand-accent/70 -rotate-12" />
      <Sparkles className="hidden sm:block absolute bottom-16 right-[8%] w-6 h-6 text-white/40 rotate-12" />

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
                className="flex items-center gap-2 bg-brand-accent text-white px-5 py-3 rounded-2xl font-medium hover:brightness-95 transition"
              >
                <PlayCircle className="w-4 h-4" />
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
          <div className="absolute -inset-4 bg-brand-accent/20 rounded-[3rem] -rotate-3 -z-0" />
          {continut.imagine ? (
            <div className="relative rounded-[2.5rem] w-full aspect-[4/5] overflow-hidden">
              <Image
                src={continut.imagine}
                alt={continut.titlu}
                fill
                sizes="(max-width: 768px) 100vw, 50vw"
                className="object-cover"
              />
            </div>
          ) : (
            <div className="relative rounded-[2.5rem] w-full aspect-[4/5] bg-white/10 flex items-center justify-center text-white/40">
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
        <div className="relative flex-1 w-full aspect-video rounded-2xl overflow-hidden bg-gray-100">
          {continut.imagine ? (
            <Image
              src={continut.imagine}
              alt={continut.titlu}
              fill
              sizes="(max-width: 768px) 100vw, 50vw"
              className="object-cover"
            />
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

function BlocCaruselProduse({ continut }: { continut: ContinutCaruselProduse }) {
  const { produseDisponibile } = useSite();
  const produseGrup = useMemo(
    () => produseDisponibile.filter((p: Produs) => p.group_id === continut.group_id),
    [produseDisponibile, continut.group_id]
  );

  if (produseGrup.length === 0) return null;

  return (
    <section id="produse" className="max-w-6xl mx-auto px-4 sm:px-6 py-12 scroll-mt-32">
      <h2 className="text-2xl font-semibold text-gray-900 mb-6">{continut.titluSectiune}</h2>
      <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-4 sm:gap-5">
        {produseGrup.map((produs) => (
          <ProdusCard key={produs.id} produs={produs} />
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
    <section id="despre" className="max-w-6xl mx-auto px-4 sm:px-6 py-16 scroll-mt-32">
      <h2 className="text-2xl sm:text-3xl font-semibold text-center text-gray-900 mb-10">
        {continut.titluSectiune}
      </h2>
      <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-6">
        {continut.carduri.map((card) => {
          const Icon = HARTA_ICOANE_CARD[card.icon] ?? Sparkles;
          return (
            <div
              key={card.id}
              className="bg-white border border-gray-100 shadow-sm hover:shadow-xl transition-shadow p-6"
              style={{ clipPath: "polygon(0 0, calc(100% - 28px) 0, 100% 28px, 100% 100%, 0 100%)" }}
            >
              <div className="w-11 h-11 rounded-xl bg-brand-primary/10 text-brand-primary flex items-center justify-center mb-4">
                <Icon className="w-5 h-5" />
              </div>
              <h3 className="font-semibold text-gray-900">{card.titlu}</h3>
              {card.text && <p className="text-sm text-gray-500 mt-2">{card.text}</p>}
              {card.link && (
                <a
                  href={card.link}
                  className="inline-flex items-center gap-1.5 text-sm font-medium text-brand-primary mt-4 hover:underline"
                >
                  Citește mai mult
                  <ArrowRight className="w-3.5 h-3.5" />
                </a>
              )}
            </div>
          );
        })}
      </div>
    </section>
  );
}

function BlocPasi({ continut }: { continut: ContinutPasi }) {
  if (continut.pasi.length === 0) return null;
  const pozitiiInsigne = ["top-4 -right-6", "bottom-8 -left-8", "top-1/3 -left-10", "bottom-1/4 -right-8"];
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
          <div className="relative mx-auto w-full max-w-sm aspect-square">
            <Image
              src={continut.imagine}
              alt={continut.titluSectiune}
              fill
              sizes="(max-width: 640px) 100vw, 384px"
              className="object-cover rounded-full"
            />
            {continut.statistici.map((stat, index) => (
              <div
                key={stat.id}
                className={`absolute ${pozitiiInsigne[index % pozitiiInsigne.length]} bg-white rounded-2xl shadow-lg px-4 py-2.5`}
              >
                <p className="text-sm font-bold text-gray-900">{stat.valoare}</p>
                <p className="text-[10px] text-gray-500">{stat.eticheta}</p>
              </div>
            ))}
          </div>
        )}
      </div>
    </section>
  );
}
