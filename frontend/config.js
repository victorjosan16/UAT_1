// URL-ul backend-ului FastAPI, folosit doar când frontend-ul este găzduit
// SEPARAT de backend (ex: frontend pe Firebase Hosting, backend rulat în
// Codespaces / Cloud Run / alt server).
//
// Exemplu: "https://backend-print-service-xxxxx.a.run.app"
//
// Lasă gol ("") dacă backend-ul FastAPI servește el însuși acest frontend
// (cazul implicit, vezi README.md).
window.PRINT_SERVICE_API_BASE = "";
