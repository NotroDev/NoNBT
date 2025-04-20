namespace NoNBT.Tags;

public class StringTag(string? name, string value) : NbtTag(name)
{
    public override NbtTagType TagType => NbtTagType.String;
    
    public string Value { get; set; } = value;
    
    public StringTag(string value) : this(null, value) { }

    public override NbtTag Clone()
    {
        return new StringTag(Name, Value);
    }

    public static explicit operator string(StringTag tag) => tag.Value;

    public override string ToString()
    {
        return $"{base.ToString()}: {Value}";
    }
    
    public override string ToJson(int indentLevel = 0)
    {
        return $"{GetIndent(indentLevel)}{FormatPropertyName()}\"{EscapeString(Value)}\"";
    }
}