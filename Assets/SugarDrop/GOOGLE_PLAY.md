# Drop Quest pe Google Play

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
- **Create app**: Drop Quest, Game, Free, limba implicită **English (United States)**.
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
2. Titlu: „Drop Quest – Privacy Policy”. Lipești textul englezesc din `POLITICA_CONFIDENTIALITATE.md`,
   apoi, pe aceeași pagină, pe cel românesc.
3. Completezi emailul de contact (unde scrie `[email]`).
4. **Publish** → alegi o adresă, ex. `sites.google.com/view/sugardrop-privacy`. Lași vizibilitatea **Public**.
5. Link-ul îl pui în Play Console la **App content → Privacy policy** și în **Store settings → Website** (opțional).

---

# Textele pentru fișa din magazin
Limite Google Play: titlu ≤ 30 de caractere, descriere scurtă ≤ 80, descriere lungă ≤ 4000. Toate textele de mai jos se încadrează.

## English (implicit) — `en-US`
**Titlu** (28/30)
```
Drop Quest: Bobo's Adventure
```
**Descriere scurtă** (73/80)
```
Help fluffy Bobo cross magical lands in a story-filled match-3 adventure!
```
**Descriere lungă** (1173/4000)
```
Welcome to Drop Quest, a cosy match-3 adventure starring Bobo!

Swap neighbouring pieces to line up 3 or more and watch them pop in lively chain reactions. Help Bobo, a fluffy white friend with leafy blue ears, cross a map of magical lands, one level at a time.

WHAT AWAITS YOU
• Classic match-3: swap pieces, make lines and trigger chain reactions
• Special pieces: match 4 or more to create rockets, bombs and rainbow pieces
• Powerful combos when you combine special pieces
• Over 100 levels with comic-style story scenes and dialogue
• New lands with new looks and new tricks as you travel the map
• Boss battles every 10 levels and Mega-Bosses every 50
• Varied goals and obstacles: ice, chains, crates and more
• Special “Save Bobo” rescue levels
• Up to 3 stars per level, daily reward, daily missions and weekend events
• Global leaderboard: how high can you climb?
• Helpful boosters when a level gets tough
• Plays offline; 11 languages

Drop Quest is free to play, with no in-app purchases. It contains ads: a small banner during levels and optional rewarded videos for extra lives, moves or boosters.

Ready for an adventure? Download Drop Quest and help Bobo!
```

## Română — `ro`
**Titlu** (29/30)
```
Drop Quest: Aventura lui Bobo
```
**Descriere scurtă** (75/80)
```
Ajută-l pe Bobo să străbată ținuturi magice într-o aventură „potrivește 3”!
```
**Descriere lungă** (1305/4000)
```
Bine ai venit în Drop Quest, o aventură „potrivește 3” cu Bobo în rol principal!

Schimbă piesele vecine ca să aliniezi 3 sau mai multe și privește-le cum pocnesc în reacții în lanț. Ajută-l pe Bobo, un prieten alb și pufos cu urechi-frunze albastre, să străbată o hartă cu ținuturi magice, nivel după nivel.

CE TE AȘTEAPTĂ
• „Potrivește 3” clasic: schimbi piese, faci linii și declanșezi reacții în lanț
• Piese speciale: 4 sau mai multe la rând dau rachete, bombe și piese curcubeu
• Combinații puternice când unești piese speciale
• Peste 100 de niveluri cu scene și dialoguri în stil benzi desenate
• Ținuturi noi, cu alt aspect și alte capcane, pe măsură ce avansezi pe hartă
• Lupte cu Boss la fiecare 10 niveluri și Mega-Boss la fiecare 50
• Obiective și obstacole variate: gheață, lanțuri, lăzi și altele
• Niveluri speciale de salvare: „Salvează-l pe Bobo”
• Până la 3 stele pe nivel, recompensă zilnică, misiuni zilnice și evenimente de weekend
• Clasament global: cât de sus urci?
• Ajutoare utile când nivelul devine greu
• Se joacă și fără internet; 11 limbi

Drop Quest e gratuit, fără achiziții în aplicație. Conține reclame: un banner mic în timpul nivelurilor și videoclipuri opționale pentru vieți, mutări sau ajutoare în plus.

Gata de aventură? Descarcă Drop Quest și ajută-l pe Bobo!
```

