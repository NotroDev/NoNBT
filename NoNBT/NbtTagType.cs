namespace NoNBT;

/// <summary>
/// Represents the different types of NBT data.
/// Used to indicate the type of data stored within an NBT tag.
/// </summary>
public enum NbtTagType : byte
{
    /// <summary>
    /// Marks the end of a Compound tag. This tag has no name or value.
    /// </summary>
    End = 0,

    /// <summary>
    /// A single signed byte.
    /// </summary>
    Byte = 1,

    /// <summary>
    /// A single signed 16-bit integer (short).
    /// </summary>
    Short = 2,

    /// <summary>
    /// A single signed 32-bit integer (int).
    /// </summary>
    Int = 3,

    /// <summary>
    /// A single signed 64-bit integer (long).
    /// </summary>
    Long = 4,

    /// <summary>
    /// A single-precision 32-bit IEEE 754 floating point number (float).
    /// </summary>
    Float = 5,

    /// <summary>
    /// A double-precision 64-bit IEEE 754 floating point number (double).
    /// </summary>
    Double = 6,

    /// <summary>
    /// An array of signed bytes.
    /// </summary>
    ByteArray = 7,

    /// <summary>
    /// A UTF-8 (specifically, Modified UTF-8) string.
    /// </summary>
    String = 8,

    /// <summary>
    /// A list of unnamed tags, all of the same type.
    /// </summary>
    List = 9,

    /// <summary>
    /// A collection of named tags (like a dictionary or map).
    /// </summary>
    Compound = 10,

    /// <summary>
    /// An array of signed 32-bit integers.
    /// </summary>
    IntArray = 11,

    /// <summary>
    /// An array of signed 64-bit integers.
    /// </summary>
    LongArray = 12
}