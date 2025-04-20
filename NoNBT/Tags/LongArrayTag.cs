namespace NoNBT.Tags;

public class LongArrayTag(string? name, long[] value) : NbtTag(name)
{
    public override NbtTagType TagType => NbtTagType.LongArray;
    
    public long[] Value { get; set; } = value;
    
    public LongArrayTag(long[] value) : this(null, value) { }

    public override NbtTag Clone()
    {
        var clonedValue = new long[Value.Length];
        Value.CopyTo(clonedValue, 0);
        return new LongArrayTag(Name, clonedValue);
    }

    public static explicit operator long[](LongArrayTag tag) => tag.Value;

    public override string ToString()
    {
        return $"{base.ToString()}: {Value}";
    }
    
    public override string ToJson(int indentLevel = 0)
    {
        return $"{GetIndent(indentLevel)}{FormatPropertyName()}[{string.Join(", ", Value.Select(l => l.ToString()))}]";
    }
}