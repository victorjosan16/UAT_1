// Înlocuitori minimi pentru UnityEngine, ca logica din Core să se poată compila și testa în afara Unity.
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;

namespace UnityEngine
{
    public struct Vector2Int : IEquatable<Vector2Int>
    {
        public int x, y;
        public Vector2Int(int x, int y) { this.x = x; this.y = y; }
        public static Vector2Int zero => new(0, 0);
        public static Vector2Int one => new(1, 1);
        public static Vector2Int up => new(0, 1);
        public static Vector2Int down => new(0, -1);
        public static Vector2Int left => new(-1, 0);
        public static Vector2Int right => new(1, 0);
        public static Vector2Int operator +(Vector2Int a, Vector2Int b) => new(a.x + b.x, a.y + b.y);
        public static Vector2Int operator -(Vector2Int a, Vector2Int b) => new(a.x - b.x, a.y - b.y);
        public static Vector2Int operator -(Vector2Int a) => new(-a.x, -a.y);
        public static Vector2Int operator *(Vector2Int a, int k) => new(a.x * k, a.y * k);
        public static bool operator ==(Vector2Int a, Vector2Int b) => a.x == b.x && a.y == b.y;
        public static bool operator !=(Vector2Int a, Vector2Int b) => !(a == b);
        public static implicit operator Vector2(Vector2Int v) => new(v.x, v.y);
        public bool Equals(Vector2Int o) => this == o;
        public override bool Equals(object o) => o is Vector2Int v && this == v;
        public override int GetHashCode() => x * 73856093 ^ y * 19349663;
        public override string ToString() => $"({x}, {y})";
    }

    public struct Vector2
    {
        public float x, y;
        public Vector2(float x, float y) { this.x = x; this.y = y; }
        public static Vector2 zero => new(0, 0);
        public static Vector2 one => new(1, 1);
        public float magnitude => MathF.Sqrt(x * x + y * y);
        public Vector2 normalized { get { float m = magnitude; return m > 1e-6f ? new Vector2(x / m, y / m) : zero; } }
        public static Vector2 operator +(Vector2 a, Vector2 b) => new(a.x + b.x, a.y + b.y);
        public static Vector2 operator -(Vector2 a, Vector2 b) => new(a.x - b.x, a.y - b.y);
        public static Vector2 operator -(Vector2 a) => new(-a.x, -a.y);
        public static Vector2 operator *(Vector2 a, float k) => new(a.x * k, a.y * k);
        public static Vector2 operator *(float k, Vector2 a) => new(a.x * k, a.y * k);
        public static Vector2 operator /(Vector2 a, float k) => new(a.x / k, a.y / k);
        public static float Distance(Vector2 a, Vector2 b) => (a - b).magnitude;
        public static float Dot(Vector2 a, Vector2 b) => a.x * b.x + a.y * b.y;
        public static Vector2 Lerp(Vector2 a, Vector2 b, float t) => a + (b - a) * Mathf.Clamp01(t);
    }

    public struct Vector3
    {
        public float x, y, z;
        public Vector3(float x, float y, float z) { this.x = x; this.y = y; this.z = z; }
        public static Vector3 zero => new(0, 0, 0);
    }

    public static class Mathf
    {
        public const float PI = MathF.PI;
        public const float Deg2Rad = MathF.PI / 180f;
        public const float Rad2Deg = 180f / MathF.PI;
        public static int Min(int a, int b) => Math.Min(a, b);
        public static float Min(float a, float b) => Math.Min(a, b);
        public static int Min(params int[] v) { int m = v[0]; foreach (int x in v) m = Math.Min(m, x); return m; }
        public static int Max(int a, int b) => Math.Max(a, b);
        public static float Max(float a, float b) => Math.Max(a, b);
        public static int Max(params int[] v) { int m = v[0]; foreach (int x in v) m = Math.Max(m, x); return m; }
        public static int Clamp(int v, int min, int max) => v < min ? min : v > max ? max : v;
        public static float Clamp(float v, float min, float max) => v < min ? min : v > max ? max : v;
        public static float Clamp01(float v) => Clamp(v, 0f, 1f);
        public static int Abs(int v) => Math.Abs(v);
        public static float Abs(float v) => Math.Abs(v);
        public static int RoundToInt(float v) => (int)Math.Round(v, MidpointRounding.ToEven);
        public static int FloorToInt(float v) => (int)Math.Floor(v);
        public static int CeilToInt(float v) => (int)Math.Ceiling(v);
        public static float Sqrt(float v) => MathF.Sqrt(v);
        public static float Sin(float v) => MathF.Sin(v);
        public static float Cos(float v) => MathF.Cos(v);
        public static float Atan2(float y, float x) => MathF.Atan2(y, x);
        public static float Pow(float a, float b) => MathF.Pow(a, b);
        public static float Lerp(float a, float b, float t) => a + (b - a) * Clamp01(t);
        public static float Repeat(float t, float length) => Clamp(t - MathF.Floor(t / length) * length, 0f, length);
        public static float MoveTowards(float a, float b, float d) => Math.Abs(b - a) <= d ? b : a + Math.Sign(b - a) * d;
    }

