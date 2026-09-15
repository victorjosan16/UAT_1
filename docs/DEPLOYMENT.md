# Deployment

This assumes no prior Firebase/Cloudflare knowledge. Tiny Tower deploys
entirely to Firebase: Hosting (static build) + Cloud Functions (API) +
Firestore (database) + Auth (guest identity), as a **project separate**
from any other Firebase project already used by this repository's
unrelated image-processing service.

## 1. Create a Firebase project

1. Go to <https://console.firebase.google.com/> and click **Add project**.
2. Give it a name (e.g. `tiny-tower-prod`) and finish the wizard (Google
   Analytics is optional, not required by this app).
3. In the project, go to **Build → Authentication → Get started**, then
   enable the **Anonymous** sign-in provider (Sign-in method tab).
4. Go to **Build → Firestore Database → Create database**, start in
   **production mode** (the repo's `firestore.rules` already lock it down
   correctly), pick any region close to your players.
5. Go to **Project settings → General → Your apps**, click the **Web**
   icon (`</>`), register an app (any nickname), and copy the config
   object it shows you — you'll need every value in the next step.

## 2. Configure the CLI and environment

```bash
npm install                       # installs firebase-tools locally (devDependency)
npx firebase login                # opens a browser to authenticate

# Point the repo's Firebase config at YOUR project:
# edit firebase/.firebaserc, replacing the placeholder with your project id
```

```bash
cp .env.example .env
# fill in every VITE_FIREBASE_* value from step 1.5 above
```

## 3. Install dependencies (client + functions)

```bash
npm install
npm --prefix firebase/functions install
```

## 4. Run locally against the Firebase Emulator Suite

```bash
npm run emulators   # Hosting, Functions, Firestore, Auth emulators + Emulator UI on :4000
# in a second terminal:
npm run dev          # Vite dev server on :5173, proxying /api to the Hosting emulator on :5000
```

Set `VITE_USE_FIREBASE_EMULATORS=true` in `.env` while doing this, so the
client's Firebase Auth SDK talks to the local Auth emulator instead of
your real project. Open <http://localhost:5173> to play; open
<http://localhost:4000> for the Emulator UI (inspect Firestore documents,
Auth users, Functions logs).

```bash
npm run test              # gameplay/scoring logic (Vitest)
npm run test:functions    # anti-cheat re-validation logic (Vitest, no emulator needed)
npm run typecheck         # client + functions TypeScript, strict mode
```

## 5. Deploy

```bash
npm run build              # tsc --noEmit + vite build → firebase/dist/
npm run deploy              # hosting + functions + firestore rules/indexes, in one command
```

Or deploy pieces individually while iterating:

```bash
npm run deploy:hosting      # just the built frontend
npm run deploy:functions    # just the API
npm run deploy:firestore    # just rules/indexes
```

The first `deploy:functions` will prompt to enable the Cloud Functions
and Cloud Build APIs on the project (one-time, free tier covers this
app's expected traffic comfortably) and to confirm the Node 20 runtime.

## 6. Verify

- Visit the Hosting URL Firebase prints after deploy
  (`https://<project-id>.web.app`).
- Play a full run; confirm the score screen shows a rank and the
  leaderboard populates (**Leaderboard** button on the start screen).
- In the Firebase Console → Firestore, confirm a `players/{uid}` and
  `leaderboard_alltime/{uid}` document appeared.
- In Console → Functions → Logs, confirm no errors on `session/complete`.

## Error handling / offline behavior

If the network is unavailable at any point:

- Gameplay is unaffected — it runs entirely client-side.
- `PlayerService`/`RunSubmitter` calls fail silently (caught `ApiError`s)
  and queue the pending submission in `LocalStorageService`'s pending
  queue; a leaderboard/session write is retried automatically on the
  browser's next `online` event.
- The HUD shows a small **"CONNECTION LOST"** banner
  (`src/ui/HUD.ts`) rather than any technical error text.
- Nothing here is faked — if a score never successfully syncs, it simply
  never appears on a leaderboard; local personal bests still update from
  the run itself.

## Recommended before a public launch

- Enable **Firebase App Check** (reCAPTCHA Enterprise/v3) on the `api`
  function and Firestore — see `docs/SECURITY.md`. Not enabled by
  default in this repo since it requires a reCAPTCHA site key tied to
  your specific domain.
- Set a Firestore **budget alert** (Cloud Console → Billing) — the app's
  read/write pattern is deliberately cheap (one doc per player per
  leaderboard window, no fan-out), but any public leaderboard is worth
  monitoring.
- Consider Cloud Functions `minInstances: 1` on the `api` function if
  cold starts become noticeable under real traffic (costs a small,
  predictable amount more; not needed for moderate traffic).
