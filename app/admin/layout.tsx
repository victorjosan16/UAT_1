"use client";

import Link from "next/link";
import { usePathname } from "next/navigation";
import {
  LayoutDashboard,
  Package,
  ClipboardList,
  Users,
  Settings,
  Menu,
  X,
} from "lucide-react";
import { useState } from "react";

const NAV = [
  { href: "/admin", label: "Dashboard", icon: LayoutDashboard },
  { href: "/admin/catalog", label: "Catalog Produse", icon: Package },
  { href: "/admin/comenzi", label: "Procesare Comenzi", icon: ClipboardList },
  { href: "/admin/clienti", label: "CRM & Clienți", icon: Users },
  { href: "/admin/setari", label: "Setări", icon: Settings },
];

export default function AdminLayout({ children }: { children: React.ReactNode }) {
  const pathname = usePathname();
  const [menuDeschis, setMenuDeschis] = useState(false);

  return (
    <div className="min-h-screen bg-gray-50 lg:flex">
      {/* Sidebar - desktop */}
      <aside className="hidden lg:flex lg:w-64 lg:flex-col lg:fixed lg:inset-y-0 border-r border-gray-100 bg-white">
        <div className="flex items-center gap-2 px-6 h-16 border-b border-gray-100">
          <div className="w-8 h-8 rounded-xl bg-brand-primary flex items-center justify-center text-white font-bold text-sm">
            P
          </div>
          <span className="font-semibold text-gray-900">PosterART</span>
        </div>
        <nav className="flex-1 px-3 py-4 space-y-1">
          {NAV.map((item) => {
            const activ = item.href === "/admin" ? pathname === item.href : pathname?.startsWith(item.href);
            const Icon = item.icon;
            return (
              <Link
                key={item.href}
                href={item.href}
                className={`flex items-center gap-3 px-3 py-2.5 rounded-xl text-sm font-medium transition-colors ${
                  activ
                    ? "bg-brand-primary/10 text-brand-primary"
                    : "text-gray-600 hover:bg-gray-100 hover:text-gray-900"
                }`}
              >
                <Icon className="w-4 h-4" />
                {item.label}
              </Link>
            );
          })}
        </nav>
      </aside>

      {/* Sidebar - mobile */}
      <div className="lg:hidden flex items-center justify-between px-4 h-16 bg-white border-b border-gray-100 sticky top-0 z-30">
        <div className="flex items-center gap-2">
          <div className="w-8 h-8 rounded-xl bg-brand-primary flex items-center justify-center text-white font-bold text-sm">
            P
          </div>
          <span className="font-semibold text-gray-900">PosterART</span>
        </div>
        <button
          onClick={() => setMenuDeschis(!menuDeschis)}
          className="p-2 rounded-lg hover:bg-gray-100"
          aria-label="Meniu"
        >
          {menuDeschis ? <X className="w-5 h-5" /> : <Menu className="w-5 h-5" />}
        </button>
      </div>
      {menuDeschis && (
        <div className="lg:hidden bg-white border-b border-gray-100 px-3 py-2 space-y-1 animate-in fade-in slide-in-from-top-2">
          {NAV.map((item) => {
            const Icon = item.icon;
            return (
              <Link
                key={item.href}
                href={item.href}
                onClick={() => setMenuDeschis(false)}
                className="flex items-center gap-3 px-3 py-2.5 rounded-xl text-sm font-medium text-gray-600 hover:bg-gray-100"
              >
                <Icon className="w-4 h-4" />
                {item.label}
              </Link>
            );
          })}
        </div>
      )}

      <main className="flex-1 lg:pl-64">
        <div className="p-4 sm:p-6 lg:p-8">{children}</div>
      </main>
    </div>
  );
}
