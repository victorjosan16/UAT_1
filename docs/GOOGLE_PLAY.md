# Publicare pe Google Play — listă de pași

## 1. Cont
- Cont **Google Play Console** (taxă unică 25 $). Conturile personale noi trebuie să facă un
  **test închis cu minim 12 testeri timp de 14 zile** înainte de producție.

## 2. Cheie de semnare (keystore)
- Pentru Play Store NU se folosește „Auto-generated debug keystore”.
- Se creează un keystore de upload (o singură dată) și se încarcă în Unity Build Automation:
  *Build Automation → Credentials → Android → Add credentials* (fișier `.keystore`, alias, parole).
- **Păstrează keystore-ul și parolele în siguranță** — fără ele nu mai poți publica actualizări.
- În configurația de build: bifează **Build App Bundle (AAB)** (Google Play cere AAB).

## 3. Versiuni
- `bundleVersion` (ex. `1.0.0`) și `bundleVersionCode` (număr care crește la fiecare upload) se setează în
  `Editor/GameSetup.Project.cs` → `SetupPlayerSettings()`.

## 4. Fișa magazinului (texte propuse)
- **Nume:** Quarry Empire: Dig & Build
- **Descriere scurtă (RO):** Sapă, vinde și construiește-ți imperiul de cariere — strat cu strat!
- **Descriere scurtă (EN):** Dig, sell and build your quarry empire — layer by layer!
- **Descriere lungă (EN):**
  Grab your pickaxe and start digging! Break rocks, collect copper, iron, gold and diamonds, and carry
  them on your back to sell. Refine ore into valuable ingots, deliver truck orders before time runs out,
  buy bulldozers, drills and trucks, and hire workers who keep mining even while you're away.
  Clear each layer to descend deeper and discover rarer treasures. Simple one-thumb controls,
  satisfying progression — how deep can your empire go?
- **Grafică necesară:** icon 512×512, feature graphic 1024×500, minim 2 capturi de ecran portret
  (poți desena icon-ul și feature graphic-ul în Illustrator).
- **Categorie:** Games → Casual / Simulation.

## 5. Formulare obligatorii în Play Console
- **Privacy policy** (URL public) — necesar pentru că jocul va avea reclame.
- **Data safety** — declari ce date colectează SDK-ul de reclame (identificatori de dispozitiv etc.).
- **Content rating** (chestionar IARC), **Target audience** (nu sub 13 ani dacă folosești reclame personalizate),
  **Ads** = Da (după integrarea LevelPlay).

## 6. Înainte de upload
- Testează APK-ul pe 2–3 telefoane diferite.
- Verifică: pornire, salvare/încărcare (închide și redeschide), sunet/vibrații din setări, reclame.
