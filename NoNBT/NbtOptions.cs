namespace NoNBT;

public class NbtOptions
{
    public NbtCompression Compression { get; init; } = NbtCompression.AutoDetect;
    public bool BigEndian { get; init; } = true;
    public bool NetworkRoot { get; init; } = false;
    public bool LeaveStreamOpen { get; init; } = false;
}