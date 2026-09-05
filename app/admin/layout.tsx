"use client";

import Link from "next/link";
import { usePathname, useRouter } from "next/navigation";
import {
  LayoutDashboard,
  Package,
  ClipboardList,
  Users,
  Settings,
  LayoutTemplate,
  Menu,
  X,
  Search,
  Bell,
  Plus,
  Loader2,
  LogOut,
} from "lucide-react";
import { useEffect, useMemo, useRef, useState } from "react";
import { collection, addDoc, onSnapshot, serverTimestamp, Timestamp } from "firebase/firestore";
import { signOut } from "firebase/auth";
import { db, getFirebaseAuth } from "@/lib/firebase";
import { useAdminAuth } from "@/lib/use-admin-auth";

interface NotificarePreview {
  id: string;
  tip: "comanda" | "mesaj";
  titlu: string;
  detaliu: string;
  data: Timestamp | null;
  href: string;
}

function formateazaOraRelativa(ts: Timestamp | null): string {
  if (!ts) return "";
  const diffMs = Date.now() - ts.toDate().getTime();
  const minute = Math.floor(diffMs / 60000);
  if (minute < 1) return "acum";
  if (minute < 60) return `acum ${minute} min`;
  const ore = Math.floor(minute / 60);
  if (ore < 24) return `acum ${ore} h`;
  const zile = Math.floor(ore / 24);
  return `acum ${zile} zile`;
}

const NAV = [
  { href: "/admin", label: "Dashboard", icon: LayoutDashboard },
  { href: "/admin/catalog", label: "Catalog Produse", icon: Package },
  { href: "/admin/storefront", label: "Constructor Pagină", icon: LayoutTemplate },
  { href: "/admin/comenzi", label: "Procesare Comenzi", icon: ClipboardList },
  { href: "/admin/clienti", label: "CRM & Clienți", icon: Users },
  { href: "/admin/setari", label: "Setări", icon: Settings },
];

