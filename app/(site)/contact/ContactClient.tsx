"use client";

import { useState } from "react";
import { collection, addDoc, serverTimestamp } from "firebase/firestore";
import { db } from "@/lib/firebase";
import { useSite } from "@/lib/site-context";
import { MapPin, Phone, Clock, CheckCircle2, Loader2 } from "lucide-react";

const stilInput =
  "w-full rounded-xl border border-gray-200 px-3 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-brand-primary/30 focus:border-brand-primary";

export default function ContactClient() {
  const { configHeader } = useSite();

  const [nume, setNume] = useState("");
  const [telefon, setTelefon] = useState("");
  const [mesaj, setMesaj] = useState("");
  const [siteWeb, setSiteWeb] = useState("");
  const [seTrimite, setSeTrimite] = useState(false);
  const [eroare, setEroare] = useState<string | null>(null);
  const [trimis, setTrimis] = useState(false);

  async function trimiteMesaj(e: React.FormEvent) {
    e.preventDefault();
    setEroare(null);

    if (!nume.trim() || !telefon.trim() || !mesaj.trim()) {
      setEroare("Te rugăm să completezi toate câmpurile.");
      return;
    }

    // Câmp-capcană pentru roboți — dacă a fost completat, prefacem succesul
    // fără să scriem nimic în Firestore.
    if (siteWeb.trim() !== "") {
      setTrimis(true);
      setNume("");
      setTelefon("");
      setMesaj("");
      return;
    }

    setSeTrimite(true);
    try {
      await addDoc(collection(db, "contact_mesaje"), {
        nume: nume.trim(),
        telefon: telefon.trim(),
        mesaj: mesaj.trim(),
        citit: false,
        data_creare: serverTimestamp(),
      });
      setTrimis(true);
      setNume("");
      setTelefon("");
      setMesaj("");
    } catch (err) {
      console.error("Eroare la trimiterea mesajului:", err);
      setEroare("A apărut o eroare. Te rugăm să încerci din nou.");
    } finally {
      setSeTrimite(false);
    }
  }

  return (
    <div className="animate-in fade-in">
      <section className="bg-brand-primary text-white px-6 py-16 sm:py-20 text-center">
        <h1 className="text-3xl sm:text-4xl font-bold">Contact</h1>
        <p className="mt-4 text-white/80 max-w-xl mx-auto">
          Ai o întrebare despre o comandă sau un proiect nou? Scrie-ne.
        </p>
      </section>

      <section className="max-w-5xl mx-auto px-4 sm:px-6 py-14 grid md:grid-cols-2 gap-10">
        <div className="space-y-4">
          <h2 className="text-xl font-semibold text-gray-900 mb-2">Date de contact</h2>
          {configHeader.locatie && (
            <div className="flex items-center gap-3 bg-white rounded-2xl border border-gray-100 shadow-sm p-4">
              <div className="w-10 h-10 rounded-xl bg-brand-primary/10 text-brand-primary flex items-center justify-center flex-shrink-0">
                <MapPin className="w-4.5 h-4.5" />
              </div>
              <div>
                <p className="text-xs text-gray-400">Locație</p>
                <p className="text-sm font-medium text-gray-900">{configHeader.locatie}</p>
              </div>
            </div>
          )}
          {configHeader.telefon && (
            <div className="flex items-center gap-3 bg-white rounded-2xl border border-gray-100 shadow-sm p-4">
              <div className="w-10 h-10 rounded-xl bg-brand-primary/10 text-brand-primary flex items-center justify-center flex-shrink-0">
                <Phone className="w-4.5 h-4.5" />
              </div>
              <div>
                <p className="text-xs text-gray-400">Telefon</p>
                <a href={`tel:${configHeader.telefon}`} className="text-sm font-medium text-gray-900 hover:underline">
                  {configHeader.telefon}
                </a>
              </div>
            </div>
          )}
          {configHeader.program && (
            <div className="flex items-center gap-3 bg-white rounded-2xl border border-gray-100 shadow-sm p-4">
              <div className="w-10 h-10 rounded-xl bg-brand-primary/10 text-brand-primary flex items-center justify-center flex-shrink-0">
                <Clock className="w-4.5 h-4.5" />
              </div>
              <div>
                <p className="text-xs text-gray-400">Program</p>
                <p className="text-sm font-medium text-gray-900">{configHeader.program}</p>
              </div>
            </div>
          )}
          {!configHeader.locatie && !configHeader.telefon && !configHeader.program && (
            <p className="text-sm text-gray-400">
              Datele de contact nu sunt încă completate — administratorul le poate adăuga din Admin → Setări.
            </p>
          )}
        </div>

        <div className="bg-white rounded-2xl border border-gray-100 shadow-sm p-6">
          {trimis ? (
            <div className="flex flex-col items-center justify-center text-center py-10 gap-3">
              <div className="w-14 h-14 rounded-full bg-emerald-50 text-emerald-600 flex items-center justify-center">
                <CheckCircle2 className="w-7 h-7" />
              </div>
              <p className="font-semibold text-gray-900">Mesaj trimis!</p>
              <p className="text-sm text-gray-500">Îți răspundem cât mai curând posibil.</p>
              <button
                onClick={() => setTrimis(false)}
                className="mt-2 text-sm font-medium text-brand-primary hover:underline"
              >
                Trimite alt mesaj
              </button>
            </div>
          ) : (
            <form onSubmit={trimiteMesaj} className="space-y-4">
              <h2 className="text-xl font-semibold text-gray-900">Trimite-ne un mesaj</h2>
              <input
                type="text"
                value={nume}
                onChange={(e) => setNume(e.target.value)}
                placeholder="Nume complet"
                className={stilInput}
              />
              <input
                type="tel"
                value={telefon}
                onChange={(e) => setTelefon(e.target.value)}
                placeholder="Telefon"
                className={stilInput}
              />
              <textarea
                value={mesaj}
                onChange={(e) => setMesaj(e.target.value)}
                placeholder="Cu ce te putem ajuta?"
                rows={4}
                className={`${stilInput} resize-none`}
              />

              {/* Câmp-capcană pentru roboți — invizibil și inaccesibil pentru oameni */}
              <input
                type="text"
                value={siteWeb}
                onChange={(e) => setSiteWeb(e.target.value)}
                tabIndex={-1}
                autoComplete="off"
                aria-hidden="true"
                style={{ position: "absolute", left: "-9999px", width: 1, height: 1, opacity: 0 }}
              />

              {eroare && <p className="text-xs text-red-600">{eroare}</p>}
              <button
                type="submit"
                disabled={seTrimite}
                className="w-full flex items-center justify-center gap-2 bg-brand-primary text-white font-medium py-3 rounded-xl hover:bg-brand-primary-dark transition-colors disabled:opacity-60"
              >
                {seTrimite ? <Loader2 className="w-4 h-4 animate-spin" /> : "Trimite Mesajul"}
              </button>
            </form>
          )}
        </div>
      </section>
    </div>
  );
}
