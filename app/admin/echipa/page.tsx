"use client";

import { useEffect, useMemo, useState } from "react";
import {
  collection,
  doc,
  deleteDoc,
  getDoc,
  onSnapshot,
  serverTimestamp,
  setDoc,
  updateDoc,
  Timestamp,
} from "firebase/firestore";
import { sendSignInLinkToEmail } from "firebase/auth";
import { db, getFirebaseAuth } from "@/lib/firebase";
import { useAdminAuth } from "@/lib/use-admin-auth";
import { LISTA_ROLURI, ROLURI, esteRolValid, type CheieRol, type StatusAngajat } from "@/lib/admin-roles";
import { ConfirmDialog, type ConfirmDialogState } from "@/components/ConfirmDialog";
import {
  UserPlus,
  Loader2,
  Mail,
  CheckCircle2,
  X,
  Clock,
  Ban,
  RotateCcw,
  Trash2,
  ShieldCheck,
} from "lucide-react";

// ---------------------------------------------------------------------------
// Tipuri
// ---------------------------------------------------------------------------

interface AngajatDoc {
  email: string;
  nume: string;
  rol: CheieRol;
  status: StatusAngajat;
  invitatDe: string;
  dataInvitatie: Timestamp | null;
  dataActivare: Timestamp | null;
}

const ETICHETA_STATUS: Record<StatusAngajat, { text: string; clasa: string }> = {
  invitat: { text: "Invitat", clasa: "bg-amber-50 text-amber-700" },
  activ: { text: "Activ", clasa: "bg-emerald-50 text-emerald-700" },
  dezactivat: { text: "Dezactivat", clasa: "bg-gray-100 text-gray-500" },
};

const stilInput =
  "w-full rounded-xl border border-gray-200 px-3 py-2.5 text-sm focus:outline-none focus:ring-2 focus:ring-brand-primary/30 focus:border-brand-primary";

function formateazaData(ts: Timestamp | null): string {
  if (!ts) return "—";
  return ts.toDate().toLocaleDateString("ro-RO", { day: "2-digit", month: "short", year: "numeric" });
}

async function trimiteEmailInvitatie(emailCheie: string) {
  const actionCodeSettings = {
    url: `${window.location.origin}/admin/inregistrare?email=${encodeURIComponent(emailCheie)}`,
    handleCodeInApp: true,
  };
  await sendSignInLinkToEmail(getFirebaseAuth(), emailCheie, actionCodeSettings);
}

// ---------------------------------------------------------------------------
// Pagina
// ---------------------------------------------------------------------------

