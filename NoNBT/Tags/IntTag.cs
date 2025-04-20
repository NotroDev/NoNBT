namespace NoNBT.Tags;

public class IntTag(string? name, int value) : NbtTag(name)
{
    public override NbtTagType TagType => NbtTagType.Int;
    
    public int Value { get; set; } = value;
    
    public IntTag(int value) : this(null, value) { }

    public override NbtTag Clone()
    {
        return new IntTag(Name, Value);
    }

    public static explicit operator int(IntTag tag) => tag.Value;

    public override string ToString()
    {
        return $"{base.ToString()}: {Value}";
    }
    
    public override string ToJson(int indentLevel = 0)
    {
        return $"{GetIndent(indentLevel)}{FormatPropertyName()}{Value}";
    }
}