# Toll Road Tycoon – descriere funcțională

Joc idle pentru Android (portret), în care jucătorul administrează o autostradă cu taxă.
Mașinile vin singure, stau la coadă, plătesc la cabină și trec de barieră, iar banii încasați se
investesc în cabine, angajați, benzi, servicii și porțiuni noi de drum. Jucătorul nu conduce.

Valorile de mai jos sunt cele implicite din `GameConfig` / `ProgressionConfig` și pot fi ajustate
fără modificări de cod.

---

## 1. Bucla de bază

1. Jucătorul pornește cu **$250**, un drum cu **o bandă**, **o cabină** și **un angajat**.
2. Mașinile apar pe banda de intrare, aleg o cabină și intră în coada ei.
3. La linia de stop, angajatul încasează taxa, apoi bariera se ridică și mașina pleacă.
4. **Banii se primesc doar la plată**, o singură dată pentru fiecare mașină.
5. Cu banii încasați, jucătorul cumpără upgrade-uri care cresc viteza sau încasările.

### Vehicule și taxe (porțiunea 1)

| Vehicul | Taxă | Disponibil |
|---|---|---|
| Mașină compactă | $5 | de la început |
| Sedan | $8 | de la început |
| SUV | $10 | deblocare $600 |
| Dubă | $15 | deblocare $2.000 |
| Camion | $25 | deblocare $5.000 |
| Autobuz | $35 | deblocare $12.000 |
| TIR | $50 | deblocare $30.000 |
| Vehicul de urgență | $0 (nu se taxează) | de la început |

* Vehiculele mai mari plătesc mai mult, dar se procesează mai încet.
* Fiecare tip deblocat crește și traficul.
* Pe porțiunile mai scumpe taxele sunt mai mari (vezi secțiunea 5).

### Traficul

* Fiecare mașină alege cabina cu cea mai scurtă așteptare estimată, ținând cont de capacitatea cozii
  și de cât de departe e cabina.
* Mașinile păstrează distanța între ele, frânează lin și nu se suprapun.
* Pe benzile de ieșire, mașinile intră pe rând.
* Pe partea stângă circulă trafic din sens opus, doar ca decor (nu plătește taxă).

---

## 2. Cabine (tab CABINE)

* Se pot construi până la **6 cabine**, în funcție de porțiune. Cabinele 3–4 cer 2 benzi, iar 5–6 cer 3 benzi.
* Prețuri pe porțiunea 1: cabina 2 costă $400, cabina 3 $1.200, cabina 4 $3.500 (cabinele 5–6 apar pe
  porțiunile următoare).
* O cabină nouă se construiește cu confirmare și apare imediat pe drum, cu bandă și racorduri.
* Când atingi o cabină pe drum, se deschide fereastra **UPGRADE CABINĂ**.

### Upgrade-uri pentru o cabină

| Upgrade | Efect | Nivel maxim |
|---|---|---|
| Viteză de taxare | timp de procesare ÷ (1 + 0,15 × nivel) | 10 |
| Venit per mașină | taxă × (1 + 0,25 × nivel) | 10 |
| Capacitate coadă | 4 → 8 mașini | 4 |
| Viteză barieră | barieră mai rapidă | 5 |
| Plată automată | nu mai e nevoie de angajat | necesită viteza la nivelul 3 |

* Pentru fiecare upgrade se afișează valoarea actuală și cea de după, de exemplu „3,0s → 2,6s”.
* Costul crește la fiecare nivel (cost de bază × factor ^ nivel).

---

## 3. Benzi (tab BENZI)

* Drumul poate avea de la 1 la **3 benzi** de intrare și de ieșire. Pe porțiunea 1, banda a 2-a costă $1.500.
* O bandă nouă aduce mai mult trafic și deblocează mai multe cabine.

## 4. Angajați (tab ANGAJAȚI)

* Cabinele manuale funcționează doar cu un angajat. Un angajat poate lucra la o singură cabină.
* Angajarea costă din ce în ce mai mult: $200, apoi mai scump la fiecare angajat nou.
* Upgrade-ul de angajat scurtează timpul pe mașină de la 3,0s (nivelul 1) la 1,1s (nivelul 10).
* **Alocă / Retrage**: când retragi un angajat, cabina servește întâi mașinile deja la coadă și abia
  apoi se închide. Nicio mașină nu rămâne blocată.

---

## 5. Porțiuni de drum (ecranul HARTĂ)

