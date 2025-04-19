namespace NoNBT.Tags;

public class NbtLongArray(string? name, long[] value) : NbtTag(name)
{
    public override NbtTagType TagType => NbtTagType.LongArray;
    
    public long[] Value { get; set; } = value;
    
    public NbtLongArray(long[] value) : this(null, value) { }

    public override NbtTag Clone()
    {
        return new NbtLongArray(Name, Value);
    }

    public static explicit operator long[](NbtLongArray tag) => tag.Value;

    public override string ToString()
    {
        return $"{base.ToString()}: {Value}";
    }
    
    public override string ToJson(int indentLevel = 0)
    {
        return $"{GetIndent(indentLevel)}{FormatPropertyName()}[{string.Join(", ", Value.Select(l => l.ToString()))}]";
    }
}