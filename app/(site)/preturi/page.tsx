"use client";

import { useEffect, useMemo, useState } from "react";
import { collection, onSnapshot } from "firebase/firestore";
import { db } from "@/lib/firebase";
import { useSite } from "@/lib/site-context";
import { ProdusCard } from "@/components/ProdusCard";
import { Loader2 } from "lucide-react";

interface Grupa {
  id: string;
  nume: string;
}

export default function PreturiPage() {
  const { produseDisponibile } = useSite();
  const [grupe, setGrupe] = useState<Grupa[]>([]);
  const [seIncarca, setSeIncarca] = useState(true);

  useEffect(() => {
    const unsubscribe = onSnapshot(collection(db, "groups"), (snapshot) => {
      const lista: Grupa[] = snapshot.docs
        .map((docSnap) => ({ id: docSnap.id, nume: docSnap.data().nume ?? "Grupă" }))
        .sort((a, b) => a.nume.localeCompare(b.nume, "ro"));
      setGrupe(lista);
      setSeIncarca(false);
    });
    return () => unsubscribe();
  }, []);

  const grupuriCuProduse = useMemo(
    () =>
      grupe
        .map((grupa) => ({ grupa, produse: produseDisponibile.filter((p) => p.group_id === grupa.id) }))
        .filter((g) => g.produse.length > 0),
    [grupe, produseDisponibile]
  );

  return (
    <div className="animate-in fade-in">
      <section className="bg-brand-primary text-white px-6 py-16 sm:py-20 text-center">
        <h1 className="text-3xl sm:text-4xl font-bold">Prețuri</h1>
        <p className="mt-4 text-white/80 max-w-xl mx-auto">
          Lista completă de produse, actualizată live — fără costuri ascunse.
        </p>
      </section>

      <section className="max-w-6xl mx-auto px-4 sm:px-6 py-14 space-y-14">
        {seIncarca ? (
          <div className="flex items-center justify-center h-48 text-gray-400">
            <Loader2 className="w-6 h-6 animate-spin mr-2" />
            Se încarcă lista de prețuri...
          </div>
        ) : grupuriCuProduse.length === 0 ? (
          <div className="text-center text-gray-400 py-16">Momentan nu avem produse disponibile.</div>
        ) : (
          grupuriCuProduse.map(({ grupa, produse }) => (
            <div key={grupa.id}>
              <h2 className="text-2xl font-semibold text-gray-900 mb-6">{grupa.nume}</h2>
              <div className="grid grid-cols-2 sm:grid-cols-3 lg:grid-cols-4 gap-4 sm:gap-5">
                {produse.map((produs) => (
                  <ProdusCard key={produs.id} produs={produs} />
                ))}
              </div>
            </div>
          ))
        )}
      </section>
    </div>
  );
}
