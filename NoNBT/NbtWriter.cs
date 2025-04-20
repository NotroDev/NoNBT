using System.Buffers;
using System.Net;
using NoNBT.Tags;

namespace NoNBT;

public class NbtWriter(Stream stream, bool leaveOpen = false) : IDisposable, IAsyncDisposable
{
    private readonly Stream _stream = stream ?? throw new ArgumentNullException(nameof(stream));
    private bool _disposed;
    
    public void WriteTag(NbtTag tag, bool named = true)
    {
        CheckDisposed();
        if (tag.TagType == NbtTagType.End)
            throw new ArgumentException("Cannot write TAG_End directly. It is written automatically at the end of a TAG_Compound.");

        WriteByte((byte)tag.TagType);

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
        CheckDisposed();
        switch (tag.TagType)
        {
            case NbtTagType.Byte: WriteByte(((ByteTag)tag).Value); break;
            case NbtTagType.Short: WriteShort(((ShortTag)tag).Value); break;
            case NbtTagType.Int: WriteInt(((IntTag)tag).Value); break;
            case NbtTagType.Long: WriteLong(((LongTag)tag).Value); break;
            case NbtTagType.Float: WriteFloat(((FloatTag)tag).Value); break;
            case NbtTagType.Double: WriteDouble(((DoubleTag)tag).Value); break;
            case NbtTagType.ByteArray: WriteByteArrayPayload((ByteArrayTag)tag); break;
            case NbtTagType.String: WriteString(((StringTag)tag).Value); break;
            case NbtTagType.List: WriteListPayload((ListTag)tag); break;
            case NbtTagType.Compound: WriteCompoundPayload((CompoundTag)tag); break;
            case NbtTagType.IntArray: WriteIntArrayPayload((IntArrayTag)tag); break;
            case NbtTagType.LongArray: WriteLongArrayPayload((LongArrayTag)tag); break;
            case NbtTagType.End: break;
            default: throw new NotImplementedException($"Unsupported tag type for writing: {tag.TagType}");
        }
    }

    private void WriteListPayload(ListTag tag)
    {
        WriteByte((byte)tag.ListType);
        WriteInt(tag.Count);
        foreach (NbtTag item in tag) WriteTagPayload(item);
    }

    private void WriteCompoundPayload(CompoundTag tag)
    {
        foreach (KeyValuePair<string, NbtTag> childTagPair in tag) WriteTag(childTagPair.Value);
        WriteByte((byte)NbtTagType.End);
    }

    private void WriteByteArrayPayload(ByteArrayTag tag)
    {
        WriteInt(tag.Value.Length);
        Write(tag.Value);
    }

    private void WriteIntArrayPayload(IntArrayTag tag)
    {
        WriteInt(tag.Value.Length);
        foreach (int value in tag.Value) WriteInt(value);
    }

    private void WriteLongArrayPayload(LongArrayTag tag)
    {
        WriteInt(tag.Value.Length);
        foreach (long value in tag.Value) WriteLong(value);
    }

    public void WriteString(string value)
    {
        CheckDisposed();
        byte[] stringBytes = ModifiedUtf8.GetBytes(value);
        if (stringBytes.Length > short.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(value), $"String length in bytes ({stringBytes.Length}) exceeds maximum allowed ({short.MaxValue}).");
        WriteShort((short)stringBytes.Length);
        Write(stringBytes);
    }

    public void WriteInt(int value)
    {
        CheckDisposed();
        int networkValue = IPAddress.HostToNetworkOrder(value);
        byte[] bytes = BitConverter.GetBytes(networkValue);
        Write(bytes);
    }

    public void WriteFloat(float value)
    {
        CheckDisposed();
        byte[] bytes = BitConverter.GetBytes(HostToNetworkOrder(value));
        Write(bytes);
    }

    public void WriteDouble(double value)
    {
        CheckDisposed();
        byte[] bytes = HostToNetworkOrder(value);
        Write(bytes);
    }

    public void WriteShort(short value)
    {
        CheckDisposed();
        short networkValue = IPAddress.HostToNetworkOrder(value);
        byte[] bytes = BitConverter.GetBytes(networkValue);
        Write(bytes);
    }

    public void WriteLong(long value)
    {
        CheckDisposed();
        long networkValue = IPAddress.HostToNetworkOrder(value);
        byte[] bytes = BitConverter.GetBytes(networkValue);
        Write(bytes);
    }

    public void WriteByte(byte value)
    {
        CheckDisposed();
        _stream.WriteByte(value);
    }

    public void Write(byte[] data)
    {
        CheckDisposed();
        if (data.Length > 0) _stream.Write(data, 0, data.Length);
    }
    
    public async ValueTask WriteTagAsync(NbtTag tag, bool named = true, CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        if (tag.TagType == NbtTagType.End)
            throw new ArgumentException("Cannot write TAG_End directly. It is written automatically at the end of a TAG_Compound.");

        await WriteByteAsync((byte)tag.TagType, cancellationToken).ConfigureAwait(false);

        if (named)
        {
            if (tag.Name == null)
                throw new ArgumentNullException(nameof(tag), "Tag name cannot be null when writing a named tag.");
            await WriteStringAsync(tag.Name, cancellationToken).ConfigureAwait(false);
        }

        await WriteTagPayloadAsync(tag, cancellationToken).ConfigureAwait(false);
    }

