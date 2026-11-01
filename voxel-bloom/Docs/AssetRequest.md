# Ce trebuie ca jocul să fie identic cu mockup-ul

Mockup-ul este o singură imagine de 1536×1024 cu 10 ecrane, deci fiecare ecran are doar ~300 px lățime. Din ea nu se pot
decupa piese la calitatea unui telefon (1080 px lățime). Grafica de mai jos trebuie primită separat.

## Format
* **PNG cu fundal transparent.** Varianta acceptată: o planșă pe verde pur #00FF00, cu cel puțin 20 px între piese și fără verde
  pe marginile pieselor.
* **Rezoluție:** desenat pentru un ecran de 1080×1920. Ideal ×2 (fundaluri 2160×4320); minim ×1.
* **Texte variabile:** fără text în imagine pentru tot ce se schimbă (Nivel N, 3/8, numere, prețuri, contoare); textul îl pun eu.
  Textele fixe (Joacă, Niveluri, Colecție...) pot fi în imagine.
* **Panouri și pastile:** colțurile la fel, ca să se poată întinde fără deformare (9-slice).
* **Fontul din mockup:** numele lui (și licența). Acum folosesc Baloo 2.

## 1. Fundaluri (fără butoane pe ele)
| Fișier | Ecran |
|---|---|
| `bg_title.png` | Titlu (peisaj cu drum de piatră și flori) |
| `bg_home.png` | Principal (cascade, stânci, moară de vânt, flori) |
| `bg_map.png` + coordonatele celor 8 puncte de pe drum | Niveluri (drum auriu șerpuit, râu, castel) |
| `bg_game.png` | Joc (interior aeroport violet, ferestre, plante) |
| `bg_meta.png` | Boostere / Magazin / Evenimente / Setări (violet cu podea și plante) |
| `bg_win.png` | Victorie (raze, confetti) |

## 2. Butoane și interfață
* **Butoane mari:** Niveluri (cu harta), Joacă (am), Următorul, Reia, Înțeles!.
* **Butoane pătrate cu iconiță:** Recompense, Evenimente, Clasament, Colecție (în noul mockup e cu cub, nu cu labă), Magazin.
* **Butoane rotunde și pastile:** înapoi, setări, pauză; pastila de titlu (goală), tab activ și inactiv, pastila verde de preț
  (goală), comutator pornit/oprit, săgeată „>”, „X” de închidere.
* **Bara de sus:** ramă de avatar și avatarul din mockup, insigna de nivel; pastilele de monede și gemuri goale (le am).
* **HUD-ul de joc:** pastila „Nivel” goală și pastila de progres goală.

## 3. Joc
* **Banda:** o bucată dreaptă și un colț (sau banda întreagă la 9-slice), cu marginea cyan și săgețile.
* **Cuburi:** cubul de pe bandă, alb, ca să-l colorez exact, sau câte unul pentru fiecare din cele 8 culori.
* **Tăvi:** goale, orizontale și verticale, cu 2, 3 și 4 compartimente; blocul dintr-un compartiment (alb sau cele 8 culori).
* **Blocaje și coadă:** placa de piatră cu insigna pentru număr (fără cifră), tava cu „+” verde, cartonașul din coadă, bifa „✓”.
* **Boostere:** cele 3 iconițe (Reîmprospătează, Amestecă culorile, Adaugă o tavă) și bulina roșie pentru număr.

## 4. Celelalte ecrane
* **Victorie:** panglica „Finalizat!” goală, cele 3 cuburi pe postament auriu, cardul de recompensă.
* **Niveluri:** cercul violet de nivel (normal și curent), postamentul cu lacăt, steaua mică, punctele de pagină, săgețile.
* **Cum se joacă?:** mâna, cele 3 ilustrații mici, cercul cu număr, cardul.
* **Boostere și Magazin:** cardurile, iconițele mari ale celor 3 boostere, monedă, gem, pachete.
* **Evenimente:** iconițele calendar, țintă, cristal, cadouri; bara de progres; pastila cu ceasul.
* **Setări:** iconițele sunet, muzică, vibrații, glob, nor, info.

## 5. Funcții care au nevoie de servicii externe
| Funcție | Ce trebuie |
|---|---|
| Cumpărare gems cu bani reali | cont Google Play Console, produsele create (ID-uri și prețuri) |
| Salvare în cloud și clasament online | Google Play Games Services (ID-ul aplicației, ID-ul clasamentului) |
| Video pentru recompense (opțional) | ID-urile AdMob sau ale altei rețele |

## 6. Mecanica jocului
* **Video-ul 13224.mp4** (nu a ajuns la mine), ca să compar regulile cadru cu cadru: de unde intră cuburile, ce înseamnă plăcile
  1 și 2, cum se mișcă tăvile.
