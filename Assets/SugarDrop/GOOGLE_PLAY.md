# Sugar Drop pe Google Play

Pașii completi sunt mai jos. Textele pentru fișa din magazin sunt la final, în cele 11 limbi ale jocului.
În Play Console, engleza e limba implicită. Celelalte se adaugă din **Store presence → Main store listing →
Manage translations → Add your own translations**.

## 1. Contul
- Faci contul pe **play.google.com/console**: o taxă unică de 25 $ și verificarea identității.
- Cont personal nou: înainte de producție, Google cere un **test închis cu minimum 12 testeri timp de 14 zile**.

## 2. Cheia de semnare (upload keystore)
- Google Play nu acceptă cheia de test (debug).
- Faci o cheie de upload o singură dată și o încarci în Unity: **Build Automation → Credentials → Android → Add**.
- Păstrează fișierul `.keystore` și parolele în 2 locuri sigure. Fără ele nu mai poți publica actualizări.
- În Play Console lași activat **Play App Signing** (opțiunea implicită).

## 3. Build-ul pentru magazin
- În configurația `sugar-drop` din Unity Build Automation:
  - bifezi **Build App Bundle (AAB)**;
  - alegi credențialele de la pasul 2.
- **Versiunea crește singură.** `versionCode` este numărul build-ului din cloud, iar numele versiunii e
  `1.0.<număr build>`, din `SugarDropSetup.VersionPrefix`. Pentru o versiune mare nouă (ex. 1.1), schimbă prefixul.
  Build-urile vechi nu mai pot fi încărcate după unul mai nou: Google cere ca numărul să crească.

## 4. Aplicația în Play Console
- **Create app**: Sugar Drop, Game, Free, limba implicită **English (United States)**.
- **App content** (formularele obligatorii):
  - **Privacy policy**: link-ul public la `POLITICA_CONFIDENTIALITATE.md` (vezi secțiunea de mai jos);
  - **Ads**: Yes, my app contains ads;
  - **Data safety**:
    - *Device or other IDs*: ID-ul de publicitate și ID-ul anonim de jucător;
    - *App activity → Other user-generated content*: nickname-ul;
    - *App activity → Other actions*: scorul;
    - *App activity → Other user-generated content / App info and performance*: progresul din joc (salvarea în cloud);
    - scopul: *App functionality*, plus *Advertising* pentru ID-ul de publicitate;
    - datele sunt criptate în tranzit; jucătorul poate cere ștergerea prin email;
  - **Content rating**: chestionarul IARC (fără violență, fără chat, fără cumpărături reale);
  - **Target audience**: 13+;
  - **Government apps / Financial features / Health**: No.
- **Main store listing**: textele de mai jos, plus grafica:
  - iconiță 512×512 PNG (`Art/Icon/AppIcon.png`, la 512 px);
  - imagine de prezentare 1024×500;
  - cel puțin 2 capturi de ecran pe verticală (recomandat 4–8: meniu, hartă, nivel, Boss, clasament);
  - categoria **Puzzle**; etichete: Match 3, Casual, Offline.

## 5. Testul închis (14 zile)
- **Testing → Closed testing → Create track**.
- Adaugi lista cu cei 12+ testeri (adresele lor Gmail).
- Încarci `.aab`-ul și trimiți spre verificare (**Send for review**).
- Le trimiți testerilor link-ul de opt-in. Ei instalează jocul din Play Store și îl joacă cel puțin 14 zile.

## 6. Producția
- După 14 zile: **Dashboard → Apply for production**. Răspunzi la întrebările despre test.
- Apoi **Production → Create release**, încarci un `.aab` nou și trimiți. Verificarea durează de obicei câteva zile.

## 7. Înainte de producție
- Cheile LevelPlay în `Scripts/Ads/SugarAdsConfig.cs` (vezi `RECLAME.md`). Altfel jocul arată doar reclame de test.
- Clasamentul creat în Unity Cloud (vezi `CLASAMENT.md`).
- **Target API:** dacă Play Console refuză AAB-ul pentru nivelul Android țintă, trimite mesajul asistentului.

## Politica de confidențialitate: cum o publici gratuit
1. Deschide **sites.google.com** → **Blank site**.
2. Titlu: „Sugar Drop – Privacy Policy”. Lipești textul englezesc din `POLITICA_CONFIDENTIALITATE.md`,
   apoi, pe aceeași pagină, pe cel românesc.
