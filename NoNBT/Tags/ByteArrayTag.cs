namespace NoNBT.Tags;

public class ByteArrayTag(string? name, byte[] value) : NbtTag(name)
{
    public override NbtTagType TagType => NbtTagType.ByteArray;
    
    public byte[] Value { get; set; } = value;
    
    public ByteArrayTag(byte[] value) : this(null, value) { }

    public override NbtTag Clone()
    {
        var clonedValue = new byte[Value.Length];
        Value.CopyTo(clonedValue, 0);
        return new ByteArrayTag(Name, clonedValue);
    }

    public static explicit operator byte[](ByteArrayTag tag) => tag.Value;

    public override string ToString()
    {
        return $"{base.ToString()}: {Value}";
    }
    
    public override string ToJson(int indentLevel = 0)
    {
        return $"{GetIndent(indentLevel)}{FormatPropertyName()}[{string.Join(", ", Value.Select(b => b.ToString()))}]";
    }
}