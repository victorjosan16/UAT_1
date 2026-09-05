"use client";

import {
  createContext,
  useContext,
  useEffect,
  useMemo,
  useRef,
  useState,
  type ReactNode,
} from "react";
import { collection, doc, onSnapshot, addDoc, serverTimestamp } from "firebase/firestore";
import { db } from "@/lib/firebase";

// ---------------------------------------------------------------------------
// Tipuri
// ---------------------------------------------------------------------------

export interface Produs {
  id: string;
  nume: string;
  descriere: string;
  pret: number;
  pretRedus: number | null;
  sku: string;
  stoc: number;
  group_id: string;
  imagini: string[];
  vizibil: boolean;
}

export interface ItemCos {
  produsId: string;
  nume: string;
  pretUnitar: number;
  imagine: string | null;
  cantitate: number;
}

export interface LinkMeniu {
  id: string;
  eticheta: string;
  link: string;
  vizibil: boolean;
}

export interface ConfigHeader {
  logoText: string;
  locatie: string;
  telefon: string;
  program: string;
  textButonLogin: string;
  linkuriMeniu: LinkMeniu[];
}

export const CONFIG_HEADER_IMPLICIT: ConfigHeader = {
  logoText: "PosterART",
  locatie: "",
  telefon: "",
  program: "",
  textButonLogin: "",
  linkuriMeniu: [],
};

const CHEIE_COS_LOCALSTORAGE = "posterart_cos";

interface SiteContextValue {
  configHeader: ConfigHeader;
  produseDisponibile: Produs[];
  cos: ItemCos[];
  cosDeschis: boolean;
  setCosDeschis: (deschis: boolean) => void;
  adaugaInCos: (produs: Produs, cantitate?: number) => void;
  modificaCantitate: (produsId: string, delta: number) => void;
  eliminaDinCos: (produsId: string) => void;
  totalCos: number;
  numarItemiCos: number;
  comandaTrimisa: boolean;
  numarComandaFinalizata: string | null;
  seTrimite: boolean;
  eroareComanda: string | null;
  numeClient: string;
  telefonClient: string;
  setNumeClient: (nume: string) => void;
  setTelefonClient: (telefon: string) => void;
  trimiteComanda: (honeypot?: string) => Promise<void>;
  inchideCos: () => void;
  toast: string | null;
}

// ---------------------------------------------------------------------------
// Helpers
// ---------------------------------------------------------------------------

export function formateazaPret(pret: number): string {
  return new Intl.NumberFormat("ro-RO", { minimumFractionDigits: 0 }).format(pret) + " lei";
}

export function pretEfectiv(produs: Produs): number {
  return produs.pretRedus !== null && produs.pretRedus !== undefined && produs.pretRedus < produs.pret
    ? produs.pretRedus
    : produs.pret;
}

// ---------------------------------------------------------------------------
// Context
// ---------------------------------------------------------------------------

const SiteContext = createContext<SiteContextValue | null>(null);

export function useSite(): SiteContextValue {
  const context = useContext(SiteContext);
  if (!context) throw new Error("useSite() trebuie folosit sub <SiteProvider>.");
  return context;
}

