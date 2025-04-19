namespace NoNBT.Tags;

public class NbtIntArray(string? name, int[] value) : NbtTag(name)
{
    public override NbtTagType TagType => NbtTagType.IntArray;
    
    public int[] Value { get; set; } = value;
    
    public NbtIntArray(int[] value) : this(null, value) { }

    public override NbtTag Clone()
    {
        return new NbtIntArray(Name, Value);
    }

    public static explicit operator int[](NbtIntArray tag) => tag.Value;

    public override string ToString()
    {
        return $"{base.ToString()}: {Value}";
    }
}