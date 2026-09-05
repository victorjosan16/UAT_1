"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { signInWithEmailAndPassword } from "firebase/auth";
import { getFirebaseAuth } from "@/lib/firebase";
import { useAdminAuth } from "@/lib/use-admin-auth";
import { Loader2 } from "lucide-react";

const stilInput =
  "w-full rounded-xl border border-gray-200 px-3 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-brand-primary/30 focus:border-brand-primary";

export default function AdminLoginPage() {
  const router = useRouter();
  const { user, seIncarca } = useAdminAuth();
  const [email, setEmail] = useState("");
  const [parola, setParola] = useState("");
  const [seConecteaza, setSeConecteaza] = useState(false);
  const [eroare, setEroare] = useState<string | null>(null);

  useEffect(() => {
    if (!seIncarca && user) router.replace("/admin");
  }, [seIncarca, user, router]);

  async function conecteaza(e: React.FormEvent) {
    e.preventDefault();
    setEroare(null);

    if (!email.trim() || !parola) {
      setEroare("Completează email și parolă.");
      return;
    }

    setSeConecteaza(true);
    try {
      await signInWithEmailAndPassword(getFirebaseAuth(), email.trim(), parola);
      router.replace("/admin");
    } catch (err) {
      console.error("Eroare la autentificare:", err);
      setEroare("Email sau parolă incorectă.");
    } finally {
      setSeConecteaza(false);
    }
  }

  if (seIncarca || user) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-50">
        <Loader2 className="w-6 h-6 animate-spin text-gray-400" />
      </div>
    );
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 px-4">
      <form
        onSubmit={conecteaza}
        className="bg-white rounded-2xl shadow-sm border border-gray-100 p-8 w-full max-w-sm space-y-4"
      >
        <div className="text-center mb-2">
          <div className="w-10 h-10 rounded-xl bg-brand-primary flex items-center justify-center text-white font-bold mx-auto mb-3">
            P
          </div>
          <h1 className="text-lg font-semibold text-gray-900">Autentificare Admin</h1>
          <p className="text-sm text-gray-500 mt-1">PosterART</p>
        </div>

        <input
          type="email"
          value={email}
          onChange={(e) => setEmail(e.target.value)}
          placeholder="Email"
          autoComplete="username"
          className={stilInput}
        />
        <input
          type="password"
          value={parola}
          onChange={(e) => setParola(e.target.value)}
          placeholder="Parolă"
          autoComplete="current-password"
          className={stilInput}
        />

        {eroare && <p className="text-xs text-red-600">{eroare}</p>}

        <button
          type="submit"
          disabled={seConecteaza}
          className="w-full flex items-center justify-center gap-2 bg-brand-primary text-white font-medium py-2.5 rounded-xl hover:bg-brand-primary-dark transition-colors disabled:opacity-60"
        >
          {seConecteaza ? <Loader2 className="w-4 h-4 animate-spin" /> : "Conectează-te"}
        </button>
      </form>
    </div>
  );
}