3. Completezi emailul de contact (unde scrie `[email]`).
4. **Publish** → alegi o adresă, ex. `sites.google.com/view/sugardrop-privacy`. Lași vizibilitatea **Public**.
5. Link-ul îl pui în Play Console la **App content → Privacy policy** și în **Store settings → Website** (opțional).

---

# Textele pentru fișa din magazin
Limite Google Play: titlu ≤ 30 de caractere, descriere scurtă ≤ 80, descriere lungă ≤ 4000. Toate textele de mai jos se încadrează.

## English (implicit) — `en-US`
**Titlu** (25/30)
```
Sugar Drop: Candy Match 3
```
**Descriere scurtă** (77/80)
```
Swap candies, blast boosters and beat sweet bosses in a juicy match-3 puzzle!
```
**Descriere lungă** (1192/4000)
```
Welcome to Sugar Drop, the sweetest match-3 puzzle adventure!

Swap neighbouring candies to line up 3 or more of the same flavour and watch them pop in juicy, jelly-wobbling explosions. Help Bobo the cupcake fill his candy jar as you travel along a colourful map packed with levels.

SWEET FEATURES
• Classic match-3: swipe candies, make lines and trigger chain reactions
• Powerful specials: match 4 for a rocket, an L or T for a bomb, 5 for a rainbow candy
• Six special combos: rocket + bomb, bomb + bomb, rainbow + rainbow and more
• Boss battles every 10 levels and three-phase Mega-Bosses every 50
• Tricky obstacles: ice, chains, wooden crates and growing chocolate
• Level goals: drop candies into the jar, collect flavours, melt frosting or reach the score
• Earn up to 3 stars and replay levels for a better score
• Global leaderboard: your best score on every level adds up. How high can you climb?
• Helpful boosters: bomb, +5 moves and shuffle
• Plays offline; lives refill over time
• 11 languages

Sugar Drop is free to play. Ads are optional and only give you rewards such as extra lives, moves or boosters.

Ready for a sweet challenge? Download Sugar Drop and start matching!
```

## Română — `ro`
**Titlu** (29/30)
```
Sugar Drop: Bomboane 3 la fel
```
**Descriere scurtă** (80/80)
```
Schimbă bomboane, fă explozii dulci și învinge boși într-un puzzle potrivește-3!
```
**Descriere lungă** (1255/4000)
```
Bine ai venit în Sugar Drop, cea mai dulce aventură „potrivește 3”!

Schimbă bomboanele vecine ca să aliniezi 3 sau mai multe de aceeași aromă și privește-le cum pocnesc în explozii de jeleu. Ajută-l pe Bobo, cupcake-ul, să-și umple borcanul cu bomboane, pe o hartă colorată plină de niveluri.

CE TE AȘTEAPTĂ
• „Potrivește 3” clasic: tragi bomboanele, faci linii și declanșezi reacții în lanț
• Piese speciale: 4 la rând dau o rachetă, L sau T o bombă, 5 la rând o bomboană curcubeu
• Șase combinații speciale: rachetă + bombă, bombă + bombă, curcubeu + curcubeu și altele
• Lupte cu Boss la fiecare 10 niveluri și Mega-Boss cu trei faze la fiecare 50
• Obstacole: gheață, lanțuri, lăzi de lemn și ciocolată care crește
• Obiective variate: bomboane în borcan, culori de strâns, glazură de topit sau scor de atins
• Până la 3 stele pe nivel; rejoacă pentru un scor mai mare
• Clasament global: cele mai bune scoruri de pe fiecare nivel se adună. Cât de sus urci?
• Ajutoare: bombă, +5 mutări și amestecare
• Se joacă și fără internet; viețile se refac în timp
• 11 limbi

Sugar Drop e gratuit. Reclamele sunt opționale și îți dau doar recompense: vieți, mutări sau ajutoare în plus.

Gata de o provocare dulce? Descarcă Sugar Drop și începe să potrivești!
```

