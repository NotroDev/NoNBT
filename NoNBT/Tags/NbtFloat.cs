namespace NoNBT.Tags;

public class NbtFloat(string? name, float value) : NbtTag(name)
{
    public override NbtTagType TagType => NbtTagType.Float;
    
    public float Value { get; set; } = value;
    
    public NbtFloat(float value) : this(null, value) { }

    public override NbtTag Clone()
    {
        return new NbtFloat(Name, Value);
    }

    public static explicit operator float(NbtFloat tag) => tag.Value;

    public override string ToString()
    {
        return $"{base.ToString()}: {Value}";
    }
}