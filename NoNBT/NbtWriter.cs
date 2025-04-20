using System.Net;
using NoNBT.Tags;

namespace NoNBT;

public class NbtWriter(Stream stream) : IDisposable
{
    private readonly Stream _stream = stream ?? throw new ArgumentNullException(nameof(stream));
    private bool _disposed;

    public void WriteTag(NbtTag tag, bool named = true)
    {
        if (tag.TagType == NbtTagType.End)
            throw new ArgumentException("Cannot write TAG_End directly. It is written automatically at the end of a TAG_Compound.");

        _stream.WriteByte((byte)tag.TagType);

        if (named)
        {
            if (tag.Name == null)
                throw new ArgumentNullException(nameof(tag), "Tag name cannot be null when writing a named tag.");
            WriteString(tag.Name);
        }

        WriteTagPayload(tag);
    }

    private void WriteTagPayload(NbtTag tag)
    {
        switch (tag.TagType)
        {
            case NbtTagType.Byte:
                _stream.WriteByte(((NbtByte)tag).Value);
                break;
            case NbtTagType.Short:
                WriteShort(((NbtShort)tag).Value);
                break;
            case NbtTagType.Int:
                WriteInt(((NbtInt)tag).Value);
                break;
            case NbtTagType.Long:
                WriteLong(((NbtLong)tag).Value);
                break;
            case NbtTagType.Float:
                WriteFloat(((NbtFloat)tag).Value);
                break;
            case NbtTagType.Double:
                WriteDouble(((NbtDouble)tag).Value);
                break;
            case NbtTagType.ByteArray:
                WriteByteArrayPayload((NbtByteArray)tag);
                break;
            case NbtTagType.String:
                WriteString(((NbtString)tag).Value);
                break;
            case NbtTagType.List:
                WriteListPayload((NbtList)tag);
                break;
            case NbtTagType.Compound:
                WriteCompoundPayload((NbtCompound)tag);
                break;
            case NbtTagType.IntArray:
                WriteIntArrayPayload((NbtIntArray)tag);
                break;
            case NbtTagType.LongArray:
                WriteLongArrayPayload((NbtLongArray)tag);
                break;
            case NbtTagType.End:
                break;
            default:
                throw new NotImplementedException($"Unsupported tag type for writing: {tag.TagType}");
        }
    }

    private void WriteListPayload(NbtList tag)
    {
        _stream.WriteByte((byte)tag.ListType);
        WriteInt(tag.Count);

        foreach (NbtTag item in tag)
        {
            WriteTagPayload(item);
        }
    }

    private void WriteCompoundPayload(NbtCompound tag)
    {
        foreach (KeyValuePair<string, NbtTag> childTagPair in tag)
        {
            WriteTag(childTagPair.Value);
        }
        _stream.WriteByte((byte)NbtTagType.End);
    }

    private void WriteByteArrayPayload(NbtByteArray tag)
    {
        WriteInt(tag.Value.Length);
        Write(tag.Value);
    }

    private void WriteIntArrayPayload(NbtIntArray tag)
    {
        WriteInt(tag.Value.Length);
        foreach (int value in tag.Value)
        {
            WriteInt(value);
        }
    }

    private void WriteLongArrayPayload(NbtLongArray tag)
    {
        WriteInt(tag.Value.Length);
        foreach (long value in tag.Value)
        {
            WriteLong(value);
        }
    }

    public void WriteString(string value)
    {
        byte[] stringBytes = ModifiedUtf8.GetBytes(value);
        if (stringBytes.Length > short.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(value), $"String length in bytes ({stringBytes.Length}) exceeds maximum allowed ({short.MaxValue}).");

        WriteShort((short)stringBytes.Length);
        Write(stringBytes);
    }

    public void WriteInt(int value)
    {
        int networkValue = IPAddress.HostToNetworkOrder(value);
        byte[] bytes = BitConverter.GetBytes(networkValue);
        Write(bytes);
    }

    public void WriteFloat(float value)
    {
        byte[] bytes = BitConverter.GetBytes(HostToNetworkOrder(value));
        Write(bytes);
    }

    public void WriteDouble(double value)
    {
        byte[] bytes = HostToNetworkOrder(value);
        Write(bytes);
    }

    public void WriteShort(short value)
    {
        short networkValue = IPAddress.HostToNetworkOrder(value);
        byte[] bytes = BitConverter.GetBytes(networkValue);
        Write(bytes);
    }

    public void WriteLong(long value)
    {
        long networkValue = IPAddress.HostToNetworkOrder(value);
        byte[] bytes = BitConverter.GetBytes(networkValue);
        Write(bytes);
    }

    public void WriteByte(byte value)
    {
        _stream.WriteByte(value);
    }

    public void Write(byte[] data)
    {
        if (data.Length > 0)
        {
            _stream.Write(data, 0, data.Length);
        }
    }

    private static byte[] HostToNetworkOrder(double host)
    {
        byte[] bytes = BitConverter.GetBytes(host);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(bytes);
        return bytes;
    }

    private static float HostToNetworkOrder(float host)
    {
        byte[] bytes = BitConverter.GetBytes(host);
        if (BitConverter.IsLittleEndian)
            Array.Reverse(bytes);
        return BitConverter.ToSingle(bytes, 0);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected void Dispose(bool disposing)
    {
        if (_disposed) return;
        
        if (disposing)
        {
            stream?.Dispose(); 
        }
            
        _disposed = true;
    }
    
    ~NbtWriter()
    {
        Dispose(false);
    }
}