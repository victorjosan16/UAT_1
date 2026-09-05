"use client";

import { MapPin, Phone } from "lucide-react";
import { useSite } from "@/lib/site-context";

export function SiteFooter() {
  const { configHeader } = useSite();

  return (
    <footer id="contact" className="scroll-mt-20 bg-gray-900 text-white mt-12">
      <div className="max-w-6xl mx-auto px-4 sm:px-6 py-12 grid sm:grid-cols-2 gap-8">
        <div>
          <div className="flex items-center gap-2 mb-3">
            <div className="w-8 h-8 rounded-xl bg-brand-primary flex items-center justify-center text-white font-bold text-sm">
              P
            </div>
            <span className="font-semibold text-lg">{configHeader.logoText}</span>
          </div>
          <p className="text-sm text-gray-400 max-w-sm">Printuri profesionale, comandate online, livrate rapid.</p>
        </div>

        <div className="space-y-2 text-sm text-gray-300 sm:text-right">
          {configHeader.locatie && (
            <p className="flex items-center gap-1.5 sm:justify-end">
              <MapPin className="w-4 h-4 text-gray-500" />
              {configHeader.locatie}
            </p>
          )}
          {configHeader.telefon && (
            <p className="flex items-center gap-1.5 sm:justify-end">
              <Phone className="w-4 h-4 text-gray-500" />
              {configHeader.telefon}
            </p>
          )}
          {configHeader.program && <p className="text-gray-500">{configHeader.program}</p>}
        </div>
      </div>
      <div className="border-t border-white/10 py-4 text-center text-xs text-gray-500">
        © {new Date().getFullYear()} {configHeader.logoText}. Toate drepturile rezervate.
      </div>
    </footer>
  );
}
