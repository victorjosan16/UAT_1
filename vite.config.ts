import { defineConfig } from "vite";
import react from "@vitejs/plugin-react";
import { VitePWA } from "vite-plugin-pwa";
import path from "node:path";
import { fileURLToPath } from "node:url";
import { GAME_NAME, GAME_SUBTITLE, THEME_COLOR, BACKGROUND_COLOR } from "./src/branding";

const dirname = path.dirname(fileURLToPath(import.meta.url));

export default defineConfig({
  root: ".",
  publicDir: "public",
  resolve: {
    alias: {
      "@": path.resolve(dirname, "src"),
    },
  },
  server: {
    port: 5173,
    host: true,
  },
  build: {
    target: "es2020",
    // Built straight into firebase/dist (not a root-level dist/) because the
    // Firebase CLI refuses a hosting "public" path that resolves outside the
    // directory containing firebase.json — see firebase/firebase.json.
    outDir: "firebase/dist",
    emptyOutDir: true,
    sourcemap: true,
  },
  plugins: [
    react(),
    VitePWA({
      registerType: "autoUpdate",
      injectRegister: "auto",
      includeAssets: ["icons/*.svg"],
      manifest: {
        name: `${GAME_NAME} — ${GAME_SUBTITLE}`,
        short_name: GAME_NAME,
        description: `${GAME_NAME}: fast quiz rounds across flags, geography, science and more.`,
        theme_color: THEME_COLOR,
        background_color: BACKGROUND_COLOR,
        display: "standalone",
        orientation: "portrait",
        start_url: "/",
        scope: "/",
        icons: [
          { src: "icons/icon-192.svg", sizes: "192x192", type: "image/svg+xml", purpose: "any" },
          { src: "icons/icon-512.svg", sizes: "512x512", type: "image/svg+xml", purpose: "any" },
          { src: "icons/icon-maskable.svg", sizes: "512x512", type: "image/svg+xml", purpose: "maskable" },
        ],
      },
      workbox: {
        globPatterns: ["**/*.{js,css,html,svg}"],
      },
    }),
  ],
});
