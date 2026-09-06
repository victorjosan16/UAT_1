"use client";

import { Suspense, useEffect, useRef, useState } from "react";
import { useRouter, useSearchParams } from "next/navigation";
import {
  isSignInWithEmailLink,
  signInWithEmailLink,
  updatePassword,
  type User,
} from "firebase/auth";
import { doc, serverTimestamp, setDoc } from "firebase/firestore";
import { db, getFirebaseAuth } from "@/lib/firebase";
import { Loader2, CheckCircle2, XCircle, Lock } from "lucide-react";

const stilInput =
  "w-full rounded-xl border border-gray-200 px-3 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-brand-primary/30 focus:border-brand-primary";

type Stare = "verificare" | "confirma_email" | "parola" | "succes" | "eroare";

export default function InregistrarePage() {
  return (
    <Suspense
      fallback={
        <div className="min-h-screen flex items-center justify-center bg-gray-50">
          <Loader2 className="w-6 h-6 animate-spin text-gray-400" />
        </div>
      }
    >
      <InregistrareContinut />
    </Suspense>
  );
}

function InregistrareContinut() {
  const router = useRouter();
  const searchParams = useSearchParams();

  const [stare, setStare] = useState<Stare>("verificare");
  const [email, setEmail] = useState(searchParams.get("email") ?? "");
  const [nume, setNume] = useState("");
  const [parola, setParola] = useState("");
  const [parola2, setParola2] = useState("");
  const [eroare, setEroare] = useState<string | null>(null);
  const [seProceseaza, setSeProceseaza] = useState(false);
  const userRef = useRef<User | null>(null);

  useEffect(() => {
    const auth = getFirebaseAuth();
    if (!isSignInWithEmailLink(auth, window.location.href)) {
      setEroare("Linkul de invitație este invalid sau incomplet. Cere administratorului o invitație nouă.");
      setStare("eroare");
      return;
    }

    const emailDinUrl = searchParams.get("email");
    if (emailDinUrl) {
      finalizeazaLogin(emailDinUrl);
    } else {
      setStare("confirma_email");
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  async function finalizeazaLogin(emailFinal: string) {
    setEroare(null);
    setSeProceseaza(true);
    try {
      const auth = getFirebaseAuth();
      const credential = await signInWithEmailLink(auth, emailFinal.trim().toLowerCase(), window.location.href);
      userRef.current = credential.user;
      setStare("parola");
    } catch (err) {
      console.error("Eroare la finalizarea autentificării din invitație:", err);
      setEroare("Linkul de invitație este invalid, a expirat sau a fost deja folosit. Cere administratorului una nouă.");
      setStare("eroare");
    } finally {
      setSeProceseaza(false);
    }
  }

  function confirmaEmail(e: React.FormEvent) {
    e.preventDefault();
    if (!email.trim()) return;
    finalizeazaLogin(email);
  }

  async function finalizeazaInregistrarea(e: React.FormEvent) {
    e.preventDefault();
    setEroare(null);

    if (!nume.trim()) {
      setEroare("Introdu numele tău.");
      return;
    }
    if (parola.length < 6) {
      setEroare("Parola trebuie să aibă minim 6 caractere.");
      return;
    }
    if (parola !== parola2) {
      setEroare("Parolele nu coincid.");
      return;
    }

    const user = userRef.current;
    if (!user || !user.email) {
      setEroare("Sesiunea a expirat. Reia procesul din linkul primit pe email.");
      setStare("eroare");
      return;
    }

    setSeProceseaza(true);
    try {
      await updatePassword(user, parola);
      const emailCheie = user.email.toLowerCase();
      await setDoc(
        doc(db, "admin_users", emailCheie),
        {
          status: "activ",
          uid: user.uid,
          nume: nume.trim(),
          dataActivare: serverTimestamp(),
        },
        { merge: true }
      );
      setStare("succes");
      setTimeout(() => router.replace("/admin"), 1200);
    } catch (err) {
      console.error("Eroare la finalizarea înregistrării:", err);
      setEroare("A apărut o eroare la finalizarea înregistrării. Încearcă din nou.");
    } finally {
      setSeProceseaza(false);
    }
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 px-4">
      <div className="bg-white rounded-2xl shadow-sm border border-gray-100 p-8 w-full max-w-sm">
        <div className="text-center mb-6">
          <div className="w-10 h-10 rounded-xl bg-brand-primary flex items-center justify-center text-white font-bold mx-auto mb-3">
            P
          </div>
          <h1 className="text-lg font-semibold text-gray-900">Finalizează înregistrarea</h1>
          <p className="text-sm text-gray-500 mt-1">PosterART Admin</p>
        </div>

        {(stare === "verificare") && (
          <div className="flex flex-col items-center justify-center gap-3 py-8 text-gray-400">
            <Loader2 className="w-6 h-6 animate-spin" />
            <p className="text-sm">Se verifică invitația...</p>
          </div>
        )}

        {stare === "confirma_email" && (
          <form onSubmit={confirmaEmail} className="space-y-4">
            <p className="text-sm text-gray-500">
              Confirmă adresa de email pe care ai primit invitația, pentru a continua.
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
              disabled={seProceseaza}
              className="w-full flex items-center justify-center gap-2 bg-brand-primary text-white font-medium py-2.5 rounded-xl hover:bg-brand-primary-dark transition-colors disabled:opacity-60"
            >
              {seProceseaza ? <Loader2 className="w-4 h-4 animate-spin" /> : "Continuă"}
            </button>
          </form>
        )}

        {stare === "parola" && (
          <form onSubmit={finalizeazaInregistrarea} className="space-y-4">
            <p className="text-sm text-gray-500">
              Alege-ți numele și o parolă pentru contul tău de <strong>{userRef.current?.email}</strong>.
            </p>
            <input
              type="text"
              value={nume}
              onChange={(e) => setNume(e.target.value)}
              placeholder="Numele tău"
              autoComplete="name"
              className={stilInput}
            />
            <input
              type="password"
              value={parola}
              onChange={(e) => setParola(e.target.value)}
              placeholder="Parolă (minim 6 caractere)"
              autoComplete="new-password"
              className={stilInput}
            />
            <input
              type="password"
              value={parola2}
              onChange={(e) => setParola2(e.target.value)}
              placeholder="Confirmă parola"
              autoComplete="new-password"
              className={stilInput}
            />
            {eroare && <p className="text-xs text-red-600">{eroare}</p>}
            <button
              type="submit"
              disabled={seProceseaza}
              className="w-full flex items-center justify-center gap-2 bg-brand-primary text-white font-medium py-2.5 rounded-xl hover:bg-brand-primary-dark transition-colors disabled:opacity-60"
            >
              {seProceseaza ? <Loader2 className="w-4 h-4 animate-spin" /> : <Lock className="w-4 h-4" />}
              Activează contul
            </button>
          </form>
        )}

        {stare === "succes" && (
          <div className="flex flex-col items-center justify-center gap-3 py-8 text-center">
            <CheckCircle2 className="w-10 h-10 text-emerald-500" />
            <p className="text-sm text-gray-700 font-medium">Cont activat cu succes!</p>
            <p className="text-xs text-gray-400">Te redirecționăm spre panou...</p>
          </div>
        )}

        {stare === "eroare" && (
          <div className="flex flex-col items-center justify-center gap-3 py-6 text-center">
            <XCircle className="w-10 h-10 text-red-500" />
            <p className="text-sm text-gray-700">{eroare}</p>
            <a href="/admin/login" className="text-sm font-medium text-brand-primary hover:underline">
              Mergi la pagina de autentificare
            </a>
          </div>
        )}
      </div>
    </div>
  );
}
