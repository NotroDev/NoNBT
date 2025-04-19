namespace NoNBT.Tags;

public class NbtString(string? name, string value) : NbtTag(name)
{
    public override NbtTagType TagType => NbtTagType.String;
    
    public string Value { get; set; } = value;
    
    public NbtString(string value) : this(null, value) { }

    public override NbtTag Clone()
    {
        return new NbtString(Name, Value);
    }

    public static explicit operator string(NbtString tag) => tag.Value;

    public override string ToString()
    {
        return $"{base.ToString()}: {Value}";
    }
}