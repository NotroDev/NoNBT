namespace NoNBT.Tags;

public class NbtByte(string? name, byte value) : NbtTag(name)
{
    public override NbtTagType TagType => NbtTagType.Byte;
    
    public byte Value { get; set; } = value;

    public bool BoolValue
    {
        get => Value == 1;
        set => Value = (byte)(value ? 1 : 0);
    }
    
    public NbtByte(byte value) : this(null, value) { }
    public NbtByte(string? name, bool value) : this(name, (byte)(value ? 1 : 0)) { }
    public NbtByte(bool value) : this(null, value) { }
    
    public override NbtTag Clone()
    {
        return new NbtByte(Name, Value);
    }
    
    public static explicit operator byte(NbtByte tag) => tag.Value;
    public static explicit operator bool(NbtByte tag) => tag.BoolValue;
    
    public override string ToString()
    {
        return $"{base.ToString()}: {Value}";
    }
    
    public override string ToJson(int indentLevel = 0)
    {
        return $"{GetIndent(indentLevel)}{FormatPropertyName()}{Value}";
    }
}