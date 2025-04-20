namespace NoNBT.Tags;

public class IntArrayTag(string? name, int[] value) : NbtTag(name)
{
    public override NbtTagType TagType => NbtTagType.IntArray;
    
    public int[] Value { get; set; } = value;
    
    public IntArrayTag(int[] value) : this(null, value) { }

    public override NbtTag Clone()
    {
        return new IntArrayTag(Name, Value);
    }

    public static explicit operator int[](IntArrayTag tag) => tag.Value;

    public override string ToString()
    {
        return $"{base.ToString()}: {Value}";
    }
    
    public override string ToJson(int indentLevel = 0)
    {
        return $"{GetIndent(indentLevel)}{FormatPropertyName()}[{string.Join(", ", Value.Select(i => i.ToString()))}]";
    }
}