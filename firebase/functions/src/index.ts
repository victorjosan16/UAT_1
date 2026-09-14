import { onRequest } from "firebase-functions/v2/https";
import { app } from "./app";

/**
 * Single HTTPS function serving the whole /api/* surface (Express
 * handles routing internally) — Firebase Hosting rewrites "/api/**" to
 * this function (see firebase/firebase.json), so the client's
 * ApiClient never needs to know a function even exists.
 */
export const api = onRequest({ region: "us-central1", cors: true }, app);
