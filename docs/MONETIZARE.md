# Monetizare — reclame rewarded

Jocul folosește doar reclame **rewarded** (opționale, cu recompensă), nu reclame care întrerup jocul.
Toate trec prin `Scripts/Monetization/AdService.cs` → metoda `ShowRewarded(placement, onComplete)`.

## Unde apar
| Placement | Unde | Recompensă |
|---|---|---|
| `boost_x2` | buton dreapta „BANI x2” | venituri x2 timp de 90 s |
| `bonus_cash` | buton dreapta „BONUS” | 150 $ × numărul stratului (pauză 3 min) |
| `offline_x2` | fereastra „Bine ai revenit” | dublează câștigul offline |

## Acum: reclamă de TEST
`AdService` afișează un ecran negru cu numărătoare inversă (3 s) și apoi dă recompensa.
Așa se poate testa tot fluxul fără cont de reclame.

## Pentru reclame reale (Unity LevelPlay)
1. Cont pe **unity.com → Grow → LevelPlay**, adaugă aplicația Android (Bundle ID `com.victorjosan.quarryempire`).
   Notează **App Key** și ID-ul unității **Rewarded**.
2. În proiect se adaugă pachetul **Ads Mediation (LevelPlay)** (`com.unity.services.levelplay`).
3. Se înlocuiește doar corpul lui `AdService.ShowRewarded` cu apelul LevelPlay
   (inițializare cu App Key la pornire, `LoadAd`, `ShowAd`, iar în callback-ul de recompensă `onComplete(true)`).
   Restul jocului nu se schimbă.
4. Google Play cere declararea reclamelor în **Data safety** și „Contains ads” în fișa aplicației.

> Pasul 2–3 îl facem împreună când contul LevelPlay e gata (am nevoie de App Key și ID-ul unității,
> pe care le pui tu în proiect — nu le trimite în chat dacă repo-ul e public).
