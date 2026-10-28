# Audit Drop Quest – listă de lucru (se continuă între sesiuni)

Ramura: `audit/quality`. Actualizează această listă la fiecare sesiune; nu bifa nimic fără dovadă.

## Faze
- [x] **Faza 1** – inventar, versiune Unity, pachete, hartă de arhitectură (dovadă: `AUDIT_REPORT.md` A–C, `INVENTORY.md`)
- [ ] **Faza 2** – citirea manuală a tuturor fișierelor (24/101 integral, 4 parțial; vezi `INVENTORY.md`)
- [x] **Faza 3 (parțial)** – simulare niveluri 1–100 × 20 seed-uri (dovadă: `sim-levels-1-100.txt`); lipsesc nivelurile 101+
- [x] **Faza 4 (parțial)** – povestea: LevelStory/StoryPanel citite, testate; lipsesc reacțiile în timpul nivelului
- [ ] **Faza 5** – UI/animații/performanță **pe dispozitiv** (cere Unity + telefon)
- [x] **Faza 6 (parțial)** – reparate 2 probleme P2 (reclame, salvare stricată); P1 de lansare așteaptă decizii (R1, R2)
- [x] **Faza 7 (parțial)** – EditMode 102/102 prin `Tools/CoreTests`; PlayMode nerulat
- [x] **Faza 8 (parțial)** – setări Android citite din `SugarDropSetup`; target SDK de confirmat în Play Console
- [x] **Faza 9** – raport (`AUDIT_REPORT.md`)

## Următorii pași
1. Decizii de la proprietar: R1 (reclame reale sau ascunse), R2 (target SDK).
2. Citire manuală: `Board/*`, `Core/BlastEngine.cs`, `Core/BoardStep.cs`, restul din `SugarDropGame.cs`, `UI/MapScreen.cs`, `Online/*`.
3. Teste PlayMode în Unity (pornire → hartă → nivel → victorie/pierdere → pauză → reîncearcă).
4. Profilare pe telefon (cascade mari, harta lungă) înainte de pool-ul de piese.

## Comenzi
- Teste: `cd Tools/CoreTests && dotnet test`
- Compilare fără Unity: proiectul de verificare (stub-uri) – vezi `Tools/CoreTests` pentru model
