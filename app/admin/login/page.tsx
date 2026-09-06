"use client";

import { useEffect, useState } from "react";
import { useRouter } from "next/navigation";
import { sendPasswordResetEmail, signInWithEmailAndPassword } from "firebase/auth";
import { getFirebaseAuth } from "@/lib/firebase";
import { useAdminAuth } from "@/lib/use-admin-auth";
import { CheckCircle2, Loader2 } from "lucide-react";

const stilInput =
  "w-full rounded-xl border border-gray-200 px-3 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-brand-primary/30 focus:border-brand-primary";

type Mod = "login" | "resetare";

export default function AdminLoginPage() {
  const router = useRouter();
  const { user, seIncarca } = useAdminAuth();
  const [mod, setMod] = useState<Mod>("login");
  const [email, setEmail] = useState("");
  const [parola, setParola] = useState("");
  const [seConecteaza, setSeConecteaza] = useState(false);
  const [eroare, setEroare] = useState<string | null>(null);
  const [resetareTrimisa, setResetareTrimisa] = useState(false);

  useEffect(() => {
    if (!seIncarca && user) router.replace("/admin");
  }, [seIncarca, user, router]);

  function schimbaMod(modNou: Mod) {
    setMod(modNou);
    setEroare(null);
    setResetareTrimisa(false);
  }

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

  async function trimiteResetarea(e: React.FormEvent) {
    e.preventDefault();
    setEroare(null);

    const emailCurat = email.trim();
    if (!emailCurat || !/^\S+@\S+\.\S+$/.test(emailCurat)) {
      setEroare("Introdu un email valid.");
      return;
    }

    setSeConecteaza(true);
    try {
      await sendPasswordResetEmail(getFirebaseAuth(), emailCurat);
    } catch (err) {
      // Nu dezvăluim dacă emailul există sau nu în sistem — din perspectiva
      // utilizatorului, mesajul de succes e mereu același.
      console.error("Eroare la trimiterea resetării de parolă:", err);
    } finally {
      setResetareTrimisa(true);
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
      <div className="bg-white rounded-2xl shadow-sm border border-gray-100 p-8 w-full max-w-sm">
        <div className="text-center mb-6">
          <div className="w-10 h-10 rounded-xl bg-brand-primary flex items-center justify-center text-white font-bold mx-auto mb-3">
            P
          </div>
          <h1 className="text-lg font-semibold text-gray-900">
            {mod === "login" ? "Autentificare Admin" : "Resetare parolă"}
          </h1>
          <p className="text-sm text-gray-500 mt-1">PosterART</p>
        </div>

        {mod === "login" ? (
          <form onSubmit={conecteaza} className="space-y-4">
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

            <button
              type="button"
              onClick={() => schimbaMod("resetare")}
              className="w-full text-center text-sm text-gray-500 hover:text-brand-primary transition-colors"
            >
              Ai uitat parola?
            </button>
          </form>
        ) : resetareTrimisa ? (
          <div className="space-y-4">
            <div className="flex items-start gap-2 text-emerald-700 text-sm bg-emerald-50 rounded-xl px-3.5 py-3">
              <CheckCircle2 className="w-4 h-4 flex-shrink-0 mt-0.5" />
              <span>
                Dacă există un cont cu adresa <strong>{email.trim()}</strong>, vei primi în scurt timp un
                email cu un link de resetare a parolei.
              </span>
            </div>
            <button
              type="button"
              onClick={() => schimbaMod("login")}
              className="w-full flex items-center justify-center gap-2 rounded-xl border border-gray-200 text-gray-700 font-medium py-2.5 hover:bg-gray-50 transition-colors"
            >
              Înapoi la autentificare
            </button>
          </div>
        ) : (
          <form onSubmit={trimiteResetarea} className="space-y-4">
            <p className="text-sm text-gray-500">
              Introdu adresa de email a contului tău și îți trimitem un link pentru resetarea parolei.
            </p>
            <input
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="Email"
              autoComplete="username"
              className={stilInput}
            />

            {eroare && <p className="text-xs text-red-600">{eroare}</p>}

            <button
              type="submit"
              disabled={seConecteaza}
              className="w-full flex items-center justify-center gap-2 bg-brand-primary text-white font-medium py-2.5 rounded-xl hover:bg-brand-primary-dark transition-colors disabled:opacity-60"
            >
              {seConecteaza ? <Loader2 className="w-4 h-4 animate-spin" /> : "Trimite link de resetare"}
            </button>

            <button
              type="button"
              onClick={() => schimbaMod("login")}
              className="w-full text-center text-sm text-gray-500 hover:text-brand-primary transition-colors"
            >
              Înapoi la autentificare
            </button>
          </form>
        )}
      </div>
    </div>
  );
}
