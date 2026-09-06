"use client";

import { useState } from "react";
import Image from "next/image";
import { ImageOff, Minus, Plus } from "lucide-react";
import { formateazaPret, pretEfectiv, useSite, type Produs } from "@/lib/site-context";

export function ProdusCard({ produs }: { produs: Produs }) {
  const { adaugaInCos } = useSite();
  const [cantitate, setCantitate] = useState(1);
  const areReducere = produs.pretRedus !== null && produs.pretRedus < produs.pret;
  const imagine = produs.imagini[0] ?? null;

  function adauga() {
    adaugaInCos(produs, cantitate);
    setCantitate(1);
  }

  return (
    <div className="bg-white rounded-2xl overflow-hidden shadow-sm hover:shadow-xl transition-shadow border border-gray-100 flex flex-col">
      <div className="relative aspect-square bg-gray-100">
        {imagine ? (
          <Image
            src={imagine}
            alt={produs.nume}
            fill
            sizes="(max-width: 640px) 50vw, (max-width: 1024px) 33vw, 25vw"
            className="object-cover"
          />
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
          <div className="flex items-center gap-1.5">
            <button
              onClick={() => setCantitate((c) => Math.max(1, c - 1))}
              className="w-6 h-6 rounded-lg border border-gray-200 flex items-center justify-center hover:bg-gray-100"
              aria-label="Scade cantitatea"
            >
              <Minus className="w-3 h-3" />
            </button>
            <span className="text-xs font-medium w-4 text-center">{cantitate}</span>
            <button
              onClick={() => setCantitate((c) => Math.min(produs.stoc, c + 1))}
              className="w-6 h-6 rounded-lg border border-gray-200 flex items-center justify-center hover:bg-gray-100"
              aria-label="Crește cantitatea"
            >
              <Plus className="w-3 h-3" />
            </button>
          </div>
        </div>

        <button
          onClick={adauga}
          className="mt-3 w-full bg-gray-900 text-white text-xs font-medium py-2.5 rounded-xl hover:bg-brand-primary transition-colors"
        >
          Adaugă în coș
        </button>
      </div>
    </div>
  );
}