## Español — `es-ES`
**Titlu** (28/30)
```
Drop Quest: Aventura de Bobo
```
**Descriere scurtă** (69/80)
```
¡Ayuda a Bobo a cruzar tierras mágicas en una aventura de combinar 3!
```
**Descriere lungă** (1305/4000)
```
¡Bienvenido a Drop Quest, una aventura de combinar 3 protagonizada por Bobo!

Intercambia piezas vecinas para alinear 3 o más y míralas explotar en reacciones en cadena. Ayuda a Bobo, un amigo blanco y peludo con orejas de hoja azules, a cruzar un mapa de tierras mágicas, nivel a nivel.

QUÉ TE ESPERA
• Clásico de combinar 3: intercambia piezas, haz líneas y provoca reacciones en cadena
• Piezas especiales: combina 4 o más para crear cohetes, bombas y piezas arcoíris
• Combos potentes al unir piezas especiales
• Más de 100 niveles con escenas y diálogos al estilo cómic
• Tierras nuevas con otro aspecto y nuevas trampas a medida que avanzas
• Combates contra jefes cada 10 niveles y Mega-Jefes cada 50
• Objetivos y obstáculos variados: hielo, cadenas, cajas y más
• Niveles especiales de rescate: «Salva a Bobo»
• Hasta 3 estrellas por nivel, recompensa diaria, misiones diarias y eventos de fin de semana
• Clasificación mundial: ¿hasta dónde llegarás?
• Potenciadores útiles cuando el nivel se complica
• Se juega sin conexión; 11 idiomas

Drop Quest es gratis y no tiene compras dentro de la aplicación. Contiene anuncios: un pequeño banner durante los niveles y vídeos opcionales para conseguir vidas, movimientos o potenciadores.

¿Listo para la aventura? ¡Descarga Drop Quest y ayuda a Bobo!
```

## Português (Brasil) — `pt-BR`
**Titlu** (28/30)
```
Drop Quest: Aventura do Bobo
```
**Descriere scurtă** (69/80)
```
Ajude o Bobo a atravessar terras mágicas numa aventura de combinar 3!
```
**Descriere lungă** (1262/4000)
```
Bem-vindo ao Drop Quest, uma aventura de combinar 3 estrelada pelo Bobo!

Troque peças vizinhas para alinhar 3 ou mais e veja-as estourar em reações em cadeia. Ajude o Bobo, um amigo branco e fofinho com orelhas de folha azuis, a atravessar um mapa de terras mágicas, fase por fase.

O QUE TE ESPERA
• Clássico de combinar 3: troque peças, faça linhas e provoque reações em cadeia
• Peças especiais: combine 4 ou mais para criar foguetes, bombas e peças arco-íris
• Combos poderosos ao juntar peças especiais
• Mais de 100 fases com cenas e diálogos em estilo de quadrinhos
• Terras novas com visual diferente e novas armadilhas conforme você avança
• Batalhas contra chefes a cada 10 fases e Mega-Chefes a cada 50
• Objetivos e obstáculos variados: gelo, correntes, caixas e mais
• Fases especiais de resgate: “Salve o Bobo”
• Até 3 estrelas por fase, recompensa diária, missões diárias e eventos de fim de semana
• Ranking global: até onde você chega?
• Reforços úteis quando a fase fica difícil
• Funciona sem internet; 11 idiomas

O Drop Quest é gratuito e não tem compras no aplicativo. Contém anúncios: um pequeno banner durante as fases e vídeos opcionais para ganhar vidas, jogadas ou reforços.

Pronto para a aventura? Baixe o Drop Quest e ajude o Bobo!
```

