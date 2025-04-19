namespace NoNBT.Tags;

public class NbtDouble(string? name, double value) : NbtTag(name)
{
    public override NbtTagType TagType => NbtTagType.Double;
    
    public double Value { get; set; } = value;
    
    public NbtDouble(double value) : this(null, value) { }

    public override NbtTag Clone()
    {
        return new NbtDouble(Name, Value);
    }

    public static explicit operator double(NbtDouble tag) => tag.Value;

    public override string ToString()
    {
        return $"{base.ToString()}: {Value}";
    }
    
    public override string ToJson(int indentLevel = 0)
    {
        return $"{GetIndent(indentLevel)}{FormatPropertyName()}{Value.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
    }
}