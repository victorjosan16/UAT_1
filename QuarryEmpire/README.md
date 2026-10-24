# Quarry Empire (Unity 6 LTS, URP, Android portret)

Acest folder este un proiect Unity complet (`Assets/`, `Packages/`, `ProjectSettings/`).

## Generare automată a conținutului
`Assets/_Game/Editor/Stage1Setup.cs` creează layerele, URP, setările Android, materialele,
ScriptableObjects, prefab-urile și scena `Game`.
- În Editor: meniul **Quarry Empire → Setup Etapa 1**.
- În Unity Build Automation: **Pre-Export Method = `QuarryCloudBuild.PreExport`**.

## Build în cloud (fără calculator) — Unity Build Automation
1. cloud.unity.com → autentificare cu Unity ID → creează proiectul „Quarry Empire”.
2. DevOps → Build Automation → configurează (free tier: minute gratuite pe lună).
3. Source control: GitHub → repo `victorjosan16/UAT_1`.
4. Build configuration: Platform **Android**, Branch `ccr-1d10ca5c-f6qz54`,
   Project subfolder **`QuarryEmpire`**, Unity version: cel mai nou **Unity 6 (6000.0) LTS**,
   Pre-Export Method **`QuarryCloudBuild.PreExport`**, output **APK** (nu App Bundle).
5. Start build → descarcă APK-ul pe telefon → instalează.

Etape: 1 ✅ Mișcare + minat + drop · 2 Stivă + Sell + bani · 3 Upgrade-uri · 4 Utilaje ·
5 Rafinărie + comenzi · 6 Straturi + generare procedurală · 7 NPC · 8 Salvare/efecte/meniu ·
9 Monetizare + Play Store
