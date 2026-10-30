using TollRoadTycoon.Save;

namespace TollRoadTycoon.Tests
{
#if UNITY_5_3_OR_NEWER
    internal sealed class TestSerializer : ISaveSerializer
    {
        public static readonly TestSerializer Instance = new TestSerializer();
        public string Serialize(SaveData data) => UnityEngine.JsonUtility.ToJson(data);
        public SaveData Deserialize(string json) => UnityEngine.JsonUtility.FromJson<SaveData>(json);
    }
#else
    internal sealed class TestSerializer : ISaveSerializer
    {
        public static readonly TestSerializer Instance = new TestSerializer();

        private static readonly System.Text.Json.JsonSerializerOptions Options = new System.Text.Json.JsonSerializerOptions
        {
            IncludeFields = true,
        };

        public string Serialize(SaveData data) => System.Text.Json.JsonSerializer.Serialize(data, Options);
        public SaveData Deserialize(string json) => System.Text.Json.JsonSerializer.Deserialize<SaveData>(json, Options);
    }
#endif
}
