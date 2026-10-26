using System;
using System.IO;
using UndergroundMetroTycoon.Simulation;

namespace UndergroundMetroTycoon.Tests
{
    internal static class TestHelpers
    {
        /// <summary>Uses Unity's JsonUtility inside the Editor and System.Text.Json in the standalone .NET test runner.</summary>
        public static ISaveSerializer CreateSerializer()
        {
#if UNITY_5_3_OR_NEWER
            return new UndergroundMetroTycoon.Game.JsonUtilitySaveSerializer();
#else
            return new SystemTextJsonSaveSerializer();
#endif
        }

        public static string CreateTempDirectory()
        {
            string dir = Path.Combine(Path.GetTempPath(), "umt_tests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            return dir;
        }

        /// <summary>Digs every contract cell of the active contract, top layer first, the way a player would.</summary>
        public static int DigOutActiveContract(GameSession session)
        {
            int swings = 0;
            for (int guard = 0; guard < 100000 && !session.Grid.IsContractComplete; guard++)
            {
                bool progressed = false;
                for (int i = 0; i < session.Grid.CellCount; i++)
                {
                    DigResult r = session.Dig(i, GameSession.PlayerActorId);
                    if (r.Succeeded)
                    {
                        swings++;
                        progressed = true;
                    }
                }
                if (!progressed) break;
            }
            return swings;
        }
    }

#if !UNITY_5_3_OR_NEWER
    internal sealed class SystemTextJsonSaveSerializer : ISaveSerializer
    {
        private static readonly System.Text.Json.JsonSerializerOptions Options = new System.Text.Json.JsonSerializerOptions { IncludeFields = true };

        public string Serialize<T>(T value) => System.Text.Json.JsonSerializer.Serialize(value, Options);

        public T Deserialize<T>(string json) => System.Text.Json.JsonSerializer.Deserialize<T>(json, Options);
    }
#endif
}
