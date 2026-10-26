using UnityEngine;

namespace UndergroundMetroTycoon.Game
{
    internal static class SimVectorExtensions
    {
        public static Vector3 ToUnity(this System.Numerics.Vector3 v) => new Vector3(v.X, v.Y, v.Z);
        public static System.Numerics.Vector3 ToSim(this Vector3 v) => new System.Numerics.Vector3(v.x, v.y, v.z);
    }
}
