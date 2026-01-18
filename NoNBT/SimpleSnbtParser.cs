using System.Globalization;
using System.Text;
using NoNBT.Tags;

namespace NoNBT;

/// <summary>
/// Provides functionality to parse basic SNBT (Stringified NBT) strings into NbtTag objects.
/// </summary>
/// <remarks>Supports only a subset of basic SNBT features for simplicity.</remarks>
public static class SimpleSnbtParser
{
    /// <summary>
    /// Parses an SNBT string into an NBT tag.
    /// </summary>
    /// <param name="snbt">The SNBT string to parse.</param>
    /// <returns>The parsed NbtTag (usually a CompoundTag).</returns>
    /// <exception cref="FormatException">Thrown when the SNBT string is invalid.</exception>
    public static NbtTag Parse(string snbt)
    {
        if (string.IsNullOrWhiteSpace(snbt))
            throw new ArgumentException("Input string cannot be null or empty.", nameof(snbt));

        var reader = new StringReader(snbt);
        NbtTag tag = ParseTag(reader);

        reader.SkipWhitespace();
        return !reader.IsEOF
            ? throw new FormatException($"Unexpected characters at the end of SNBT string at index {reader.Index}.")
            : tag;
    }

    private static NbtTag ParseTag(StringReader reader)
    {
        reader.SkipWhitespace();
        char c = reader.Peek();

        switch (c)
        {
            case '{':
                return ParseCompound(reader);
            case '[':
                return ParseListOrArray(reader);
            case '"':
            case '\'':
            {
                string value = ParseQuotedString(reader);
                return new StringTag(value);
            }
            default:
                return ParsePrimitive(reader);
        }
    }

    private static CompoundTag ParseCompound(StringReader reader)
    {
        reader.Read();
        var compound = new CompoundTag();

        while (true)
        {
            reader.SkipWhitespace();
            if (reader.Peek() == '}')
            {
                reader.Read();
                break;
            }

            string key = ParseKey(reader);

            reader.SkipWhitespace();
            if (reader.Read() != ':')
            {
                throw new FormatException($"Expected ':' after key '{key}' at index {reader.Index}.");
            }

            NbtTag value = ParseTag(reader);

            compound.Add(key, value);

            reader.SkipWhitespace();
            char next = reader.Peek();
            switch (next)
            {
                case ',':
                    reader.Read();
                    continue;
                case '}':
                    continue;
                default:
                    throw new FormatException($"Expected ',' or '}}' in compound at index {reader.Index}.");
            }
        }

        return compound;
    }

    private static NbtTag ParseListOrArray(StringReader reader)
    {
        reader.Read();

        char c1 = reader.Peek();
        char c2 = reader.Peek(1);

        if (c2 == ';')
        {
            switch (c1)
            {
                case 'B':
                    return ParseByteArray(reader);
                case 'I':
                    return ParseIntArray(reader);
                case 'L':
                    return ParseLongArray(reader);
            }
        }

        var list = new List<NbtTag>();
        while (true)
        {
            reader.SkipWhitespace();
            if (reader.Peek() == ']')
            {
                reader.Read();
                break;
            }

            list.Add(ParseTag(reader));

            reader.SkipWhitespace();
            if (reader.Peek() == ',')
            {
                reader.Read();
                continue;
            }

            if (reader.Peek() == ']') continue;

            throw new FormatException($"Expected ',' or ']' in list at index {reader.Index}.");
        }

        if (list.Count == 0)
        {
            return new ListTag(null, NbtTagType.End);
        }

        NbtTagType type = list[0].TagType;

        foreach (NbtTag tag in list.Where(tag => tag.TagType != type))
        {
            throw new FormatException(
                $"SNBT List contains mixed types ({type} and {tag.TagType}). This parser requires homogeneous lists.");
        }

        return new ListTag(null, type, list);
    }

    private static ByteArrayTag ParseByteArray(StringReader reader)
    {
        reader.Read();
        reader.Read();

        var bytes = new List<byte>();
        while (true)
        {
            reader.SkipWhitespace();
            if (reader.Peek() == ']')
            {
                reader.Read();
                break;
            }

            NbtTag tag = ParsePrimitive(reader);
            if (tag is ByteTag bTag) bytes.Add(bTag.Value);
            else throw new FormatException($"Expected ByteTag in Byte Array at index {reader.Index}.");

            reader.SkipWhitespace();
            if (reader.Peek() == ',')
            {
                reader.Read();
                continue;
            }

            if (reader.Peek() == ']') continue;
            throw new FormatException("Expected ',' or ']' in Byte Array.");
        }

        return new ByteArrayTag(bytes.ToArray());
    }

