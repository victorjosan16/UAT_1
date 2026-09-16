import { useEffect, useState } from "react";

interface BeforeInstallPromptEvent extends Event {
  prompt(): Promise<void>;
  userChoice: Promise<{ outcome: "accepted" | "dismissed" }>;
}

export interface UsePwaInstallResult {
  /** True once the browser has told us installation is available and the app isn't already installed. */
  canInstall: boolean;
  /** Shows the native install prompt; resolves once the user has responded. */
  promptInstall: () => Promise<void>;
}

function isStandalone(): boolean {
  if (typeof window === "undefined") return false;
  return window.matchMedia?.("(display-mode: standalone)").matches || (window.navigator as unknown as { standalone?: boolean }).standalone === true;
}

/** Wraps the `beforeinstallprompt` event (Chrome/Edge/Android) — Safari/iOS never fires it, so `canInstall` just stays false there. */
export function usePwaInstall(): UsePwaInstallResult {
  const [deferredEvent, setDeferredEvent] = useState<BeforeInstallPromptEvent | null>(null);

  useEffect(() => {
    if (isStandalone()) return;

    function handleBeforeInstallPrompt(e: Event): void {
      e.preventDefault();
      setDeferredEvent(e as BeforeInstallPromptEvent);
    }

    function handleInstalled(): void {
      setDeferredEvent(null);
    }

    window.addEventListener("beforeinstallprompt", handleBeforeInstallPrompt);
    window.addEventListener("appinstalled", handleInstalled);
    return () => {
      window.removeEventListener("beforeinstallprompt", handleBeforeInstallPrompt);
      window.removeEventListener("appinstalled", handleInstalled);
    };
  }, []);

  return {
    canInstall: deferredEvent !== null,
    promptInstall: async () => {
      if (!deferredEvent) return;
      await deferredEvent.prompt();
      await deferredEvent.userChoice;
      setDeferredEvent(null);
    },
  };
}