## Español — `es-ES`
**Titlu** (28/30)
```
Sugar Drop: Dulces 3 en raya
```
**Descriere scurtă** (80/80)
```
¡Intercambia caramelos, lanza potenciadores y vence a jefes en este puzle dulce!
```
**Descriere lungă** (1322/4000)
```
¡Bienvenido a Sugar Drop, la aventura de puzles de 3 en raya más dulce!

Intercambia caramelos vecinos para alinear 3 o más del mismo sabor y míralos estallar en explosiones de gelatina. Ayuda a Bobo, el cupcake, a llenar su frasco de caramelos mientras recorres un mapa lleno de color y niveles.

CARACTERÍSTICAS
• 3 en raya clásico: desliza caramelos, haz líneas y provoca reacciones en cadena
• Piezas especiales: 4 en línea crean un cohete, una L o T una bomba, 5 en línea un caramelo arcoíris
• Seis combinaciones especiales: cohete + bomba, bomba + bomba, arcoíris + arcoíris y más
• Batallas contra jefes cada 10 niveles y megajefes de tres fases cada 50
• Obstáculos: hielo, cadenas, cajas de madera y chocolate que crece
• Objetivos variados: lleva caramelos al frasco, reúne sabores, derrite el glaseado o alcanza la puntuación
• Hasta 3 estrellas por nivel; repite niveles para mejorar tu puntuación
• Clasificación mundial: tus mejores puntuaciones de cada nivel se suman. ¿Hasta dónde llegarás?
• Potenciadores: bomba, +5 movimientos y mezclar
• Juega sin conexión; las vidas se recargan con el tiempo
• 11 idiomas

Sugar Drop es gratis. Los anuncios son opcionales y solo te dan recompensas, como vidas, movimientos o potenciadores extra.

¿Listo para un reto dulce? ¡Descarga Sugar Drop y empieza a combinar!
```

## Português (Brasil) — `pt-BR`
**Titlu** (27/30)
```
Sugar Drop: Doces Combina 3
```
**Descriere scurtă** (69/80)
```
Troque doces, solte reforços e vença chefes neste quebra-cabeça doce!
```
**Descriere lungă** (1258/4000)
```
Boas-vindas ao Sugar Drop, a aventura de combinar 3 mais doce de todas!

Troque doces vizinhos para alinhar 3 ou mais do mesmo sabor e veja-os estourar em explosões de geleia. Ajude o Bobo, o cupcake, a encher o pote de doces enquanto percorre um mapa colorido cheio de níveis.

DESTAQUES
• Combinar 3 clássico: deslize doces, forme linhas e crie reações em cadeia
• Peças especiais: 4 em linha criam um foguete, um L ou T uma bomba, 5 em linha um doce arco-íris
• Seis combinações especiais: foguete + bomba, bomba + bomba, arco-íris + arco-íris e mais
• Batalhas contra chefes a cada 10 níveis e megachefes com três fases a cada 50
• Obstáculos: gelo, correntes, caixas de madeira e chocolate que cresce
• Objetivos variados: leve doces ao pote, junte sabores, derreta a cobertura ou alcance a pontuação
• Até 3 estrelas por nível; jogue de novo para melhorar a pontuação
• Ranking mundial: suas melhores pontuações em cada nível se somam. Até onde você vai chegar?
• Reforços: bomba, +5 jogadas e embaralhar
• Jogue sem internet; as vidas recarregam com o tempo
• 11 idiomas

Sugar Drop é gratuito. Os anúncios são opcionais e só dão recompensas, como vidas, jogadas ou reforços extras.

Pronto para um desafio doce? Baixe o Sugar Drop e comece a combinar!
```

