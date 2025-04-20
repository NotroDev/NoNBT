namespace NoNBT.Tags;

/// <summary>
/// Represents an NBT tag containing a single-precision floating point value.
/// </summary>
/// <param name="name">The name of the tag.</param>
/// <param name="value">The float value.</param>
public class FloatTag(string? name, float value) : NbtTag(name)
{
    /// <summary>
    /// Gets the type of this NBT tag.
    /// </summary>
    public override NbtTagType TagType => NbtTagType.Float;
    
    /// <summary>
    /// Gets or sets the float value of this tag.
    /// </summary>
    public float Value { get; set; } = value;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="FloatTag"/> class with a null name.
    /// </summary>
    /// <param name="value">The float value.</param>
    public FloatTag(float value) : this(null, value) { }

    /// <summary>
    /// Creates a copy of this tag.
    /// </summary>
    /// <returns>A new <see cref="FloatTag"/> with the same name and value.</returns>
    public override NbtTag Clone()
    {
        return new FloatTag(Name, Value);
    }

    /// <summary>
    /// Converts a <see cref="FloatTag"/> to a float.
    /// </summary>
    /// <param name="tag">The tag to convert.</param>
    public static explicit operator float(FloatTag tag) => tag.Value;

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