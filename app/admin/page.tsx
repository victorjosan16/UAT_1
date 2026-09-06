"use client";

import { useEffect, useMemo, useState } from "react";
import { collection, onSnapshot, Timestamp } from "firebase/firestore";
import { db } from "@/lib/firebase";
import { DollarSign, FileSearch, Printer, ClipboardCheck, AlertTriangle, Loader2 } from "lucide-react";

// ---------------------------------------------------------------------------
// Tipuri
// ---------------------------------------------------------------------------

type OrderStatus = "noua" | "grafica" | "productie" | "expediata";

interface Comanda {
  id: string;
  client: string;
  suma: number;
  produse: string;
  status: OrderStatus;
  termen: string;
  data_creare: Timestamp | null;
}

const ETICHETA_STATUS: Record<OrderStatus, { eticheta: string; culoare: string }> = {
  noua: { eticheta: "În Așteptare", culoare: "bg-amber-50 text-amber-700" },
  grafica: { eticheta: "Verificare DTP", culoare: "bg-blue-50 text-blue-700" },
  productie: { eticheta: "La Tipar", culoare: "bg-emerald-50 text-emerald-700" },
  expediata: { eticheta: "Expediată", culoare: "bg-gray-100 text-gray-600" },
};

const STATUSURI_VALIDE: OrderStatus[] = ["noua", "grafica", "productie", "expediata"];

function normalizeazaStatus(status: unknown): OrderStatus {
  return STATUSURI_VALIDE.includes(status as OrderStatus) ? (status as OrderStatus) : "noua";
}

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

function formateazaPret(suma: number): string {
  return new Intl.NumberFormat("ro-RO", { minimumFractionDigits: 0 }).format(suma) + " lei";
}

function esteAceeasiZi(a: Date, b: Date): boolean {
  return a.getFullYear() === b.getFullYear() && a.getMonth() === b.getMonth() && a.getDate() === b.getDate();
}

function esteLunaAceasta(data: Date): boolean {
  const azi = new Date();
  return data.getFullYear() === azi.getFullYear() && data.getMonth() === azi.getMonth();
}

// ---------------------------------------------------------------------------
// Pagina
// ---------------------------------------------------------------------------