## Français — `fr-FR`
**Titlu** (29/30)
```
Drop Quest : Aventure de Bobo
```
**Descriere scurtă** (69/80)
```
Aide Bobo à traverser des terres magiques dans une aventure match-3 !
```
**Descriere lungă** (1360/4000)
```
Bienvenue dans Drop Quest, une aventure match-3 avec Bobo en vedette !

Échange des pièces voisines pour en aligner 3 ou plus et regarde-les éclater en réactions en chaîne. Aide Bobo, un ami blanc et douillet aux oreilles-feuilles bleues, à traverser une carte de terres magiques, niveau après niveau.

CE QUI T'ATTEND
• Match-3 classique : échange des pièces, fais des lignes et déclenche des réactions en chaîne
• Pièces spéciales : aligne 4 pièces ou plus pour créer fusées, bombes et pièces arc-en-ciel
• Combos puissants en combinant des pièces spéciales
• Plus de 100 niveaux avec scènes et dialogues façon bande dessinée
• De nouvelles terres, avec un autre décor et de nouveaux pièges, au fil de la carte
• Combats de boss tous les 10 niveaux et Méga-Boss tous les 50
• Objectifs et obstacles variés : glace, chaînes, caisses et plus
• Niveaux de sauvetage spéciaux : « Sauve Bobo »
• Jusqu'à 3 étoiles par niveau, récompense quotidienne, missions quotidiennes et événements du week-end
• Classement mondial : jusqu'où iras-tu ?
• Bonus utiles quand un niveau se corse
• Jouable hors ligne ; 11 langues

Drop Quest est gratuit, sans achats intégrés. Il contient des publicités : une petite bannière pendant les niveaux et des vidéos facultatives pour gagner des vies, des coups ou des bonus.

Prêt pour l'aventure ? Télécharge Drop Quest et aide Bobo !
```

## Deutsch — `de-DE`
**Titlu** (27/30)
```
Drop Quest: Bobos Abenteuer
```
**Descriere scurtă** (71/80)
```
Hilf Bobo durch magische Länder – ein Match-3-Abenteuer mit Geschichte!
```
**Descriere lungă** (1298/4000)
```
Willkommen bei Drop Quest, einem gemütlichen Match-3-Abenteuer mit Bobo in der Hauptrolle!

Tausche benachbarte Steine, um 3 oder mehr in eine Reihe zu bringen, und sieh zu, wie sie in Kettenreaktionen zerplatzen. Hilf Bobo, einem weißen Plüschfreund mit blauen Blätterohren, über eine Karte voller magischer Länder – Level für Level.

DAS ERWARTET DICH
• Klassisches Match-3: Steine tauschen, Reihen bilden, Kettenreaktionen auslösen
• Spezialsteine: Aus 4 oder mehr entstehen Raketen, Bomben und Regenbogensteine
• Starke Kombos, wenn du Spezialsteine verbindest
• Über 100 Level mit Szenen und Dialogen im Comic-Stil
• Neue Länder mit neuem Look und neuen Tücken auf deiner Reise
• Bosskämpfe alle 10 Level und Mega-Bosse alle 50
• Abwechslungsreiche Ziele und Hindernisse: Eis, Ketten, Kisten und mehr
• Besondere Rettungslevel: „Rette Bobo“
• Bis zu 3 Sterne pro Level, tägliche Belohnung, Tagesmissionen und Wochenend-Events
• Weltweite Bestenliste: Wie weit kommst du?
• Hilfreiche Booster, wenn ein Level knifflig wird
• Auch offline spielbar; 11 Sprachen

Drop Quest ist kostenlos und hat keine In-App-Käufe. Es enthält Werbung: ein kleines Banner während der Level und optionale Videos für Extra-Leben, Züge oder Booster.

Bereit für das Abenteuer? Lade Drop Quest herunter und hilf Bobo!
```

## Italiano — `it-IT`
**Titlu** (29/30)
```
Drop Quest: Avventura di Bobo
```
**Descriere scurtă** (65/80)
```
Aiuta Bobo ad attraversare terre magiche in un'avventura match-3!
```
**Descriere lungă** (1250/4000)
```
Benvenuto in Drop Quest, un'avventura match-3 con Bobo protagonista!

Scambia i pezzi vicini per allinearne 3 o più e guardali esplodere in reazioni a catena. Aiuta Bobo, un amico bianco e soffice con orecchie-foglia blu, ad attraversare una mappa di terre magiche, livello dopo livello.

COSA TI ASPETTA
• Match-3 classico: scambia i pezzi, crea linee e innesca reazioni a catena
• Pezzi speciali: allinea 4 o più pezzi per creare razzi, bombe e pezzi arcobaleno
• Combo potenti unendo i pezzi speciali
• Oltre 100 livelli con scene e dialoghi in stile fumetto
• Terre nuove con un aspetto diverso e nuove insidie man mano che avanzi
• Battaglie contro i boss ogni 10 livelli e Mega-Boss ogni 50
• Obiettivi e ostacoli vari: ghiaccio, catene, casse e altro
• Livelli speciali di salvataggio: «Salva Bobo»
• Fino a 3 stelle per livello, premio giornaliero, missioni giornaliere ed eventi del weekend
• Classifica globale: fin dove arriverai?
• Potenziamenti utili quando il livello si fa duro
• Si gioca anche offline; 11 lingue

Drop Quest è gratuito e senza acquisti in-app. Contiene annunci: un piccolo banner durante i livelli e video facoltativi per ottenere vite, mosse o potenziamenti.

Pronto per l'avventura? Scarica Drop Quest e aiuta Bobo!
```