## Français — `fr-FR`
**Titlu** (28/30)
```
Sugar Drop : Bonbons Match 3
```
**Descriere scurtă** (76/80)
```
Échange des bonbons, lance des bonus et bats des boss dans un puzzle sucré !
```
**Descriere lungă** (1351/4000)
```
Bienvenue dans Sugar Drop, l'aventure match-3 la plus sucrée !

Échange des bonbons voisins pour en aligner 3 ou plus de la même saveur et regarde-les éclater en explosions de gelée. Aide Bobo le cupcake à remplir son bocal de bonbons en parcourant une carte colorée remplie de niveaux.

AU MENU
• Match-3 classique : fais glisser les bonbons, forme des lignes et déclenche des réactions en chaîne
• Pièces spéciales : 4 alignés donnent une fusée, un L ou un T une bombe, 5 alignés un bonbon arc-en-ciel
• Six combinaisons spéciales : fusée + bombe, bombe + bombe, arc-en-ciel + arc-en-ciel et plus encore
• Combats de boss tous les 10 niveaux et méga boss en trois phases tous les 50
• Obstacles : glace, chaînes, caisses en bois et chocolat qui pousse
• Objectifs variés : fais tomber les bonbons dans le bocal, récupère des saveurs, fais fondre le glaçage ou atteins le score
• Jusqu'à 3 étoiles par niveau ; rejoue pour améliorer ton score
• Classement mondial : tes meilleurs scores de chaque niveau s'additionnent. Jusqu'où iras-tu ?
• Bonus : bombe, +5 coups et mélange
• Jouable hors ligne ; les vies se rechargent avec le temps
• 11 langues

Sugar Drop est gratuit. Les publicités sont facultatives et te donnent seulement des récompenses : vies, coups ou bonus en plus.

Prêt pour un défi sucré ? Télécharge Sugar Drop et commence à aligner !
```

## Deutsch — `de-DE`
**Titlu** (26/30)
```
Sugar Drop: Bonbon Match 3
```
**Descriere scurtă** (79/80)
```
Tausche Bonbons, zünde Booster und besiege Bosse in einem süßen Match-3-Puzzle!
```
**Descriere lungă** (1353/4000)
```
Willkommen bei Sugar Drop, dem süßesten Match-3-Abenteuer!

Tausche benachbarte Bonbons, um 3 oder mehr gleiche Sorten in eine Reihe zu bringen, und sieh zu, wie sie in wackeligen Gelee-Explosionen platzen. Hilf Bobo, dem Cupcake, sein Bonbonglas zu füllen, während du über eine bunte Karte voller Level reist.

DAS ERWARTET DICH
• Klassisches Match-3: Bonbons wischen, Reihen bilden und Kettenreaktionen auslösen
• Spezialteile: 4 in einer Reihe ergeben eine Rakete, ein L oder T eine Bombe, 5 in einer Reihe ein Regenbogen-Bonbon
• Sechs Spezial-Kombos: Rakete + Bombe, Bombe + Bombe, Regenbogen + Regenbogen und mehr
• Bosskämpfe alle 10 Level und Mega-Bosse mit drei Phasen alle 50 Level
• Hindernisse: Eis, Ketten, Holzkisten und wachsende Schokolade
• Abwechslungsreiche Ziele: Bonbons ins Glas bringen, Sorten sammeln, Glasur schmelzen oder Punktzahl erreichen
• Bis zu 3 Sterne pro Level; spiele Level erneut für eine bessere Punktzahl
• Weltweite Bestenliste: Deine Bestwerte aus jedem Level werden addiert. Wie weit kommst du?
• Booster: Bombe, +5 Züge und Mischen
• Auch offline spielbar; Leben laden sich mit der Zeit wieder auf
• 11 Sprachen

Sugar Drop ist kostenlos. Werbung ist freiwillig und bringt dir nur Belohnungen wie zusätzliche Leben, Züge oder Booster.

Bereit für eine süße Herausforderung? Lade Sugar Drop herunter und leg los!
```

## Italiano — `it-IT`
**Titlu** (29/30)
```
Sugar Drop: Caramelle Match 3
```
**Descriere scurtă** (75/80)
```
Scambia caramelle, usa potenziamenti e sconfiggi i boss in un dolce puzzle!
```
**Descriere lungă** (1299/4000)
```
Benvenuto in Sugar Drop, l'avventura match-3 più dolce che ci sia!

Scambia caramelle vicine per allinearne 3 o più dello stesso gusto e guardale scoppiare in esplosioni di gelatina. Aiuta Bobo, il cupcake, a riempire il suo barattolo di caramelle mentre attraversi una mappa colorata piena di livelli.

COSA TROVERAI
• Match-3 classico: trascina le caramelle, crea file e scatena reazioni a catena
• Pezzi speciali: 4 in fila creano un razzo, una L o una T una bomba, 5 in fila una caramella arcobaleno
• Sei combo speciali: razzo + bomba, bomba + bomba, arcobaleno + arcobaleno e altre
• Battaglie contro i boss ogni 10 livelli e mega boss in tre fasi ogni 50
• Ostacoli: ghiaccio, catene, casse di legno e cioccolato che cresce
• Obiettivi vari: porta le caramelle nel barattolo, raccogli i gusti, sciogli la glassa o raggiungi il punteggio
• Fino a 3 stelle per livello; rigioca per migliorare il punteggio
• Classifica mondiale: i tuoi record di ogni livello si sommano. Fin dove arriverai?
• Potenziamenti: bomba, +5 mosse e mescola
• Si gioca anche offline; le vite si ricaricano col tempo
• 11 lingue

Sugar Drop è gratuito. La pubblicità è facoltativa e ti dà solo ricompense, come vite, mosse o potenziamenti extra.

Pronto per una sfida dolcissima? Scarica Sugar Drop e inizia ad abbinare!
```

