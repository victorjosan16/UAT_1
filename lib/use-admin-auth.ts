"use client";

import { useEffect, useState } from "react";
import { onAuthStateChanged, type User } from "firebase/auth";
import { doc, onSnapshot, serverTimestamp, setDoc } from "firebase/firestore";
import { db, getFirebaseAuth } from "@/lib/firebase";
import { EMAIL_PROPRIETAR, esteRolValid, type AngajatCurent } from "@/lib/admin-roles";

export function useAdminAuth() {
  const [user, setUser] = useState<User | null>(null);
  const [seIncarcaAuth, setSeIncarcaAuth] = useState(true);
  const [angajat, setAngajat] = useState<AngajatCurent | null>(null);
  const [seIncarcaAngajat, setSeIncarcaAngajat] = useState(true);

  useEffect(() => {
    const unsubscribe = onAuthStateChanged(getFirebaseAuth(), (u) => {
      setUser(u);
      setSeIncarcaAuth(false);
    });
    return () => unsubscribe();
  }, []);

  useEffect(() => {
    const email = user?.email?.toLowerCase().trim() ?? null;
    if (!email) {
      setAngajat(null);
      setSeIncarcaAngajat(false);
      return;
    }

    setSeIncarcaAngajat(true);
    const refAngajat = doc(db, "admin_users", email);

    const unsubscribe = onSnapshot(
      refAngajat,
      async (snap) => {
        const data = snap.data();
        if (data && esteRolValid(data.rol)) {
          setAngajat({
            rol: data.rol,
            status: data.status === "activ" || data.status === "dezactivat" ? data.status : "invitat",
            nume: data.nume ?? "",
            email,
          });
        } else if (email === EMAIL_PROPRIETAR) {
          // Bootstrap: contul proprietar își creează automat documentul de
          // administrator la prima logare, dacă nu există încă unul.
          try {
            await setDoc(refAngajat, {
              email,
              nume: "",
              rol: "administrator",
              status: "activ",
              invitatDe: "sistem",
              dataInvitatie: serverTimestamp(),
              dataActivare: serverTimestamp(),
              uid: user?.uid ?? null,
            });
          } catch (err) {
            console.error("Eroare la crearea contului de proprietar:", err);
          }
        } else {
          setAngajat(null);
        }
        setSeIncarcaAngajat(false);
      },
      (err) => {
        console.error("Eroare la citirea contului de angajat:", err);
        setAngajat(null);
        setSeIncarcaAngajat(false);
      }
    );

    return () => unsubscribe();
  }, [user?.email, user?.uid]);

  return { user, seIncarca: seIncarcaAuth || (!!user && seIncarcaAngajat), angajat };
}
