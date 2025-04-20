using System.Buffers;
using System.Net;
using NoNBT.Tags;

namespace NoNBT;

public class NbtReader(Stream stream, bool leaveOpen = false) : IDisposable, IAsyncDisposable
{
    private readonly Stream _stream = stream ?? throw new ArgumentNullException(nameof(stream));
    private bool _disposed;

    private const int MaxVarIntSize = 5;
    

    public NbtTag? ReadTag(bool named = true)
    {
        CheckDisposed();
        int tagTypeByte = ReadByte();
        if (tagTypeByte == -1) throw new EndOfStreamException("Unexpected end of stream while reading tag type.");
        var tagType = (NbtTagType)tagTypeByte;
        if (tagType == NbtTagType.End) return null;

        string? name = named ? ReadString() : null;

        NbtTag tag = tagType switch
        {
            NbtTagType.Byte => new ByteTag(name, (byte)ReadByteChecked()),
            NbtTagType.Short => new ShortTag(name, ReadShort()),
            NbtTagType.Int => new IntTag(name, ReadInt()),
            NbtTagType.Long => new LongTag(name, ReadLong()),
            NbtTagType.Float => new FloatTag(name, ReadFloat()),
            NbtTagType.Double => new DoubleTag(name, ReadDouble()),
            NbtTagType.ByteArray => new ByteArrayTag(name, ReadByteArray()),
            NbtTagType.String => new StringTag(name, ReadString()),
            NbtTagType.List => ReadListTag(name),
            NbtTagType.Compound => ReadCompoundTag(name),
            NbtTagType.IntArray => new IntArrayTag(name, ReadIntArray()),
            NbtTagType.LongArray => new LongArrayTag(name, ReadLongArray()),
            NbtTagType.End => throw new IOException("Unexpected TAG_End while reading tag."),
            _ => throw new IOException($"Unsupported tag type: {tagType}")
        };

        return tag;
    }

    private ListTag ReadListTag(string? name)
    {
        CheckDisposed();
        var listType = (NbtTagType)ReadByteChecked();
        int count = ReadInt();
        if (count < 0) throw new IOException($"Invalid list count: {count}");

        var list = new ListTag(name, listType);
        if (count == 0) return list;

        for (var i = 0; i < count; i++)
        {
            NbtTag element = listType switch
            {
                NbtTagType.Byte => new ByteTag(null, (byte)ReadByteChecked()),
                NbtTagType.Short => new ShortTag(null, ReadShort()),
                NbtTagType.Int => new IntTag(null, ReadInt()),
                NbtTagType.Long => new LongTag(null, ReadLong()),
                NbtTagType.Float => new FloatTag(null, ReadFloat()),
                NbtTagType.Double => new DoubleTag(null, ReadDouble()),
                NbtTagType.ByteArray => new ByteArrayTag(null, ReadByteArray()),
                NbtTagType.String => new StringTag(null, ReadString()),
                NbtTagType.List => ReadListTag(null),
                NbtTagType.Compound => ReadCompoundTag(null),
                NbtTagType.IntArray => new IntArrayTag(null, ReadIntArray()),
                NbtTagType.LongArray => new LongArrayTag(null, ReadLongArray()),
                NbtTagType.End => throw new IOException("Empty list element type (TAG_End) is not allowed."),
                _ => throw new IOException($"Unsupported list element type: {listType}")
            };
            list.Add(element);
        }
        return list;
    }

    private CompoundTag ReadCompoundTag(string? name)
    {
        CheckDisposed();
        var compound = new CompoundTag(name);
        while (true)
        {
            NbtTag? tag = ReadTag();
            if (tag == null) break;
            compound.Add(tag);
        }
        return compound;
    }

    private byte[] ReadByteArray() => Read(ReadIntCheckedLength());
    private int[] ReadIntArray()
    {
        int length = ReadIntCheckedLength();
        var result = new int[length];
        for (var i = 0; i < length; i++) result[i] = ReadInt();
        return result;
    }

    private long[] ReadLongArray()
    {
        int length = ReadIntCheckedLength();
        var result = new long[length];
        for (var i = 0; i < length; i++) result[i] = ReadLong();
        return result;
    }

    public string ReadString()
    {
        CheckDisposed();
        short length = ReadShort();
        if (length < 0) throw new IOException($"Invalid string length: {length}");
        byte[] stringValue = Read(length);
        return ModifiedUtf8.GetString(stringValue);
    }

