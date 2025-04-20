namespace NoNBT.Tags;

/// <summary>
/// Represents an NBT tag containing a UTF-8 string value.
/// </summary>
/// <param name="name">The name of the tag.</param>
/// <param name="value">The string value.</param>
public class StringTag(string? name, string value) : NbtTag(name)
{
    /// <summary>
    /// Gets the type of this NBT tag.
    /// </summary>
    public override NbtTagType TagType => NbtTagType.String;
    
    /// <summary>
    /// Gets or sets the string value of this tag.
    /// </summary>
    public string Value { get; set; } = value;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="StringTag"/> class with a null name.
    /// </summary>
    /// <param name="value">The string value.</param>
    public StringTag(string value) : this(null, value) { }

    /// <summary>
    /// Creates a copy of this tag.
    /// </summary>
    /// <returns>A new <see cref="StringTag"/> with the same name and value.</returns>
    public override NbtTag Clone()
    {
        return new StringTag(Name, Value);
    }

    /// <summary>
    /// Converts a <see cref="StringTag"/> to a string.
    /// </summary>
    /// <param name="tag">The tag to convert.</param>
    public static explicit operator string(StringTag tag) => tag.Value;

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
        return $"{GetIndent(indentLevel)}{FormatPropertyName()}\"{EscapeString(Value)}\"";
    }
}