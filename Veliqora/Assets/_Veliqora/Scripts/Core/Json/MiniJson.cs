using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Veliqora.Core.Json
{
    /// <summary>
    /// Minimal, dependency-free JSON reader/writer used by the pure core (save files, level catalog, configs).
    /// Values map to: Dictionary&lt;string, object&gt;, List&lt;object&gt;, string, double, long, bool, null.
    /// Integers without fraction/exponent are parsed as long; everything else as double.
    /// </summary>
    public static class MiniJson
    {
        public static object Parse(string json)
        {
            if (json == null) throw new ArgumentNullException(nameof(json));
            var parser = new Parser(json);
            object value = parser.ParseValue();
            parser.SkipWhitespace();
            if (!parser.AtEnd) throw new FormatException("Unexpected trailing characters at " + parser.Position);
            return value;
        }

        public static string Serialize(object value, bool pretty = false)
        {
            var sb = new StringBuilder(256);
            Write(sb, value, pretty, 0);
            return sb.ToString();
        }

        // ---------------- typed accessors ----------------

        public static Dictionary<string, object> AsObject(object o)
        {
            return o as Dictionary<string, object> ?? throw new FormatException("Expected JSON object");
        }

        public static List<object> AsList(object o)
        {
            return o as List<object> ?? throw new FormatException("Expected JSON array");
        }

        public static bool TryGet(Dictionary<string, object> d, string key, out object value)
        {
            return d.TryGetValue(key, out value) && value != null;
        }

        public static long GetLong(Dictionary<string, object> d, string key, long fallback = 0)
        {
            if (!TryGet(d, key, out var v)) return fallback;
            return ToLong(v);
        }

        public static int GetInt(Dictionary<string, object> d, string key, int fallback = 0)
        {
            if (!TryGet(d, key, out var v)) return fallback;
            return checked((int)ToLong(v));
        }

        public static double GetDouble(Dictionary<string, object> d, string key, double fallback = 0)
        {
            if (!TryGet(d, key, out var v)) return fallback;
            if (v is double dd) return dd;
            if (v is long l) return l;
            throw new FormatException("Expected number for '" + key + "'");
        }

        public static bool GetBool(Dictionary<string, object> d, string key, bool fallback = false)
        {
            if (!TryGet(d, key, out var v)) return fallback;
            if (v is bool b) return b;
            throw new FormatException("Expected bool for '" + key + "'");
        }

        public static string GetString(Dictionary<string, object> d, string key, string fallback = null)
        {
            if (!TryGet(d, key, out var v)) return fallback;
            return v as string ?? throw new FormatException("Expected string for '" + key + "'");
        }

        public static List<object> GetList(Dictionary<string, object> d, string key)
        {
            if (!TryGet(d, key, out var v)) return new List<object>();
            return AsList(v);
        }

        public static Dictionary<string, object> GetObject(Dictionary<string, object> d, string key)
        {
            if (!TryGet(d, key, out var v)) return new Dictionary<string, object>();
            return AsObject(v);
        }

        public static long ToLong(object v)
        {
            if (v is long l) return l;
            if (v is double d)
            {
                if (Math.Abs(d - Math.Round(d)) > 1e-9) throw new FormatException("Expected integer, got " + d);
                return (long)Math.Round(d);
            }
            if (v is int i) return i;
            throw new FormatException("Expected integer");
        }

        public static int[] ToIntArray(object v)
        {
            var list = AsList(v);
            var result = new int[list.Count];
            for (int i = 0; i < list.Count; i++) result[i] = checked((int)ToLong(list[i]));
            return result;
        }

        public static List<object> FromIntArray(int[] values)
        {
            var list = new List<object>(values.Length);
            for (int i = 0; i < values.Length; i++) list.Add((long)values[i]);
            return list;
        }

        // ---------------- writer ----------------

        private static void Write(StringBuilder sb, object value, bool pretty, int indent)
        {
            switch (value)
            {
                case null: sb.Append("null"); return;
                case string s: WriteString(sb, s); return;
                case bool b: sb.Append(b ? "true" : "false"); return;
                case int i: sb.Append(i.ToString(CultureInfo.InvariantCulture)); return;
                case long l: sb.Append(l.ToString(CultureInfo.InvariantCulture)); return;
                case ulong ul: sb.Append(ul.ToString(CultureInfo.InvariantCulture)); return;
                case float f: WriteDouble(sb, f); return;
                case double d: WriteDouble(sb, d); return;
                case Dictionary<string, object> dict: WriteObject(sb, dict, pretty, indent); return;
                case IList<object> list: WriteArray(sb, list, pretty, indent); return;
                case int[] ints: WriteArray(sb, FromIntArray(ints), pretty, indent); return;
                default: throw new NotSupportedException("Cannot serialize " + value.GetType());
            }
        }

        private static void WriteDouble(StringBuilder sb, double d)
        {
            if (double.IsNaN(d) || double.IsInfinity(d)) { sb.Append("0"); return; }
            sb.Append(d.ToString("R", CultureInfo.InvariantCulture));
        }

        private static void WriteObject(StringBuilder sb, Dictionary<string, object> dict, bool pretty, int indent)
        {
            sb.Append('{');
            bool first = true;
            foreach (var kv in dict)
            {
                if (!first) sb.Append(',');
                first = false;
                if (pretty) { sb.Append('\n'); sb.Append(' ', (indent + 1) * 2); }
                WriteString(sb, kv.Key);
                sb.Append(pretty ? ": " : ":");
                Write(sb, kv.Value, pretty, indent + 1);
            }
            if (pretty && !first) { sb.Append('\n'); sb.Append(' ', indent * 2); }
            sb.Append('}');
        }

        private static void WriteArray(StringBuilder sb, IList<object> list, bool pretty, int indent)
        {
            sb.Append('[');
            // Arrays of scalars stay on one line for readability of level files.
            bool scalarOnly = true;
            for (int i = 0; i < list.Count; i++)
            {
                if (list[i] is Dictionary<string, object> || list[i] is IList<object>) { scalarOnly = false; break; }
            }
            for (int i = 0; i < list.Count; i++)
            {
                if (i > 0) sb.Append(',');
                if (pretty && !scalarOnly) { sb.Append('\n'); sb.Append(' ', (indent + 1) * 2); }
                Write(sb, list[i], pretty, indent + 1);
            }
            if (pretty && !scalarOnly && list.Count > 0) { sb.Append('\n'); sb.Append(' ', indent * 2); }
            sb.Append(']');
        }

        private static void WriteString(StringBuilder sb, string s)
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
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    default:
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }

        // ---------------- parser ----------------

        private sealed class Parser
        {
            private readonly string _s;
            private int _i;

            public Parser(string s) { _s = s; }
            public bool AtEnd => _i >= _s.Length;
            public int Position => _i;

            public void SkipWhitespace()
            {
                while (_i < _s.Length && char.IsWhiteSpace(_s[_i])) _i++;
            }

            public object ParseValue()
            {
                SkipWhitespace();
                if (AtEnd) throw new FormatException("Unexpected end of JSON");
                char c = _s[_i];
                switch (c)
                {
                    case '{': return ParseObject();
                    case '[': return ParseArray();
                    case '"': return ParseString();
                    case 't': Expect("true"); return true;
                    case 'f': Expect("false"); return false;
                    case 'n': Expect("null"); return null;
                    default:
                        if (c == '-' || (c >= '0' && c <= '9')) return ParseNumber();
                        throw new FormatException("Unexpected character '" + c + "' at " + _i);
                }
            }

            private void Expect(string word)
            {
                if (string.CompareOrdinal(_s, _i, word, 0, word.Length) != 0)
                    throw new FormatException("Expected '" + word + "' at " + _i);
                _i += word.Length;
            }

            private Dictionary<string, object> ParseObject()
            {
                var d = new Dictionary<string, object>();
                _i++; // {
                SkipWhitespace();
                if (!AtEnd && _s[_i] == '}') { _i++; return d; }
                while (true)
                {
                    SkipWhitespace();
                    if (AtEnd || _s[_i] != '"') throw new FormatException("Expected property name at " + _i);
                    string key = ParseString();
                    SkipWhitespace();
                    if (AtEnd || _s[_i] != ':') throw new FormatException("Expected ':' at " + _i);
                    _i++;
                    d[key] = ParseValue();
                    SkipWhitespace();
                    if (AtEnd) throw new FormatException("Unterminated object");
                    if (_s[_i] == ',') { _i++; continue; }
                    if (_s[_i] == '}') { _i++; return d; }
                    throw new FormatException("Expected ',' or '}' at " + _i);
                }
            }

            private List<object> ParseArray()
            {
                var list = new List<object>();
                _i++; // [
                SkipWhitespace();
                if (!AtEnd && _s[_i] == ']') { _i++; return list; }
                while (true)
                {
                    list.Add(ParseValue());
                    SkipWhitespace();
                    if (AtEnd) throw new FormatException("Unterminated array");
                    if (_s[_i] == ',') { _i++; continue; }
                    if (_s[_i] == ']') { _i++; return list; }
                    throw new FormatException("Expected ',' or ']' at " + _i);
                }
            }

            private string ParseString()
            {
                _i++; // opening quote
                var sb = new StringBuilder();
                while (true)
                {
                    if (AtEnd) throw new FormatException("Unterminated string");
                    char c = _s[_i++];
                    if (c == '"') return sb.ToString();
                    if (c != '\\') { sb.Append(c); continue; }
                    if (AtEnd) throw new FormatException("Unterminated escape");
                    char e = _s[_i++];
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
                            if (_i + 4 > _s.Length) throw new FormatException("Bad unicode escape");
                            sb.Append((char)int.Parse(_s.Substring(_i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                            _i += 4;
                            break;
                        default: throw new FormatException("Bad escape \\" + e);
                    }
                }
            }

            private object ParseNumber()
            {
                int start = _i;
                bool isFloat = false;
                if (_s[_i] == '-') _i++;
                while (!AtEnd)
                {
                    char c = _s[_i];
                    if (c >= '0' && c <= '9') { _i++; continue; }
                    if (c == '.' || c == 'e' || c == 'E' || c == '+' || c == '-') { isFloat = true; _i++; continue; }
                    break;
                }
                string token = _s.Substring(start, _i - start);
                if (!isFloat && long.TryParse(token, NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out long l)) return l;
                if (double.TryParse(token, NumberStyles.Float, CultureInfo.InvariantCulture, out double d)) return d;
                throw new FormatException("Bad number '" + token + "'");
            }
        }
    }
}