export function SiteProvider({ children }: { children: ReactNode }) {
  const [configHeader, setConfigHeader] = useState<ConfigHeader>(CONFIG_HEADER_IMPLICIT);
  const [produse, setProduse] = useState<Produs[]>([]);

  const [cosDeschis, setCosDeschis] = useState(false);
  const [cos, setCos] = useState<ItemCos[]>([]);
  const [cosIncarcatDinStocare, setCosIncarcatDinStocare] = useState(false);
  const [comandaTrimisa, setComandaTrimisa] = useState(false);
  const [numarComandaFinalizata, setNumarComandaFinalizata] = useState<string | null>(null);
  const [seTrimite, setSeTrimite] = useState(false);
  const [eroareComanda, setEroareComanda] = useState<string | null>(null);
  const [numeClient, setNumeClient] = useState("");
  const [telefonClient, setTelefonClient] = useState("");
  const [toast, setToast] = useState<string | null>(null);
  const toastTimeoutRef = useRef<ReturnType<typeof setTimeout> | null>(null);

  // Coșul supraviețuiește la refresh — citit o singură dată la montare.
  useEffect(() => {
    try {
      const stocat = localStorage.getItem(CHEIE_COS_LOCALSTORAGE);
      if (stocat) {
        const parsat = JSON.parse(stocat);
        if (Array.isArray(parsat)) setCos(parsat);
      }
    } catch (err) {
      console.error("Eroare la citirea coșului salvat:", err);
    } finally {
      setCosIncarcatDinStocare(true);
    }
  }, []);

  // ...și se salvează la fiecare schimbare, după ce citirea inițială s-a
  // terminat (altfel am suprascrie coșul salvat cu unul gol la montare).
  useEffect(() => {
    if (!cosIncarcatDinStocare) return;
    try {
      localStorage.setItem(CHEIE_COS_LOCALSTORAGE, JSON.stringify(cos));
    } catch (err) {
      console.error("Eroare la salvarea coșului:", err);
    }
  }, [cos, cosIncarcatDinStocare]);

  // Configurarea antetului — live din Firestore
  useEffect(() => {
    const unsubscribe = onSnapshot(doc(db, "site_config", "header"), (snap) => {
      const data = snap.data();
      if (data) {
        setConfigHeader({
          logoText: data.logoText ?? CONFIG_HEADER_IMPLICIT.logoText,
          locatie: data.locatie ?? "",
          telefon: data.telefon ?? "",
          program: data.program ?? "",
          textButonLogin: data.textButonLogin ?? "",
          linkuriMeniu: Array.isArray(data.linkuriMeniu) ? data.linkuriMeniu : [],
        });
      }
    });
    return () => unsubscribe();
  }, []);

  // Produse — live din Firestore
  useEffect(() => {
    const unsubscribe = onSnapshot(
      collection(db, "products"),
      (snapshot) => {
        const lista: Produs[] = snapshot.docs.map((docSnap) => {
          const data = docSnap.data();
          return {
            id: docSnap.id,
            nume: data.nume ?? "Produs",
            descriere: data.descriere ?? "",
            pret: typeof data.pret === "number" ? data.pret : 0,
            pretRedus: typeof data.pretRedus === "number" ? data.pretRedus : null,
            sku: data.sku ?? "",
            stoc: typeof data.stoc === "number" ? data.stoc : 0,
            group_id: data.group_id ?? "",
            imagini: Array.isArray(data.imagini) ? data.imagini : [],
            vizibil: data.vizibil !== false,
          };
        });
        setProduse(lista);
      },
      () => {}
    );
    return () => unsubscribe();
  }, []);

  // Ascunde automat produsele fără stoc sau marcate ca invizibile
  const produseDisponibile = useMemo(() => produse.filter((p) => p.vizibil && p.stoc > 0), [produse]);

  function afiseazaToast(mesaj: string) {
    setToast(mesaj);
    if (toastTimeoutRef.current) clearTimeout(toastTimeoutRef.current);
    toastTimeoutRef.current = setTimeout(() => setToast(null), 2500);
  }

  function adaugaInCos(produs: Produs, cantitate: number = 1) {
    setCos((prev) => {
      const existent = prev.find((item) => item.produsId === produs.id);
      if (existent) {
        return prev.map((item) =>
          item.produsId === produs.id
            ? { ...item, cantitate: Math.min(item.cantitate + cantitate, produs.stoc) }
            : item
        );
      }
      return [
        ...prev,
        {
          produsId: produs.id,
          nume: produs.nume,
          pretUnitar: pretEfectiv(produs),
          imagine: produs.imagini[0] ?? null,
          cantitate: Math.min(cantitate, produs.stoc),
        },
      ];
    });
    setCosDeschis(true);
    afiseazaToast(`${produs.nume} adăugat în coș`);
  }

  function modificaCantitate(produsId: string, delta: number) {
    setCos((prev) =>
      prev.map((item) => (item.produsId === produsId ? { ...item, cantitate: item.cantitate + delta } : item)).filter(
        (item) => item.cantitate > 0
      )
    );
  }

  function eliminaDinCos(produsId: string) {
    setCos((prev) => prev.filter((item) => item.produsId !== produsId));
  }

  const totalCos = useMemo(() => cos.reduce((suma, item) => suma + item.pretUnitar * item.cantitate, 0), [cos]);
  const numarItemiCos = useMemo(() => cos.reduce((suma, item) => suma + item.cantitate, 0), [cos]);

  async function trimiteComanda(honeypot: string = "") {
    setEroareComanda(null);

    if (cos.length === 0) return;
    if (!numeClient.trim() || !telefonClient.trim()) {
      setEroareComanda("Te rugăm să completezi numele și telefonul.");
      return;
    }

    // Câmp-capcană invizibil pentru oameni, dar completat de bots. Dacă are
    // conținut, prefacem succesul fără să scriem nimic în Firestore.
    if (honeypot.trim() !== "") {
      setNumarComandaFinalizata(null);
      setComandaTrimisa(true);
      setCos([]);
      setNumeClient("");
      setTelefonClient("");
      return;
    }

    setSeTrimite(true);
    try {
      const produseText = cos.map((item) => `${item.cantitate}x ${item.nume}`).join(" + ");
      const refComanda = await addDoc(collection(db, "orders"), {
        client: numeClient.trim(),
        telefon: telefonClient.trim(),
        suma: totalCos,
        produse: produseText,
        status: "noua",
        termen: "",
        data_creare: serverTimestamp(),
      });
      setNumarComandaFinalizata(refComanda.id.slice(0, 6).toUpperCase());
      setComandaTrimisa(true);
      setCos([]);
      setNumeClient("");
      setTelefonClient("");
    } catch (err) {
      console.error("Eroare la trimiterea comenzii:", err);
      setEroareComanda("A apărut o eroare. Te rugăm să încerci din nou.");
    } finally {
      setSeTrimite(false);
    }
  }

  function inchideCos() {
    setCosDeschis(false);
    setComandaTrimisa(false);
    setEroareComanda(null);
    setNumarComandaFinalizata(null);
  }

  const value: SiteContextValue = {
    configHeader,
    produseDisponibile,
    cos,
    cosDeschis,
    setCosDeschis,
    adaugaInCos,
    modificaCantitate,
    eliminaDinCos,
    totalCos,
    numarItemiCos,
    comandaTrimisa,
    numarComandaFinalizata,
    seTrimite,
    eroareComanda,
    numeClient,
    telefonClient,
    setNumeClient,
    setTelefonClient,
    trimiteComanda,
    inchideCos,
    toast,
  };

  return <SiteContext.Provider value={value}>{children}</SiteContext.Provider>;
}
