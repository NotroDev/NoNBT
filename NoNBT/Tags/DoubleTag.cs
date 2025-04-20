namespace NoNBT.Tags;

/// <summary>
/// Represents an NBT tag containing a double-precision floating point value.
/// </summary>
/// <param name="name">The name of the tag.</param>
/// <param name="value">The double value.</param>
public class DoubleTag(string? name, double value) : NbtTag(name)
{
    /// <summary>
    /// Gets the type of this NBT tag.
    /// </summary>
    public override NbtTagType TagType => NbtTagType.Double;
    
    /// <summary>
    /// Gets or sets the double value of this tag.
    /// </summary>
    public double Value { get; set; } = value;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="DoubleTag"/> class with a null name.
    /// </summary>
    /// <param name="value">The double value.</param>
    public DoubleTag(double value) : this(null, value) { }

    /// <summary>
    /// Creates a copy of this tag.
    /// </summary>
    /// <returns>A new <see cref="DoubleTag"/> with the same name and value.</returns>
    public override NbtTag Clone()
    {
        return new DoubleTag(Name, Value);
    }

    /// <summary>
    /// Converts a <see cref="DoubleTag"/> to a double.
    /// </summary>
    /// <param name="tag">The tag to convert.</param>
    public static explicit operator double(DoubleTag tag) => tag.Value;

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
        return $"{GetIndent(indentLevel)}{FormatPropertyName()}{Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
    }
}