export default function AdminDashboard() {
  const [comenzi, setComenzi] = useState<Comanda[]>([]);
  const [seIncarca, setSeIncarca] = useState(true);

  useEffect(() => {
    const unsubscribe = onSnapshot(collection(db, "orders"), (snapshot) => {
      const lista: Comanda[] = snapshot.docs.map((docSnap) => {
        const data = docSnap.data();
        return {
          id: docSnap.id,
          client: data.client ?? "Client necunoscut",
          suma: typeof data.suma === "number" ? data.suma : 0,
          produse: data.produse ?? "",
          status: normalizeazaStatus(data.status),
          termen: data.termen ?? "",
          data_creare: data.data_creare ?? null,
        };
      });
      setComenzi(lista);
      setSeIncarca(false);
    });
    return () => unsubscribe();
  }, []);

  const comenziNoiAstazi = useMemo(
    () => comenzi.filter((c) => c.data_creare && esteAceeasiZi(c.data_creare.toDate(), new Date())).length,
    [comenzi]
  );
  const inVerificareGrafica = useMemo(() => comenzi.filter((c) => c.status === "grafica").length, [comenzi]);
  const inProductie = useMemo(() => comenzi.filter((c) => c.status === "productie").length, [comenzi]);
  const venitLunaCurenta = useMemo(
    () =>
      comenzi
        .filter((c) => c.data_creare && esteLunaAceasta(c.data_creare.toDate()))
        .reduce((suma, c) => suma + c.suma, 0),
    [comenzi]
  );

  const KPI = [
    {
      titlu: "Comenzi Noi Astăzi",
      valoare: String(comenziNoiAstazi),
      icon: ClipboardCheck,
      culoare: "text-brand-primary bg-brand-primary/10",
    },
    {
      titlu: "În Verificare Grafică",
      valoare: String(inVerificareGrafica),
      icon: FileSearch,
      culoare: "text-brand-accent bg-brand-accent/10",
    },
    { titlu: "În Producție", valoare: String(inProductie), icon: Printer, culoare: "text-blue-600 bg-blue-50" },
    {
      titlu: "Venituri Luna Curentă",
      valoare: formateazaPret(venitLunaCurenta),
      icon: DollarSign,
      culoare: "text-emerald-600 bg-emerald-50",
    },
  ];

  // Volum comenzi în ultimele 7 zile
  const volumSaptamana = useMemo(() => {
    const zile: { eticheta: string; numar: number }[] = [];
    for (let i = 6; i >= 0; i--) {
      const zi = new Date();
      zi.setDate(zi.getDate() - i);
      const numar = comenzi.filter((c) => c.data_creare && esteAceeasiZi(c.data_creare.toDate(), zi)).length;
      zile.push({ eticheta: zi.toLocaleDateString("ro-RO", { weekday: "short" }), numar });
    }
    return zile;
  }, [comenzi]);

  const maxVolum = Math.max(1, ...volumSaptamana.map((z) => z.numar));

  // Comenzi cu termen apropiat sau depășit — folosim date reale, nu alerte de fișiere fictive
  const comenziUrgente = useMemo(() => {
    const azi = new Date();
    azi.setHours(0, 0, 0, 0);
    return comenzi
      .filter((c) => c.status !== "expediata" && c.termen)
      .filter((c) => {
        const termenData = new Date(c.termen);
        if (Number.isNaN(termenData.getTime())) return false;
        const diffZile = Math.ceil((termenData.getTime() - azi.getTime()) / 86400000);
        return diffZile <= 1;
      })
      .slice(0, 5);
  }, [comenzi]);

  const comenziRecente = useMemo(
    () => [...comenzi].sort((a, b) => (b.data_creare?.toMillis() ?? 0) - (a.data_creare?.toMillis() ?? 0)).slice(0, 8),
    [comenzi]
  );

  if (seIncarca) {
    return (
      <div className="flex items-center justify-center h-64 text-gray-400">
        <Loader2 className="w-6 h-6 animate-spin mr-2" />
        Se încarcă dashboard-ul...
      </div>
    );
  }

  return (
    <div className="space-y-6 animate-in fade-in">
      <div>
        <h1 className="text-2xl font-semibold text-gray-900">Dashboard</h1>
        <p className="text-sm text-gray-500 mt-1">Vedere hibridă: E-commerce + Producție, din date live</p>
      </div>

      <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-4 gap-4">
        {KPI.map((kpi) => {
          const Icon = kpi.icon;
          return (
            <div
              key={kpi.titlu}
              className="bg-white rounded-2xl p-5 shadow-sm hover:shadow-xl transition-shadow border border-gray-100"
            >
              <div className={`w-10 h-10 rounded-xl flex items-center justify-center ${kpi.culoare}`}>
                <Icon className="w-5 h-5" />
              </div>
              <p className="text-sm text-gray-500 mt-4">{kpi.titlu}</p>
              <p className="text-xl font-semibold text-gray-900 mt-1">{kpi.valoare}</p>
            </div>
          );
        })}
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-4">
        <div className="lg:col-span-2 bg-white rounded-2xl p-6 shadow-sm border border-gray-100">
          <h3 className="font-medium text-gray-900 mb-6">Volum comenzi — ultimele 7 zile</h3>
          <div className="flex items-end gap-3 h-40">
            {volumSaptamana.map((zi, index) => (
              <div key={index} className="flex-1 flex flex-col items-center gap-2 h-full justify-end">
                <div
                  className={`w-full rounded-t-lg transition-all ${index === 6 ? "bg-brand-accent" : "bg-brand-primary"}`}
                  style={{ height: `${Math.max((zi.numar / maxVolum) * 100, zi.numar > 0 ? 6 : 2)}%` }}
                />
                <span className="text-xs text-gray-400 capitalize">{zi.eticheta}</span>
              </div>
            ))}
          </div>
        </div>

        <div className="bg-white rounded-2xl p-6 shadow-sm border border-gray-100">
          <div className="flex items-center gap-2 mb-4">
            <AlertTriangle className="w-4 h-4 text-brand-accent" />
            <h3 className="font-medium text-gray-900">Comenzi Urgente</h3>
          </div>
          {comenziUrgente.length === 0 ? (
            <p className="text-sm text-gray-400">Nicio comandă cu termen apropiat.</p>
          ) : (
            <ul className="space-y-3">
              {comenziUrgente.map((c) => (
                <li key={c.id} className="text-sm">
                  <p className="font-medium text-gray-900">{c.client}</p>
                  <p className="text-xs text-gray-500">
                    Termen: {c.termen} · {ETICHETA_STATUS[c.status].eticheta}
                  </p>
                </li>
              ))}
            </ul>
          )}
        </div>
      </div>

      <div className="bg-white rounded-2xl shadow-sm border border-gray-100 overflow-hidden">
        <div className="px-6 py-4 border-b border-gray-100">
          <h3 className="font-medium text-gray-900">Comenzi Recente</h3>
        </div>
        <div className="overflow-x-auto">
          <table className="w-full text-sm">
            <thead>
              <tr className="border-b border-gray-100 text-left text-gray-500">
                <th className="px-6 py-3 font-medium">ID</th>
                <th className="px-6 py-3 font-medium">Client</th>
                <th className="px-6 py-3 font-medium">Produse</th>
                <th className="px-6 py-3 font-medium">Termen</th>
                <th className="px-6 py-3 font-medium">Status</th>
              </tr>
            </thead>
            <tbody>
              {comenziRecente.map((c) => (
                <tr key={c.id} className="border-b border-gray-50 last:border-0 hover:bg-gray-50/60">
                  <td className="px-6 py-3 text-gray-400 font-mono text-xs">#{c.id.slice(0, 6)}</td>
                  <td className="px-6 py-3 font-medium text-gray-900">{c.client}</td>
                  <td className="px-6 py-3 text-gray-600 max-w-[220px] truncate">{c.produse}</td>
                  <td className="px-6 py-3 text-gray-500">{c.termen || "—"}</td>
                  <td className="px-6 py-3">
                    <span
                      className={`text-xs font-medium px-2.5 py-1 rounded-full ${ETICHETA_STATUS[c.status].culoare}`}
                    >
                      {ETICHETA_STATUS[c.status].eticheta}
                    </span>
                  </td>
                </tr>
              ))}
              {comenziRecente.length === 0 && (
                <tr>
                  <td colSpan={5} className="px-6 py-10 text-center text-gray-400">
                    Nicio comandă încă.
                  </td>
                </tr>
              )}
            </tbody>
          </table>
        </div>
      </div>
    </div>
  );
}
