// Email-ul contului "proprietar" — singurul care își poate crea automat
// documentul de admin (bootstrap), inclusiv în reguli Firestore. Trebuie să
// coincidă exact (case-insensitive) cu emailul folosit la logarea în panou.
export const EMAIL_PROPRIETAR = "victor.josan.16@gmail.com";

export type ModulAdmin =
  | "dashboard"
  | "catalog"
  | "storefront"
  | "comenzi"
  | "clienti"
  | "setari"
  | "echipa";

export type CheieRol =
  | "administrator"
  | "manager"
  | "grafician"
  | "productie"
  | "vanzari"
  | "ecommerce";

export type StatusAngajat = "invitat" | "activ" | "dezactivat";

export interface DefinitieRol {
  eticheta: string;
  descriere: string;
  module: ModulAdmin[];
}

export const ROLURI: Record<CheieRol, DefinitieRol> = {
  administrator: {
    eticheta: "Administrator",
    descriere: "Acces total la tot panoul, inclusiv gestionarea angajaților și a invitațiilor.",
    module: ["dashboard", "catalog", "storefront", "comenzi", "clienti", "setari", "echipa"],
  },
  manager: {
    eticheta: "Manager Magazin",
    descriere: "Acces total la operațiuni, fără gestionarea angajaților.",
    module: ["dashboard", "catalog", "storefront", "comenzi", "clienti", "setari"],
  },
  grafician: {
    eticheta: "Grafician / DTP",
    descriere: "Procesează comenzile la etapa de grafică. Fără acces la clienți sau setări.",
    module: ["dashboard", "comenzi"],
  },
  productie: {
    eticheta: "Operator Producție",
    descriere: "Procesează comenzile în producție și gestionează stocul din catalog.",
    module: ["dashboard", "comenzi", "catalog"],
  },
  vanzari: {
    eticheta: "Vânzări / CRM",
    descriere: "Gestionează comenzile și relația cu clienții. Fără acces la catalog sau setări.",
    module: ["dashboard", "comenzi", "clienti"],
  },
  ecommerce: {
    eticheta: "E-commerce / Conținut",
    descriere: "Administrează catalogul, pagina principală (Constructor Pagină) și setările magazinului online.",
    module: ["dashboard", "catalog", "storefront", "setari"],
  },
};

export const LISTA_ROLURI = Object.keys(ROLURI) as CheieRol[];

export function esteRolValid(rol: unknown): rol is CheieRol {
  return typeof rol === "string" && rol in ROLURI;
}

export function poateAccesa(rol: CheieRol, modul: ModulAdmin): boolean {
  return ROLURI[rol]?.module.includes(modul) ?? false;
}

export interface AngajatCurent {
  rol: CheieRol;
  status: StatusAngajat;
  nume: string;
  email: string;
}
