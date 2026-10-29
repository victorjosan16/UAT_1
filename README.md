# Drop Quest – Aventurile lui Bobo

Joc mobil de puzzle „potrivește 3” pentru Android (Unity 6, 2D, portret), cu povestea lui Bobo, un mic aventurier
care adună bucățile Cristalului Izvorului prin zece ținuturi. Peste 100 de niveluri, boși, mecanici noi pe fiecare
ținut, harta aventurii, roată, cadouri, misiuni, clasament online și 11 limbi.

Ramura de lucru: `sugar-drop` (build în Unity Build Automation: Pre-Export `SugarDropCloudBuild.PreExport`,
Bundle ID `com.victorjosan.sugardrop`).

## Structură
```
Assets/SugarDrop/
  Scripts/Core     reguli pure (tablă, niveluri, obiective, scor, boși, salvare, traduceri), fără Unity
  Scripts/Board    animarea tablei, piese, câmpul de joc
  Scripts/Game     orchestratorul jocului (SugarDropGame)
  Scripts/UI       ecrane: meniu, hartă, HUD, pauză, victorie, povești, setări, evenimente
  Scripts/Visual   desenul pieselor, Bobo, fundaluri, pericolul de sub tablă
  Scripts/Effects  particule și sunet procedural
  Scripts/Online   clasament, salvare în cloud, notificări
  Scripts/Ads      reclame (LevelPlay) și configurația lor
  Editor/          construiește scena, tema și setările Android la fiecare build
  Resources/Art/   imagini înlocuibile (piese, Bobo, scene de poveste, logo)
  Tests/           teste EditMode și PlayMode
Assets/_Game/      interfața comună (UiFactory, UiTheme, ...) și reclama de test
Tools/CoreTests/   testele de logică, rulate fără Unity: `dotnet test Tools/CoreTests`
docs/              audituri, lista de grafică necesară, design system
```

## Documentație
- `Assets/SugarDrop/README.md` – regulile jocului și unde se reglează fiecare lucru
- `Assets/SugarDrop/RECLAME.md` – reclame reale (rewarded + banner) cu Unity LevelPlay
- `Assets/SugarDrop/CLASAMENT.md` – clasamentul online (Unity Gaming Services)
- `Assets/SugarDrop/GOOGLE_PLAY.md` – publicarea pe Google Play și textele de magazin
- `Assets/SugarDrop/POLITICA_CONFIDENTIALITATE.md` – ciorna politicii de confidențialitate
- `docs/redesign/` – design system și lista imaginilor necesare (`ART_LIST.md`)
- `docs/audit/`, `docs/visual/` – auditul tehnic și cel vizual
