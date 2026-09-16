import { createContext, useContext, useMemo, useState } from "react";
import { LocalStorageService } from "@/storage/LocalStorage";
import { translate, type StringKey } from "./strings";
import type { Language } from "@/types";

export interface LanguageContextValue {
  language: Language;
  setLanguage: (language: Language) => void;
  t: (key: StringKey, vars?: Record<string, string | number>) => string;
}

const LanguageContext = createContext<LanguageContextValue | null>(null);

export function LanguageProvider({ children }: { children: React.ReactNode }) {
  const [language, setLanguageState] = useState<Language>(() => LocalStorageService.getLanguage());

  const value = useMemo<LanguageContextValue>(
    () => ({
      language,
      setLanguage: (next) => {
        LocalStorageService.setLanguage(next);
        setLanguageState(next);
      },
      t: (key, vars) => translate(key, language, vars),
    }),
    [language],
  );

  return <LanguageContext.Provider value={value}>{children}</LanguageContext.Provider>;
}

/** Every screen reads UI copy through this — never hardcode English (or any language) directly in a component. */
export function useLanguage(): LanguageContextValue {
  const ctx = useContext(LanguageContext);
  if (!ctx) throw new Error("useLanguage must be used within a LanguageProvider");
  return ctx;
}
