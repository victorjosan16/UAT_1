using UndergroundMetroTycoon.Simulation;
using UnityEngine;

namespace UndergroundMetroTycoon.Game
{
    public sealed class JsonUtilitySaveSerializer : ISaveSerializer
    {
        public string Serialize<T>(T value) => JsonUtility.ToJson(value);
        public T Deserialize<T>(string json) => JsonUtility.FromJson<T>(json);
    }
}
