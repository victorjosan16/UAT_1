"use client";

import { useState } from "react";
import { AlertTriangle, Loader2, X } from "lucide-react";

export interface ConfirmDialogState {
  titlu?: string;
  mesaj: string;
  textConfirmare?: string;
  onConfirm: () => void | Promise<void>;
}

export function ConfirmDialog({
  stare,
  onClose,
}: {
  stare: ConfirmDialogState | null;
  onClose: () => void;
}) {
  const [seProceseaza, setSeProceseaza] = useState(false);

  if (!stare) return null;

  async function confirma() {
    setSeProceseaza(true);
    try {
      await stare!.onConfirm();
    } finally {
      setSeProceseaza(false);
      onClose();
    }
  }

  return (
    <div className="fixed inset-0 z-[60] flex items-center justify-center p-4">
      <div className="absolute inset-0 bg-black/40 animate-in fade-in" onClick={onClose} />
      <div className="relative bg-white rounded-2xl shadow-xl w-full max-w-sm p-6 animate-in fade-in slide-in-from-bottom-4">
        <button
          onClick={onClose}
          className="absolute top-4 right-4 p-1 rounded-lg text-gray-400 hover:bg-gray-100 hover:text-gray-600"
          aria-label="Închide"
        >
          <X className="w-4 h-4" />
        </button>

        <div className="w-11 h-11 rounded-full bg-red-50 text-red-600 flex items-center justify-center mb-4">
          <AlertTriangle className="w-5 h-5" />
        </div>

        <h3 className="font-semibold text-gray-900 mb-1.5">{stare.titlu ?? "Confirmă acțiunea"}</h3>
        <p className="text-sm text-gray-500 mb-6">{stare.mesaj}</p>

        <div className="flex gap-3">
          <button
            onClick={onClose}
            className="flex-1 rounded-xl border border-gray-200 text-gray-700 font-medium py-2.5 hover:bg-gray-50 transition-colors"
          >
            Anulează
          </button>
          <button
            onClick={confirma}
            disabled={seProceseaza}
            className="flex-1 flex items-center justify-center gap-2 rounded-xl bg-red-600 text-white font-medium py-2.5 hover:bg-red-700 transition-colors disabled:opacity-60"
          >
            {seProceseaza ? <Loader2 className="w-4 h-4 animate-spin" /> : (stare.textConfirmare ?? "Șterge")}
          </button>
        </div>
      </div>
    </div>
  );
}
