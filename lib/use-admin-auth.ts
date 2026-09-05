"use client";

import { useEffect, useState } from "react";
import { onAuthStateChanged, type User } from "firebase/auth";
import { getFirebaseAuth } from "@/lib/firebase";

export function useAdminAuth() {
  const [user, setUser] = useState<User | null>(null);
  const [seIncarca, setSeIncarca] = useState(true);

  useEffect(() => {
    const unsubscribe = onAuthStateChanged(getFirebaseAuth(), (u) => {
      setUser(u);
      setSeIncarca(false);
    });
    return () => unsubscribe();
  }, []);

  return { user, seIncarca };
}
