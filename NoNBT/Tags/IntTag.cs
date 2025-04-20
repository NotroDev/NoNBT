namespace NoNBT.Tags;

/// <summary>
/// Represents an NBT tag containing a 32-bit signed integer value.
/// </summary>
/// <param name="name">The name of the tag.</param>
/// <param name="value">The integer value.</param>
public class IntTag(string? name, int value) : NbtTag(name)
{
    /// <summary>
    /// Gets the type of this NBT tag.
    /// </summary>
    public override NbtTagType TagType => NbtTagType.Int;
    
    /// <summary>
    /// Gets or sets the integer value of this tag.
    /// </summary>
    public int Value { get; set; } = value;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="IntTag"/> class with a null name.
    /// </summary>
    /// <param name="value">The integer value.</param>
    public IntTag(int value) : this(null, value) { }

    /// <summary>
    /// Creates a copy of this tag.
    /// </summary>
    /// <returns>A new <see cref="IntTag"/> with the same name and value.</returns>
    public override NbtTag Clone()
    {
        return new IntTag(Name, Value);
    }

    /// <summary>
    /// Converts an <see cref="IntTag"/> to an integer.
    /// </summary>
    /// <param name="tag">The tag to convert.</param>
    public static explicit operator int(IntTag tag) => tag.Value;

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