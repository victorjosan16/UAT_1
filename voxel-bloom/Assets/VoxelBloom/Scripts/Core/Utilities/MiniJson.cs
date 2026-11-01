using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace VoxelBloom.Core
{
    /// <summary>
    /// Small, allocation-conscious JSON reader/writer with no engine dependency.
    /// Objects map to Dictionary&lt;string, object&gt;, arrays to List&lt;object&gt;,
    /// numbers to double, plus string, bool and null.
    /// </summary>
    public static class MiniJson
    {
        public static object Parse(string json)
        {
            if (json == null) throw new ArgumentNullException(nameof(json));
            var p = new Parser(json);
            p.SkipWs();
            object v = p.ReadValue();
            p.SkipWs();
            if (!p.AtEnd) throw new FormatException("Unexpected trailing characters at " + p.Pos);
            return v;
        }

        public static string Serialize(object value, bool pretty = false)
        {
            var sb = new StringBuilder(256);
            Write(sb, value, pretty, 0);
            return sb.ToString();
        }

        // ---------------------------------------------------------------- typed helpers
        public static Dictionary<string, object> Obj(object o) => o as Dictionary<string, object>;
        public static List<object> Arr(object o) => o as List<object>;

        public static int GetInt(Dictionary<string, object> d, string key, int fallback = 0)
            => d != null && d.TryGetValue(key, out var v) && v is double n ? (int)n : fallback;
        public static long GetLong(Dictionary<string, object> d, string key, long fallback = 0)
            => d != null && d.TryGetValue(key, out var v) && v is double n ? (long)n : fallback;
        public static double GetDouble(Dictionary<string, object> d, string key, double fallback = 0)
            => d != null && d.TryGetValue(key, out var v) && v is double n ? n : fallback;
        public static bool GetBool(Dictionary<string, object> d, string key, bool fallback = false)
            => d != null && d.TryGetValue(key, out var v) && v is bool b ? b : fallback;
        public static string GetString(Dictionary<string, object> d, string key, string fallback = null)
            => d != null && d.TryGetValue(key, out var v) && v is string s ? s : fallback;
        public static Dictionary<string, object> GetObj(Dictionary<string, object> d, string key)
            => d != null && d.TryGetValue(key, out var v) ? v as Dictionary<string, object> : null;
        public static List<object> GetArr(Dictionary<string, object> d, string key)
            => d != null && d.TryGetValue(key, out var v) ? v as List<object> : null;

        public static int[] GetIntArray(Dictionary<string, object> d, string key)
        {
            var a = GetArr(d, key);
            if (a == null) return Array.Empty<int>();
            var r = new int[a.Count];
            for (int i = 0; i < a.Count; i++) r[i] = a[i] is double n ? (int)n : 0;
            return r;
        }

        // ---------------------------------------------------------------- writer
        static void Write(StringBuilder sb, object v, bool pretty, int depth)
        {
            switch (v)
            {
                case null: sb.Append("null"); return;
                case string s: WriteString(sb, s); return;
                case bool b: sb.Append(b ? "true" : "false"); return;
                case double d: sb.Append(d.ToString("R", CultureInfo.InvariantCulture)); return;
                case float f: sb.Append(((double)f).ToString("R", CultureInfo.InvariantCulture)); return;
                case int i: sb.Append(i.ToString(CultureInfo.InvariantCulture)); return;
                case long l: sb.Append(l.ToString(CultureInfo.InvariantCulture)); return;
                case IDictionary<string, object> dict:
                {
                    sb.Append('{');
                    bool first = true;
                    foreach (var kv in dict)
                    {
                        if (!first) sb.Append(',');
                        first = false;
                        if (pretty) { sb.Append('\n').Append(' ', (depth + 1) * 2); }
                        WriteString(sb, kv.Key);
                        sb.Append(pretty ? ": " : ":");
                        Write(sb, kv.Value, pretty, depth + 1);
                    }
                    if (pretty && dict.Count > 0) { sb.Append('\n').Append(' ', depth * 2); }
                    sb.Append('}');
                    return;
                }
                case System.Collections.IEnumerable list:
                {
                    sb.Append('[');
                    bool first = true;
                    foreach (var item in list)
                    {
                        if (!first) sb.Append(',');
                        first = false;
                        Write(sb, item, pretty, depth + 1);
                    }
                    sb.Append(']');
                    return;
                }
                default:
                    WriteString(sb, Convert.ToString(v, CultureInfo.InvariantCulture));
                    return;
            }
        }

        static void WriteString(StringBuilder sb, string s)
        {
            sb.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }

        // ---------------------------------------------------------------- parser
        sealed class Parser
        {
            readonly string s;
            public int Pos;
            public Parser(string json) { s = json; }
            public bool AtEnd => Pos >= s.Length;

            public void SkipWs()
            {
                while (Pos < s.Length && char.IsWhiteSpace(s[Pos])) Pos++;
            }

            public object ReadValue()
            {
                if (AtEnd) throw new FormatException("Unexpected end of JSON");
                char c = s[Pos];
                switch (c)
                {
                    case '{': return ReadObject();
                    case '[': return ReadArray();
                    case '"': return ReadString();
                    case 't': Expect("true"); return true;
                    case 'f': Expect("false"); return false;
                    case 'n': Expect("null"); return null;
                    default:
                        if (c == '-' || (c >= '0' && c <= '9')) return ReadNumber();
                        throw new FormatException("Unexpected character '" + c + "' at " + Pos);
                }
            }

            void Expect(string word)
            {
                if (string.CompareOrdinal(s, Pos, word, 0, word.Length) != 0) throw new FormatException("Expected " + word + " at " + Pos);
                Pos += word.Length;
            }

            Dictionary<string, object> ReadObject()
            {
                var d = new Dictionary<string, object>();
                Pos++; // {
                SkipWs();
                if (!AtEnd && s[Pos] == '}') { Pos++; return d; }
                while (true)
                {
                    SkipWs();
                    if (AtEnd || s[Pos] != '"') throw new FormatException("Expected key at " + Pos);
                    string key = ReadString();
                    SkipWs();
                    if (AtEnd || s[Pos] != ':') throw new FormatException("Expected ':' at " + Pos);
                    Pos++;
                    SkipWs();
                    d[key] = ReadValue();
                    SkipWs();
                    if (AtEnd) throw new FormatException("Unterminated object");
                    if (s[Pos] == ',') { Pos++; continue; }
                    if (s[Pos] == '}') { Pos++; return d; }
                    throw new FormatException("Expected ',' or '}' at " + Pos);
                }
            }

            List<object> ReadArray()
            {
                var l = new List<object>();
                Pos++; // [
                SkipWs();
                if (!AtEnd && s[Pos] == ']') { Pos++; return l; }
                while (true)
                {
                    SkipWs();
                    l.Add(ReadValue());
                    SkipWs();
                    if (AtEnd) throw new FormatException("Unterminated array");
                    if (s[Pos] == ',') { Pos++; continue; }
                    if (s[Pos] == ']') { Pos++; return l; }
                    throw new FormatException("Expected ',' or ']' at " + Pos);
                }
            }

            string ReadString()
            {
                Pos++; // opening quote
                var sb = new StringBuilder();
                while (true)
                {
                    if (AtEnd) throw new FormatException("Unterminated string");
                    char c = s[Pos++];
                    if (c == '"') return sb.ToString();
                    if (c != '\\') { sb.Append(c); continue; }
                    if (AtEnd) throw new FormatException("Bad escape");
                    char e = s[Pos++];
                    switch (e)
                    {
                        case '"': sb.Append('"'); break;
                        case '\\': sb.Append('\\'); break;
                        case '/': sb.Append('/'); break;
                        case 'b': sb.Append('\b'); break;
                        case 'f': sb.Append('\f'); break;
                        case 'n': sb.Append('\n'); break;
                        case 'r': sb.Append('\r'); break;
                        case 't': sb.Append('\t'); break;
                        case 'u':
                            if (Pos + 4 > s.Length) throw new FormatException("Bad unicode escape");
                            sb.Append((char)Convert.ToInt32(s.Substring(Pos, 4), 16));
                            Pos += 4;
                            break;
                        default: throw new FormatException("Bad escape \\" + e);
                    }
                }
            }

            double ReadNumber()
            {
                int start = Pos;
                if (s[Pos] == '-') Pos++;
                while (Pos < s.Length)
                {
                    char c = s[Pos];
                    if ((c >= '0' && c <= '9') || c == '.' || c == 'e' || c == 'E' || c == '+' || c == '-') Pos++;
                    else break;
                }
                return double.Parse(s.Substring(start, Pos - start), NumberStyles.Float, CultureInfo.InvariantCulture);
            }
        }
    }
}
