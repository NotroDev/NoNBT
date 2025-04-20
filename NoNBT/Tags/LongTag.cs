namespace NoNBT.Tags;

public class LongTag(string? name, long value) : NbtTag(name)
{
    public override NbtTagType TagType => NbtTagType.Long;
    
    public long Value { get; set; } = value;
    
    public LongTag(long value) : this(null, value) { }

    public override NbtTag Clone()
    {
        return new LongTag(Name, Value);
    }

    public static explicit operator long(LongTag tag) => tag.Value;

    public override string ToString()
    {
        return $"{base.ToString()}: {Value}";
    }
    
    public override string ToJson(int indentLevel = 0)
    {
        return $"{GetIndent(indentLevel)}{FormatPropertyName()}{Value}";
    }
}