| # | Porțiune | Km | Preț deblocare | Condiții suplimentare | Max cabine / benzi |
|---|---|---|---|---|---|
| 1 | Drum local | 0–5 | – | – | 4 / 2 |
| 2 | Șosea suburbană | 5–10 | $20.000 | 3 cabine pe porțiunea 1 | 5 / 2 |
| 3 | Drum expres regional | 10–20 | $200.000 | 4 cabine pe porțiunea 2 | 6 / 3 |
| 4 | Autostradă principală | 20–30 | $2.000.000 | 5 cabine + venit $15.000/min | 6 / 3 |
| 5 | Autostradă premium | 30–50 | $20.000.000 | 6 cabine + venit $80.000/min | 6 / 3 |

* Fiecare porțiune are propriile prețuri, taxe, trafic și decor. De la porțiunea 1 la 5:
  * prețurile cresc de la ×1 la ×250;
  * taxele cresc de la ×1 la ×7;
  * traficul crește de la ×1 la ×2,3.
* **Vehicule permise:**
  * porțiunea 1: doar mașini ușoare;
  * de la porțiunea 2: și camioane;
  * de la porțiunea 3: și autobuze și TIR-uri.
* Pe porțiunile 4–5, plata automată e mai ieftină și se poate cumpăra mai devreme.
* Porțiunile deblocate își păstrează progresul. Se poate trece oricând de la una la alta din HARTĂ.
* **Doar porțiunea pe care ești e simulată mașină cu mașină.** Celelalte porțiuni deblocate aduc bani
  în continuare, după o **estimare** (mașini pe secundă × taxa medie × 0,85). Astfel nimic nu se
  numără de două ori.

---

## 6. Servicii pe marginea drumului (tab SERVICII)

| Serviciu | Disponibil de la | Preț (porțiunea 1) | Locuri | Durată | Câștig / client |
|---|---|---|---|---|---|
| Parcare | porțiunea 1 | $600 | 6 | 20s | $2 |
| Cafenea | porțiunea 1 | $1.000 | 4 | 14s | $6 |
| Benzinărie | porțiunea 2 | $4.000 | 4 | 10s | $18 |
| Zonă de odihnă (WC) | porțiunea 2 | $3.000 | 6 | 12s | $5 |
| Magazin de proximitate | porțiunea 3 | $15.000 | 4 | 12s | $30 |
| Spălătorie auto | porțiunea 3 | $25.000 | 2 | 16s | $60 |
| Stație de încărcare | porțiunea 4 | $120.000 | 4 | 25s | $250 |

* Pe porțiunile superioare, prețurile și câștigurile cresc odată cu porțiunea.
* Fiecare porțiune are **8 locuri** de construcție lângă drum.
* **Construire**: alegi serviciul, locurile libere se luminează pe hartă, atingi unul și confirmi prețul.
* **Venitul vine doar de la clienți.** O mașină care tocmai a plătit taxa poate opri la un serviciu
  care are loc liber. Plata vine când clientul termină. Camioanele preferă benzinăria și zona de odihnă.
* **Upgrade** până la nivelul 5: fiecare nivel crește câștigul cu 35%, iar la fiecare 2 niveluri se
  adaugă un loc.
* **Mută**: serviciul se mută pe alt loc liber, gratuit.
* **Demolează**: primești înapoi 50% din preț.

## 7. Decor (tab DECOR)

* Elemente disponibile: bancă ($150), felinar ($250), ronduri de flori ($400), coș de gunoi ($100),
  con rutier ($80), gard ($300).
* Fiecare porțiune are 8 locuri pentru decor. Elementele se pot muta sau elimina.
* Decorul e **doar estetic** și nu schimbă venitul.

---

## 8. Misiuni și obiective (ecranul MISIUNI)

### Obiective (servesc și ca tutorial)

Obiectivul curent apare ca un banner în partea de sus a ecranului de joc. Când e îndeplinit, se
atinge bannerul pentru a primi recompensa. Obiectivele se rezolvă în ordine:

1. Deservește 10 mașini
2. Încasează $100
3. Primul upgrade de viteză
4. Construiește a doua cabină
5. Deblochează SUV-urile
6. Îmbunătățește un angajat
7. Construiește a treia cabină
8. Mărește capacitatea cozii
9. Construiește primul serviciu
10. Deblochează camioanele
11. Deblochează porțiunea 2

### Misiuni zilnice

* În fiecare zi (după ora UTC) apar 4 misiuni noi, alese dintre: deservește N mașini, încasează N,
  deservește camioane, îmbunătățește angajați, construiește o cabină, construiește un serviciu,
  atinge un venit pe minut, fă upgrade-uri de viteză.