    public int ReadInt()
    {
        CheckDisposed();
        return IPAddress.NetworkToHostOrder(BitConverter.ToInt32(Read(sizeof(int)), 0));
    }
    public float ReadFloat()
    {
        CheckDisposed();
        return NetworkToHostOrder(BitConverter.ToSingle(Read(sizeof(float)), 0));
    }
    public double ReadDouble()
    {
        CheckDisposed();
        return NetworkToHostOrder(Read(sizeof(double)));
    }
    public short ReadShort()
    {
        CheckDisposed();
        return IPAddress.NetworkToHostOrder(BitConverter.ToInt16(Read(sizeof(short)), 0));
    }
    public long ReadLong()
    {
        CheckDisposed();
        return IPAddress.NetworkToHostOrder(BitConverter.ToInt64(Read(sizeof(long)), 0));
    }
    public int ReadByte()
    {
        CheckDisposed();
        return _stream.ReadByte();
    }

    public byte[] Read(int length)
    {
        CheckDisposed();
        switch (length)
        {
            case < 0:
                throw new ArgumentOutOfRangeException(nameof(length), "Length cannot be negative.");
            case 0:
                return [];
        }

        var buffer = new byte[length];
        var totalRead = 0;
        while (totalRead < length)
        {
            int bytesRead = _stream.Read(buffer, totalRead, length - totalRead);
            if (bytesRead == 0) throw new EndOfStreamException($"Expected {length} bytes, but stream ended after {totalRead} bytes.");
            totalRead += bytesRead;
        }
        return buffer;
    }

    public int ReadVarInt(out int bytesRead)
    {
        CheckDisposed();
        var numRead = 0;
        var result = 0;
        byte readByte;
        do
        {
            if (numRead >= MaxVarIntSize) throw new IOException("VarInt is too big");
            int b = ReadByte();
            if (b == -1) throw new EndOfStreamException("Stream ended while reading VarInt.");
            readByte = (byte)b;

            int value = readByte & 0x7f;
            result |= value << (7 * numRead);
            numRead++;
        } while ((readByte & 0x80) != 0);

        bytesRead = numRead;
        return result;
    }
    public int ReadVarInt() => ReadVarInt(out int _);
    

    public async ValueTask<NbtTag?> ReadTagAsync(bool named = true, CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        byte tagTypeByte = await ReadByteCheckedAsync(cancellationToken).ConfigureAwait(false);
        var tagType = (NbtTagType)tagTypeByte;
        if (tagType == NbtTagType.End) return null;

        string? name = named ? await ReadStringAsync(cancellationToken).ConfigureAwait(false) : null;

        NbtTag tag = tagType switch
        {
            NbtTagType.Byte => new ByteTag(name, await ReadByteCheckedAsync(cancellationToken).ConfigureAwait(false)),
            NbtTagType.Short => new ShortTag(name, await ReadShortAsync(cancellationToken).ConfigureAwait(false)),
            NbtTagType.Int => new IntTag(name, await ReadIntAsync(cancellationToken).ConfigureAwait(false)),
            NbtTagType.Long => new LongTag(name, await ReadLongAsync(cancellationToken).ConfigureAwait(false)),
            NbtTagType.Float => new FloatTag(name, await ReadFloatAsync(cancellationToken).ConfigureAwait(false)),
            NbtTagType.Double => new DoubleTag(name, await ReadDoubleAsync(cancellationToken).ConfigureAwait(false)),
            NbtTagType.ByteArray => new ByteArrayTag(name, await ReadByteArrayAsync(cancellationToken).ConfigureAwait(false)),
            NbtTagType.String => new StringTag(name, await ReadStringAsync(cancellationToken).ConfigureAwait(false)),
            NbtTagType.List => await ReadListTagAsync(name, cancellationToken).ConfigureAwait(false),
            NbtTagType.Compound => await ReadCompoundTagAsync(name, cancellationToken).ConfigureAwait(false),
            NbtTagType.IntArray => new IntArrayTag(name, await ReadIntArrayAsync(cancellationToken).ConfigureAwait(false)),
            NbtTagType.LongArray => new LongArrayTag(name, await ReadLongArrayAsync(cancellationToken).ConfigureAwait(false)),
            NbtTagType.End => throw new IOException("Unexpected TAG_End while reading tag."),
            _ => throw new IOException($"Unsupported tag type: {tagType}")
        };
        return tag;
    }