export default function EchipaPage() {
  const { user } = useAdminAuth();
  const [angajati, setAngajati] = useState<AngajatDoc[]>([]);
  const [seIncarca, setSeIncarca] = useState(true);

  const [email, setEmail] = useState("");
  const [nume, setNume] = useState("");
  const [rol, setRol] = useState<CheieRol>("grafician");
  const [seTrimite, setSeTrimite] = useState(false);
  const [eroareForm, setEroareForm] = useState<string | null>(null);
  const [succesForm, setSuccesForm] = useState<string | null>(null);

  const [idInLucru, setIdInLucru] = useState<string | null>(null);
  const [confirmare, setConfirmare] = useState<ConfirmDialogState | null>(null);

  const emailCurent = user?.email?.toLowerCase().trim() ?? null;

  useEffect(() => {
    const unsubscribe = onSnapshot(
      collection(db, "admin_users"),
      (snapshot) => {
        const lista: AngajatDoc[] = snapshot.docs
          .map((docSnap) => {
            const data = docSnap.data();
            return {
              email: docSnap.id,
              nume: data.nume ?? "",
              rol: esteRolValid(data.rol) ? data.rol : "grafician",
              status: data.status === "activ" || data.status === "dezactivat" ? data.status : "invitat",
              invitatDe: data.invitatDe ?? "",
              dataInvitatie: data.dataInvitatie ?? null,
              dataActivare: data.dataActivare ?? null,
            };
          })
          .sort((a, b) => (b.dataInvitatie?.toMillis() ?? 0) - (a.dataInvitatie?.toMillis() ?? 0));
        setAngajati(lista);
        setSeIncarca(false);
      },
      (err) => {
        console.error("Eroare la citirea echipei:", err);
        setSeIncarca(false);
      }
    );
    return () => unsubscribe();
  }, []);

  const descriereRolSelectat = useMemo(() => ROLURI[rol]?.descriere ?? "", [rol]);

  async function trimiteInvitatia(e: React.FormEvent) {
    e.preventDefault();
    setEroareForm(null);
    setSuccesForm(null);

    const emailCheie = email.trim().toLowerCase();
    if (!emailCheie || !/^\S+@\S+\.\S+$/.test(emailCheie)) {
      setEroareForm("Introdu un email valid.");
      return;
    }
    if (!nume.trim()) {
      setEroareForm("Introdu numele angajatului.");
      return;
    }

    setSeTrimite(true);
    try {
      const refDoc = doc(db, "admin_users", emailCheie);
      const existent = await getDoc(refDoc);
      if (existent.exists() && existent.data().status !== "dezactivat") {
        setEroareForm("Există deja un cont sau o invitație pentru acest email.");
        return;
      }

      await setDoc(refDoc, {
        email: emailCheie,
        nume: nume.trim(),
        rol,
        status: "invitat",
        invitatDe: emailCurent ?? "necunoscut",
        dataInvitatie: serverTimestamp(),
        dataActivare: null,
        uid: null,
      });

      await trimiteEmailInvitatie(emailCheie);

      setSuccesForm(`Invitație trimisă către ${emailCheie}.`);
      setEmail("");
      setNume("");
      setRol("grafician");
    } catch (err) {
      console.error("Eroare la trimiterea invitației:", err);
      setEroareForm(
        "A apărut o eroare la trimiterea invitației. Verifică dacă „Email Link (passwordless sign-in)” este activat în Firebase Console → Authentication → Sign-in method."
      );
    } finally {
      setSeTrimite(false);
    }
  }

  async function retrimiteInvitatia(a: AngajatDoc) {
    setIdInLucru(a.email);
    setEroareForm(null);
    setSuccesForm(null);
    try {
      await trimiteEmailInvitatie(a.email);
      await updateDoc(doc(db, "admin_users", a.email), { dataInvitatie: serverTimestamp() });
      setSuccesForm(`Invitație retrimisă către ${a.email}.`);
    } catch (err) {
      console.error("Eroare la retrimiterea invitației:", err);
      setEroareForm("A apărut o eroare la retrimiterea invitației.");
    } finally {
      setIdInLucru(null);
    }
  }

  async function schimbaRol(a: AngajatDoc, rolNou: CheieRol) {
    setIdInLucru(a.email);
    try {
      await updateDoc(doc(db, "admin_users", a.email), { rol: rolNou });
    } catch (err) {
      console.error("Eroare la schimbarea rolului:", err);
    } finally {
      setIdInLucru(null);
    }
  }

  async function comutaStatus(a: AngajatDoc) {
    const statusNou: StatusAngajat = a.status === "dezactivat" ? "activ" : "dezactivat";
    setIdInLucru(a.email);
    try {
      await updateDoc(doc(db, "admin_users", a.email), { status: statusNou });
    } catch (err) {
      console.error("Eroare la actualizarea statusului:", err);
    } finally {
      setIdInLucru(null);
    }
  }

  function stergeAngajat(a: AngajatDoc) {
    setConfirmare({
      titlu: "Șterge angajatul",
      mesaj: `Ștergi definitiv contul „${a.nume || a.email}"? Nu va mai putea accesa panoul de administrare.`,
      onConfirm: async () => {
        await deleteDoc(doc(db, "admin_users", a.email));
      },
    });
  }

  return (
    <div className="animate-in fade-in">
      <div className="mb-6">
        <h1 className="text-2xl font-semibold text-gray-900">Echipă</h1>
        <p className="text-sm text-gray-500 mt-1">
          Invită angajați prin email și gestionează rolurile lor de acces în panou.
        </p>
      </div>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        <form
          onSubmit={trimiteInvitatia}
          className="bg-white rounded-2xl border border-gray-100 shadow-sm p-6 space-y-4 h-fit lg:col-span-1"
        >
          <h3 className="font-medium text-gray-900 flex items-center gap-2">
            <UserPlus className="w-4 h-4 text-brand-primary" />
            Invită angajat
          </h3>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1.5">Nume</label>
            <input
              type="text"
              value={nume}
              onChange={(e) => setNume(e.target.value)}
              placeholder="ex: Ana Popescu"
              className={stilInput}
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1.5">Email</label>
            <input
              type="email"
              value={email}
              onChange={(e) => setEmail(e.target.value)}
              placeholder="ex: ana@posterart.ro"
              className={stilInput}
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700 mb-1.5">Rol</label>
            <select value={rol} onChange={(e) => setRol(e.target.value as CheieRol)} className={stilInput}>
              {LISTA_ROLURI.map((cheie) => (
                <option key={cheie} value={cheie}>
                  {ROLURI[cheie].eticheta}
                </option>
              ))}
            </select>
            <p className="text-xs text-gray-400 mt-1.5">{descriereRolSelectat}</p>
          </div>

          {eroareForm && (
            <div className="flex items-start gap-2 text-red-600 text-xs bg-red-50 rounded-xl px-3 py-2.5">
              <X className="w-3.5 h-3.5 flex-shrink-0 mt-0.5" />
              {eroareForm}
            </div>
          )}
          {succesForm && (
            <div className="flex items-center gap-2 text-emerald-700 text-xs bg-emerald-50 rounded-xl px-3 py-2.5">
              <CheckCircle2 className="w-3.5 h-3.5 flex-shrink-0" />
              {succesForm}
            </div>
          )}

          <button
            type="submit"
            disabled={seTrimite}
            className="w-full flex items-center justify-center gap-2 bg-brand-primary text-white font-medium py-2.5 rounded-xl hover:bg-brand-primary-dark transition-colors disabled:opacity-60"
          >
            {seTrimite ? <Loader2 className="w-4 h-4 animate-spin" /> : <Mail className="w-4 h-4" />}
            Trimite invitație
          </button>
        </form>

        <div className="bg-white rounded-2xl border border-gray-100 shadow-sm overflow-hidden lg:col-span-2">
          <div className="px-6 py-4 border-b border-gray-100">
            <h3 className="font-medium text-gray-900">Membrii echipei</h3>
          </div>

          {seIncarca ? (
            <div className="flex items-center justify-center h-48 text-gray-400">
              <Loader2 className="w-6 h-6 animate-spin mr-2" />
              Se încarcă echipa...
            </div>
          ) : angajati.length === 0 ? (
            <div className="p-12 text-center text-gray-400">Nicio invitație trimisă încă.</div>
          ) : (
            <div className="overflow-x-auto">
              <table className="w-full text-sm">
                <thead>
                  <tr className="border-b border-gray-100 text-left text-gray-500">
                    <th className="px-6 py-3 font-medium">Nume / Email</th>
                    <th className="px-6 py-3 font-medium">Rol</th>
                    <th className="px-6 py-3 font-medium">Status</th>
                    <th className="px-6 py-3 font-medium">Invitat la</th>
                    <th className="px-6 py-3 font-medium text-right">Acțiuni</th>
                  </tr>
                </thead>
                <tbody>
                  {angajati.map((a) => {
                    const esteEuInsumi = a.email === emailCurent;
                    const inLucru = idInLucru === a.email;
                    const statusInfo = ETICHETA_STATUS[a.status];
                    return (
                      <tr key={a.email} className="border-b border-gray-50 last:border-0 hover:bg-gray-50/60">
                        <td className="px-6 py-3">
                          <div className="flex items-center gap-3">
                            <div className="w-8 h-8 rounded-full bg-brand-primary/10 text-brand-primary text-xs font-semibold flex items-center justify-center flex-shrink-0">
                              {(a.nume || a.email).slice(0, 1).toUpperCase()}
                            </div>
                            <div className="min-w-0">
                              <p className="font-medium text-gray-900 truncate flex items-center gap-1.5">
                                {a.nume || "—"}
                                {esteEuInsumi && <ShieldCheck className="w-3.5 h-3.5 text-brand-primary" />}
                              </p>
                              <p className="text-xs text-gray-400 truncate">{a.email}</p>
                            </div>
                          </div>
                        </td>
                        <td className="px-6 py-3">
                          <select
                            value={a.rol}
                            onChange={(e) => schimbaRol(a, e.target.value as CheieRol)}
                            disabled={inLucru || esteEuInsumi}
                            className="rounded-lg border border-gray-200 px-2 py-1.5 text-xs focus:outline-none focus:ring-2 focus:ring-brand-primary/30 focus:border-brand-primary disabled:opacity-60 disabled:bg-gray-50"
                          >
                            {LISTA_ROLURI.map((cheie) => (
                              <option key={cheie} value={cheie}>
                                {ROLURI[cheie].eticheta}
                              </option>
                            ))}
                          </select>
                        </td>
                        <td className="px-6 py-3">
                          <span className={`text-xs font-medium px-2.5 py-1 rounded-full ${statusInfo.clasa}`}>
                            {statusInfo.text}
                          </span>
                        </td>
                        <td className="px-6 py-3 text-gray-500 flex items-center gap-1.5">
                          <Clock className="w-3.5 h-3.5 text-gray-400" />
                          {formateazaData(a.dataInvitatie)}
                        </td>
                        <td className="px-6 py-3">
                          <div className="flex items-center justify-end gap-3">
                            {a.status === "invitat" && (
                              <button
                                onClick={() => retrimiteInvitatia(a)}
                                disabled={inLucru}
                                className="text-gray-400 hover:text-brand-primary transition-colors disabled:opacity-40"
                                aria-label="Retrimite invitația"
                                title="Retrimite invitația"
                              >
                                {inLucru ? (
                                  <Loader2 className="w-4 h-4 animate-spin" />
                                ) : (
                                  <RotateCcw className="w-4 h-4" />
                                )}
                              </button>
                            )}
                            {a.status !== "invitat" && !esteEuInsumi && (
                              <button
                                onClick={() => comutaStatus(a)}
                                disabled={inLucru}
                                className={`transition-colors disabled:opacity-40 ${
                                  a.status === "activ"
                                    ? "text-gray-400 hover:text-red-500"
                                    : "text-gray-400 hover:text-emerald-600"
                                }`}
                                aria-label={a.status === "activ" ? "Dezactivează" : "Activează"}
                                title={a.status === "activ" ? "Dezactivează accesul" : "Reactivează accesul"}
                              >
                                {a.status === "activ" ? (
                                  <Ban className="w-4 h-4" />
                                ) : (
                                  <CheckCircle2 className="w-4 h-4" />
                                )}
                              </button>
                            )}
                            {!esteEuInsumi && (
                              <button
                                onClick={() => stergeAngajat(a)}
                                disabled={inLucru}
                                className="text-gray-300 hover:text-red-500 transition-colors disabled:opacity-40"
                                aria-label="Șterge angajatul"
                                title="Șterge definitiv"
                              >
                                <Trash2 className="w-4 h-4" />
                              </button>
                            )}
                          </div>
                        </td>
                      </tr>
                    );
                  })}
                </tbody>
              </table>
            </div>
          )}
        </div>
      </div>

      <ConfirmDialog stare={confirmare} onClose={() => setConfirmare(null)} />
    </div>
  );
}
