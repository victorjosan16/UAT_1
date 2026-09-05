"use client";

import { CheckCircle2 } from "lucide-react";
import { useSite } from "@/lib/site-context";

export function ToastNotification() {
  const { toast } = useSite();

  if (!toast) return null;

  return (
    <div className="fixed bottom-5 left-1/2 -translate-x-1/2 z-[60] animate-in fade-in slide-in-from-bottom-4">
      <div className="flex items-center gap-2 bg-gray-900 text-white text-sm font-medium px-4 py-3 rounded-2xl shadow-xl">
        <CheckCircle2 className="w-4 h-4 text-emerald-400 flex-shrink-0" />
        {toast}
      </div>
    </div>
  );
}