* Țintele și recompensele cresc odată cu progresul jucătorului.
* Progresul se numără de la momentul în care apare misiunea.
* Orice recompensă se primește **o singură dată**. Când există o recompensă de luat, pe butonul
  MISIUNI din meniu apare un „!” roșu.

---

## 9. Magazin (ecranul MAGAZIN)

| Categorie | Conținut |
|---|---|
| VEHICULE | deblocarea tipurilor de vehicule (valabilă pe toate porțiunile) |
| DECOR | cumpărare de decor, apoi alegerea locului pe hartă |
| CLĂDIRI | serviciile permise pe porțiunea curentă |
| BONUSURI | venit x2 pe 2 minute (reclamă), bani instant (reclamă), venit x2 pe 5 minute ($2.500) |

* Bonusul x2 dublează taxele încasate cât timp e activ. Timpul rămas se vede pe butonul „x2” din ecranul de joc.
* Banii instant valorează 3 minute din venitul curent, dar cel puțin $200.

## 10. Reclame

* **În Unity și în build-urile de test:** reclamele rulează în **mod TEST**. Apare un ecran marcat
  clar „RECLAMĂ DE TEST” cu numărătoare inversă. Recompensa se primește doar dacă aștepți până la
  final; dacă închizi ecranul, nu primești nimic.
* **În build-ul final (release):** reclamele sunt dezactivate, iar butoanele lor nu apar.
* Nu există reclame forțate (interstițiale) și nici un SDK de reclame sau ID-uri de producție.

## 11. Venit offline

* La redeschiderea jocului se calculează cât au produs **toate porțiunile deblocate** în lipsa
  jucătorului, după estimare, pentru **cel mult 8 ore**.
* Apare fereastra „VENIT OFFLINE” cu suma și două butoane:
  * **Colectează**;
  * **Dublează cu reclamă**, doar dacă reclamele sunt disponibile.
* Suma se primește o singură dată. Dacă nu e colectată, se păstrează la salvare.
* Ora salvată se verifică: o oră lipsă, invalidă sau din viitor nu aduce bani.

---

## 12. Statistici (ecranul STATISTICI)

* venitul pe minut al porțiunii curente;
* mașinile și vehiculele grele deservite;
* timpul mediu de așteptare la coadă;
* cât de ocupate sunt cabinele;
* cabinele active și porțiunile deblocate;
* banii din servicii, de la porțiunile inactive (estimat) și din perioadele offline (estimat);
* totalul câștigat și cheltuit;
* timpul de joc.

## 13. Setări

* Muzică (generată din cod), sunete, vibrații.
* Calitate grafică: Scăzută, Medie sau Înaltă.
* **Limbă**: engleză (implicită), română, maghiară, bulgară, ucraineană, rusă, poloneză, germană,
  turcă. La prima pornire se folosește limba telefonului, dacă e una dintre acestea.
* Confidențialitate: jocul nu colectează date, iar salvarea rămâne doar pe dispozitiv.
* **Resetează progresul**, cu confirmare.

## 14. Salvare

* Jocul se salvează automat la fiecare 30 de secunde, când e pus în fundal, la ieșire și la
  schimbarea porțiunii.
* Fișierul are o copie de rezervă. Dacă fișierul principal e corupt, se încarcă automat copia.
* Salvările vechi se actualizează automat la formatul nou. O salvare făcută de o versiune mai nouă
  nu se suprascrie.
* Mașinile aflate pe drum în momentul salvării nu se păstrează. Nu plătiseră încă, deci nu se pot
  plăti de două ori.

## 15. Ecrane și control

* **Meniu principal**: JOACĂ, DRUMURI (hartă), MAGAZIN, MISIUNI, SETĂRI, peste drumul care rulează în fundal.
* **Ecranul de joc**:
  * sus: sold, mașini deservite, pauză, porțiunea curentă cu bară de progres, venit/min, obiectivul
    curent, butoanele HARTĂ / MISIUNI / MAGAZIN / STATISTICI / x2;
  * jos: tab-urile CABINE, BENZI, ANGAJAȚI, UPGRADE, SERVICII, DECOR.
* **Control**: tragi cu degetul ca să muți imaginea, prinzi cu două degete ca să faci zoom, atingi
  ca să selectezi cabine, clădiri și decor sau ca să alegi un loc liber la construcție.
* Toate achizițiile importante se confirmă. Butoanele pe care nu ți le permiți apar gri.
