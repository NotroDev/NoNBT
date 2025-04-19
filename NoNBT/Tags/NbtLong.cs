namespace NoNBT.Tags;

public class NbtLong(string? name, long value) : NbtTag(name)
{
    public override NbtTagType TagType => NbtTagType.Long;
    
    public long Value { get; set; } = value;
    
    public NbtLong(long value) : this(null, value) { }

    public override NbtTag Clone()
    {
        return new NbtLong(Name, Value);
    }

    public static explicit operator long(NbtLong tag) => tag.Value;

    public override string ToString()
    {
        return $"{base.ToString()}: {Value}";
    }
}