    public struct Color
    {
        public float r, g, b, a;
        public Color(float r, float g, float b, float a = 1f) { this.r = r; this.g = g; this.b = b; this.a = a; }
        public static Color white => new(1, 1, 1);
        public static Color black => new(0, 0, 0);
        public static Color clear => new(0, 0, 0, 0);
        public static Color magenta => new(1, 0, 1);
        public static Color Lerp(Color x, Color y, float t)
        {
            t = Mathf.Clamp01(t);
            return new Color(x.r + (y.r - x.r) * t, x.g + (y.g - x.g) * t, x.b + (y.b - x.b) * t, x.a + (y.a - x.a) * t);
        }
        public static Color operator *(Color c, float k) => new(c.r * k, c.g * k, c.b * k, c.a * k);
        public static bool operator ==(Color x, Color y) => x.r == y.r && x.g == y.g && x.b == y.b && x.a == y.a;
        public static bool operator !=(Color x, Color y) => !(x == y);
        public override bool Equals(object o) => o is Color c && c == this;
        public override int GetHashCode() => HashCode.Combine(r, g, b, a);
        public static Color HSVToRGB(float h, float s, float v) => new(v, v, v);
    }

    public static class ColorUtility
    {
        public static bool TryParseHtmlString(string hex, out Color color)
        {
            color = Color.magenta;
            if (string.IsNullOrEmpty(hex) || hex[0] != '#' || hex.Length != 7) return false;
            int v = int.Parse(hex.Substring(1), NumberStyles.HexNumber);
            color = new Color(((v >> 16) & 255) / 255f, ((v >> 8) & 255) / 255f, (v & 255) / 255f);
            return true;
        }
    }

    public enum SystemLanguage
    {
        Afrikaans, Arabic, Basque, Belarusian, Bulgarian, Catalan, Chinese, Czech, Danish, Dutch, English, Estonian, Faroese,
        Finnish, French, German, Greek, Hebrew, Hungarian, Icelandic, Indonesian, Italian, Japanese, Korean, Latvian, Lithuanian,
        Norwegian, Polish, Portuguese, Romanian, Russian, SerboCroatian, Slovak, Slovenian, Spanish, Swedish, Thai, Turkish,
        Ukrainian, Vietnamese, ChineseSimplified, ChineseTraditional, Unknown,
    }

    public static class Application
    {
        public static int targetFrameRate;
        public static SystemLanguage systemLanguage = SystemLanguage.English;
        public static string cloudProjectId = "";
    }

    public static class SystemInfo { public static int systemMemorySize = 4096; }

    public static class Debug
    {
        public static void Log(object m) { }
        public static void LogWarning(object m) { }
        public static void LogError(object m) => Console.Error.WriteLine(m);
    }

    public static class Random
    {
        private static readonly System.Random R = new(1);
        public static float value => (float)R.NextDouble();
        public static int Range(int min, int max) => R.Next(min, max);
        public static float Range(float min, float max) => min + (float)R.NextDouble() * (max - min);
    }

    public static class PlayerPrefs
    {
        private static readonly Dictionary<string, string> Values = new();
        public static string GetString(string key, string fallback) => Values.TryGetValue(key, out string v) ? v : fallback;
        public static void SetString(string key, string value) => Values[key] = value;
        public static void DeleteKey(string key) => Values.Remove(key);
        public static void Save() { }
    }

    public static class JsonUtility
    {
        private static readonly JsonSerializerOptions Options = new() { IncludeFields = true };
        public static string ToJson(object o) => JsonSerializer.Serialize(o, o.GetType(), Options);
        public static T FromJson<T>(string json) => JsonSerializer.Deserialize<T>(json, Options);
    }

    public enum RuntimeInitializeLoadType { AfterSceneLoad, BeforeSceneLoad, SubsystemRegistration }

    [AttributeUsage(AttributeTargets.Method)]
    public class RuntimeInitializeOnLoadMethodAttribute : Attribute
    {
        public RuntimeInitializeOnLoadMethodAttribute() { }
        public RuntimeInitializeOnLoadMethodAttribute(RuntimeInitializeLoadType type) { }
    }

    [AttributeUsage(AttributeTargets.Field)] public class SerializeField : Attribute { }
    [AttributeUsage(AttributeTargets.Field)] public class HeaderAttribute : Attribute { public HeaderAttribute(string h) { } }
    [AttributeUsage(AttributeTargets.Field)] public class TooltipAttribute : Attribute { public TooltipAttribute(string t) { } }
}
