namespace NoNBT.Tags;

/// <summary>
/// Represents an NBT tag containing an array of 64-bit signed integers.
/// </summary>
/// <param name="name">The name of the tag.</param>
/// <param name="value">The long integer array value.</param>
public class LongArrayTag(string? name, long[] value) : NbtTag(name)
{
    /// <summary>
    /// Gets the type of this NBT tag.
    /// </summary>
    public override NbtTagType TagType => NbtTagType.LongArray;
    
    /// <summary>
    /// Gets or sets the long integer array value of this tag.
    /// </summary>
    public long[] Value { get; set; } = value;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="LongArrayTag"/> class with a null name.
    /// </summary>
    /// <param name="value">The long integer array value.</param>
    public LongArrayTag(long[] value) : this(null, value) { }

    /// <summary>
    /// Creates a deep copy of this tag.
    /// </summary>
    /// <returns>A new <see cref="LongArrayTag"/> with the same name and a copy of the value.</returns>
    public override NbtTag Clone()
    {
        var clonedValue = new long[Value.Length];
        Value.CopyTo(clonedValue, 0);
        return new LongArrayTag(Name, clonedValue);
    }

    /// <summary>
    /// Converts a <see cref="LongArrayTag"/> to a long integer array.
    /// </summary>
    /// <param name="tag">The tag to convert.</param>
    public static explicit operator long[](LongArrayTag tag) => tag.Value;

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
        return $"{GetIndent(indentLevel)}{FormatPropertyName()}[{string.Join(", ", Value.Select(l => l.ToString()))}]";
    }
}