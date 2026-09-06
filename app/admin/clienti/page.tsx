"use client";

import { Fragment, useEffect, useMemo, useState } from "react";
import { collection, doc, onSnapshot, updateDoc, Timestamp } from "firebase/firestore";
import { db } from "@/lib/firebase";
import {
  Users,
  Repeat,
  TrendingUp,
  Wallet,
  Search,
  Loader2,
  Phone,
  Package,
  MessageSquare,
  Mail,
} from "lucide-react";

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

interface Client {
  id: string;
  telefon: string;
  nume: string;
  numarComenzi: number;
  totalCheltuit: number;
  ultimaComanda: Timestamp | null;
  comenzi: Comanda[];
}

interface MesajContact {
  id: string;
  nume: string;
  telefon: string;
  mesaj: string;
  citit: boolean;
  data_creare: Timestamp | null;
}

const STATUSURI_VALIDE: OrderStatus[] = ["noua", "grafica", "productie", "expediata"];

function normalizeazaStatus(status: unknown): OrderStatus {
  return STATUSURI_VALIDE.includes(status as OrderStatus) ? (status as OrderStatus) : "noua";
}

type Tab = "clienti" | "mesaje";

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

function formateazaPret(suma: number): string {
  return new Intl.NumberFormat("ro-RO", { minimumFractionDigits: 0 }).format(suma) + " lei";
}

function formateazaData(ts: Timestamp | null): string {
  if (!ts) return "—";
  return ts.toDate().toLocaleDateString("ro-RO", { day: "2-digit", month: "short", year: "numeric" });
}

// ---------------------------------------------------------------------------
// Pagina
// ---------------------------------------------------------------------------

export default function ClientiPage() {
  const [tabActiv, setTabActiv] = useState<Tab>("clienti");
  const [mesajeNecitite, setMesajeNecitite] = useState(0);

  return (
    <div className="animate-in fade-in">
      <div className="mb-6">
        <h1 className="text-2xl font-semibold text-gray-900">CRM & Clienți</h1>
        <p className="text-sm text-gray-500 mt-1">Clienți agregați din comenzi și mesaje primite prin site.</p>
      </div>

      <div className="flex gap-1 border-b border-gray-200 mb-6">
        <button
          onClick={() => setTabActiv("clienti")}
          className={`flex items-center gap-2 px-4 py-2.5 text-sm font-medium border-b-2 -mb-px transition-colors ${
            tabActiv === "clienti"
              ? "border-brand-primary text-brand-primary"
              : "border-transparent text-gray-500 hover:text-gray-800"
          }`}
        >
          <Users className="w-4 h-4" />
          Clienți
        </button>
        <button
          onClick={() => setTabActiv("mesaje")}
          className={`relative flex items-center gap-2 px-4 py-2.5 text-sm font-medium border-b-2 -mb-px transition-colors ${
            tabActiv === "mesaje"
              ? "border-brand-primary text-brand-primary"
              : "border-transparent text-gray-500 hover:text-gray-800"
          }`}
        >
          <MessageSquare className="w-4 h-4" />
          Mesaje Contact
          {mesajeNecitite > 0 && (
            <span className="bg-brand-accent text-white text-xs font-semibold w-5 h-5 rounded-full flex items-center justify-center">
              {mesajeNecitite}
            </span>
          )}
        </button>
      </div>

      {tabActiv === "clienti" ? <TabClienti /> : <TabMesajeContact onMesajeNecititeChange={setMesajeNecitite} />}
    </div>
  );
}

// ---------------------------------------------------------------------------
// Tab: Clienți
// ---------------------------------------------------------------------------

