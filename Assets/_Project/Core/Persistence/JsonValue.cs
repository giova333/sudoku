using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Sudoku.Core.Persistence
{
    enum JsonKind
    {
        Null,
        Bool,
        Number,
        Text,
        Array,
        Object
    }

    /// <summary>
    /// A deliberately small JSON tree, just large enough for a save payload.
    ///
    /// It lives in Core rather than leaning on UnityEngine.JsonUtility because
    /// the save format is the thing under test, and Core is where a test can
    /// reach it with no engine, no scene and no file system. Numbers are kept
    /// as the text they were written with, so a float that survives a round
    /// trip survives it exactly rather than to within a formatting whim.
    /// </summary>
    sealed class JsonValue
    {
        readonly List<JsonValue> _items;
        readonly List<string> _names;
        readonly List<JsonValue> _values;
        readonly string _text;

        JsonValue(JsonKind kind, string text)
        {
            Kind = kind;
            _text = text;

            if (kind == JsonKind.Array)
                _items = new List<JsonValue>();

            if (kind == JsonKind.Object)
            {
                // Parallel lists rather than a dictionary: member order is
                // written back out as authored, which keeps payloads diffable.
                _names = new List<string>();
                _values = new List<JsonValue>();
            }
        }

        public JsonKind Kind { get; }

        public static JsonValue Object() => new JsonValue(JsonKind.Object, null);

        public static JsonValue Array() => new JsonValue(JsonKind.Array, null);

        public static JsonValue Number(int value) =>
            new JsonValue(JsonKind.Number, value.ToString(CultureInfo.InvariantCulture));

        public static JsonValue Number(long value) =>
            new JsonValue(JsonKind.Number, value.ToString(CultureInfo.InvariantCulture));

        /// <summary>
        /// G9 is the shortest form that round-trips every float exactly, which
        /// is what the elapsed clock needs to survive a save.
        /// </summary>
        public static JsonValue Number(float value) =>
            new JsonValue(JsonKind.Number, value.ToString("G9", CultureInfo.InvariantCulture));

        public static JsonValue Text(string value) =>
            value == null ? new JsonValue(JsonKind.Null, null) : new JsonValue(JsonKind.Text, value);

        public static JsonValue Bool(bool value) =>
            new JsonValue(JsonKind.Bool, value ? "true" : "false");

        public static JsonValue Numbers(IReadOnlyList<int> values)
        {
            var array = Array();
            for (var i = 0; i < values.Count; i++)
                array.Add(Number(values[i]));
            return array;
        }

        public void Add(JsonValue item) => _items.Add(item);

        public void Set(string name, JsonValue value)
        {
            for (var i = 0; i < _names.Count; i++)
            {
                if (_names[i] != name) continue;
                _values[i] = value;
                return;
            }

            _names.Add(name);
            _values.Add(value);
        }

        public void Remove(string name)
        {
            for (var i = 0; i < _names.Count; i++)
            {
                if (_names[i] != name) continue;
                _names.RemoveAt(i);
                _values.RemoveAt(i);
                return;
            }
        }

        /// <summary>The value of a member, or null when this is not an object with that member.</summary>
        public JsonValue Member(string name)
        {
            if (_names == null) return null;

            for (var i = 0; i < _names.Count; i++)
                if (_names[i] == name)
                    return _values[i];

            return null;
        }

        public IReadOnlyList<JsonValue> Items => _items ?? EmptyItems;

        static readonly List<JsonValue> EmptyItems = new List<JsonValue>();

        public int IntOr(string name, int fallback)
        {
            var member = Member(name);
            if (member == null || member.Kind != JsonKind.Number) return fallback;
            return int.TryParse(member._text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : fallback;
        }

        public long LongOr(string name, long fallback)
        {
            var member = Member(name);
            if (member == null || member.Kind != JsonKind.Number) return fallback;
            return long.TryParse(member._text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : fallback;
        }

        public float FloatOr(string name, float fallback)
        {
            var member = Member(name);
            if (member == null || member.Kind != JsonKind.Number) return fallback;
            return float.TryParse(member._text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed)
                ? parsed
                : fallback;
        }

        public bool BoolOr(string name, bool fallback)
        {
            var member = Member(name);
            if (member == null || member.Kind != JsonKind.Bool) return fallback;
            return member._text == "true";
        }

        public string TextOr(string name, string fallback)
        {
            var member = Member(name);
            if (member == null || member.Kind != JsonKind.Text) return fallback;
            return member._text;
        }

        /// <summary>An int per array element; a missing or non-array member yields an empty array.</summary>
        public int[] IntsOr(string name, int[] fallback)
        {
            var member = Member(name);
            if (member == null || member.Kind != JsonKind.Array) return fallback;

            var values = new int[member._items.Count];
            for (var i = 0; i < values.Length; i++)
            {
                var item = member._items[i];
                if (item.Kind == JsonKind.Number &&
                    int.TryParse(item._text, NumberStyles.Float, CultureInfo.InvariantCulture, out var parsed))
                    values[i] = parsed;
            }
            return values;
        }

        public void Write(StringBuilder sb)
        {
            switch (Kind)
            {
                case JsonKind.Null:
                    sb.Append("null");
                    break;
                case JsonKind.Bool:
                case JsonKind.Number:
                    sb.Append(_text);
                    break;
                case JsonKind.Text:
                    Escape(sb, _text);
                    break;
                case JsonKind.Array:
                    sb.Append('[');
                    for (var i = 0; i < _items.Count; i++)
                    {
                        if (i > 0) sb.Append(',');
                        _items[i].Write(sb);
                    }
                    sb.Append(']');
                    break;
                case JsonKind.Object:
                    sb.Append('{');
                    for (var i = 0; i < _names.Count; i++)
                    {
                        if (i > 0) sb.Append(',');
                        Escape(sb, _names[i]);
                        sb.Append(':');
                        _values[i].Write(sb);
                    }
                    sb.Append('}');
                    break;
            }
        }

        static void Escape(StringBuilder sb, string value)
        {
            sb.Append('"');
            foreach (var c in value)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < ' ')
                            sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        else
                            sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }

        /// <summary>
        /// Reads a payload. Anything that is not well-formed JSON is a
        /// <see cref="SaveFormatException"/> rather than a silent half-read -
        /// a truncated save must announce itself, not resurrect half a board.
        /// </summary>
        public static JsonValue Parse(string text)
        {
            if (text == null)
                throw new SaveFormatException("A save payload cannot be null.");

            var at = 0;
            var value = ParseValue(text, ref at);
            SkipWhitespace(text, ref at);
            if (at != text.Length)
                throw new SaveFormatException($"Unexpected trailing content at character {at}.");

            return value;
        }

        static JsonValue ParseValue(string text, ref int at)
        {
            SkipWhitespace(text, ref at);
            if (at >= text.Length)
                throw new SaveFormatException("A save payload ended before its value.");

            switch (text[at])
            {
                case '{': return ParseObject(text, ref at);
                case '[': return ParseArray(text, ref at);
                case '"': return Text(ParseString(text, ref at));
                case 't': Expect(text, ref at, "true"); return Bool(true);
                case 'f': Expect(text, ref at, "false"); return Bool(false);
                case 'n': Expect(text, ref at, "null"); return new JsonValue(JsonKind.Null, null);
                default: return ParseNumber(text, ref at);
            }
        }

        static JsonValue ParseObject(string text, ref int at)
        {
            var result = Object();
            at++;
            SkipWhitespace(text, ref at);

            if (at < text.Length && text[at] == '}')
            {
                at++;
                return result;
            }

            while (true)
            {
                SkipWhitespace(text, ref at);
                if (at >= text.Length || text[at] != '"')
                    throw new SaveFormatException($"Expected a member name at character {at}.");

                var name = ParseString(text, ref at);
                SkipWhitespace(text, ref at);
                if (at >= text.Length || text[at] != ':')
                    throw new SaveFormatException($"Expected ':' after '{name}' at character {at}.");

                at++;
                result.Set(name, ParseValue(text, ref at));
                SkipWhitespace(text, ref at);

                if (at >= text.Length)
                    throw new SaveFormatException("A save payload ended inside an object.");
                if (text[at] == ',') { at++; continue; }
                if (text[at] == '}') { at++; return result; }

                throw new SaveFormatException($"Expected ',' or '}}' at character {at}.");
            }
        }

        static JsonValue ParseArray(string text, ref int at)
        {
            var result = Array();
            at++;
            SkipWhitespace(text, ref at);

            if (at < text.Length && text[at] == ']')
            {
                at++;
                return result;
            }

            while (true)
            {
                result.Add(ParseValue(text, ref at));
                SkipWhitespace(text, ref at);

                if (at >= text.Length)
                    throw new SaveFormatException("A save payload ended inside an array.");
                if (text[at] == ',') { at++; continue; }
                if (text[at] == ']') { at++; return result; }

                throw new SaveFormatException($"Expected ',' or ']' at character {at}.");
            }
        }

        static string ParseString(string text, ref int at)
        {
            at++;
            var sb = new StringBuilder();

            while (at < text.Length)
            {
                var c = text[at++];
                if (c == '"')
                    return sb.ToString();

                if (c != '\\')
                {
                    sb.Append(c);
                    continue;
                }

                if (at >= text.Length)
                    break;

                var escape = text[at++];
                switch (escape)
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
                        if (at + 4 > text.Length)
                            throw new SaveFormatException($"A truncated \\u escape at character {at}.");
                        sb.Append((char)int.Parse(text.Substring(at, 4), NumberStyles.HexNumber,
                            CultureInfo.InvariantCulture));
                        at += 4;
                        break;
                    default:
                        throw new SaveFormatException($"Unknown escape '\\{escape}' at character {at}.");
                }
            }

            throw new SaveFormatException("A save payload ended inside a string.");
        }

        static JsonValue ParseNumber(string text, ref int at)
        {
            var start = at;
            if (at < text.Length && (text[at] == '-' || text[at] == '+'))
                at++;

            while (at < text.Length)
            {
                var c = text[at];
                if ((c >= '0' && c <= '9') || c == '.' || c == 'e' || c == 'E' || c == '+' || c == '-')
                    at++;
                else
                    break;
            }

            if (at == start)
                throw new SaveFormatException($"Expected a value at character {start}.");

            return new JsonValue(JsonKind.Number, text.Substring(start, at - start));
        }

        static void Expect(string text, ref int at, string literal)
        {
            if (at + literal.Length > text.Length ||
                string.CompareOrdinal(text, at, literal, 0, literal.Length) != 0)
                throw new SaveFormatException($"Expected '{literal}' at character {at}.");

            at += literal.Length;
        }

        static void SkipWhitespace(string text, ref int at)
        {
            while (at < text.Length)
            {
                var c = text[at];
                if (c == ' ' || c == '\t' || c == '\n' || c == '\r')
                    at++;
                else
                    break;
            }
        }
    }
}
