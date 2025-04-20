namespace NoNBT.Tags;

/// <summary>
/// Represents an NBT tag containing a 16-bit signed integer value.
/// </summary>
/// <param name="name">The name of the tag.</param>
/// <param name="value">The short integer value.</param>
public class ShortTag(string? name, short value) : NbtTag(name)
{
    /// <summary>
    /// Gets the type of this NBT tag.
    /// </summary>
    public override NbtTagType TagType => NbtTagType.Short;
    
    /// <summary>
    /// Gets or sets the short integer value of this tag.
    /// </summary>
    public short Value { get; set; } = value;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ShortTag"/> class with a null name.
    /// </summary>
    /// <param name="value">The short integer value.</param>
    public ShortTag(short value) : this(null, value) { }

    /// <summary>
    /// Creates a copy of this tag.
    /// </summary>
    /// <returns>A new <see cref="ShortTag"/> with the same name and value.</returns>
    public override NbtTag Clone()
    {
        return new ShortTag(Name, Value);
    }

    /// <summary>
    /// Converts a <see cref="ShortTag"/> to a short integer.
    /// </summary>
    /// <param name="tag">The tag to convert.</param>
    public static explicit operator short(ShortTag tag) => tag.Value;

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