    private async ValueTask<ListTag> ReadListTagAsync(string? name, CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        var listType = (NbtTagType)await ReadByteCheckedAsync(cancellationToken).ConfigureAwait(false);
        int count = await ReadIntAsync(cancellationToken).ConfigureAwait(false);
        if (count < 0) throw new IOException($"Invalid list count: {count}");

        var list = new ListTag(name, listType);
        if (count == 0) return list;

        for (var i = 0; i < count; i++)
        {
            NbtTag element = listType switch
            {
                NbtTagType.Byte => new ByteTag(null, await ReadByteCheckedAsync(cancellationToken).ConfigureAwait(false)),
                NbtTagType.Short => new ShortTag(null, await ReadShortAsync(cancellationToken).ConfigureAwait(false)),
                NbtTagType.Int => new IntTag(null, await ReadIntAsync(cancellationToken).ConfigureAwait(false)),
                NbtTagType.Long => new LongTag(null, await ReadLongAsync(cancellationToken).ConfigureAwait(false)),
                NbtTagType.Float => new FloatTag(null, await ReadFloatAsync(cancellationToken).ConfigureAwait(false)),
                NbtTagType.Double => new DoubleTag(null, await ReadDoubleAsync(cancellationToken).ConfigureAwait(false)),
                NbtTagType.ByteArray => new ByteArrayTag(null, await ReadByteArrayAsync(cancellationToken).ConfigureAwait(false)),
                NbtTagType.String => new StringTag(null, await ReadStringAsync(cancellationToken).ConfigureAwait(false)),
                NbtTagType.List => await ReadListTagAsync(null, cancellationToken).ConfigureAwait(false),
                NbtTagType.Compound => await ReadCompoundTagAsync(null, cancellationToken).ConfigureAwait(false),
                NbtTagType.IntArray => new IntArrayTag(null, await ReadIntArrayAsync(cancellationToken).ConfigureAwait(false)),
                NbtTagType.LongArray => new LongArrayTag(null, await ReadLongArrayAsync(cancellationToken).ConfigureAwait(false)),
                NbtTagType.End => throw new IOException("Empty list element type (TAG_End) is not allowed."),
                _ => throw new IOException($"Unsupported list element type: {listType}")
            };
            list.Add(element);
        }
        return list;
    }

    private async ValueTask<CompoundTag> ReadCompoundTagAsync(string? name, CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        var compound = new CompoundTag(name);
        while (true)
        {
            NbtTag? tag = await ReadTagAsync(true, cancellationToken).ConfigureAwait(false);
            if (tag == null) break;
            compound.Add(tag);
        }
        return compound;
    }

    private async ValueTask<byte[]> ReadByteArrayAsync(CancellationToken cancellationToken = default) => await ReadAsync(await ReadIntCheckedLengthAsync(cancellationToken).ConfigureAwait(false), cancellationToken).ConfigureAwait(false);

    private async ValueTask<int[]> ReadIntArrayAsync(CancellationToken cancellationToken = default)
    {
        int length = await ReadIntCheckedLengthAsync(cancellationToken).ConfigureAwait(false);
        var result = new int[length];
        for (var i = 0; i < length; i++) result[i] = await ReadIntAsync(cancellationToken).ConfigureAwait(false);
        return result;
    }

    private async ValueTask<long[]> ReadLongArrayAsync(CancellationToken cancellationToken = default)
    {
        int length = await ReadIntCheckedLengthAsync(cancellationToken).ConfigureAwait(false);
        var result = new long[length];
        for (var i = 0; i < length; i++) result[i] = await ReadLongAsync(cancellationToken).ConfigureAwait(false);
        return result;
    }

    public async ValueTask<string> ReadStringAsync(CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        short length = await ReadShortAsync(cancellationToken).ConfigureAwait(false);
        if (length < 0) throw new IOException($"Invalid string length: {length}");
        byte[] stringValue = await ReadAsync(length, cancellationToken).ConfigureAwait(false);
        return ModifiedUtf8.GetString(stringValue);
    }

