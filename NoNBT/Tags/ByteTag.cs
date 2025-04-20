namespace NoNBT.Tags;

/// <summary>
/// Represents an NBT tag containing a single byte value, which can also be used as a boolean.
/// </summary>
/// <param name="name">The name of the tag.</param>
/// <param name="value">The byte value.</param>
public class ByteTag(string? name, byte value) : NbtTag(name)
{
    /// <summary>
    /// Gets the type of this NBT tag.
    /// </summary>
    public override NbtTagType TagType => NbtTagType.Byte;
    
    /// <summary>
    /// Gets or sets the byte value of this tag.
    /// </summary>
    public byte Value { get; set; } = value;

    /// <summary>
    /// Gets or sets the value of this tag as a boolean, where 1 is true and 0 is false.
    /// </summary>
    public bool BoolValue
    {
        get => Value == 1;
        set => Value = (byte)(value ? 1 : 0);
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ByteTag"/> class with a null name.
    /// </summary>
    /// <param name="value">The byte value.</param>
    public ByteTag(byte value) : this(null, value) { }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ByteTag"/> class with a boolean value.
    /// </summary>
    /// <param name="name">The name of the tag.</param>
    /// <param name="value">The boolean value to store (1 for true, 0 for false).</param>
    public ByteTag(string? name, bool value) : this(name, (byte)(value ? 1 : 0)) { }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ByteTag"/> class with a null name and a boolean value.
    /// </summary>
    /// <param name="value">The boolean value to store (1 for true, 0 for false).</param>
    public ByteTag(bool value) : this(null, value) { }
    
    /// <summary>
    /// Creates a copy of this tag.
    /// </summary>
    /// <returns>A new <see cref="ByteTag"/> with the same name and value.</returns>
    public override NbtTag Clone()
    {
        return new ByteTag(Name, Value);
    }
    
    /// <summary>
    /// Converts a <see cref="ByteTag"/> to a byte.
    /// </summary>
    /// <param name="tag">The tag to convert.</param>
    public static explicit operator byte(ByteTag tag) => tag.Value;
    
    /// <summary>
    /// Converts a <see cref="ByteTag"/> to a boolean.
    /// </summary>
    /// <param name="tag">The tag to convert.</param>
    public static explicit operator bool(ByteTag tag) => tag.BoolValue;
    
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