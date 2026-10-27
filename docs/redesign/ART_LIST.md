# Drop Quest — lista de imagini (opționale, înlocuiesc desenele din cod)

**Unde:** `Assets/SugarDrop/Resources/Art/`, cu exact numele de mai jos (`.png`). Jocul le încarcă singur. Ce lipsește rămâne desenat din cod, deci le poți adăuga pe rând.

**Format:**
- PNG, sRGB.
- **Obiectele** (piese, obstacole, iconițe): fundal transparent, obiectul centrat, cam 85% din imagine, fără umbră pe fundal și fără text.
- **Fundalurile:** fără transparență, compoziție verticală, cu zona din mijloc mai liniștită (acolo stă tabla).

**Stil comun (pune-l la începutul fiecărui prompt):**
> Stylized 3D mobile puzzle game asset, glossy and polished, soft studio lighting from the top-left, vibrant but harmonious colors, clean silhouette, nature adventure theme, high detail, centered, transparent background, no text, no watermark.

**Licență:** folosește doar un generator sau un ilustrator care îți dă **drept de utilizare comercială**. Nu copia elemente din jocuri existente.

## Piesele (256×256)
Indexul trebuie păstrat, pentru că regulile depind de el. Piesa 1 e **apa** de la nivelurile cu foc.

| Fișier | Descriere / prompt |
|---|---|
| `piece0.png` | Red mushroom with white spots, cute rounded cap, cream stem |
| `piece1.png` | Blue water drop, translucent, glossy highlight |
| `piece2.png` | Fresh green leaf, slightly tilted, visible veins |
| `piece3.png` | Golden faceted gemstone (octagon cut), shiny |
| `piece4.png` | Purple amethyst crystal, elongated hexagon, faceted |

## Piesele speciale (256×256)
| Fișier | Descriere |
|---|---|
| `rocket.png` | Small cartoon rocket, pointing **right**. Jocul o rotește pentru cea verticală. |
| `bomb.png` | Round cartoon bomb with lit fuse |
| `pearl.png` | Plain glossy white sphere (baza bilei curcubeu) |
| `rainbow.png` | Rainbow swirl pattern on a sphere, transparent around. Se pune peste `pearl`. |

## Obstacole (256×256)
| Fișier | Descriere |
|---|---|
| `crate.png` | Wooden crate with metal corners |
| `chocolate.png` | Grey spiral stone block (obstacolul care se întinde) |
| `ice.png` | Translucent ice cube, **semi-transparent in the middle** (se pune peste o piesă) |
| `chain.png` | Green vines wrapping a square, **empty center** (se pune peste o piesă) |
| `pot.png` | Terracotta flower pot (fără plantă) |
| `flame.png` | Bright cartoon flame, orange with a yellow core |
| `portal.png` | Magic swirl portal ring, blue-violet |
| `flower.png` | Pink five-petal flower |

## Personaje (256×256)
| Fișier | Descriere |
|---|---|
| `bobo.png` | Bobo în picioare, liniștit: blănos alb, urechi și moț ca niște frunze albastre, eșarfă albastră cu stea aurie (pătrat, picioarele la ~4% de jos) |
| `bobohappy.png` | Bucuros, sărbătorește (victorie, combo mare) |
| `boboscared.png` | Surprins / speriat (pericol, foc aproape) |
| `bobosad.png` | Plânge (nivel eșuat, Bobo sub tablă la pierdere) |
| `bobowave.png` | Salută (meniul principal, salutul, harta, invitația din poveste) |
| `bobothumbs.png` | Cu degetul mare (când îl atingi în meniu) |
| `bobothink.png` | Gânditor, cu „?” (antrenamentul) |
| `boboworried.png` | Îngrijorat (pagina cu răufăcătorul, fără monede, fără reclamă) |
| `bobosleep.png` | Doarme (fără vieți) |
| `bobolove.png` | Ține o inimă (cadoul zilnic) |
| `bobocrystal.png` | Ține cristalul albastru (finalul episodului) |
| `bobogem.png` | Ține gema verde (rezervă) |
| `bobostar.png` | Ține steaua (3 stele, misiuni, capitol terminat) |
| `boborun.png` | Fuge (pagina „Bobo pornește la drum”) |
| `bobotoken.png` | Capul lui Bobo, zâmbind (piesa de pe tablă, butonul Puterea lui Bobo) |
| `bobotokenscared.png` | Capul lui Bobo, speriat (nivelurile cu foc) |
| `boss0.png` … `boss6.png` | Fețele boșilor: 0 Regele Noroiului, 1 Regina Gheții, 2 Dragonul de Foc, 3 Monstrul Mlaștinii, 4 Împăratul Furtunii, 5 Regina Spinilor, 6 Titanul de Piatră. Prieten-amenințători, nu înfricoșători. |

## Iconițe (256×256)
`gift.png` (cadou), `trophy.png` (trofeu), `lifeheart.png` (inimă roșie, vieți).

## Fundaluri (1242×2688, vertical, fără transparență)
| Fișier | Ținut | Prompt (după stilul comun, fără „transparent background”) |
|---|---|---|
| `bggradient.png` | meniul principal | Floating islands with waterfalls and a wooden bridge, blue sky, lush greenery |
| `sky0.png` | 1. Câmpia cu Flori | Sunny meadow with strawberry bushes, rolling hills, soft clouds |
| `sky1.png` | 2. Pădurea de Smarald | Enchanted mint-green forest, tall trees, light rays |
| `sky2.png` | 3. Lacul Albastru | Calm blueberry lake, waterfalls, misty mountains |
| `sky3.png` | 4. Deșertul Auriu | Warm lemon desert, dunes, palm oasis |
| `sky4.png` | 5. Castelul de Ametist | Purple fairytale castle on a hill, vineyards |
| `sky5.png` | 6. Muntele de Foc | Brown rocky mountains with chocolate rivers |
| `sky6.png` | 7. Regatul de Gheață | Snowy ice kingdom, crystal spires, aurora |
| `sky7.png` | 8. Canionul de Chihlimbar | Orange canyon, layered rocks, warm sunset |
| `sky8.png` | 9. Insulele din Nori | Pastel cloud kingdom, floating islands |
| `sky9.png` | 10. Galaxia Curcubeu | Starry rainbow galaxy, planets, night sky |

## Logo (1024×600, transparent)
`logo.png`: titlul „DROP QUEST” în litere 3D groase, cu „DROP” albastru-apă și „QUEST” auriu, cu frunze și o picătură ca decor. Sau scris de un designer. Logo-ul generat cu AI are adesea litere greșite, așa că verifică textul.

## După ce adaugi imaginile
Fă un build. Pe telefon verifică:
- dacă piesele au aceeași mărime vizuală;
- dacă fundalurile nu acoperă tabla cu detalii prea aglomerate.

Dacă ceva iese prea mare sau prea mic, spune-mi și ajustez dimensiunea din cod.
