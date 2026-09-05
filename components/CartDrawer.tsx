"use client";

import { CheckCircle2, ImageOff, Loader2, Minus, Plus, ShoppingCart, Trash2, X } from "lucide-react";
import { formateazaPret, useSite } from "@/lib/site-context";

export function CartDrawer() {
  const {
    cosDeschis,
    inchideCos,
    comandaTrimisa,
    cos,
    modificaCantitate,
    eliminaDinCos,
    totalCos,
    numeClient,
    setNumeClient,
    telefonClient,
    setTelefonClient,
    eroareComanda,
    trimiteComanda,
    seTrimite,
  } = useSite();

  if (!cosDeschis) return null;

  return (
    <div className="fixed inset-0 z-50">
      <div className="absolute inset-0 bg-black/40 animate-in fade-in" onClick={inchideCos} />
      <div className="absolute right-0 top-0 h-full w-full max-w-md bg-white shadow-xl flex flex-col animate-in slide-in-from-right">
        <div className="flex items-center justify-between px-5 h-16 border-b border-gray-100 flex-shrink-0">
          <h2 className="font-semibold text-gray-900">Coșul tău</h2>
          <button onClick={inchideCos} className="p-2 rounded-lg hover:bg-gray-100" aria-label="Închide">
            <X className="w-5 h-5" />
          </button>
        </div>

        {comandaTrimisa ? (
          <div className="flex-1 flex flex-col items-center justify-center text-center px-8 gap-3 animate-in fade-in">
            <div className="w-14 h-14 rounded-full bg-emerald-50 text-emerald-600 flex items-center justify-center">
              <CheckCircle2 className="w-7 h-7" />
            </div>
            <p className="font-semibold text-gray-900">Comandă trimisă cu succes!</p>
            <p className="text-sm text-gray-500">Te contactăm în curând pentru confirmare.</p>
            <button
              onClick={inchideCos}
              className="mt-4 bg-brand-primary text-white text-sm font-medium px-5 py-2.5 rounded-xl hover:bg-brand-primary-dark transition-colors"
            >
              Închide
            </button>
          </div>
        ) : (
          <>
            <div className="flex-1 overflow-y-auto px-5 py-4 space-y-3">
              {cos.length === 0 ? (
                <div className="flex flex-col items-center justify-center h-full text-gray-400 gap-2">
                  <ShoppingCart className="w-8 h-8" />
                  Coșul este gol.
                </div>
              ) : (
                cos.map((item) => (
                  <div key={item.produsId} className="flex items-center gap-3 bg-gray-50 rounded-2xl p-3">
                    <div className="w-14 h-14 rounded-xl bg-gray-200 flex-shrink-0 overflow-hidden">
                      {item.imagine ? (
                        // eslint-disable-next-line @next/next/no-img-element
                        <img src={item.imagine} alt={item.nume} className="w-full h-full object-cover" />
                      ) : (
                        <div className="w-full h-full flex items-center justify-center text-gray-400">
                          <ImageOff className="w-5 h-5" />
                        </div>
                      )}
                    </div>
                    <div className="flex-1 min-w-0">
                      <p className="text-sm font-medium text-gray-900 truncate">{item.nume}</p>
                      <p className="text-xs text-gray-500">{formateazaPret(item.pretUnitar)} / buc</p>
                      <div className="flex items-center gap-2 mt-1.5">
                        <button
                          onClick={() => modificaCantitate(item.produsId, -1)}
                          className="w-6 h-6 rounded-lg bg-white border border-gray-200 flex items-center justify-center hover:bg-gray-100"
                          aria-label="Scade cantitatea"
                        >
                          <Minus className="w-3 h-3" />
                        </button>
                        <span className="text-sm font-medium w-4 text-center">{item.cantitate}</span>
                        <button
                          onClick={() => modificaCantitate(item.produsId, 1)}
                          className="w-6 h-6 rounded-lg bg-white border border-gray-200 flex items-center justify-center hover:bg-gray-100"
                          aria-label="Crește cantitatea"
                        >
                          <Plus className="w-3 h-3" />
                        </button>
                      </div>
                    </div>
                    <div className="flex flex-col items-end justify-between self-stretch">
                      <button
                        onClick={() => eliminaDinCos(item.produsId)}
                        className="text-gray-300 hover:text-red-500 transition-colors"
                        aria-label="Elimină produsul"
                      >
                        <Trash2 className="w-4 h-4" />
                      </button>
                      <span className="text-sm font-semibold text-gray-900 whitespace-nowrap">
                        {formateazaPret(item.pretUnitar * item.cantitate)}
                      </span>
                    </div>
                  </div>
                ))
              )}
            </div>

            {cos.length > 0 && (
              <div className="flex-shrink-0 border-t border-gray-100 p-5 space-y-3">
                <div className="flex items-center justify-between">
                  <span className="text-sm text-gray-500">Total</span>
                  <span className="text-lg font-semibold text-gray-900">{formateazaPret(totalCos)}</span>
                </div>

                <input
                  type="text"
                  placeholder="Nume complet"
                  value={numeClient}
                  onChange={(e) => setNumeClient(e.target.value)}
                  className="w-full rounded-xl border border-gray-200 px-3 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-brand-primary/30 focus:border-brand-primary"
                />
                <input
                  type="tel"
                  placeholder="Telefon"
                  value={telefonClient}
                  onChange={(e) => setTelefonClient(e.target.value)}
                  className="w-full rounded-xl border border-gray-200 px-3 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-brand-primary/30 focus:border-brand-primary"
                />

                {eroareComanda && <p className="text-xs text-red-600">{eroareComanda}</p>}

                <button
                  onClick={trimiteComanda}
                  disabled={seTrimite}
                  className="w-full flex items-center justify-center gap-2 bg-brand-primary text-white font-medium py-3 rounded-xl hover:bg-brand-primary-dark transition-colors disabled:opacity-60"
                >
                  {seTrimite ? <Loader2 className="w-4 h-4 animate-spin" /> : "Trimite Comanda"}
                </button>
              </div>
            )}
          </>
        )}
      </div>
    </div>
  );
}