## Türkçe — `tr-TR`
**Titlu** (28/30)
```
Sugar Drop: Şeker Eşleştirme
```
**Descriere scurtă** (75/80)
```
Şekerleri değiştir, güçlendiricileri patlat, tatlı bulmacada boss'ları yen!
```
**Descriere lungă** (1269/4000)
```
En tatlı 3'lü eşleştirme macerası Sugar Drop'a hoş geldin!

Komşu şekerlerin yerini değiştirerek aynı lezzetten 3 veya daha fazlasını sırala ve jöle gibi titreyen patlamalarla yok olmalarını izle. Rengârenk ve seviyelerle dolu bir haritada ilerlerken cupcake Bobo'nun şeker kavanozunu doldurmasına yardım et.

ÖZELLİKLER
• Klasik 3'lü eşleştirme: şekerleri kaydır, sıra oluştur ve zincirleme reaksiyonlar başlat
• Özel parçalar: 4'lü sıra roket, L veya T şekli bomba, 5'li sıra gökkuşağı şekeri yapar
• Altı özel kombo: roket + bomba, bomba + bomba, gökkuşağı + gökkuşağı ve dahası
• Her 10 seviyede bir boss savaşı, her 50 seviyede üç aşamalı Mega Boss
• Engeller: buz, zincirler, tahta sandıklar ve büyüyen çikolata
• Farklı hedefler: şekerleri kavanoza indir, lezzetleri topla, kremayı erit veya puana ulaş
• Her seviyede 3 yıldıza kadar kazan; daha iyi puan için tekrar oyna
• Dünya sıralaması: her seviyedeki en iyi skorların toplanır. Ne kadar yükselebilirsin?
• Güçlendiriciler: bomba, +5 hamle ve karıştırma
• İnternetsiz oynanabilir; canlar zamanla dolar
• 11 dil

Sugar Drop ücretsizdir. Reklamlar isteğe bağlıdır ve sadece ekstra can, hamle veya güçlendirici gibi ödüller verir.

Tatlı bir meydan okumaya hazır mısın? Sugar Drop'u indir ve eşleştirmeye başla!
```

## Bahasa Indonesia — `id`
**Titlu** (27/30)
```
Sugar Drop: Cocokkan Permen
```
**Descriere scurtă** (73/80)
```
Tukar permen, ledakkan booster, dan kalahkan bos di puzzle match-3 manis!
```
**Descriere lungă** (1277/4000)
```
Selamat datang di Sugar Drop, petualangan puzzle match-3 paling manis!

Tukar permen yang bersebelahan untuk menjajarkan 3 atau lebih dengan rasa yang sama, lalu lihat mereka meletus dalam ledakan jeli yang kenyal. Bantu Bobo si cupcake mengisi stoples permennya sambil menjelajahi peta penuh warna dan level.

FITUR MANIS
• Match-3 klasik: geser permen, buat barisan, dan picu reaksi berantai
• Kepingan spesial: 4 berjajar jadi roket, bentuk L atau T jadi bom, 5 berjajar jadi permen pelangi
• Enam kombo spesial: roket + bom, bom + bom, pelangi + pelangi, dan lainnya
• Pertarungan bos setiap 10 level dan Mega Bos tiga fase setiap 50 level
• Rintangan: es, rantai, peti kayu, dan cokelat yang terus tumbuh
• Target beragam: turunkan permen ke stoples, kumpulkan rasa, lelehkan krim, atau capai skor
• Raih hingga 3 bintang per level; mainkan lagi untuk skor lebih baik
• Papan peringkat dunia: skor terbaikmu di setiap level dijumlahkan. Seberapa tinggi kamu bisa naik?
• Booster: bom, +5 langkah, dan acak
• Bisa dimainkan offline; nyawa terisi ulang seiring waktu
• 11 bahasa

Sugar Drop gratis dimainkan. Iklan bersifat opsional dan hanya memberimu hadiah seperti nyawa, langkah, atau booster tambahan.

Siap untuk tantangan manis? Unduh Sugar Drop dan mulai mencocokkan!
```

