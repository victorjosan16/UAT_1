import { initializeApp, type FirebaseApp } from "firebase/app";
import { getAuth, signInAnonymously, onAuthStateChanged, connectAuthEmulator, type Auth, type User } from "firebase/auth";

/**
 * Firebase API keys are not secret (see Firebase's own docs) — safety
 * comes from Firestore rules + App Check, not from hiding these values.
 * They still live in env vars (not hardcoded) so a fork can point at its
 * own Firebase project without touching code. See .env.example.
 */
const firebaseConfig = {
  apiKey: import.meta.env.VITE_FIREBASE_API_KEY,
  authDomain: import.meta.env.VITE_FIREBASE_AUTH_DOMAIN,
  projectId: import.meta.env.VITE_FIREBASE_PROJECT_ID,
  storageBucket: import.meta.env.VITE_FIREBASE_STORAGE_BUCKET,
  messagingSenderId: import.meta.env.VITE_FIREBASE_MESSAGING_SENDER_ID,
  appId: import.meta.env.VITE_FIREBASE_APP_ID,
};

let app: FirebaseApp | null = null;
let auth: Auth | null = null;
let emulatorConnected = false;

function ensureApp(): FirebaseApp {
  if (!app) app = initializeApp(firebaseConfig);
  return app;
}

export function getFirebaseAuth(): Auth {
  if (!auth) {
    auth = getAuth(ensureApp());
    if (import.meta.env.DEV && import.meta.env.VITE_USE_FIREBASE_EMULATORS === "true" && !emulatorConnected) {
      connectAuthEmulator(auth, "http://127.0.0.1:9099", { disableWarnings: true });
      emulatorConnected = true;
    }
  }
  return auth;
}

let signInPromise: Promise<User> | null = null;

/**
 * Resolves once a Firebase user (anonymous by default) is signed in.
 * Firebase persists anonymous sessions across reloads, so a returning
 * player keeps the same uid — this *is* the guest player id (see
 * docs/GAME_DESIGN.md §Guest Player System / §23 in the original brief).
 */
export function ensureSignedIn(): Promise<User> {
  if (signInPromise) return signInPromise;

  signInPromise = new Promise((resolve, reject) => {
    const auth = getFirebaseAuth();
    const unsubscribe = onAuthStateChanged(
      auth,
      (user) => {
        if (user) {
          unsubscribe();
          resolve(user);
        }
      },
      (error) => {
        unsubscribe();
        reject(error);
      },
    );
    if (!auth.currentUser) {
      signInAnonymously(auth).catch((error: unknown) => {
        unsubscribe();
        reject(error);
      });
    }
  });

  return signInPromise;
}