export default function AdminLayout({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  const router = useRouter();
  const { user, seIncarca } = useAdminAuth();
  const [menuDeschis, setMenuDeschis] = useState(false);
  const [modalComandaDeschis, setModalComandaDeschis] = useState(false);
  const [textCautare, setTextCautare] = useState("");
  const [notificariDeschise, setNotificariDeschise] = useState(false);
  const [comenziNoi, setComenziNoi] = useState<NotificarePreview[]>([]);
  const [mesajeNecitite, setMesajeNecitite] = useState<NotificarePreview[]>([]);
  const notificariRef = useRef<HTMLDivElement>(null);

  const esteLogin = pathname === "/admin/login";

  function cauta(e: React.FormEvent) {
    e.preventDefault();
    const termen = textCautare.trim();
    if (!termen) return;
    router.push(`/admin/comenzi?cauta=${encodeURIComponent(termen)}`);
  }

  useEffect(() => {
    if (!esteLogin && !seIncarca && !user) {
      router.replace("/admin/login");
    }
  }, [esteLogin, seIncarca, user, router]);

  useEffect(() => {
    if (esteLogin || !user) return;

    const unsubComenzi = onSnapshot(collection(db, "orders"), (snapshot) => {
      const lista: NotificarePreview[] = snapshot.docs
        .filter((docSnap) => {
          const status = docSnap.data().status;
          return !status || status === "noua";
        })
        .map((docSnap) => {
          const data = docSnap.data();
          return {
            id: docSnap.id,
            tip: "comanda" as const,
            titlu: data.client ?? "Client necunoscut",
            detaliu: data.produse ?? "Comandă nouă",
            data: data.data_creare ?? null,
            href: "/admin/comenzi",
          };
        })
        .sort((a, b) => (b.data?.toMillis() ?? 0) - (a.data?.toMillis() ?? 0));
      setComenziNoi(lista);
    });

    const unsubMesaje = onSnapshot(collection(db, "contact_mesaje"), (snapshot) => {
      const lista: NotificarePreview[] = snapshot.docs
        .filter((docSnap) => docSnap.data().citit !== true)
        .map((docSnap) => {
          const data = docSnap.data();
          return {
            id: docSnap.id,
            tip: "mesaj" as const,
            titlu: data.nume ?? "Necunoscut",
            detaliu: data.mesaj ?? "Mesaj nou de contact",
            data: data.data_creare ?? null,
            href: "/admin/clienti",
          };
        })
        .sort((a, b) => (b.data?.toMillis() ?? 0) - (a.data?.toMillis() ?? 0));
      setMesajeNecitite(lista);
    });

    return () => {
      unsubComenzi();
      unsubMesaje();
    };
  }, [esteLogin, user]);

  useEffect(() => {
    if (!notificariDeschise) return;
    function pePeClicExterior(e: MouseEvent) {
      if (notificariRef.current && !notificariRef.current.contains(e.target as Node)) {
        setNotificariDeschise(false);
      }
    }
    document.addEventListener("mousedown", pePeClicExterior);
    return () => document.removeEventListener("mousedown", pePeClicExterior);
  }, [notificariDeschise]);

  const notificari = useMemo(
    () =>
      [...comenziNoi, ...mesajeNecitite].sort(
        (a, b) => (b.data?.toMillis() ?? 0) - (a.data?.toMillis() ?? 0)
      ),
    [comenziNoi, mesajeNecitite]
  );
  const numarNotificari = notificari.length;

  async function delogheaza() {
    await signOut(getFirebaseAuth());
    router.replace("/admin/login");
  }

  if (esteLogin) {
    return <>{children}</>;
  }

  if (seIncarca || !user) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-50">
        <Loader2 className="w-6 h-6 animate-spin text-gray-400" />
      </div>
    );
  }

  return (
    <div className="min-h-screen bg-gray-50 lg:flex">
      {/* Sidebar - desktop */}
      <aside className="hidden lg:flex lg:w-64 lg:flex-col lg:fixed lg:inset-y-0 bg-brand-primary">
        <div className="flex items-center gap-2.5 px-6 h-16 border-b border-white/10">
          <span className="w-2 h-2 rounded-full bg-brand-accent flex-shrink-0" />
          <span className="font-semibold text-white tracking-tight">
            PosterART <span className="text-white/50 font-normal">Admin</span>
          </span>
        </div>
        <nav className="flex-1 px-3 py-4 space-y-1">
          {NAV.map((item) => {
            const activ = item.href === "/admin" ? pathname === item.href : pathname?.startsWith(item.href);
            const Icon = item.icon;
            return (
              <Link
                key={item.href}
                href={item.href}
                className={`flex items-center gap-3 px-3 py-2.5 rounded-xl text-sm font-medium transition-colors border-l-2 ${
                  activ
                    ? "bg-white/10 text-white border-brand-accent"
                    : "text-white/70 border-transparent hover:bg-white/5 hover:text-white"
                }`}
              >
                <Icon className="w-4 h-4" />
                {item.label}
              </Link>
            );
          })}
        </nav>
        <div className="p-4 border-t border-white/10 flex items-center gap-3">
          <div className="w-9 h-9 rounded-full bg-brand-accent flex items-center justify-center text-white text-sm font-semibold flex-shrink-0">
            {(user.email ?? "A").slice(0, 1).toUpperCase()}
          </div>
          <div className="min-w-0 flex-1">
            <p className="text-sm font-medium text-white truncate">{user.email}</p>
            <p className="text-xs text-white/50">Super Admin</p>
          </div>
          <button
            onClick={delogheaza}
            className="p-1.5 rounded-lg text-white/60 hover:text-white hover:bg-white/10 transition-colors flex-shrink-0"
            aria-label="Deconectare"
            title="Deconectare"
          >
            <LogOut className="w-4 h-4" />
          </button>
        </div>
      </aside>

      {/* Sidebar - mobile */}
      <div className="lg:hidden flex items-center justify-between px-4 h-16 bg-brand-primary sticky top-0 z-30">
        <div className="flex items-center gap-2.5">
          <span className="w-2 h-2 rounded-full bg-brand-accent flex-shrink-0" />
          <span className="font-semibold text-white">
            PosterART <span className="text-white/50 font-normal">Admin</span>
          </span>
        </div>
        <button
          onClick={() => setMenuDeschis(!menuDeschis)}
          className="p-2 rounded-lg text-white hover:bg-white/10"
          aria-label="Meniu"
        >
          {menuDeschis ? <X className="w-5 h-5" /> : <Menu className="w-5 h-5" />}
        </button>
      </div>
      {menuDeschis && (
        <div className="lg:hidden bg-brand-primary px-3 py-2 space-y-1 animate-in fade-in slide-in-from-top-2">
          {NAV.map((item) => {
            const Icon = item.icon;
            return (
              <Link
                key={item.href}
                href={item.href}
                onClick={() => setMenuDeschis(false)}
                className="flex items-center gap-3 px-3 py-2.5 rounded-xl text-sm font-medium text-white/80 hover:bg-white/10"
              >
                <Icon className="w-4 h-4" />
                {item.label}
              </Link>
            );
          })}
          <button
            onClick={delogheaza}
            className="w-full flex items-center gap-3 px-3 py-2.5 rounded-xl text-sm font-medium text-white/80 hover:bg-white/10"
          >
            <LogOut className="w-4 h-4" />
            Deconectare
          </button>
        </div>
      )}

      <main className="flex-1 lg:pl-64 flex flex-col min-h-screen">
        {/* Top bar - desktop */}
        <div className="hidden lg:flex items-center gap-3 h-16 px-6 bg-white border-b border-gray-100 sticky top-0 z-20">
          <form onSubmit={cauta} className="relative flex-1 max-w-md">
            <Search className="w-4 h-4 absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" />
            <input
              type="text"
              value={textCautare}
              onChange={(e) => setTextCautare(e.target.value)}
              placeholder="Caută comandă, client sau telefon..."
              className="w-full rounded-xl border border-gray-200 pl-9 pr-3 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-brand-primary/30 focus:border-brand-primary"
            />
          </form>
          <div className="flex-1" />
          <div className="relative" ref={notificariRef}>
            <button
              onClick={() => setNotificariDeschise((v) => !v)}
              className="relative p-2.5 rounded-xl text-gray-500 hover:bg-gray-100 transition-colors"
              aria-label="Notificări"
            >
              <Bell className="w-5 h-5" />
              {numarNotificari > 0 && (
                <span className="absolute top-1.5 right-1.5 min-w-[18px] h-[18px] px-1 rounded-full bg-brand-accent text-white text-[10px] font-semibold flex items-center justify-center">
                  {numarNotificari > 9 ? "9+" : numarNotificari}
                </span>
              )}
            </button>

            {notificariDeschise && (
              <div className="absolute right-0 top-full mt-2 w-80 bg-white rounded-2xl shadow-xl border border-gray-100 overflow-hidden z-30 animate-in fade-in slide-in-from-top-2">
                <div className="px-4 py-3 border-b border-gray-100">
                  <p className="text-sm font-semibold text-gray-900">Notificări</p>
                  <p className="text-xs text-gray-400">
                    {numarNotificari === 0
                      ? "Nimic nou momentan"
                      : `${numarNotificari} lucru${numarNotificari === 1 ? "" : "ri"} necesită atenție`}
                  </p>
                </div>
                <div className="max-h-80 overflow-y-auto">
                  {notificari.length === 0 ? (
                    <div className="p-6 text-center text-sm text-gray-400">Ești la zi cu totul.</div>
                  ) : (
                    notificari.slice(0, 8).map((n) => (
                      <button
                        key={`${n.tip}-${n.id}`}
                        onClick={() => {
                          setNotificariDeschise(false);
                          router.push(n.href);
                        }}
                        className="w-full text-left px-4 py-3 border-b border-gray-50 last:border-0 hover:bg-gray-50/70 transition-colors flex items-start gap-3"
                      >
                        <div
                          className={`w-8 h-8 rounded-full flex items-center justify-center flex-shrink-0 ${
                            n.tip === "comanda"
                              ? "bg-brand-primary/10 text-brand-primary"
                              : "bg-brand-accent/10 text-brand-accent"
                          }`}
                        >
                          {n.tip === "comanda" ? (
                            <ClipboardList className="w-4 h-4" />
                          ) : (
                            <Bell className="w-4 h-4" />
                          )}
                        </div>
                        <div className="min-w-0 flex-1">
                          <p className="text-sm font-medium text-gray-900 truncate">
                            {n.tip === "comanda" ? "Comandă nouă" : "Mesaj nou"} · {n.titlu}
                          </p>
                          <p className="text-xs text-gray-500 truncate">{n.detaliu}</p>
                        </div>
                        <span className="text-[11px] text-gray-400 flex-shrink-0 mt-0.5">
                          {formateazaOraRelativa(n.data)}
                        </span>
                      </button>
                    ))
                  )}
                </div>
              </div>
            )}
          </div>
          <button
            onClick={() => setModalComandaDeschis(true)}
            className="flex items-center gap-2 bg-brand-accent text-white text-sm font-medium px-4 py-2.5 rounded-xl hover:brightness-95 transition-all"
          >
            <Plus className="w-4 h-4" />
            Adaugă Comandă Manuală
          </button>
        </div>

        <div className="p-4 sm:p-6 lg:p-8 flex-1">{children}</div>
      </main>

      {modalComandaDeschis && <ModalComandaManuala onClose={() => setModalComandaDeschis(false)} />}
    </div>
  );
}

