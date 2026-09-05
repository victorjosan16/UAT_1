"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
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
} from "lucide-react";
import { useState } from "react";
import { collection, addDoc, serverTimestamp } from "firebase/firestore";
import { db } from "@/lib/firebase";

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
  const [menuDeschis, setMenuDeschis] = useState(false);
  const [modalComandaDeschis, setModalComandaDeschis] = useState(false);

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
            A
          </div>
          <div className="min-w-0">
            <p className="text-sm font-medium text-white truncate">Admin</p>
            <p className="text-xs text-white/50">Super Admin</p>
          </div>
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
        </div>
      )}

      <main className="flex-1 lg:pl-64 flex flex-col min-h-screen">
        {/* Top bar - desktop */}
        <div className="hidden lg:flex items-center gap-3 h-16 px-6 bg-white border-b border-gray-100 sticky top-0 z-20">
          <div className="relative flex-1 max-w-md">
            <Search className="w-4 h-4 absolute left-3 top-1/2 -translate-y-1/2 text-gray-400" />
            <input
              type="text"
              placeholder="Caută comandă, client sau fișier ID..."
              className="w-full rounded-xl border border-gray-200 pl-9 pr-3 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-brand-primary/30 focus:border-brand-primary"
            />
          </div>
          <div className="flex-1" />
          <button
            className="relative p-2.5 rounded-xl text-gray-500 hover:bg-gray-100 transition-colors"
            aria-label="Notificări"
          >
            <Bell className="w-5 h-5" />
            <span className="absolute top-2 right-2 w-2 h-2 rounded-full bg-brand-accent" />
          </button>
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