function TabClienti() {
  const [comenzi, setComenzi] = useState<Comanda[]>([]);
  const [seIncarca, setSeIncarca] = useState(true);
  const [cautare, setCautare] = useState("");
  const [clientExtins, setClientExtins] = useState<string | null>(null);

  useEffect(() => {
    const unsubscribe = onSnapshot(collection(db, "orders"), (snapshot) => {
      const lista: Comanda[] = snapshot.docs.map((docSnap) => {
        const data = docSnap.data();
        return {
          id: docSnap.id,
          client: data.client ?? "Client necunoscut",
          telefon: data.telefon ?? "",
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

  // Agregă comenzile în clienți unici — cheia e telefonul (cel mai fiabil
  // identificator pe care îl avem, din moment ce nu există cont de client).
  const clienti = useMemo(() => {
    const harta = new Map<string, Client>();
    for (const comanda of comenzi) {
      const cheie = comanda.telefon.trim() || `fara-telefon-${comanda.id}`;
      const existent = harta.get(cheie);
      if (existent) {
        existent.numarComenzi += 1;
        existent.totalCheltuit += comanda.suma;
        existent.comenzi.push(comanda);
        const dataNoua = comanda.data_creare?.toMillis() ?? 0;
        const dataExistenta = existent.ultimaComanda?.toMillis() ?? 0;
        if (dataNoua > dataExistenta) {
          existent.ultimaComanda = comanda.data_creare;
          existent.nume = comanda.client;
        }
      } else {
        harta.set(cheie, {
          id: cheie,
          telefon: comanda.telefon,
          nume: comanda.client,
          numarComenzi: 1,
          totalCheltuit: comanda.suma,
          ultimaComanda: comanda.data_creare,
          comenzi: [comanda],
        });
      }
    }
    return Array.from(harta.values()).sort(
      (a, b) => (b.ultimaComanda?.toMillis() ?? 0) - (a.ultimaComanda?.toMillis() ?? 0)
    );
  }, [comenzi]);

  const clientiFiltrati = useMemo(() => {
    const interogare = cautare.trim().toLowerCase();
    if (!interogare) return clienti;
    return clienti.filter(
      (c) => c.nume.toLowerCase().includes(interogare) || c.telefon.toLowerCase().includes(interogare)
    );
  }, [clienti, cautare]);

  const clientiRecurenti = useMemo(() => clienti.filter((c) => c.numarComenzi > 1).length, [clienti]);
  const valoareMedieComanda = useMemo(
    () => (comenzi.length === 0 ? 0 : comenzi.reduce((suma, c) => suma + c.suma, 0) / comenzi.length),
    [comenzi]
  );
  const venitTotal = useMemo(() => comenzi.reduce((suma, c) => suma + c.suma, 0), [comenzi]);

  const KPI = [
    {
      titlu: "Total Clienți",
      valoare: String(clienti.length),
      icon: Users,
      culoare: "text-brand-primary bg-brand-primary/10",
    },
    {
      titlu: "Clienți Recurenți",
      valoare: String(clientiRecurenti),
      icon: Repeat,
      culoare: "text-brand-accent bg-brand-accent/10",
    },
    {
      titlu: "Valoare Medie Comandă",
      valoare: formateazaPret(valoareMedieComanda),
      icon: TrendingUp,
      culoare: "text-blue-600 bg-blue-50",
    },
    {
      titlu: "Venit Total (all-time)",
      valoare: formateazaPret(venitTotal),
      icon: Wallet,
      culoare: "text-emerald-600 bg-emerald-50",
    },
  ];

  if (seIncarca) {
    return (
      <div className="flex items-center justify-center h-64 text-gray-400">
        <Loader2 className="w-6 h-6 animate-spin mr-2" />
        Se încarcă clienții...
      </div>
    );
  }

  return (
    <div className="space-y-6">
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

      <div className="bg-white rounded-2xl shadow-sm border border-gray-100 overflow-hidden">
        <div className="px-6 py-4 border-b border-gray-100">
          <div className="relative max-w-xs">
            <Search className="w-4 h-4 absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" />
            <input
              type="text"
              value={cautare}
              onChange={(e) => setCautare(e.target.value)}
              placeholder="Caută după nume sau telefon..."
              className="w-full rounded-xl border border-gray-200 pl-9 pr-3 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand-primary/30 focus:border-brand-primary"
            />
          </div>
        </div>

        {clientiFiltrati.length === 0 ? (
          <div className="p-12 text-center text-gray-400">
            {clienti.length === 0 ? "Niciun client încă — apar automat după prima comandă." : "Niciun client găsit."}
          </div>
        ) : (
          <div className="overflow-x-auto">
            <table className="w-full text-sm">
              <thead>
                <tr className="border-b border-gray-100 text-left text-gray-500">
                  <th className="px-6 py-3 font-medium">Client</th>
                  <th className="px-6 py-3 font-medium">Telefon</th>
                  <th className="px-6 py-3 font-medium">Nr. Comenzi</th>
                  <th className="px-6 py-3 font-medium">Total Cheltuit</th>
                  <th className="px-6 py-3 font-medium">Ultima Comandă</th>
                </tr>
              </thead>
              <tbody>
                {clientiFiltrati.map((client) => {
                  const extins = clientExtins === client.id;
                  return (
                    <Fragment key={client.id}>
                      <tr
                        onClick={() => setClientExtins(extins ? null : client.id)}
                        className="border-b border-gray-50 last:border-0 hover:bg-gray-50/60 cursor-pointer"
                      >
                        <td className="px-6 py-3">
                          <div className="flex items-center gap-3">
                            <div className="w-8 h-8 rounded-full bg-brand-primary/10 text-brand-primary text-xs font-semibold flex items-center justify-center flex-shrink-0">
                              {client.nume.slice(0, 1).toUpperCase()}
                            </div>
                            <span className="font-medium text-gray-900">{client.nume}</span>
                          </div>
                        </td>
                        <td className="px-6 py-3 text-gray-600">
                          {client.telefon ? (
                            <span className="flex items-center gap-1.5">
                              <Phone className="w-3.5 h-3.5 text-gray-400" />
                              {client.telefon}
                            </span>
                          ) : (
                            "—"
                          )}
                        </td>
                        <td className="px-6 py-3">
                          <span
                            className={`text-xs font-medium px-2.5 py-1 rounded-full ${
                              client.numarComenzi > 1
                                ? "bg-brand-primary/10 text-brand-primary"
                                : "bg-gray-100 text-gray-500"
                            }`}
                          >
                            {client.numarComenzi} {client.numarComenzi === 1 ? "comandă" : "comenzi"}
                          </span>
                        </td>
                        <td className="px-6 py-3 font-medium text-gray-900">{formateazaPret(client.totalCheltuit)}</td>
                        <td className="px-6 py-3 text-gray-500">{formateazaData(client.ultimaComanda)}</td>
                      </tr>
                      {extins && (
                        <tr className="bg-gray-50/60">
                          <td colSpan={5} className="px-6 py-4">
                            <p className="text-xs font-medium text-gray-500 mb-2 flex items-center gap-1.5">
                              <Package className="w-3.5 h-3.5" />
                              Istoric comenzi
                            </p>
                            <ul className="space-y-1.5">
                              {[...client.comenzi]
                                .sort((a, b) => (b.data_creare?.toMillis() ?? 0) - (a.data_creare?.toMillis() ?? 0))
                                .map((comanda) => (
                                  <li
                                    key={comanda.id}
                                    className="text-xs text-gray-600 flex items-center justify-between gap-3"
                                  >
                                    <span className="truncate">{comanda.produse || "—"}</span>
                                    <span className="text-gray-400 flex-shrink-0">
                                      {formateazaData(comanda.data_creare)} · {formateazaPret(comanda.suma)}
                                    </span>
                                  </li>
                                ))}
                            </ul>
                          </td>
                        </tr>
                      )}
                    </Fragment>
                  );
                })}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  );
}

// ---------------------------------------------------------------------------
// Tab: Mesaje Contact
// ---------------------------------------------------------------------------

function TabMesajeContact({ onMesajeNecititeChange }: { onMesajeNecititeChange: (numar: number) => void }) {
  const [mesaje, setMesaje] = useState<MesajContact[]>([]);
  const [seIncarca, setSeIncarca] = useState(true);
  const [mesajExtins, setMesajExtins] = useState<string | null>(null);

  useEffect(() => {
    const unsubscribe = onSnapshot(collection(db, "contact_mesaje"), (snapshot) => {
      const lista: MesajContact[] = snapshot.docs
        .map((docSnap) => {
          const data = docSnap.data();
          return {
            id: docSnap.id,
            nume: data.nume ?? "Necunoscut",
            telefon: data.telefon ?? "",
            mesaj: data.mesaj ?? "",
            citit: data.citit === true,
            data_creare: data.data_creare ?? null,
          };
        })
        .sort((a, b) => (b.data_creare?.toMillis() ?? 0) - (a.data_creare?.toMillis() ?? 0));
      setMesaje(lista);
      setSeIncarca(false);
      onMesajeNecititeChange(lista.filter((m) => !m.citit).length);
    });
    return () => unsubscribe();
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  async function deschideMesaj(mesaj: MesajContact) {
    setMesajExtins(mesajExtins === mesaj.id ? null : mesaj.id);
    if (!mesaj.citit) {
      try {
        await updateDoc(doc(db, "contact_mesaje", mesaj.id), { citit: true });
      } catch (err) {
        console.error("Eroare la marcarea mesajului ca citit:", err);
      }
    }
  }

  if (seIncarca) {
    return (
      <div className="flex items-center justify-center h-64 text-gray-400">
        <Loader2 className="w-6 h-6 animate-spin mr-2" />
        Se încarcă mesajele...
      </div>
    );
  }

  if (mesaje.length === 0) {
    return (
      <div className="bg-white rounded-2xl border border-gray-100 p-12 text-center text-gray-400">
        Niciun mesaj primit încă — apar aici automat din formularul de Contact al site-ului.
      </div>
    );
  }

  return (
    <div className="space-y-3">
      {mesaje.map((mesaj) => {
        const extins = mesajExtins === mesaj.id;
        return (
          <div
            key={mesaj.id}
            onClick={() => deschideMesaj(mesaj)}
            className={`bg-white rounded-2xl border shadow-sm hover:shadow-xl transition-shadow p-4 cursor-pointer ${
              mesaj.citit ? "border-gray-100" : "border-brand-primary/30"
            }`}
          >
            <div className="flex items-start justify-between gap-3">
              <div className="flex items-center gap-3 min-w-0">
                <div className="w-9 h-9 rounded-full bg-brand-primary/10 text-brand-primary flex items-center justify-center flex-shrink-0">
                  <Mail className="w-4 h-4" />
                </div>
                <div className="min-w-0">
                  <div className="flex items-center gap-2">
                    <p className="font-medium text-gray-900 truncate">{mesaj.nume}</p>
                    {!mesaj.citit && <span className="w-2 h-2 rounded-full bg-brand-accent flex-shrink-0" />}
                  </div>
                  <p className="text-xs text-gray-400 flex items-center gap-1">
                    <Phone className="w-3 h-3" />
                    {mesaj.telefon || "—"}
                  </p>
                </div>
              </div>
              <span className="text-xs text-gray-400 flex-shrink-0">{formateazaData(mesaj.data_creare)}</span>
            </div>
            <p className={`text-sm text-gray-600 mt-3 ${extins ? "" : "line-clamp-2"}`}>{mesaj.mesaj}</p>
          </div>
        );
      })}
    </div>
  );
}
