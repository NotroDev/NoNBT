namespace NoNBT.Tags;

public class NbtShort(string? name, short value) : NbtTag(name)
{
    public override NbtTagType TagType => NbtTagType.Short;
    
    public short Value { get; set; }
    
    public NbtShort(short value) : this(null, value) { }

    public override NbtTag Clone()
    {
        return new NbtShort(Name, Value);
    }

    public static explicit operator short(NbtShort tag) => tag.Value;

    public override string ToString()
    {
        return $"{base.ToString()}: {Value}";
    }
}