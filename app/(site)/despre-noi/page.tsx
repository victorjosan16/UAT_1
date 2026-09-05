"use client";

import { Award, Heart, Sparkles, Target } from "lucide-react";
import { useSite } from "@/lib/site-context";

const VALORI = [
  {
    icon: Target,
    titlu: "Precizie",
    text: "Fiecare fișier trece printr-un control de calitate înainte de a ajunge la tipar.",
  },
  {
    icon: Sparkles,
    titlu: "Calitate Premium",
    text: "Lucrăm cu materiale și finisaje alese pentru un rezultat care arată profesionist.",
  },
  {
    icon: Award,
    titlu: "Experiență",
    text: "Ani de experiență în tipar, de la cărți de vizită la bannere de format mare.",
  },
  {
    icon: Heart,
    titlu: "Grijă pentru Client",
    text: "Te ținem la curent cu fiecare etapă a comenzii, de la grafică până la livrare.",
  },
];

export default function DespreNoiPage() {
  const { configHeader } = useSite();

  return (
    <div className="animate-in fade-in">
      <section className="bg-brand-primary text-white px-6 py-16 sm:py-20 text-center">
        <h1 className="text-3xl sm:text-4xl font-bold">Despre {configHeader.logoText}</h1>
        <p className="mt-4 text-white/80 max-w-xl mx-auto">
          Suntem o tipografie digitală axată pe viteză, calitate și o experiență simplă de comandă online.
        </p>
      </section>

      <section className="max-w-4xl mx-auto px-4 sm:px-6 py-14">
        <h2 className="text-2xl font-semibold text-gray-900 mb-4">Povestea noastră</h2>
        <p className="text-gray-600 leading-relaxed">
          {configHeader.logoText} a pornit din dorința de a face printarea profesională accesibilă oricui — fără
          drumuri la sediu, fără așteptări lungi și fără compromisuri de calitate. Astăzi combinăm un flux de
          producție rapid cu un magazin online simplu de folosit, ca tu să te poți concentra pe afacerea ta, nu pe
          logistica din spatele unui print.
        </p>
        <p className="text-gray-600 leading-relaxed mt-4">
          De la cărți de vizită și materiale de marketing, până la bannere de format mare, fiecare comandă trece prin
          verificare grafică înainte de producție, astfel încât rezultatul final să corespundă exact cu ce ai
          comandat.
        </p>
      </section>

      <section className="bg-white border-y border-gray-100">
        <div className="max-w-6xl mx-auto px-4 sm:px-6 py-14">
          <h2 className="text-2xl font-semibold text-gray-900 text-center mb-10">Ce ne ghidează</h2>
          <div className="grid sm:grid-cols-2 lg:grid-cols-4 gap-6">
            {VALORI.map((valoare) => {
              const Icon = valoare.icon;
              return (
                <div
                  key={valoare.titlu}
                  className="bg-gray-50 rounded-2xl p-6 border border-gray-100 hover:shadow-xl transition-shadow"
                >
                  <div className="w-11 h-11 rounded-xl bg-brand-primary/10 text-brand-primary flex items-center justify-center mb-4">
                    <Icon className="w-5 h-5" />
                  </div>
                  <h3 className="font-semibold text-gray-900">{valoare.titlu}</h3>
                  <p className="text-sm text-gray-500 mt-2">{valoare.text}</p>
                </div>
              );
            })}
          </div>
        </div>
      </section>

      <section className="max-w-4xl mx-auto px-4 sm:px-6 py-14 text-center">
        <h2 className="text-2xl font-semibold text-gray-900 mb-3">Hai să lucrăm împreună</h2>
        <p className="text-gray-500 mb-6">Vezi produsele disponibile sau ia legătura cu noi direct.</p>
        <div className="flex flex-wrap justify-center gap-3">
          <a
            href="/#produse"
            className="bg-brand-primary text-white font-medium px-6 py-3 rounded-2xl hover:bg-brand-primary-dark transition-colors"
          >
            Vezi Produsele
          </a>
          <a
            href="/contact"
            className="bg-gray-100 text-gray-700 font-medium px-6 py-3 rounded-2xl hover:bg-gray-200 transition-colors"
          >
            Contactează-ne
          </a>
        </div>
      </section>
    </div>
  );
}
