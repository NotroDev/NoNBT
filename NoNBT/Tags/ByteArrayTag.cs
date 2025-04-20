namespace NoNBT.Tags;

/// <summary>
/// Represents an NBT tag containing an array of bytes.
/// </summary>
/// <param name="name">The name of the tag.</param>
/// <param name="value">The byte array value.</param>
public class ByteArrayTag(string? name, byte[] value) : NbtTag(name)
{
    /// <summary>
    /// Gets the type of this NBT tag.
    /// </summary>
    public override NbtTagType TagType => NbtTagType.ByteArray;
    
    /// <summary>
    /// Gets or sets the byte array value of this tag.
    /// </summary>
    public byte[] Value { get; set; } = value;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ByteArrayTag"/> class with a null name.
    /// </summary>
    /// <param name="value">The byte array value.</param>
    public ByteArrayTag(byte[] value) : this(null, value) { }

    /// <summary>
    /// Creates a deep copy of this tag.
    /// </summary>
    /// <returns>A new <see cref="ByteArrayTag"/> with the same name and a copy of the value.</returns>
    public override NbtTag Clone()
    {
        var clonedValue = new byte[Value.Length];
        Value.CopyTo(clonedValue, 0);
        return new ByteArrayTag(Name, clonedValue);
    }

    /// <summary>
    /// Converts a <see cref="ByteArrayTag"/> to a byte array.
    /// </summary>
    /// <param name="tag">The tag to convert.</param>
    public static explicit operator byte[](ByteArrayTag tag) => tag.Value;

    /// <summary>
    /// Returns a string representation of this tag.
    /// </summary>
    /// <returns>A string representing this tag and its value.</returns>
    public override string ToString()
    {
        return $"{base.ToString()}: {Value}";
    }
    
    /// <summary>
    /// Converts this tag to a JSON string representation.
    /// </summary>
    /// <param name="indentLevel">The indentation level for formatting.</param>
    /// <returns>A JSON string representing this tag.</returns>
    public override string ToJson(int indentLevel = 0)
    {
        return $"{GetIndent(indentLevel)}{FormatPropertyName()}[{string.Join(", ", Value.Select(b => b.ToString()))}]";
    }
}