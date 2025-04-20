namespace NoNBT.Tags;

/// <summary>
/// Represents an NBT tag containing a 64-bit signed integer value.
/// </summary>
/// <param name="name">The name of the tag.</param>
/// <param name="value">The long integer value.</param>
public class LongTag(string? name, long value) : NbtTag(name)
{
    /// <summary>
    /// Gets the type of this NBT tag.
    /// </summary>
    public override NbtTagType TagType => NbtTagType.Long;
    
    /// <summary>
    /// Gets or sets the long integer value of this tag.
    /// </summary>
    public long Value { get; set; } = value;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="LongTag"/> class with a null name.
    /// </summary>
    /// <param name="value">The long integer value.</param>
    public LongTag(long value) : this(null, value) { }

    /// <summary>
    /// Creates a copy of this tag.
    /// </summary>
    /// <returns>A new <see cref="LongTag"/> with the same name and value.</returns>
    public override NbtTag Clone()
    {
        return new LongTag(Name, Value);
    }

    /// <summary>
    /// Converts a <see cref="LongTag"/> to a long integer.
    /// </summary>
    /// <param name="tag">The tag to convert.</param>
    public static explicit operator long(LongTag tag) => tag.Value;

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
        return $"{GetIndent(indentLevel)}{FormatPropertyName()}{Value}";
    }
}