namespace NoNBT;

public abstract class NbtTag(string? name)
{
    public string? Name { get; init; } = name;

    public abstract NbtTagType TagType { get; }
    
    public abstract NbtTag Clone();
    
    public override string ToString()
    {
        return $"[{TagType}] {Name ?? "''"}";
    }
}