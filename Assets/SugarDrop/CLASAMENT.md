# Clasamentul online în Drop Quest (Unity Gaming Services)

**Cum se calculează punctele:** fiecare nivel păstrează cel mai bun scor al tău. Punctele din clasament sunt **suma
acestor recorduri** de pe toate nivelurile. Dacă rejoci un nivel și faci un scor mai mare, punctele cresc.

Codul e gata:
- `Scripts/Online/SugarLeaderboard.cs` intră anonim, trimite punctele și încarcă primii 50 de jucători;
- `Scripts/UI/LeaderboardPanel.cs` este ecranul, deschis de butonul cu trofeu de pe hartă;
- `Scripts/UI/WelcomePanel.cs` alege nickname-ul la prima pornire.

Pachetul `com.unity.services.leaderboards` e deja în `Packages/manifest.json`. Până faci pașii de mai jos,
clasamentul arată doar rândul tău și mesajul „Clasamentul online nu e disponibil acum”.

## 1. Proiectul în Unity Cloud (o singură dată)
1. Intră pe **https://cloud.unity.com** cu contul tău Unity.
2. Alege proiectul folosit de Build Automation pentru Drop Quest. Build-ul din cloud **nu** leagă singur proiectul: Project ID-ul e scris în
   `Scripts/Online/SugarOnlineConfig.cs` (`ProjectId`) și aplicat de Pre-Export. Dacă schimbi proiectul, schimbă-l și acolo.
3. Notează **Project ID**-ul din **Project settings**. Se completează în `SugarOnlineConfig.ProjectId`.

## 2. Autentificarea anonimă
**Products → Player Authentication → Identity providers**: verifică dacă **Anonymous** e activ (de obicei e, din oficiu).
Jucătorul nu trebuie să-și facă un cont: telefonul primește un ID anonim.

## 3. Clasamentul
**Products → Leaderboards → Create leaderboard**:

| Câmp | Valoare |
|---|---|
| Leaderboard ID | `total_score` (exact așa) |
| Name | Drop Quest – puncte totale |
| Sort order | **Descending** (cele mai multe puncte primele) |
| Update type | **Keep best score** |
| Reset | fără resetare |

## 4. Build și verificare
Dai build ca de obicei, cu Pre-Export `SugarDropCloudBuild.PreExport`. Pre-Export activează singur codul online,
prin simbolul `SUGARDROP_UGS`. Pe telefon:
- termini un nivel, apoi deschizi trofeul de pe hartă. Trebuie să apari în listă, cu nickname-ul tău.

Dacă apare tot „nu e disponibil”, deși telefonul are internet:
- spune-i asistentului **Project ID**-ul. El îl pune în `Scripts/Online/SugarOnlineConfig.cs`, iar Pre-Export
  leagă proiectul de el.

## 5. Nickname
- La prima pornire, Bobo te salută și te întreabă cum să-ți spună. Primești un nume propus, de exemplu „Izvor4821”.
- Nickname-ul are între 3 și 16 caractere: litere, cifre, spații și `_ - .`.
- Se poate schimba din **Setări → NICKNAME** (pe hartă).
- În meniu apare „Salut, <nickname>!”.

## 6. Google Play
- În **Data safety** declari: **ID-uri** (ID-ul anonim de jucător) și **Activitate în aplicație** (scorul și nickname-ul),
  folosite pentru funcționalitatea aplicației (clasament).
- Politica de confidențialitate (vezi `POLITICA_CONFIDENTIALITATE.md`) le menționează deja.