    public async ValueTask<int> ReadIntAsync(CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        byte[] buffer = await ReadAsync(sizeof(int), cancellationToken).ConfigureAwait(false);
        return IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buffer, 0));
    }

    public async ValueTask<float> ReadFloatAsync(CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        byte[] buffer = await ReadAsync(sizeof(float), cancellationToken).ConfigureAwait(false);
        return NetworkToHostOrder(BitConverter.ToSingle(buffer, 0));
    }

    public async ValueTask<double> ReadDoubleAsync(CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        byte[] buffer = await ReadAsync(sizeof(double), cancellationToken).ConfigureAwait(false);
        return NetworkToHostOrder(buffer);
    }

    public async ValueTask<short> ReadShortAsync(CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        byte[] buffer = await ReadAsync(sizeof(short), cancellationToken).ConfigureAwait(false);
        return IPAddress.NetworkToHostOrder(BitConverter.ToInt16(buffer, 0));
    }

    public async ValueTask<long> ReadLongAsync(CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        byte[] buffer = await ReadAsync(sizeof(long), cancellationToken).ConfigureAwait(false);
        return IPAddress.NetworkToHostOrder(BitConverter.ToInt64(buffer, 0));
    }

    public async ValueTask<int> ReadVarIntAsync(CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        (_, int value) = await ReadVarIntWithBytesReadAsync(cancellationToken).ConfigureAwait(false);
        return value;
    }

    public async ValueTask<(int BytesRead, int Value)> ReadVarIntWithBytesReadAsync(CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        var numRead = 0;
        var result = 0;
        byte[] singleByteBuffer = ArrayPool<byte>.Shared.Rent(1);
        try
        {
            byte readByte;
            do
            {
                if (numRead >= MaxVarIntSize) throw new IOException("VarInt is too big");

                int bytesReadFromStream = await _stream.ReadAsync(singleByteBuffer.AsMemory(0, 1), cancellationToken).ConfigureAwait(false);
                if (bytesReadFromStream == 0) throw new EndOfStreamException("Stream ended while reading VarInt.");
                readByte = singleByteBuffer[0];

                int value = readByte & 0x7f;
                result |= value << (7 * numRead);
                numRead++;
            } while ((readByte & 0x80) != 0);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(singleByteBuffer);
        }
        return (numRead, result);
    }

    public async ValueTask<byte[]> ReadAsync(int length, CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        switch (length)
        {
            case < 0:
                throw new ArgumentOutOfRangeException(nameof(length), "Length cannot be negative.");
            case 0:
                return [];
        }

        var buffer = new byte[length];
        await _stream.ReadExactlyAsync(buffer, cancellationToken).ConfigureAwait(false);
        return buffer;
    }

    public async ValueTask<byte> ReadByteCheckedAsync(CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        byte[] buffer = ArrayPool<byte>.Shared.Rent(1);
        try
        {
            await _stream.ReadExactlyAsync(buffer.AsMemory(0,1), cancellationToken).ConfigureAwait(false);
            return buffer[0];
        }
        catch (EndOfStreamException)
        {
             throw new EndOfStreamException("Unexpected end of stream while reading required byte.");
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
    

    private void CheckDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private int ReadIntCheckedLength()
    {
        int length = ReadInt();
        if (length < 0) throw new IOException($"Invalid array/byte array length: {length}");
        return length;
    }

    private async ValueTask<int> ReadIntCheckedLengthAsync(CancellationToken cancellationToken = default)
    {
        int length = await ReadIntAsync(cancellationToken).ConfigureAwait(false);
        if (length < 0) throw new IOException($"Invalid array/byte array length: {length}");
        return length;
    }

    private byte ReadByteChecked()
    {
        int b = ReadByte();
        if (b == -1) throw new EndOfStreamException("Unexpected end of stream while reading required byte.");
        return (byte)b;
    }

    private static double NetworkToHostOrder(byte[] data)
    {
        if (BitConverter.IsLittleEndian) Array.Reverse(data);
        return BitConverter.ToDouble(data, 0);
    }

    private static float NetworkToHostOrder(float network)
    {
        byte[] bytes = BitConverter.GetBytes(network);
        if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
        return BitConverter.ToSingle(bytes, 0);
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore().ConfigureAwait(false);
        Dispose(false);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (disposing)
        {
            if (!leaveOpen)
            {
                _stream?.Dispose();
            }
        }
        _disposed = true;
    }

    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (_disposed) return;
        if (!leaveOpen && _stream != null)
        {
            await _stream.DisposeAsync().ConfigureAwait(false);
        }
    }

    ~NbtReader() => Dispose(false);
}