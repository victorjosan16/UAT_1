"use client";

import { useState } from "react";
import { useRouter } from "next/navigation";
import { MapPin, Search, ShoppingCart } from "lucide-react";
import { useSite } from "@/lib/site-context";

export function SiteHeader() {
  const { configHeader, numarItemiCos, setCosDeschis } = useSite();
  const router = useRouter();
  const [cautare, setCautare] = useState("");

  function cautaProduse(e: React.FormEvent) {
    e.preventDefault();
    const query = cautare.trim();
    router.push(query ? `/preturi?cauta=${encodeURIComponent(query)}` : "/preturi");
  }

  return (
    <header className="sticky top-0 z-30 bg-white/95 backdrop-blur border-b border-gray-100">
      <div className="h-1 bg-brand-primary" />
      <div className="max-w-6xl mx-auto px-4 sm:px-6">
        <div className="flex flex-wrap items-center gap-3 py-3">
          <a href="/" className="flex items-center gap-2 flex-shrink-0">
            <div className="w-8 h-8 rounded-xl bg-brand-primary flex items-center justify-center text-white font-bold text-sm">
              P
            </div>
            <span className="font-semibold text-gray-900 text-lg">{configHeader.logoText}</span>
          </a>

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

          <form onSubmit={cautaProduse} className="hidden sm:block flex-1 min-w-[140px] max-w-xs">
            <div className="relative">
              <input
                type="text"
                value={cautare}
                onChange={(e) => setCautare(e.target.value)}
                placeholder="Căutare produse..."
                className="w-full rounded-xl border border-gray-200 pl-3 pr-9 py-2 text-sm focus:outline-none focus:ring-2 focus:ring-brand-primary/30 focus:border-brand-primary"
              />
              <button
                type="submit"
                className="absolute right-3 top-1/2 -translate-y-1/2 text-gray-400 hover:text-brand-primary"
                aria-label="Caută"
              >
                <Search className="w-4 h-4" />
              </button>
            </div>
          </form>

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
  );
}
