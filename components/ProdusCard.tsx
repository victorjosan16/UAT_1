"use client";

import { ImageOff } from "lucide-react";
import { formateazaPret, pretEfectiv, useSite, type Produs } from "@/lib/site-context";

export function ProdusCard({ produs }: { produs: Produs }) {
  const { adaugaInCos } = useSite();
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
            <span className="text-sm font-semibold text-brand-primary">{formateazaPret(pretEfectiv(produs))}</span>
            {areReducere && <span className="text-xs text-gray-400 line-through">{formateazaPret(produs.pret)}</span>}
          </div>
        </div>

        <button
          onClick={() => adaugaInCos(produs)}
          className="mt-3 w-full bg-gray-900 text-white text-xs font-medium py-2.5 rounded-xl hover:bg-brand-primary transition-colors"
        >
          Adaugă în coș
        </button>
      </div>
    </div>
  );
}