## Русский — `ru-RU`
**Titlu** (21/30)
```
Sugar Drop: три в ряд
```
**Descriere scurtă** (71/80)
```
Меняй конфеты, взрывай бустеры и побеждай боссов в сладкой головоломке!
```
**Descriere lungă** (1224/4000)
```
Добро пожаловать в Sugar Drop — самую сладкую головоломку «три в ряд»!

Меняй местами соседние конфеты, собирай 3 и больше одного вкуса и смотри, как они лопаются желейными взрывами. Помоги кексу Бобо наполнить банку конфетами на пути по яркой карте, полной уровней.

ЧТО ТЕБЯ ЖДЁТ
• Классика «три в ряд»: сдвигай конфеты, собирай линии и запускай цепные реакции
• Особые фишки: 4 в ряд дают ракету, буквы L и T — бомбу, 5 в ряд — радужную конфету
• Шесть особых комбинаций: ракета + бомба, бомба + бомба, радуга + радуга и другие
• Битвы с боссами каждые 10 уровней и трёхфазные мегабоссы каждые 50
• Препятствия: лёд, цепи, деревянные ящики и растущий шоколад
• Разные цели: доставь конфеты в банку, собери вкусы, растопи глазурь или набери очки
• Новые земли каждые 10 уровней: от Клубничных полей до Радужной галактики
• Колесо удачи, ежедневный подарок и задания дня
• Костюмы для Бобо, которые открываются звёздами
• Мировой рейтинг: лучшие результаты на каждом уровне складываются
• Играй без интернета; жизни восстанавливаются со временем
• 11 языков

Sugar Drop — бесплатная игра. Реклама необязательна и только даёт награды: жизни, ходы или бустеры.

Готов к сладкому испытанию? Скачай Sugar Drop и начни собирать!
```

## Українська — `uk`
**Titlu** (21/30)
```
Sugar Drop: три в ряд
```
**Descriere scurtă** (73/80)
```
Міняй цукерки, підривай бустери й перемагай босів у солодкій головоломці!
```
**Descriere lungă** (1219/4000)
```
Ласкаво просимо до Sugar Drop — найсолодшої головоломки «три в ряд»!

Міняй місцями сусідні цукерки, збирай 3 й більше одного смаку та дивись, як вони лускають желейними вибухами. Допоможи кексу Бобо наповнити банку цукерками на шляху яскравою мапою, повною рівнів.

ЩО НА ТЕБЕ ЧЕКАЄ
• Класика «три в ряд»: посувай цукерки, збирай лінії та запускай ланцюгові реакції
• Особливі фішки: 4 в ряд дають ракету, літери L і T — бомбу, 5 в ряд — райдужну цукерку
• Шість особливих комбінацій: ракета + бомба, бомба + бомба, веселка + веселка та інші
• Битви з босами кожні 10 рівнів і трифазні мегабоси кожні 50
• Перешкоди: лід, ланцюги, дерев'яні ящики та шоколад, що росте
• Різні цілі: доправ цукерки в банку, збери смаки, розтопи глазур або набери очки
• Нові землі кожні 10 рівнів: від Полуничних полів до Райдужної галактики
• Колесо удачі, щоденний подарунок і завдання дня
• Костюми для Бобо, що відкриваються зірками
• Світовий рейтинг: найкращі результати на кожному рівні додаються
• Грай без інтернету; життя відновлюються з часом
• 11 мов

Sugar Drop — безкоштовна гра. Реклама необов'язкова й лише дає нагороди: життя, ходи або бустери.

Готовий до солодкого випробування? Завантаж Sugar Drop і починай збирати!
```
