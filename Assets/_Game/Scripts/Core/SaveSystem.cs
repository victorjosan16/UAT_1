using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace QuarryEmpire
{
    /// <summary>
    /// Încarcă și salvează progresul în JSON (Application.persistentDataPath/save.json).
    /// Celelalte sisteme citesc/scriu în Data și apelează MarkDirty(); salvarea pe disc
    /// se face periodic, la ieșirea din aplicație și când aplicația trece în fundal.
    /// </summary>
    [DefaultExecutionOrder(-500)] // trebuie să existe înaintea tuturor celorlalte sisteme
    public class SaveSystem : MonoBehaviour
    {
        public static SaveSystem Instance { get; private set; }

        [SerializeField] private float autoSaveInterval = 10f;

        public SaveData Data { get; private set; }

        /// <summary>Momentul ultimei salvări din sesiunea anterioară (pentru câștigurile offline).</summary>
        public long PreviousSaveUnixSeconds { get; private set; }

        private string FilePath => Path.Combine(Application.persistentDataPath, "save.json");
        private bool _dirty;
        private float _timer;

        private void Awake()
        {
            Instance = this;
            Load();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void Update()
        {
            // unscaledDeltaTime: salvăm și când jocul e pe pauză.
            _timer += Time.unscaledDeltaTime;
            if (_timer < autoSaveInterval) return;
            _timer = 0f;
            if (_dirty) Save();
        }

        private void OnApplicationPause(bool paused)
        {
            if (paused) Save();
        }

        private void OnApplicationQuit() => Save();

        public void MarkDirty() => _dirty = true;

        public void Save()
        {
            if (Data == null) return;
            try
            {
                Data.lastSaveUnixSeconds = DateTimeOffset.UtcNow.ToUnixTimeSeconds();
                File.WriteAllText(FilePath, JsonUtility.ToJson(Data));
                _dirty = false;
            }
            catch (Exception e)
            {
                Debug.LogWarning("Salvarea a eșuat: " + e.Message);
            }
        }

        private void Load()
        {
            try
            {
                if (File.Exists(FilePath)) Data = JsonUtility.FromJson<SaveData>(File.ReadAllText(FilePath));
            }
            catch (Exception e)
            {
                Debug.LogWarning("Salvarea nu a putut fi citită, pornim de la zero: " + e.Message);
            }
            Data ??= new SaveData();
            PreviousSaveUnixSeconds = Data.lastSaveUnixSeconds;
        }

        /// <summary>Șterge progresul și repornește scena.</summary>
        public void ResetProgress()
        {
            try
            {
                if (File.Exists(FilePath)) File.Delete(FilePath);
            }
            catch (Exception e)
            {
                Debug.LogWarning("Nu am putut șterge salvarea: " + e.Message);
            }
            Data = new SaveData();
            _dirty = false;
            enabled = false; // nu mai salvăm nimic din sesiunea veche
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