    private ValueTask WriteTagPayloadAsync(NbtTag tag, CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        return tag.TagType switch
        {
            NbtTagType.Byte => WriteByteAsync(((ByteTag)tag).Value, cancellationToken),
            NbtTagType.Short => WriteShortAsync(((ShortTag)tag).Value, cancellationToken),
            NbtTagType.Int => WriteIntAsync(((IntTag)tag).Value, cancellationToken),
            NbtTagType.Long => WriteLongAsync(((LongTag)tag).Value, cancellationToken),
            NbtTagType.Float => WriteFloatAsync(((FloatTag)tag).Value, cancellationToken),
            NbtTagType.Double => WriteDoubleAsync(((DoubleTag)tag).Value, cancellationToken),
            NbtTagType.ByteArray => WriteByteArrayPayloadAsync((ByteArrayTag)tag, cancellationToken),
            NbtTagType.String => WriteStringAsync(((StringTag)tag).Value, cancellationToken),
            NbtTagType.List => WriteListPayloadAsync((ListTag)tag, cancellationToken),
            NbtTagType.Compound => WriteCompoundPayloadAsync((CompoundTag)tag, cancellationToken),
            NbtTagType.IntArray => WriteIntArrayPayloadAsync((IntArrayTag)tag, cancellationToken),
            NbtTagType.LongArray => WriteLongArrayPayloadAsync((LongArrayTag)tag, cancellationToken),
            NbtTagType.End => default,
            _ => throw new NotImplementedException($"Unsupported tag type for async writing: {tag.TagType}")
        };
    }

    private async ValueTask WriteListPayloadAsync(ListTag tag, CancellationToken cancellationToken = default)
    {
        await WriteByteAsync((byte)tag.ListType, cancellationToken).ConfigureAwait(false);
        await WriteIntAsync(tag.Count, cancellationToken).ConfigureAwait(false);
        foreach (NbtTag item in tag) await WriteTagPayloadAsync(item, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask WriteCompoundPayloadAsync(CompoundTag tag, CancellationToken cancellationToken = default)
    {
        foreach (KeyValuePair<string, NbtTag> childTagPair in tag) await WriteTagAsync(childTagPair.Value, true, cancellationToken).ConfigureAwait(false);
        await WriteByteAsync((byte)NbtTagType.End, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask WriteByteArrayPayloadAsync(ByteArrayTag tag, CancellationToken cancellationToken = default)
    {
        await WriteIntAsync(tag.Value.Length, cancellationToken).ConfigureAwait(false);
        await WriteAsync(tag.Value, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask WriteIntArrayPayloadAsync(IntArrayTag tag, CancellationToken cancellationToken = default)
    {
        await WriteIntAsync(tag.Value.Length, cancellationToken).ConfigureAwait(false);
        foreach (int value in tag.Value) await WriteIntAsync(value, cancellationToken).ConfigureAwait(false);
    }

    private async ValueTask WriteLongArrayPayloadAsync(LongArrayTag tag, CancellationToken cancellationToken = default)
    {
        await WriteIntAsync(tag.Value.Length, cancellationToken).ConfigureAwait(false);
        foreach (long value in tag.Value) await WriteLongAsync(value, cancellationToken).ConfigureAwait(false);
    }

    public async ValueTask WriteStringAsync(string value, CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        byte[] stringBytes = ModifiedUtf8.GetBytes(value);
        if (stringBytes.Length > short.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(value), $"String length in bytes ({stringBytes.Length}) exceeds maximum allowed ({short.MaxValue}).");
        await WriteShortAsync((short)stringBytes.Length, cancellationToken).ConfigureAwait(false);
        await WriteAsync(stringBytes, cancellationToken).ConfigureAwait(false);
    }

    public ValueTask WriteIntAsync(int value, CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        int networkValue = IPAddress.HostToNetworkOrder(value);
        byte[] bytes = BitConverter.GetBytes(networkValue);
        return WriteAsync(bytes, cancellationToken);
    }

    public ValueTask WriteFloatAsync(float value, CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        byte[] bytes = BitConverter.GetBytes(HostToNetworkOrder(value));
        return WriteAsync(bytes, cancellationToken);
    }

    public ValueTask WriteDoubleAsync(double value, CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        byte[] bytes = HostToNetworkOrder(value);
        return WriteAsync(bytes, cancellationToken);
    }

    public ValueTask WriteShortAsync(short value, CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        short networkValue = IPAddress.HostToNetworkOrder(value);
        byte[] bytes = BitConverter.GetBytes(networkValue);
        return WriteAsync(bytes, cancellationToken);
    }

    public ValueTask WriteLongAsync(long value, CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        long networkValue = IPAddress.HostToNetworkOrder(value);
        byte[] bytes = BitConverter.GetBytes(networkValue);
        return WriteAsync(bytes, cancellationToken);
    }
    
    public async ValueTask WriteByteAsync(byte value, CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        byte[] buffer = ArrayPool<byte>.Shared.Rent(1);
        try
        {
            buffer[0] = value;
            await WriteAsync(buffer.AsMemory(0, 1), cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    public ValueTask WriteAsync(byte[] data, CancellationToken cancellationToken = default) => WriteAsync(data.AsMemory(), cancellationToken);

    public ValueTask WriteAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        return !data.IsEmpty ? _stream.WriteAsync(data, cancellationToken) : default;
    }
    
    private void CheckDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private static byte[] HostToNetworkOrder(double host)
    {
        byte[] bytes = BitConverter.GetBytes(host);
        if (BitConverter.IsLittleEndian) Array.Reverse(bytes);
        return bytes;
    }

    private static float HostToNetworkOrder(float host)
    {
        byte[] bytes = BitConverter.GetBytes(host);
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

    ~NbtWriter() => Dispose(false);
}