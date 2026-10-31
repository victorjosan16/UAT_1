using System;
using System.IO;
using TollRoadTycoon.Core;
using TollRoadTycoon.Save;
using UnityEngine;

namespace TollRoadTycoon.Presentation
{
    public sealed class JsonUtilitySaveSerializer : ISaveSerializer
    {
        public string Serialize(SaveData data) => JsonUtility.ToJson(data, true);
        public SaveData Deserialize(string json) => JsonUtility.FromJson<SaveData>(json);
    }

    /// <summary>Unity side of saving: file location, logging and error handling.</summary>
    public sealed class SaveService
    {
        private readonly SaveManager _manager;

        public SaveService()
        {
            string path = Path.Combine(Application.persistentDataPath, "tollroadtycoon_save.json");
            _manager = new SaveManager(path, new JsonUtilitySaveSerializer());
        }

        public bool LoadedFromBackup { get; private set; }

        public SaveData Load()
        {
            var data = _manager.Load();
            LoadedFromBackup = _manager.LastLoadSource == LoadSource.Backup;
            if (_manager.LastError != null) Debug.LogWarning("[Save] " + _manager.LastError);
            return data;
        }

        public bool Save(TycoonGame game, SettingsSave settings)
        {
            try
            {
                _manager.Save(game.CaptureSave(DateTime.UtcNow, Application.version, settings));
                return true;
            }
            catch (Exception e)
            {
                Debug.LogError("[Save] Failed: " + e);
                return false;
            }
        }

        public void DeleteAll() => _manager.Delete();
    }
}
