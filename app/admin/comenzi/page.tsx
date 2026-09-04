"use client";

import { useEffect, useMemo, useState } from "react";
import {
  collection,
  onSnapshot,
  orderBy,
  query,
  updateDoc,
  doc,
  Timestamp,
} from "firebase/firestore";
import { db } from "@/lib/firebase";
import { ArrowRight, Loader2, Phone, Clock, PackageSearch, CheckCircle2 } from "lucide-react";

// ---------------------------------------------------------------------------
// Tipuri
// ---------------------------------------------------------------------------

type OrderStatus = "noua" | "grafica" | "productie" | "expediata";

interface Comanda {
  id: string;
  client: string;
  telefon: string;
  suma: number;
  produse: string;
  status: OrderStatus;
  termen: string;
  data_creare: Timestamp | null;
}

interface Coloana {
  id: OrderStatus;
  titlu: string;
  accent: string;
  badge: string;
}

// ---------------------------------------------------------------------------
// Configurare coloane Kanban
// ---------------------------------------------------------------------------

const COLOANE: Coloana[] = [
  { id: "noua", titlu: "Comenzi Noi", accent: "bg-blue-500", badge: "bg-blue-50 text-blue-700" },
  { id: "grafica", titlu: "Grafică / DTP", accent: "bg-brand-accent", badge: "bg-amber-50 text-amber-700" },
  { id: "productie", titlu: "În Producție", accent: "bg-brand-primary", badge: "bg-brand-primary/10 text-brand-primary" },
  { id: "expediata", titlu: "Expediate / Gata", accent: "bg-emerald-600", badge: "bg-emerald-50 text-emerald-700" },
];

const URMATORUL_STATUS: Record<OrderStatus, OrderStatus | null> = {
  noua: "grafica",
  grafica: "productie",
  productie: "expediata",
  expediata: null,
};

const ETICHETA_PAS_URMATOR: Record<OrderStatus, string> = {
  noua: "Trimite la Grafică",
  grafica: "Trimite la Producție",
  productie: "Marchează Expediată",
  expediata: "",
};

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

function formateazaData(ts: Timestamp | null): string {
  if (!ts) return "—";
  return ts.toDate().toLocaleDateString("ro-RO", {
    day: "2-digit",
    month: "short",
    hour: "2-digit",
    minute: "2-digit",
  });
}

function formateazaSuma(suma: number): string {
  return new Intl.NumberFormat("ro-RO", { minimumFractionDigits: 0 }).format(suma) + " lei";
}

function initiale(nume: string): string {
  return nume
    .trim()
    .split(/\s+/)
    .slice(0, 2)
    .map((cuvant) => cuvant[0]?.toUpperCase() ?? "")
    .join("");
}

// ---------------------------------------------------------------------------
// Pagina
// ---------------------------------------------------------------------------

