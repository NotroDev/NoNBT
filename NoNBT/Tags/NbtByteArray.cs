namespace NoNBT.Tags;

public class NbtByteArray(string? name, byte[] value) : NbtTag(name)
{
    public override NbtTagType TagType => NbtTagType.ByteArray;
    
    public byte[] Value { get; set; } = value;
    
    public NbtByteArray(byte[] value) : this(null, value) { }

    public override NbtTag Clone()
    {
        return new NbtByteArray(Name, Value);
    }

    public static explicit operator byte[](NbtByteArray tag) => tag.Value;

    public override string ToString()
    {
        return $"{base.ToString()}: {Value}";
    }
    
    public override string ToJson(int indentLevel = 0)
    {
        return $"{GetIndent(indentLevel)}{FormatPropertyName()}[{string.Join(", ", Value.Select(b => b.ToString()))}]";
    }
}