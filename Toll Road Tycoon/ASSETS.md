# Grafica jocului: lista de imagini

> **Stadiu:** din cele două foi de grafică primite s-au decupat automat (fundal eliminat, mărire ×3–×4):
> * din foaia cu fundal verde: compacte (11 culori), sedanuri (8), SUV-uri (2), dube (5),
>   camioane (11 dube de marfă), TIR-uri (11, inclusiv cisternă și transport bușteni), ambulanță,
>   clădirile SHOP și HOTEL (case decorative pe marginea drumului);
> * din prima foaie: autobuzele, mașinile de poliție/pompieri, cabinele de taxare, copacii și iconițele.
>
> Mașinile verzi și copacii din foaia verde nu se pot separa de fundalul verde, deci nu au fost folosiți.
> Pentru grafică mai clară, aceleași obiecte generate la rezoluție mare (~512 px) le pot înlocui
> direct. Încă lipsesc: iarba, clădirea de taxare, copertina, bariera orizontală, panourile, butoanele,
> logo-ul și fontul.

Jocul caută automat imaginile de mai jos în `Assets/Resources/Art/`. Fiecare imagine pe care o adaugi
înlocuiește forma provizorie generată din cod. Cele care lipsesc rămân provizorii, deci grafica se
poate adăuga treptat, fără modificări de cod.

## Reguli generale

* **Format:** PNG cu **fundal transparent**. Fără umbră sub obiect și fără text în imagine, exceptând
  logo-ul (umbrele sunt desenate de joc).
* **Unghi:** pentru mașini, cabine, copaci și clădiri: **văzut de sus (top-down)**, ca în schiță.
  **Mașinile trebuie să fie orientate cu fața în sus.**
* **Stil comun:** pune la începutul fiecărui prompt:
  > *Bright cartoon mobile game asset, top-down view, clean thick outlines, soft shading, saturated
  > colors, casual idle tycoon style, isolated on transparent background, no text, no shadow.*
* Paleta schiței: albastru `#0879C7`, albastru închis `#064D83`, verde `#31C653`, auriu `#FFCB36`,
  roșu `#F05B54`, crem `#F5E8D2`, iarbă `#A8D78D`, asfalt `#697A88`.
* La import, Unity configurează imaginile automat (`ArtImportSettings`): sprite, 9-slice pentru panouri
  și butoane, repetare pentru iarbă.

## 1. Vehicule (`Art/Vehicles/`), văzute de sus, cu fața în sus

Variantele de culoare se numesc `nume_1.png`, `nume_2.png` … și jocul alege una la întâmplare pentru
fiecare mașină.

| Fișier | Dimensiune | Prompt (după stilul comun) |
|---|---|---|
| `compact.png`, `compact_1..5.png` | 256×400 | small compact hatchback car, red / yellow / blue / green / orange / white variants |
| `sedan.png`, `sedan_1..5.png` | 256×440 | family sedan car, various bright colors |
| `suv.png`, `suv_1..3.png` | 256×460 | SUV with roof rails |
| `van.png`, `van_1..2.png` | 256×480 | delivery van, white and yellow |
| `truck.png` | 256×640 | small truck with red cab and blue cargo box |
| `bus.png` | 256×780 | yellow city bus, roof with AC units |
| `heavy_truck.png` | 256×900 | semi truck, red cab and long grey trailer |
| `emergency.png` | 256×440 | white ambulance with red stripes and roof lights |

Proporțiile (lungime/lățime) urmează dimensiunile din simulare; jocul scalează imaginea la lungimea
reală a vehiculului.

## 2. Plaza de taxare (`Art/TollBooths/`)

| Fișier | Dimensiune | Prompt |
|---|---|---|
| `booth.png` | 256×320 | small toll booth kiosk seen from above, cream walls, blue roof, window, tiny green light |
| `barrier_arm.png` | 512×48 | horizontal toll barrier arm, white with red stripes (fără stâlp; rotirea e făcută de joc) |
| `canopy.png` | 512×160 | toll plaza canopy roof seen from above, cream with blue edge stripe (se întinde automat peste toate cabinele) |

## 3. Peisaj (`Art/Environment/`, `Art/Buildings/`)

| Fișier | Dimensiune | Prompt |
|---|---|---|
| `Environment/grass.png` | 512×512 | seamless tileable cartoon grass texture, light green `#A8D78D`, subtle tufts |
| `Environment/tree.png`, `tree_1..4.png` | 256×256 | round cartoon tree crown seen from above, two-tone green |
| `Buildings/house.png`, `house_1..3.png` | 384×320 | small suburban house seen from above, red or blue roof |
| `Buildings/toll_office.png` | 512×384 | small toll office building seen from above, red roof, parking spot |

## 4. Interfață (`Art/UI/`, `Art/Icons/`)

Panoul și butonul se colorează din cod (albastru, verde sau roșu), deci trebuie să fie **albe sau gri
deschis**, cu relief.

| Fișier | Dimensiune | Prompt / descriere |
|---|---|---|
| `UI/panel.png` | 256×256 | white rounded rectangle game UI panel, soft inner bevel, thick rounded corners |
| `UI/button.png` | 256×128 | white glossy pill-shaped game button, bottom bevel, rounded |
| `UI/logo.png` | 1024×600 | logo „TOLL ROAD TYCOON”, litere aurii groase cu contur albastru închis, ca în schiță (cu text) |
| `UI/GameFont.ttf` | – | font rotunjit și gros, cu diacritice românești (de exemplu Fredoka sau Baloo 2, licență OFL) |
| `Icons/coin.png` | 128×128 | shiny gold coin with dollar sign |
| `Icons/menu_play.png` | 128×128 | white play triangle icon |
| `Icons/menu_map.png` | 128×128 | folded map icon |
| `Icons/menu_shop.png` | 128×128 | small shop storefront icon |
| `Icons/menu_missions.png` | 128×128 | clipboard checklist icon |
| `Icons/menu_settings.png` | 128×128 | gear icon |
| `Icons/tab_booths.png` | 128×128 | toll booth icon |
| `Icons/tab_lanes.png` | 128×128 | road lanes icon |
| `Icons/tab_workers.png` | 128×128 | toll worker with cap icon |
| `Icons/tab_upgrade.png` | 128×128 | upgrade arrow icon |
| `Icons/upgrade_speed.png` | 128×128 | stopwatch / speed icon |
| `Icons/upgrade_toll.png` | 128×128 | green dollar coin icon |
| `Icons/upgrade_capacity.png` | 128×128 | queue of cars icon |
| `Icons/upgrade_barrier.png` | 128×128 | lifting barrier icon |
| `Icons/upgrade_automation.png` | 128×128 | automatic payment terminal / card reader icon |
| `Icons/vehicle_<tip>.png` (opțional) | 160×96 | iconiță laterală pentru lista „Tipuri de vehicule”; dacă lipsește, se folosește sprite-ul vehiculului |

## Cum le adaugi

1. Pune fișierele exact cu numele de mai sus în `Toll Road Tycoon/Assets/Resources/Art/<director>/`.
2. Deschide proiectul în Unity: importul le configurează automat.
3. Apasă Play: imaginile găsite înlocuiesc formele provizorii.

Sau trimite-mi imaginile în conversație și le pun eu la locul lor în proiect.

> Notă despre schiță: imaginea de referință are aproximativ 1024 px lățime pentru 12 ecrane, deci
> fiecare obiect are doar câțiva zeci de pixeli și e lipit de fundal. Din ea nu se pot decupa
> sprite-uri utilizabile; ea rămâne ghidul de stil pentru generare.
