using System.Diagnostics.CodeAnalysis;

namespace NoNBT;
public static class ModifiedUtf8
{
    public static byte[] GetBytes(string str)
    {
        if (string.IsNullOrEmpty(str))
            return [];
        
        var byteCount = 0;
        foreach (char c in str)
        {
            if (c == 0)
                byteCount += 2;
            else if (c >= 0x0001 && c <= 0x007F)
                byteCount += 1;
            else if (c <= 0x07FF)
                byteCount += 2;
            else
            {
                byteCount += 3;
            }
        }

        if (byteCount > ushort.MaxValue)
        {
            throw new FormatException($"Encoded string length ({byteCount} bytes) exceeds the NBT maximum of {ushort.MaxValue} bytes.");
        }
        
        var bytes = new byte[byteCount];
        var position = 0;
        
        foreach (char c in str)
        {
            if (c == 0)
            {
                bytes[position++] = 0xC0;
                bytes[position++] = 0x80;
            }
            else if (c >= 0x0001 && c <= 0x007F)
            {
                bytes[position++] = (byte)c;
            }
            else if (c <= 0x07FF)
            {
                bytes[position++] = (byte)(0xC0 | ((c >> 6) & 0x1F));
                bytes[position++] = (byte)(0x80 | (c & 0x3F));
            }
            else
            {
                bytes[position++] = (byte)(0xE0 | ((c >> 12) & 0x0F));
                bytes[position++] = (byte)(0x80 | ((c >> 6) & 0x3F));
                bytes[position++] = (byte)(0x80 | (c & 0x3F));
            }
        }
        return bytes;
    }
    
    public static string GetString(ReadOnlySpan<byte> bytes)
    {
        if (TryGetString(bytes, out string? result))
        {
            return result;
        }
        throw new FormatException("Input data contained invalid Modified UTF-8 bytes.");
    }
    
    public static bool TryGetString(ReadOnlySpan<byte> bytes, [NotNullWhen(true)] out string? value)
    {
        if (bytes.IsEmpty)
        {
            value = string.Empty;
            return true;
        }

        if (!TryGetCharCount(bytes, out int charCount))
        {
            value = null;
            return false;
        }

        if (charCount == 0)
        {
            value = string.Empty;
            return true;
        }

        value = string.Create(charCount, bytes, (chars, state) =>
        {
            GetStringInternal(state, chars);
        });

        return true;
    }
    
    private static bool TryGetCharCount(ReadOnlySpan<byte> bytes, out int charCount)
    {
        charCount = 0;
        var index = 0;

        while (index < bytes.Length)
        {
            byte b1 = bytes[index];

            if ((b1 & 0x80) == 0)
            {
                if (b1 == 0)
                {
                    return false;
                }
                index++;
            }
            else if ((b1 & 0xE0) == 0xC0)
            {
                if (index + 1 >= bytes.Length) return false;
                byte b2 = bytes[index + 1];
                if ((b2 & 0xC0) != 0x80) return false;
                
                int val = ((b1 & 0x1F) << 6) | (b2 & 0x3F);
                if (val < 0x80 && val != 0) return false;

                index += 2;
            }
            else if ((b1 & 0xF0) == 0xE0)
            {
                if (index + 2 >= bytes.Length) return false;
                byte b2 = bytes[index + 1];
                byte b3 = bytes[index + 2];
                if ((b2 & 0xC0) != 0x80 || (b3 & 0xC0) != 0x80) return false;

                int val = ((b1 & 0x0F) << 12) | ((b2 & 0x3F) << 6) | (b3 & 0x3F);
                if (val < 0x800) return false;
                
                index += 3;
            }
            else
            {
                return false;
            }
            charCount++;
        }
        return true;
    }
    
    private static void GetStringInternal(ReadOnlySpan<byte> bytes, Span<char> destination)
    {
        var byteIndex = 0;
        var charIndex = 0;

        while (byteIndex < bytes.Length)
        {
            byte b1 = bytes[byteIndex];
            char c;

            if ((b1 & 0x80) == 0)
            {
                c = (char)b1;
                byteIndex++;
            }
            else if ((b1 & 0xE0) == 0xC0)
            {
                byte b2 = bytes[byteIndex + 1];
                c = (char)(((b1 & 0x1F) << 6) | (b2 & 0x3F));
                byteIndex += 2;
            }
            else
            {
                byte b2 = bytes[byteIndex + 1];
                byte b3 = bytes[byteIndex + 2];
                c = (char)(((b1 & 0x0F) << 12) | ((b2 & 0x3F) << 6) | (b3 & 0x3F));
                byteIndex += 3;
            }
            destination[charIndex++] = c;
        }
    }
}