## Türkçe — `tr-TR`
**Titlu** (29/30)
```
Drop Quest: Bobo'nun Macerası
```
**Descriere scurtă** (70/80)
```
Bobo'ya büyülü diyarlarda yardım et: hikâyeli bir eşleştirme macerası!
```
**Descriere lungă** (1306/4000)
```
Drop Quest'e hoş geldin: Bobo'nun başrolde olduğu sevimli bir 3'lü eşleştirme macerası!

Yan yana parçaları değiştirerek 3 veya daha fazlasını sıraya diz ve zincirleme patlamaları izle. Mavi yaprak kulaklı, beyaz ve tüylü dostumuz Bobo'ya büyülü diyarlarla dolu bir haritada, seviye seviye yardım et.

SENİ NELER BEKLİYOR
• Klasik 3'lü eşleştirme: parçaları değiştir, sıra yap, zincirleme tepkimeleri başlat
• Özel parçalar: 4 veya daha fazlasını eşleştirerek roket, bomba ve gökkuşağı parçası yap
• Özel parçaları birleştirince güçlü kombinasyonlar
• Çizgi roman tarzı sahneler ve diyaloglarla 100'den fazla seviye
• Haritada ilerledikçe yeni görünümlü ve yeni tuzaklı diyarlar
• Her 10 seviyede patron, her 50 seviyede Mega Patron savaşı
• Çeşitli hedefler ve engeller: buz, zincir, sandık ve daha fazlası
• Özel kurtarma seviyeleri: “Bobo'yu Kurtar”
• Seviye başına 3 yıldız, günlük ödül, günlük görevler ve hafta sonu etkinlikleri
• Dünya sıralaması: ne kadar yükselebilirsin?
• Seviye zorlaşınca işe yarayan güçlendiriciler
• İnternet olmadan oynanır; 11 dil

Drop Quest ücretsizdir ve uygulama içi satın alma içermez. Reklam içerir: seviyeler sırasında küçük bir banner ve ekstra can, hamle veya güçlendirici için isteğe bağlı videolar.

Maceraya hazır mısın? Drop Quest'i indir ve Bobo'ya yardım et!
```

## Bahasa Indonesia — `id`
**Titlu** (28/30)
```
Drop Quest: Petualangan Bobo
```
**Descriere scurtă** (75/80)
```
Bantu Bobo menjelajahi negeri ajaib dalam petualangan match-3 penuh cerita!
```
**Descriere lungă** (1302/4000)
```
Selamat datang di Drop Quest, petualangan match-3 yang seru dengan Bobo sebagai bintangnya!

Tukar potongan yang bersebelahan untuk menyusun 3 atau lebih, lalu saksikan mereka meledak dalam reaksi berantai. Bantu Bobo, sahabat putih berbulu dengan telinga daun biru, menjelajahi peta negeri ajaib, level demi level.

YANG MENUNGGUMU
• Match-3 klasik: tukar potongan, buat barisan, dan picu reaksi berantai
• Potongan spesial: cocokkan 4 atau lebih untuk membuat roket, bom, dan potongan pelangi
• Kombo dahsyat saat menggabungkan potongan spesial
• Lebih dari 100 level dengan adegan dan dialog bergaya komik
• Negeri baru dengan tampilan dan jebakan baru seiring kamu maju di peta
• Pertarungan bos setiap 10 level dan Mega-Bos setiap 50 level
• Tujuan dan rintangan beragam: es, rantai, peti, dan lainnya
• Level penyelamatan khusus: “Selamatkan Bobo”
• Hingga 3 bintang per level, hadiah harian, misi harian, dan acara akhir pekan
• Papan peringkat dunia: seberapa tinggi kamu bisa naik?
• Penguat yang membantu saat level terasa sulit
• Bisa dimainkan tanpa internet; 11 bahasa

Drop Quest gratis dan tanpa pembelian dalam aplikasi. Game ini berisi iklan: banner kecil selama level dan video opsional untuk nyawa, langkah, atau penguat tambahan.

Siap berpetualang? Unduh Drop Quest dan bantu Bobo!
```

