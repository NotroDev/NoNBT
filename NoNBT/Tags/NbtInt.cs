namespace NoNBT.Tags;

public class NbtInt(string? name, int value) : NbtTag(name)
{
    public override NbtTagType TagType => NbtTagType.Long;
    
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
}