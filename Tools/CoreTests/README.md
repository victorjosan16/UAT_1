# Testele logicii Sugar Drop fără Unity

Compilează `Assets/SugarDrop/Scripts/Core` și testele EditMode cu .NET 8. Clasele Unity folosite de logică
(Vector2Int, Mathf, Color etc.) sunt înlocuite de `UnityStubs.cs`.

```
dotnet test Tools/CoreTests
```

Testele PlayMode (scena reală) rulează doar în Unity (Test Runner).
