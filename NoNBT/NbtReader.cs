using System.Buffers;
using System.Net;
using NoNBT.Tags;

namespace NoNBT;

public class NbtReader(Stream stream) : IDisposable
{
    private readonly Stream _stream = stream ?? throw new ArgumentNullException(nameof(stream));
    private bool _disposed;

    public NbtTag? ReadTag(bool named = true)
    {
        var tagType = (NbtTagType)_stream.ReadByte();
        if (tagType == NbtTagType.End)
            return null;

        string? name = null;

        if (named)
        {
            name = ReadString();
        }
        
        NbtTag tag = tagType switch
        {
            NbtTagType.Byte => new NbtByte(name, (byte)_stream.ReadByte()),
            NbtTagType.Short => new NbtShort(name, ReadShort()),
            NbtTagType.Int => new NbtInt(name, ReadInt()),
            NbtTagType.Long => new NbtLong(name, ReadLong()),
            NbtTagType.Float => new NbtFloat(name, ReadFloat()),
            NbtTagType.Double => new NbtDouble(name, ReadDouble()),
            NbtTagType.ByteArray => new NbtByteArray(name, ReadByteArray()),
            NbtTagType.String => new NbtString(name, ReadString()),
            NbtTagType.List => ReadListTag(name),
            NbtTagType.Compound => ReadCompoundTag(name),
            NbtTagType.IntArray => new NbtIntArray(name, ReadIntArray()),
            NbtTagType.LongArray => new NbtLongArray(name, ReadLongArray()),
            NbtTagType.End => throw new InvalidOperationException("End tag should not be read"),
            _ => throw new NotImplementedException($"Unsupported tag type: {tagType}")
        };

        return tag;
    }

    private NbtList ReadListTag(string? name)
    {
        var listType = (NbtTagType)_stream.ReadByte();
        int count = ReadInt();
        var list = new NbtList(name, listType);

        for (var i = 0; i < count; i++)
        {
            switch (listType)
            {
                case NbtTagType.Byte:
                    list.Add(new NbtByte(null, (byte)_stream.ReadByte()));
                    break;
                case NbtTagType.Short:
                    list.Add(new NbtShort(null, ReadShort()));
                    break;
                case NbtTagType.Int:
                    list.Add(new NbtInt(null, ReadInt()));
                    break;
                case NbtTagType.Long:
                    list.Add(new NbtLong(null, ReadLong()));
                    break;
                case NbtTagType.Float:
                    list.Add(new NbtFloat(null, ReadFloat()));
                    break;
                case NbtTagType.Double:
                    list.Add(new NbtDouble(null, ReadDouble()));
                    break;
                case NbtTagType.ByteArray:
                    list.Add(new NbtByteArray(null, ReadByteArray()));
                    break;
                case NbtTagType.String:
                    list.Add(new NbtString(null, ReadString()));
                    break;
                case NbtTagType.List:
                    list.Add(ReadListTag(null));
                    break;
                case NbtTagType.Compound:
                    list.Add(ReadCompoundTag(null));
                    break;
                case NbtTagType.IntArray:
                    list.Add(new NbtIntArray(null, ReadIntArray()));
                    break;
                case NbtTagType.LongArray:
                    list.Add(new NbtLongArray(null, ReadLongArray()));
                    break;
                case NbtTagType.End:
                default:
                    throw new NotImplementedException($"Unsupported list element type: {listType}");
            }
        }

        return list;
    }

    private NbtCompound ReadCompoundTag(string? name)
    {
        var compound = new NbtCompound(name);
        
        while (true)
        {
            NbtTag? tag = ReadTag();
            if (tag == null)
                break;
                
            compound.Add(tag);
        }
        
        return compound;
    }

    private byte[] ReadByteArray()
    {
        int length = ReadInt();
        return Read(length);
    }

    private int[] ReadIntArray()
    {
        int length = ReadInt();
        var result = new int[length];
        
        for (var i = 0; i < length; i++)
        {
            result[i] = ReadInt();
        }
        
        return result;
    }

    private long[] ReadLongArray()
    {
        int length = ReadInt();
        var result = new long[length];
        
        for (var i = 0; i < length; i++)
        {
            result[i] = ReadLong();
        }
        
        return result;
    }
    
    public string ReadString()
    {
        short length = ReadShort();
        byte[] stringValue = Read(length);

        return ModifiedUtf8.GetString(stringValue);
    }
    
    public int ReadInt()
    {
        var dat = new byte[4];
        _stream.ReadExactly(dat, 0, 4);
        var value = BitConverter.ToInt32(dat, 0);
        return IPAddress.NetworkToHostOrder(value);
    }
    
    public float ReadFloat()
    {
        byte[] almost = Read(4);
        var f = BitConverter.ToSingle(almost, 0);
        return NetworkToHostOrder(f);
    }
    
    public double ReadDouble()
    {
        byte[] almostValue = Read(8);
        return NetworkToHostOrder(almostValue);
    }
    
    public short ReadShort()
    {
        byte[] da = Read(2);
        var d = BitConverter.ToInt16(da, 0);
        return IPAddress.NetworkToHostOrder(d);
    }
    
    public long ReadLong()
    {
        byte[] l = Read(8);
        return IPAddress.NetworkToHostOrder(BitConverter.ToInt64(l, 0));
    }
    
    public int ReadByte()
    {
        return _stream.ReadByte();
    }
    
    public byte[] Read(int length)
    {
        if (length == 0) return [];

        byte[] buffer = ArrayPool<byte>.Shared.Rent(length);
        try
        {
            var totalRead = 0;
            while (totalRead < length)
            {
                int bytesRead = _stream.Read(buffer, totalRead, length - totalRead);
                if (bytesRead <= 0) break;
                totalRead += bytesRead;
            }

            var result = new byte[totalRead];
            Buffer.BlockCopy(buffer, 0, result, 0, totalRead);
            return result;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static readonly ThreadLocal<byte[]> VarIntBuffer = new(() => new byte[5]);

    public int ReadVarInt(out int bytesRead)
    {
        var numRead = 0;
        var result = 0;
        byte[] buffer = VarIntBuffer.Value!;

        do
        {
            if (numRead >= 5) throw new Exception("VarInt is too big");

            buffer[numRead] = (byte)ReadByte();
            int value = buffer[numRead] & 0x7f;
            result |= value << (7 * numRead);

            numRead++;
        } while ((buffer[numRead - 1] & 0x80) != 0);

        bytesRead = numRead;
        return result;
    }
    
    public int ReadVarInt()
    {
        return ReadVarInt(out int _);
    }
    
    private static double NetworkToHostOrder(byte[] data)
    {
        if (BitConverter.IsLittleEndian) Array.Reverse(data);
        return BitConverter.ToDouble(data, 0);
    }

    private static float NetworkToHostOrder(float network)
    {
        byte[] bytes = BitConverter.GetBytes(network);

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
            _stream?.Dispose(); 
        }
            
        _disposed = true;
    }
    
    ~NbtReader()
    {
        Dispose(false);
    }
}