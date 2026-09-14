# Security

## Identity

Every authenticated request carries a Firebase Auth ID token
(`Authorization: Bearer <token>`), verified server-side with
`getAuth().verifyIdToken()` (`firebase/functions/src/lib/auth.ts`). The
resulting `uid` is the **only** source of truth for "who is making this
request" — no route ever trusts a client-supplied `playerId` field. Guest
players get this uid for free via Firebase Anonymous Auth
(`signInAnonymously`), with zero account-creation friction.

## Firestore lockdown

`firebase/firestore.rules` denies all direct client reads/writes except a
public `read: true` on the three leaderboard collections (display data
only). Every write happens through the Cloud Function's Admin SDK, which
bypasses security rules by design — the rules exist to guarantee that
**nothing** reaches Firestore except through code we've reviewed, even if
a client somehow obtained valid Firestore SDK credentials.

## Anti-cheat: server-side score re-validation

The core defense against leaderboard tampering is
`firebase/functions/src/lib/revalidate.ts` — see
`docs/ARCHITECTURE.md §Anti-cheat model` for the full flow. Highlights:

- **Sessions are single-use.** `sessions/{id}.status` flips
  `active → completed` inside a Firestore transaction; a second
  `session/complete` call for the same id is rejected
  (`409 SESSION_ALREADY_COMPLETED`) — this is the duplicate-submission
  defense.
- **Sessions expire** (45 minutes) — an old, replayed session is rejected
  (`410 SESSION_EXPIRED`).
- **The seed is bound to the session**, not trusted from the submission —
  `session.seed !== summary.seed` is rejected outright
  (`422 SEED_MISMATCH`) before any scoring math even runs.
- **Daily Tower's seed is never client-supplied** — `session/start`
  always derives it from the server clock's UTC date, so a client cannot
  submit a Daily run against a seed nobody else played against.
- **The score itself is recomputed, not trusted.** Given only the seed
  and the raw placement trace (overlap widths, floors, perfect flags),
  the server reconstructs the exact `LevelDefinition` sequence
  (`buildLevelSequence`) and replays it through the same `ScoreEngine`
  the client runs. Any mismatch in the running score, final score,
  perfect count, best combo, or best perfect streak — or an overlap width
  that's geometrically impossible for the block it claims to be — gets
  the whole submission rejected (`accepted: false`, nothing written to
  any leaderboard).
- **Gross timing implausibility** (average time per placement far below
  human reaction speed across a long run) is rejected too, as a coarse
  backstop against fully synthetic/injected traces.

`RULES_VERSION` (`src/branding.ts`) is stamped on every summary so a
future scoring-formula change can't silently reinterpret old runs.

## Input validation

Every request body is parsed with a `zod` schema
(`firebase/functions/src/validation/schemas.ts`) before any handler logic
runs — malformed payloads, wrong types, or oversized placement traces
(`placements` capped at 5000 entries) are rejected with
`400 VALIDATION_ERROR` and never reach Firestore or the scoring engine.
Nicknames are normalized (`src/utils/nickname.ts`): stripped to
`[a-zA-Z0-9 _-]`, trimmed, capped at 16 characters, falling back to an
auto-generated `PLAYER-XXXX` if that leaves nothing — the same function
runs on the client (optimistic display) and would run again server-side
if this became a public-facing field beyond nicknames.

## Abuse mitigation

- `express.json({ limit: "256kb" })` caps request body size — generous
  for a full placement trace, small enough to block payload-flood spam.
- `session/start` is rate-limited per uid (30/min) via a Firestore
  transaction-backed fixed window (`lib/rateLimit.ts`) — a blunt backstop,
  not the primary defense.
- CORS is enabled broadly (`cors({ origin: true })`) since the API only
  ever does anything meaningful for an authenticated uid; there's no
  session cookie to protect against CSRF.
- The Express app's centralized error handler
  (`firebase/functions/src/app.ts`) always responds with a generic
  `500 { error: "Internal server error" }` and logs the real error
  server-side only — no stack trace, query, or internal detail ever
  reaches a client response.

## Firebase App Check (recommended, not yet wired)

For production, enable
[Firebase App Check](https://firebase.google.com/docs/app-check) on both
the `api` Cloud Function and Firestore, so requests must additionally
prove they originate from the real deployed app (reCAPTCHA
Enterprise/v3 for web) — this blocks scripted abuse before it ever reaches
`verifyIdToken`. This is the direct equivalent of the Cloudflare Turnstile
step in the original architecture sketch, and is the next security item
to add before a public launch (see `docs/DEPLOYMENT.md`).

## What's intentionally NOT stored

- No email/password/PII for guest players — an anonymous Firebase uid and
  a self-chosen nickname only.
- No secrets in the client bundle. The Firebase Web SDK config
  (`VITE_FIREBASE_*`) is not secret by Firebase's own design — it
  identifies the project, not a credential; real access control lives in
  Firestore rules + (once enabled) App Check.