export default function ProcesareComenziPage() {
  const [comenzi, setComenzi] = useState<Comanda[]>([]);
  const [seIncarca, setSeIncarca] = useState(true);
  const [eroare, setEroare] = useState<string | null>(null);
  const [idInActualizare, setIdInActualizare] = useState<string | null>(null);

  useEffect(() => {
    const q = query(collection(db, "orders"), orderBy("data_creare", "desc"));

    const unsubscribe = onSnapshot(
      q,
      (snapshot) => {
        const listaComenzi: Comanda[] = snapshot.docs.map((docSnap) => {
          const data = docSnap.data();
          return {
            id: docSnap.id,
            client: data.client ?? "Client necunoscut",
            telefon: data.telefon ?? "",
            suma: typeof data.suma === "number" ? data.suma : 0,
            produse: data.produse ?? "",
            status: (data.status as OrderStatus) ?? "noua",
            termen: data.termen ?? "",
            data_creare: data.data_creare ?? null,
          };
        });
        setComenzi(listaComenzi);
        setSeIncarca(false);
        setEroare(null);
      },
      (err) => {
        console.error("Eroare la citirea comenzilor:", err);
        setEroare("Nu am putut încărca comenzile din Firestore.");
        setSeIncarca(false);
      }
    );

    return () => unsubscribe();
  }, []);

  const comenziPeColoana = useMemo(() => {
    const grupate: Record<OrderStatus, Comanda[]> = {
      noua: [],
      grafica: [],
      productie: [],
      expediata: [],
    };
    for (const comanda of comenzi) {
      grupate[comanda.status]?.push(comanda);
    }
    return grupate;
  }, [comenzi]);

  async function schimbaStatus(id: string, statusCurent: OrderStatus) {
    const statusNou = URMATORUL_STATUS[statusCurent];
    if (!statusNou) return;

    setIdInActualizare(id);
    try {
      await updateDoc(doc(db, "orders", id), { status: statusNou });
    } catch (err) {
      console.error("Eroare la actualizarea statusului comenzii:", err);
      alert("A apărut o eroare la mutarea comenzii. Încearcă din nou.");
    } finally {
      setIdInActualizare(null);
    }
  }

  if (seIncarca) {
    return (
      <div className="flex items-center justify-center h-96 text-gray-400">
        <Loader2 className="w-6 h-6 animate-spin mr-2" />
        Se încarcă comenzile...
      </div>
    );
  }

  if (eroare) {
    return (
      <div className="rounded-2xl bg-red-50 border border-red-100 text-red-700 p-6 text-sm">
        {eroare}
      </div>
    );
  }

  return (
    <div className="animate-in fade-in">
      <div className="mb-6">
        <h1 className="text-2xl font-semibold text-gray-900">Procesare Comenzi</h1>
        <p className="text-sm text-gray-500 mt-1">
          Fluxul de producție, actualizat live din baza de date.
        </p>
      </div>

      <div className="flex gap-5 overflow-x-auto pb-4 -mx-4 px-4 sm:mx-0 sm:px-0">
        {COLOANE.map((coloana) => {
          const comenziColoana = comenziPeColoana[coloana.id];
          return (
            <div
              key={coloana.id}
              className="flex-shrink-0 w-80 bg-gray-100/70 rounded-[2rem] p-3 flex flex-col"
            >
              <div className="flex items-center justify-between px-2 py-2 mb-2">
                <div className="flex items-center gap-2">
                  <span className={`w-2.5 h-2.5 rounded-full ${coloana.accent}`} />
                  <h2 className="font-semibold text-gray-800 text-sm">{coloana.titlu}</h2>
                </div>
                <span className={`text-xs font-medium px-2 py-0.5 rounded-full ${coloana.badge}`}>
                  {comenziColoana.length}
                </span>
              </div>

              <div className="space-y-3 min-h-[120px]">
                {comenziColoana.length === 0 && (
                  <div className="flex flex-col items-center justify-center text-center py-10 text-gray-400 text-xs gap-2">
                    <PackageSearch className="w-6 h-6" />
                    Nicio comandă aici
                  </div>
                )}

                {comenziColoana.map((comanda) => (
                  <ComandaCard
                    key={comanda.id}
                    comanda={comanda}
                    inActualizare={idInActualizare === comanda.id}
                    onUrmatorulPas={() => schimbaStatus(comanda.id, comanda.status)}
                  />
                ))}
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
}

// ---------------------------------------------------------------------------
// Card comandă
// ---------------------------------------------------------------------------

function ComandaCard({
  comanda,
  inActualizare,
  onUrmatorulPas,
}: {
  comanda: Comanda;
  inActualizare: boolean;
  onUrmatorulPas: () => void;
}) {
  const esteFinala = comanda.status === "expediata";

  return (
    <div className="bg-white rounded-2xl p-4 shadow-sm hover:shadow-xl transition-shadow border border-gray-100 animate-in fade-in slide-in-from-right-2">
      <div className="flex items-start justify-between gap-3">
        <div className="flex items-center gap-2.5 min-w-0">
          <div className="w-8 h-8 rounded-full bg-brand-primary/10 text-brand-primary text-xs font-semibold flex items-center justify-center flex-shrink-0">
            {initiale(comanda.client)}
          </div>
          <div className="min-w-0">
            <p className="text-sm font-medium text-gray-900 truncate">{comanda.client}</p>
            {comanda.telefon && (
              <p className="text-xs text-gray-400 flex items-center gap-1">
                <Phone className="w-3 h-3" />
                {comanda.telefon}
              </p>
            )}
          </div>
        </div>
        <span className="text-sm font-semibold text-brand-primary whitespace-nowrap">
          {formateazaSuma(comanda.suma)}
        </span>
      </div>

      {comanda.produse && (
        <p className="text-xs text-gray-500 mt-3 line-clamp-2">{comanda.produse}</p>
      )}

      <div className="flex items-center justify-between mt-3 text-xs text-gray-400">
        <span className="flex items-center gap-1">
          <Clock className="w-3 h-3" />
          {formateazaData(comanda.data_creare)}
        </span>
        {comanda.termen && <span>Termen: {comanda.termen}</span>}
      </div>

      <div className="mt-4">
        {esteFinala ? (
          <div className="flex items-center justify-center gap-1.5 text-emerald-600 bg-emerald-50 rounded-xl py-2 text-xs font-medium">
            <CheckCircle2 className="w-3.5 h-3.5" />
            Finalizată
          </div>
        ) : (
          <button
            onClick={onUrmatorulPas}
            disabled={inActualizare}
            className="w-full flex items-center justify-center gap-1.5 bg-brand-primary text-white rounded-xl py-2 text-xs font-medium hover:bg-brand-primary-dark transition-colors disabled:opacity-60 disabled:cursor-not-allowed"
          >
            {inActualizare ? (
              <Loader2 className="w-3.5 h-3.5 animate-spin" />
            ) : (
              <>
                {ETICHETA_PAS_URMATOR[comanda.status]}
                <ArrowRight className="w-3.5 h-3.5" />
              </>
            )}
          </button>
        )}
      </div>
    </div>
  );
}