const stilInput =
  "w-full rounded-xl border border-gray-200 px-3 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-brand-primary/30 focus:border-brand-primary";

function ModalComandaManuala({ onClose }: { onClose: () => void }) {
  const [client, setClient] = useState("");
  const [telefon, setTelefon] = useState("");
  const [produse, setProduse] = useState("");
  const [suma, setSuma] = useState("");
  const [termen, setTermen] = useState("");
  const [seSalveaza, setSeSalveaza] = useState(false);
  const [eroare, setEroare] = useState<string | null>(null);

  async function salveaza(e: React.FormEvent) {
    e.preventDefault();
    setEroare(null);
    if (!client.trim() || !telefon.trim() || !produse.trim()) {
      setEroare("Completează cel puțin client, telefon și produse.");
      return;
    }

    setSeSalveaza(true);
    try {
      await addDoc(collection(db, "orders"), {
        client: client.trim(),
        telefon: telefon.trim(),
        suma: parseFloat(suma) || 0,
        produse: produse.trim(),
        status: "noua",
        termen: termen.trim(),
        data_creare: serverTimestamp(),
      });
      onClose();
    } catch (err) {
      console.error("Eroare la adăugarea comenzii manuale:", err);
      setEroare("A apărut o eroare la salvare. Încearcă din nou.");
    } finally {
      setSeSalveaza(false);
    }
  }

  return (
    <div className="fixed inset-0 z-50 flex items-center justify-center p-4">
      <div className="absolute inset-0 bg-black/40 animate-in fade-in" onClick={onClose} />
      <form
        onSubmit={salveaza}
        className="relative bg-white rounded-2xl shadow-xl w-full max-w-md p-6 space-y-4 animate-in fade-in slide-in-from-bottom-4"
      >
        <div className="flex items-center justify-between">
          <h3 className="font-semibold text-gray-900">Comandă Manuală</h3>
          <button
            type="button"
            onClick={onClose}
            className="p-1.5 rounded-lg hover:bg-gray-100"
            aria-label="Închide"
          >
            <X className="w-4 h-4" />
          </button>
        </div>

        <input
          type="text"
          value={client}
          onChange={(e) => setClient(e.target.value)}
          placeholder="Nume client"
          className={stilInput}
        />
        <input
          type="tel"
          value={telefon}
          onChange={(e) => setTelefon(e.target.value)}
          placeholder="Telefon"
          className={stilInput}
        />
        <input
          type="text"
          value={produse}
          onChange={(e) => setProduse(e.target.value)}
          placeholder="Produse (ex: 1000x Cărți de vizită)"
          className={stilInput}
        />
        <div className="grid grid-cols-2 gap-3">
          <input
            type="number"
            min="0"
            step="0.01"
            value={suma}
            onChange={(e) => setSuma(e.target.value)}
            placeholder="Sumă (lei)"
            className={stilInput}
          />
          <input type="date" value={termen} onChange={(e) => setTermen(e.target.value)} className={stilInput} />
        </div>

        {eroare && <p className="text-xs text-red-600">{eroare}</p>}

        <button
          type="submit"
          disabled={seSalveaza}
          className="w-full flex items-center justify-center gap-2 bg-brand-primary text-white font-medium py-2.5 rounded-xl hover:bg-brand-primary-dark transition-colors disabled:opacity-60"
        >
          {seSalveaza ? <Loader2 className="w-4 h-4 animate-spin" /> : "Adaugă Comanda"}
        </button>
      </form>
    </div>
  );
}