    private static IntArrayTag ParseIntArray(StringReader reader)
    {
        reader.Read();
        reader.Read();

        var ints = new List<int>();
        while (true)
        {
            reader.SkipWhitespace();
            if (reader.Peek() == ']')
            {
                reader.Read();
                break;
            }

            NbtTag tag = ParsePrimitive(reader);
            if (tag is IntTag iTag) ints.Add(iTag.Value);
            else throw new FormatException($"Expected IntTag in Int Array at index {reader.Index}.");

            reader.SkipWhitespace();
            if (reader.Peek() == ',')
            {
                reader.Read();
                continue;
            }

            if (reader.Peek() == ']') continue;
            throw new FormatException("Expected ',' or ']' in Int Array.");
        }

        return new IntArrayTag(ints.ToArray());
    }

    private static LongArrayTag ParseLongArray(StringReader reader)
    {
        reader.Read();
        reader.Read();

        var longs = new List<long>();
        while (true)
        {
            reader.SkipWhitespace();
            if (reader.Peek() == ']')
            {
                reader.Read();
                break;
            }

            NbtTag tag = ParsePrimitive(reader);
            if (tag is LongTag lTag) longs.Add(lTag.Value);
            else throw new FormatException($"Expected LongTag in Long Array at index {reader.Index}.");

            reader.SkipWhitespace();
            if (reader.Peek() == ',')
            {
                reader.Read();
                continue;
            }

            if (reader.Peek() == ']') continue;
            throw new FormatException("Expected ',' or ']' in Long Array.");
        }

        return new LongArrayTag(longs.ToArray());
    }

    private static string ParseKey(StringReader reader)
    {
        reader.SkipWhitespace();
        char c = reader.Peek();
        return c is '"' or '\'' ? ParseQuotedString(reader) : ReadUnquotedString(reader);
    }

    private static string ParseQuotedString(StringReader reader)
    {
        char quoteChar = reader.Read();
        var sb = new StringBuilder();
        var escaped = false;

        while (!reader.IsEOF)
        {
            char c = reader.Read();
            if (escaped)
            {
                sb.Append(c);
                escaped = false;
            }
            else
            {
                if (c == '\\')
                {
                    escaped = true;
                }
                else if (c == quoteChar)
                {
                    return sb.ToString();
                }
                else
                {
                    sb.Append(c);
                }
            }
        }

        throw new FormatException("Unterminated quoted string.");
    }

    private static string ReadUnquotedString(StringReader reader)
    {
        var sb = new StringBuilder();
        while (!reader.IsEOF)
        {
            char c = reader.Peek();
            if (char.IsLetterOrDigit(c) || c == '_' || c == '-' || c == '.' || c == '+')
            {
                sb.Append(reader.Read());
            }
            else
            {
                break;
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Parses numbers, booleans, or falls back to unquoted strings.
    /// </summary>
    private static NbtTag ParsePrimitive(StringReader reader)
    {
        string raw = ReadUnquotedString(reader);

        switch (raw)
        {
            case "true":
                return new ByteTag(1);
            case "false":
                return new ByteTag(0);
        }

        if (raw.Length > 0)
        {
            char last = char.ToUpperInvariant(raw[^1]);
            string valPart = raw[..^1];

            try
            {
                switch (last)
                {
                    case 'B':
                        return new ByteTag(
                            sbyte.Parse(valPart, CultureInfo.InvariantCulture) switch { var s => (byte)s });
                    case 'S':
                        return new ShortTag(short.Parse(valPart, CultureInfo.InvariantCulture));
                    case 'L':
                        return new LongTag(long.Parse(valPart, CultureInfo.InvariantCulture));
                    case 'F':
                        return new FloatTag(float.Parse(valPart, CultureInfo.InvariantCulture));
                    case 'D':
                        return new DoubleTag(double.Parse(valPart, CultureInfo.InvariantCulture));
                }
            }
            catch (FormatException)
            {
                // ignore
            }
            catch (OverflowException)
            {
                // ignore
            }
        }

        if (int.TryParse(raw, NumberStyles.Integer, CultureInfo.InvariantCulture, out int iVal))
        {
            return new IntTag(iVal);
        }

        if (double.TryParse(raw, NumberStyles.Float, CultureInfo.InvariantCulture, out double dVal))
        {
            return new DoubleTag(dVal);
        }

        return new StringTag(raw);
    }

    private class StringReader(string input)
    {
        public int Index { get; private set; }

        public bool IsEOF => Index >= input.Length;

        public char Peek(int offset = 0)
        {
            return Index + offset >= input.Length ? '\0' : input[Index + offset];
        }

        public char Read()
        {
            return IsEOF ? '\0' : input[Index++];
        }

        public void SkipWhitespace()
        {
            while (!IsEOF && char.IsWhiteSpace(Peek()))
            {
                Index++;
            }
        }
    }
}