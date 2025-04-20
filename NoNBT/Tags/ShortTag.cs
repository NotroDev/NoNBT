namespace NoNBT.Tags;

public class ShortTag(string? name, short value) : NbtTag(name)
{
    public override NbtTagType TagType => NbtTagType.Short;
    
    public short Value { get; set; } = value;
    
    public ShortTag(short value) : this(null, value) { }

    public override NbtTag Clone()
    {
        return new ShortTag(Name, Value);
    }

    public static explicit operator short(ShortTag tag) => tag.Value;

    public override string ToString()
    {
        return $"{base.ToString()}: {Value}";
    }
    
    public override string ToJson(int indentLevel = 0)
    {
        return $"{GetIndent(indentLevel)}{FormatPropertyName()}{Value}";
    }
}