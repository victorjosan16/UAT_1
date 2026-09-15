# TINY TOWER

**BUILD HIGHER. BEAT EVERYONE.**

A minimalist, viral, precision-stacking arcade game. Tap to drop a block
sliding above your tower; only the overlapping part survives. Blocks get
narrower, faster, and trickier with every floor. One action, one rule,
infinite mastery.

Built with TypeScript + Vite + Canvas on the client, and Firebase
(Hosting, Cloud Functions, Firestore, Auth) for leaderboards, Daily
Tower, and friend challenges. Fully playable offline — the network is
only needed for competitive/social features.

See `docs/GAME_DESIGN.md` for mechanics/scoring/levels and
`docs/ARCHITECTURE.md` for how the codebase is organized.

> This repository also contains an unrelated Python/FastAPI
> image-processing service under `backend/` + `frontend/` — see
> `backend/README.md`. It is untouched by anything below.

---

## Quick start (GitHub Codespaces or local)

### 1. Open the repository

**GitHub Codespaces:** on the repo page, click **Code → Codespaces →
Create codespace on main** (or your branch). Wait for the container to
finish provisioning — Node is already installed.

**Local:** clone the repo and open it in your editor of choice. You need
Node 20+.

### 2. Install dependencies

```bash
npm install                              # client
npm --prefix firebase/functions install  # Cloud Functions
```

### 3. Set up Firebase (one-time)

You need your own Firebase project — full walkthrough in
`docs/DEPLOYMENT.md §1-2`. Short version:

1. Create a project at <https://console.firebase.google.com/>.
2. Enable **Authentication → Anonymous** sign-in.
3. Create a **Firestore Database** (production mode).
4. Register a **Web app** in the project and copy its config.
5. `npx firebase login`, then edit `firebase/.firebaserc` to put your
   project id in place of the placeholder.
6. `cp .env.example .env` and fill in the `VITE_FIREBASE_*` values from
   step 4.

### 4. Run it locally

```bash
npm run emulators     # terminal 1 — Firebase emulator suite (Hosting/Functions/Firestore/Auth)
npm run dev            # terminal 2 — Vite dev server
```

Set `VITE_USE_FIREBASE_EMULATORS=true` in `.env` for this so you're not
hitting your real Firebase project while developing. Open
<http://localhost:5173>.

### 5. Run tests

```bash
npm run test              # gameplay/scoring/levels logic
npm run test:functions    # anti-cheat re-validation logic
```

### 6. Type-check + build

```bash
npm run typecheck   # client + Cloud Functions, strict TypeScript
npm run build        # tsc --noEmit + vite build → firebase/dist/
```

### 7. Deploy

```bash
npm run deploy   # builds, then deploys hosting + functions + firestore rules
```

Full detail (including per-piece deploys, verification checklist, and
what to do before a public launch) is in `docs/DEPLOYMENT.md`.

---

## Project structure

```
src/            Game client (TypeScript, Canvas, no framework)
firebase/       Firebase config: hosting, Firestore rules, Cloud Functions (API)
public/         Static assets: PWA manifest source, icons
tests/           Vitest — gameplay/scoring/levels logic
docs/            Design, architecture, database, API, security, deployment
backend/         Unrelated: the pre-existing image-processing service
frontend/        Unrelated: that service's static frontend
```

## Documentation

| Doc | Covers |
|---|---|
| `docs/GAME_DESIGN.md` | Core mechanic, scoring, levels, modes, modifiers |
| `docs/ARCHITECTURE.md` | Codebase layout, why the split, anti-cheat model |
| `docs/SCORING.md` | Exact scoring formulas |
| `docs/DATABASE.md` | Firestore schema |
| `docs/API.md` | `/api/*` endpoint reference |
| `docs/SECURITY.md` | Auth, Firestore rules, anti-cheat, abuse mitigation |
| `docs/DEPLOYMENT.md` | Full setup-to-production walkthrough |

## npm scripts reference

| Script | What it does |
|---|---|
| `npm run dev` | Vite dev server |
| `npm run emulators` | Firebase emulator suite (Hosting/Functions/Firestore/Auth) |
| `npm run dev:all` | Both of the above together |
| `npm run build` | Type-check + production build → `firebase/dist/` |
| `npm run preview` | Preview the production build locally |
| `npm run test` / `test:watch` | Client-side gameplay tests |
| `npm run test:functions` | Cloud Functions anti-cheat tests |
| `npm run typecheck` | Strict TypeScript, client + functions |
| `npm run deploy` | Build + deploy hosting, functions, and Firestore rules |
| `npm run deploy:hosting` / `deploy:functions` / `deploy:firestore` | Deploy one piece at a time |
