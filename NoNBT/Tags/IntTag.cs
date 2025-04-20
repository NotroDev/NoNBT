namespace NoNBT.Tags;

public class NbtInt(string? name, int value) : NbtTag(name)
{
    public override NbtTagType TagType => NbtTagType.Int;
    
    public int Value { get; set; } = value;
    
    public NbtInt(int value) : this(null, value) { }

    public override NbtTag Clone()
    {
        return new NbtInt(Name, Value);
    }

    public static explicit operator int(NbtInt tag) => tag.Value;

    public override string ToString()
    {
        return $"{base.ToString()}: {Value}";
    }
    
    public override string ToJson(int indentLevel = 0)
    {
        return $"{GetIndent(indentLevel)}{FormatPropertyName()}{Value}";
    }
}