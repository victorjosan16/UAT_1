# Reclame reale în Drop Quest (Unity LevelPlay)

Jocul are două feluri de reclame:
- **rewarded** (opționale, cu recompensă): +1 viață, +5 mutări la înfrângere, ajutor gratuit;
- **banner** (bara de jos, 320×50) pe hartă și în timpul nivelului; nu apare în meniu și nici peste povești.
  Când bannerul e afișat, ajutoarele și bara de jos urcă deasupra lui (tabla nu se mută).
Codul e gata (`Scripts/Ads/SugarAds.cs`). Până faci pașii de mai jos, jocul arată **reclama de test**
(ecran cu numărătoare inversă). În Editor se arată mereu reclama de test.

## 1. Contul LevelPlay (o singură dată)
1. Intră pe **https://unity.com** cu contul tău Unity → **Grow** → **LevelPlay** (sau cloud.unity.com → LevelPlay).
2. **Add app** → platforma **Android** → nu e încă în magazin: alege „not live” și scrie Bundle ID-ul
   **`com.victorjosan.sugardrop`**, numele **Drop Quest**.
3. Notează **App Key**-ul aplicației (un șir de litere/cifre).
4. **Ad units** → **Create ad unit** → tip **Rewarded** → notează **Ad Unit ID**-ul.
   Încă o dată: **Create ad unit** → tip **Banner** → notează și acest **Ad Unit ID**.
4b. **Networks / Setup**: activează cel puțin **Unity Ads** (vine cu LevelPlay). Alte rețele (AdMob etc.) se pot adăuga
   mai târziu, din același ecran.
5. **Test devices**: adaugă telefonul tău (Advertising ID din Setări → Google → Reclame), ca să vezi reclame de test
   și să nu-ți blochezi contul apăsând pe reclame reale.

## 2. Pachetul în Unity
1. Deschide proiectul în Unity → **Window → Package Manager**.
2. Sus-stânga: **Unity Registry** → caută **„Ads Mediation”** (pachetul `com.unity.services.levelplay`,
   versiunea 8.7 sau mai nouă) → **Install**. Unity poate instala și **Mobile Dependency Resolver**; acceptă.
3. După instalare, în **Console** apare:
   `[SugarDrop] LevelPlay instalat: reclamele reale sunt active…`
   Dacă nu apare, meniul **Sugar Drop → Reclame: verifică LevelPlay**.
4. Dacă Unity întreabă de **Android Resolver** / dependențe, alege **Enable** / **Resolve**
   (sau meniul **Assets → Mobile Dependency Resolver → Android Resolver → Force Resolve**).

## 3. Cheile
Deschide **`Assets/SugarDrop/Scripts/Ads/SugarAdsConfig.cs`** și completează:
```csharp
public const string AndroidAppKey = "APP_KEY_DIN_LEVELPLAY";
public const string AndroidRewardedAdUnitId = "AD_UNIT_ID_REWARDED";
public const string AndroidBannerAdUnitId = "AD_UNIT_ID_BANNER";   // gol = fără banner
```

## 4. Build-ul din cloud
Build-ul din Unity Build Automation ia proiectul din GitHub, deci schimbările trebuie să ajungă acolo:
- fișierul **`Packages/manifest.json`** (apare linia `"com.unity.services.levelplay": "…"` după instalare);
- fișierul **`SugarAdsConfig.cs`** cu cheile.

Cel mai simplu: spune-i asistentului **versiunea pachetului** (din Package Manager) și cele două chei, iar el le pune
în proiect și le urcă pe GitHub. Apoi dai build ca de obicei (Pre-Export `SugarDropCloudBuild.PreExport` activează
singur codul LevelPlay).

## 5. Cum verifici că merg
- Pe telefonul adăugat la **Test devices** trebuie să apară reclame marcate „Test”.
- În Unity Editor și în build-urile **Development** apare o bară gri „BANNER DE TEST (320×50)” în locul bannerului, ca să vezi
  așezarea; în build-ul final (fără „Development Build”) bara de test nu apare.
- Dacă bannerul nu apare pe telefon: în Logcat caută `[Ads]`; de obicei lipsește Ad Unit ID-ul de banner sau rețeaua nu are încă reclame.

## 6. Ce vede jucătorul
- La prima pornire: fereastra **„Reclame”** — acceptă sau nu reclame personalizate (GDPR). Se poate schimba din
  **Setări → RECLAME PERSONALIZATE**.
- La „+1 VIAȚĂ (reclamă)”, „+5 MUTĂRI (reclamă)”, „GRATIS (reclamă)”: reclama reală; recompensa vine doar dacă e văzută
  până la capăt. Fără internet / fără reclamă disponibilă: mesajul „Nicio reclamă acum”.

## 7. Google Play (când publici)
- Fișa aplicației: bifează **„Conține reclame”**.
- **Data safety**: declară ID-ul de publicitate și datele despre dispozitiv folosite de rețeaua de reclame.
- **Politica de confidențialitate**: un link public. Ciorna e în `POLITICA_CONFIDENTIALITATE.md`. Completezi
  numele și emailul, apoi o publici, de exemplu pe Google Sites.
- Recomandat: fișierul **app-ads.txt** pe site-ul tău de dezvoltator (LevelPlay îți dă conținutul).
- Dacă adaugi mai târziu **AdMob** prin mediere, Google cere fereastra lor de consimțământ (UMP).
