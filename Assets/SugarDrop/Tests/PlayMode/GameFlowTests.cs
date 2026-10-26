using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

namespace SugarDrop.Tests
{
    /// <summary>
    /// Test de fum pe scena reală: meniu → hartă → nivel → pauză → hartă, cu apăsări rapide repetate.
    /// Orice excepție sau eroare în consolă pică testul. Progresul jucătorului e salvat înainte și pus la loc după.
    /// Scena se generează cu meniul „Sugar Drop → Construiește jocul”.
    /// </summary>
    public class GameFlowTests
    {
        private const string SaveKey = "sugardrop_save";
        private string _backup;

        [SetUp]
        public void SetUp()
        {
            _backup = PlayerPrefs.GetString(SaveKey, null);
            PlayerPrefs.DeleteKey(SaveKey);
            SugarSave.Load();
            // Jucător care și-a ales deja nickname-ul (fereastra de bun venit are testul ei).
            SugarSave.Data.nickname = "Tester";
            SugarSave.Save();
            Time.timeScale = 1f;
        }

        [TearDown]
        public void TearDown()
        {
            if (_backup != null) PlayerPrefs.SetString(SaveKey, _backup);
            else PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.Save();
            SugarSave.Load();
            Time.timeScale = 1f;
        }

        [UnityTest]
        public IEnumerator MenuToMapToLevel_AndBack()
        {
            if (!Application.CanStreamedLevelBeLoaded("SugarDrop"))
                Assert.Ignore("Scena SugarDrop nu e în Build Settings: rulează „Sugar Drop → Construiește jocul”.");
            SceneManager.LoadScene("SugarDrop");
            yield return Frames(10);

            // Meniu → hartă (de două ori rapid: tranziția trebuie să ignore a doua apăsare).
            Click("Canvas/Menu/Background/Play");
            Click("Canvas/Menu/Background/Play");
            yield return Seconds(1f);
            Assert.IsNotNull(GameObject.Find("Canvas/SafeArea/Map/Background"), "Harta nu s-a deschis");
            Assert.IsNull(GameObject.Find("Canvas/Menu/Background"), "Meniul a rămas deschis");

            // Hartă → nivelul 1 (apăsări rapide).
            Click("Canvas/SafeArea/Map/Background/Content/Play");
            Click("Canvas/SafeArea/Map/Background/Content/Play");
            yield return Seconds(1.2f);
            GameObject board = GameObject.Find("Game/Board");
            Assert.IsNotNull(board);
            int tiles = 0;
            foreach (Transform child in board.transform)
                if (child.name.StartsWith("Tile_")) tiles++;
            Assert.AreEqual(LevelConfig.For(1).ValidCells, tiles, "Fiecare celulă a tablei trebuie să aibă o piesă");
            Assert.IsNull(GameObject.Find("Canvas/SafeArea/Map/Background"), "Harta a rămas deschisă");
            Assert.AreEqual(SugarSave.MaxLives, SugarSave.Data.lives, "Pornirea nivelului nu consumă vieți");

            // Pauză: jocul se oprește; „Înapoi la hartă” costă o viață.
            Click("Canvas/SafeArea/Hud/Pause");
            yield return Frames(2);
            Assert.AreEqual(0f, Time.timeScale);
            Click("Canvas/Dialog/Dim/BoxHolder/Box/Button2");
            yield return Seconds(1f);
            Assert.AreEqual(1f, Time.timeScale);
            Assert.IsNotNull(GameObject.Find("Canvas/SafeArea/Map/Background"), "Nu s-a revenit la hartă");
            Assert.AreEqual(SugarSave.MaxLives - 1, SugarSave.Data.lives);
            Assert.AreEqual(1, SugarSave.Data.level, "Progresul nu trebuie să se schimbe");
        }

        [UnityTest]
        public IEnumerator LockedLevel_DoesNotStart()
        {
            if (!Application.CanStreamedLevelBeLoaded("SugarDrop"))
                Assert.Ignore("Scena SugarDrop nu e în Build Settings.");
            SceneManager.LoadScene("SugarDrop");
            yield return Frames(10);
            Click("Canvas/Menu/Background/Play");
            yield return Seconds(1f);

            Click("Canvas/SafeArea/Map/Background/Content/Scroll/Path/Nodes/Level5");
            yield return Seconds(1f);
            Assert.IsNotNull(GameObject.Find("Canvas/SafeArea/Map/Background"), "Un nivel încuiat nu trebuie să pornească");
        }

        [UnityTest]
        public IEnumerator FirstLaunch_GreetsAndSavesNickname()
        {
            if (!Application.CanStreamedLevelBeLoaded("SugarDrop"))
                Assert.Ignore("Scena SugarDrop nu e în Build Settings.");
            SugarSave.Data.nickname = "";
            SugarSave.Save();
            SceneManager.LoadScene("SugarDrop");
            yield return Frames(10);

            Assert.IsNotNull(GameObject.Find("Canvas/Welcome/Dim"), "La prima pornire trebuie să apară fereastra de bun venit");
            GameObject.Find("Canvas/Welcome/Dim/BoxHolder/Box/NameFrame/NameField").GetComponent<InputField>().text = "  Ana  Maria!! ";
            Click("Canvas/Welcome/Dim/BoxHolder/Box/Confirm");
            yield return Frames(2);
            Assert.IsNull(GameObject.Find("Canvas/Welcome/Dim"), "Fereastra trebuie să se închidă");
            Assert.AreEqual("Ana Maria", SugarSave.Data.nickname);
        }

        [UnityTest]
        public IEnumerator Leaderboard_OpensFromMap()
        {
            if (!Application.CanStreamedLevelBeLoaded("SugarDrop"))
                Assert.Ignore("Scena SugarDrop nu e în Build Settings.");
            SceneManager.LoadScene("SugarDrop");
            yield return Frames(10);
            Click("Canvas/Menu/Background/Play");
            yield return Seconds(1f);

            Click("Canvas/SafeArea/Map/Background/Content/TopBar/Leaderboard");
            yield return Seconds(0.5f);
            Assert.IsNotNull(GameObject.Find("Canvas/Leaderboard/Dim"), "Clasamentul nu s-a deschis");
            Click("Canvas/Leaderboard/Dim/BoxHolder/Box/Close");
            yield return Frames(2);
            Assert.IsNull(GameObject.Find("Canvas/Leaderboard/Dim"));
        }

        private static void Click(string path)
        {
            GameObject go = GameObject.Find(path);
            Assert.IsNotNull(go, "Lipsește: " + path);
            Button button = go.GetComponent<Button>();
            Assert.IsNotNull(button, "Nu e buton: " + path);
            button.onClick.Invoke();
        }

        private static IEnumerator Frames(int count)
        {
            for (int i = 0; i < count; i++) yield return null;
        }

        private static IEnumerator Seconds(float seconds)
        {
            float end = Time.realtimeSinceStartup + seconds;
            while (Time.realtimeSinceStartup < end) yield return null;
        }
    }
}
