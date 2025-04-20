using System.Diagnostics.CodeAnalysis;

namespace NoNBT;

/// <summary>
/// Provides methods for encoding and decoding strings using the Modified UTF-8 encoding format.
/// </summary>
/// <remarks>
/// Modified UTF-8 is a variation of the standard UTF-8 encoding used in NBT.
/// This encoding differs from standard UTF-8 in that it uses two-byte sequences
/// to encode the null character (U+0000) instead of the single byte used in standard UTF-8.
/// </remarks>
public static class ModifiedUtf8
{
    /// Encodes a given string into a byte array using a modified UTF-8 encoding.
    /// The encoding differs from standard UTF-8 in that the null character ('\0')
    /// is encoded as two bytes (0xC0, 0x80). It also ensures that the resulting
    /// byte array does not exceed the maximum length specified for NBT data.
    /// Throws an exception if the encoded byte length exceeds the permitted maximum.
    /// <param name="str">The string to be encoded into a modified UTF-8 byte array.</param>
    /// <returns>A byte array containing the modified UTF-8 encoded representation of the input string.</returns>
    /// <exception cref="FormatException">Thrown when the encoded string length exceeds the maximum allowable byte size.</exception>
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

    /// Decodes a byte array encoded in the Modified UTF-8 format into its string representation.
    /// Throws an exception if the input byte array contains invalid Modified UTF-8 data.
    /// <param name="bytes">The byte span containing the Modified UTF-8 encoded data to decode.</param>
    /// <returns>A string that represents the decoded value from the Modified UTF-8 byte array.</returns>
    /// <exception cref="FormatException">Thrown when the input byte array contains invalid Modified UTF-8 data.</exception>
    public static string GetString(ReadOnlySpan<byte> bytes)
    {
        if (TryGetString(bytes, out string? result))
        {
            return result;
        }
        throw new FormatException("Input data contained invalid Modified UTF-8 bytes.");
    }

    /// Tries to decode a sequence of bytes encoded using the Modified UTF-8 format into a string.
    /// Returns a boolean indicating whether the operation succeeded or failed.
    /// If successful, the decoded string is assigned to the output parameter; otherwise, the output parameter is set to null.
    /// This method ensures that input data is validated to conform to the Modified UTF-8 encoding standard.
    /// <param name="bytes">The span of bytes to decode using the Modified UTF-8 format.</param>
    /// <param name="value">When this method returns, contains the decoded string if the operation was successful, or null if it failed.</param>
    /// <returns>True if the byte sequence was successfully decoded into a string; otherwise, false.</returns>
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