## Русский — `ru-RU`
**Titlu** (28/30)
```
Drop Quest: Приключение Бобо
```
**Descriere scurtă** (70/80)
```
Помоги Бобо пройти волшебные земли в сюжетной головоломке «три в ряд»!
```
**Descriere lungă** (1266/4000)
```
Добро пожаловать в Drop Quest — уютное приключение «три в ряд» с Бобо в главной роли!

Меняй соседние фишки местами, собирай 3 и более в ряд и смотри, как они лопаются цепными реакциями. Помоги Бобо — белому пушистому другу с синими ушами-листьями — пройти карту волшебных земель уровень за уровнем.

ЧТО ТЕБЯ ЖДЁТ
• Классическое «три в ряд»: меняй фишки, собирай ряды и запускай цепные реакции
• Особые фишки: 4 и более в ряд создают ракеты, бомбы и радужные фишки
• Мощные комбинации при объединении особых фишек
• Более 100 уровней со сценами и диалогами в стиле комиксов
• Новые земли с новым видом и новыми ловушками по мере продвижения по карте
• Битвы с боссами каждые 10 уровней и Мега-боссы каждые 50
• Разные цели и препятствия: лёд, цепи, ящики и не только
• Особые уровни-спасения: «Спаси Бобо»
• До 3 звёзд за уровень, ежедневная награда, ежедневные задания и события выходных
• Мировой рейтинг: как высоко ты поднимешься?
• Полезные усилители, когда уровень становится трудным
• Можно играть без интернета; 11 языков

Drop Quest бесплатна и не содержит покупок в приложении. В игре есть реклама: небольшой баннер во время уровней и необязательные видео за дополнительные жизни, ходы или усилители.

Готов к приключению? Скачай Drop Quest и помоги Бобо!
```

## Українська — `uk`
**Titlu** (24/30)
```
Drop Quest: Пригода Бобо
```
**Descriere scurtă** (70/80)
```
Допоможи Бобо пройти чарівні землі в сюжетній головоломці «три в ряд»!
```
**Descriere lungă** (1249/4000)
```
Ласкаво просимо до Drop Quest — затишної пригоди «три в ряд» з Бобо в головній ролі!

Міняй сусідні фішки місцями, збирай 3 і більше в ряд та дивись, як вони лопаються ланцюговими реакціями. Допоможи Бобо — білому пухнастому другові з синіми вухами-листочками — пройти мапу чарівних земель рівень за рівнем.

ЩО ТЕБЕ ЧЕКАЄ
• Класичне «три в ряд»: міняй фішки, збирай ряди й запускай ланцюгові реакції
• Особливі фішки: 4 і більше в ряд створюють ракети, бомби та райдужні фішки
• Потужні комбінації, коли поєднуєш особливі фішки
• Понад 100 рівнів зі сценами й діалогами в стилі коміксів
• Нові землі з новим виглядом і новими пастками, поки просуваєшся мапою
• Битви з босами кожні 10 рівнів і Мега-боси кожні 50
• Різні цілі та перешкоди: лід, ланцюги, ящики й не тільки
• Особливі рівні-порятунки: «Врятуй Бобо»
• До 3 зірок за рівень, щоденна нагорода, щоденні завдання й події вихідних
• Світовий рейтинг: як високо ти піднімешся?
• Корисні підсилювачі, коли рівень стає складним
• Можна грати без інтернету; 11 мов

Drop Quest безкоштовна і не містить покупок у застосунку. У грі є реклама: невеликий банер під час рівнів та необов'язкові відео за додаткові життя, ходи чи підсилювачі.

Готовий до пригоди? Завантаж Drop Quest і допоможи Бобо!
```
