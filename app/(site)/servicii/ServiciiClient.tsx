"use client";

import { useEffect, useMemo, useState } from "react";
import { collection, onSnapshot } from "firebase/firestore";
import { db } from "@/lib/firebase";
import { useSite } from "@/lib/site-context";
import { Loader2, Package, ArrowRight } from "lucide-react";

interface Grupa {
  id: string;
  nume: string;
  descriere: string;
}

export default function ServiciiClient() {
  const { produseDisponibile } = useSite();
  const [grupe, setGrupe] = useState<Grupa[]>([]);
  const [seIncarca, setSeIncarca] = useState(true);

  useEffect(() => {
    const unsubscribe = onSnapshot(collection(db, "groups"), (snapshot) => {
      const lista: Grupa[] = snapshot.docs
        .map((docSnap) => {
          const data = docSnap.data();
          return {
            id: docSnap.id,
            nume: data.nume ?? "Serviciu",
            descriere: data.descriere ?? "",
          };
        })
        .sort((a, b) => a.nume.localeCompare(b.nume, "ro"));
      setGrupe(lista);
      setSeIncarca(false);
    });
    return () => unsubscribe();
  }, []);

  const numarProduse = useMemo(() => {
    const harta = new Map<string, number>();
    for (const produs of produseDisponibile) {
      harta.set(produs.group_id, (harta.get(produs.group_id) ?? 0) + 1);
    }
    return harta;
  }, [produseDisponibile]);

  return (
    <div className="animate-in fade-in">
      <section className="bg-brand-primary text-white px-6 py-16 sm:py-20 text-center">
        <h1 className="text-3xl sm:text-4xl font-bold">Serviciile Noastre</h1>
        <p className="mt-4 text-white/80 max-w-xl mx-auto">
          De la materiale de birou până la print de format mare — alege categoria potrivită pentru proiectul tău.
        </p>
      </section>

      <section className="max-w-6xl mx-auto px-4 sm:px-6 py-14">
        {seIncarca ? (
          <div className="flex items-center justify-center h-48 text-gray-400">
            <Loader2 className="w-6 h-6 animate-spin mr-2" />
            Se încarcă serviciile...
          </div>
        ) : grupe.length === 0 ? (
          <div className="text-center text-gray-400 py-16">Momentan nu avem servicii publicate.</div>
        ) : (
          <div className="grid sm:grid-cols-2 lg:grid-cols-3 gap-6">
            {grupe.map((grupa) => (
              <a
                key={grupa.id}
                href="/#produse"
                className="group bg-white rounded-2xl border border-gray-100 shadow-sm hover:shadow-xl transition-shadow p-6 flex flex-col"
              >
                <div className="w-11 h-11 rounded-xl bg-brand-primary/10 text-brand-primary flex items-center justify-center mb-4">
                  <Package className="w-5 h-5" />
                </div>
                <h3 className="font-semibold text-gray-900">{grupa.nume}</h3>
                {grupa.descriere && <p className="text-sm text-gray-500 mt-2 flex-1">{grupa.descriere}</p>}
                <div className="flex items-center justify-between mt-4 pt-4 border-t border-gray-50">
                  <span className="text-xs text-gray-400">
                    {numarProduse.get(grupa.id) ?? 0} {numarProduse.get(grupa.id) === 1 ? "produs" : "produse"}
                  </span>
                  <span className="flex items-center gap-1 text-sm font-medium text-brand-primary group-hover:gap-1.5 transition-all">
                    Vezi produsele
                    <ArrowRight className="w-3.5 h-3.5" />
                  </span>
                </div>
              </a>
            ))}
          </div>
        )}
      </section>
    </div>
  );
}
