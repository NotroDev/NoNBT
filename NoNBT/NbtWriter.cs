using System.Buffers;
using System.Net;
using NoNBT.Tags;

namespace NoNBT;

/// <summary>
/// Provides functionality for writing NBT data to a stream.
/// Supports both synchronous and asynchronous operations for encoding and
/// emitting NBT tags, ensuring proper formatting and structure compatibility
/// based on the NBT specification.
/// </summary>
/// <remarks>
/// This class is used to serialize and write NBT data including primitives (e.g., integers, floats, strings)
/// and complex tag structures to a specified stream.
/// It ensures compliance with data representation and ordering requirements of the NBT format.
/// </remarks>
/// <example>
/// To use this class, instantiate it with a writable <see cref="Stream"/> and write NBT tags using the
/// provided methods like <see cref="WriteTag"/>, <see cref="WriteInt"/>, <see cref="WriteString"/>,
/// etc., optionally leveraging asynchronous alternatives.
/// </example>
/// <threadsafety>
/// Any individual instance of <see cref="NbtWriter"/> is not thread-safe. Concurrent access by multiple
/// threads to the same instance needs to be coordinated externally to avoid unexpected behavior.
/// </threadsafety>
/// <exception cref="ObjectDisposedException">
/// Raised when attempting to perform operations on a disposed instance.
/// </exception>
/// <exception cref="ArgumentException">
/// Thrown if certain arguments provided to methods do not meet expected criteria, such as invalid NBT tag
/// types or values.
/// </exception>
public class NbtWriter(Stream stream, bool leaveOpen = false) : IDisposable, IAsyncDisposable
{
    private readonly Stream _stream = stream ?? throw new ArgumentNullException(nameof(stream));
    private bool _disposed;

    /// Writes an NBT to the underlying stream.
    /// <param name="tag">The NBT tag to write. The tag must not be of type End, as it is written automatically at the end of a compound tag.</param>
    /// <param name="named">Indicates whether the tag is named. When set to true, the tag's name will be included in the output.</param>
    /// <exception cref="ArgumentException">Thrown if the tag is of type End.</exception>
    /// <exception cref="ObjectDisposedException">Thrown if the writer has already been disposed.</exception>
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

    /// Writes a string value to the underlying stream in the Modified UTF-8 format.
    /// <param name="value">The string value to write. The length of the string in bytes must not exceed the maximum value of a signed short (32767).</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the byte length of the string exceeds the maximum allowed length (32767 bytes).</exception>
    /// <exception cref="ObjectDisposedException">Thrown if the writer has already been disposed.</exception>
    public void WriteString(string value)
    {
        CheckDisposed();
        byte[] stringBytes = ModifiedUtf8.GetBytes(value);
        if (stringBytes.Length > short.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(value), $"String length in bytes ({stringBytes.Length}) exceeds maximum allowed ({short.MaxValue}).");
        WriteShort((short)stringBytes.Length);
        Write(stringBytes);
    }

    /// Writes a 32-bit signed integer to the underlying stream in network byte order.
    /// <param name="value">The integer value to write to the stream.</param>
    /// <exception cref="ObjectDisposedException">Thrown if the writer has already been disposed.</exception>
    public void WriteInt(int value)
    {
        CheckDisposed();
        int networkValue = IPAddress.HostToNetworkOrder(value);
        byte[] bytes = BitConverter.GetBytes(networkValue);
        Write(bytes);
    }

    /// Writes a single-precision floating-point value to the underlying stream.
    /// <param name="value">The single-precision floating-point value to write.</param>
    /// <exception cref="ObjectDisposedException">Thrown if the writer has already been disposed.</exception>
    public void WriteFloat(float value)
    {
        CheckDisposed();
        byte[] bytes = BitConverter.GetBytes(HostToNetworkOrder(value));
        Write(bytes);
    }

    /// Writes a double-precision floating-point number to the underlying stream.
    /// <param name="value">The double-precision floating-point value to write.</param>
    /// <exception cref="ObjectDisposedException">Thrown if the writer has already been disposed.</exception>
    public void WriteDouble(double value)
    {
        CheckDisposed();
        byte[] bytes = HostToNetworkOrder(value);
        Write(bytes);
    }

    /// Writes a short value to the underlying stream in big-endian byte order.
    /// <param name="value">The short value to write.</param>
    /// <exception cref="ObjectDisposedException">Thrown if the writer has already been disposed.</exception>
    public void WriteShort(short value)
    {
        CheckDisposed();
        short networkValue = IPAddress.HostToNetworkOrder(value);
        byte[] bytes = BitConverter.GetBytes(networkValue);
        Write(bytes);
    }

    /// Writes a 64-bit signed integer to the underlying stream in network byte order.
    /// <param name="value">The 64-bit signed integer to write.</param>
    /// <exception cref="ObjectDisposedException">Thrown if the writer has already been disposed.</exception>
    public void WriteLong(long value)
    {
        CheckDisposed();
        long networkValue = IPAddress.HostToNetworkOrder(value);
        byte[] bytes = BitConverter.GetBytes(networkValue);
        Write(bytes);
    }

    /// Writes a single byte to the underlying stream.
    /// <param name="value">The byte value to write to the stream.</param>
    /// <exception cref="ObjectDisposedException">Thrown if the writer has already been disposed.</exception>
    public void WriteByte(byte value)
    {
        CheckDisposed();
        _stream.WriteByte(value);
    }

    /// Writes the specified byte array to the underlying stream.
    /// <param name="data">The data to write to the stream. Must not be null and must have a length greater than zero.</param>
    /// <exception cref="ObjectDisposedException">Thrown when the underlying stream has already been disposed.</exception>
    public void Write(byte[] data)
    {
        CheckDisposed();
        if (data.Length > 0) _stream.Write(data, 0, data.Length);
    }

    /// Asynchronously writes an NBT tag to the underlying stream.
    /// <param name="tag">The NBT tag to write. The tag must not be of type End, as it is written automatically at the end of a compound tag.</param>
    /// <param name="named">Indicates whether the tag is named. When set to true, the tag's name will be included in the output.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task that represents the asynchronous write operation.</returns>
    /// <exception cref="ArgumentException">Thrown if the tag is of type End.</exception>
    /// <exception cref="ArgumentNullException">Thrown if the tag is named but its name is null.</exception>
    /// <exception cref="ObjectDisposedException">Thrown if the writer has already been disposed.</exception>
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

    /// Writes a string to the underlying stream in Modified UTF-8 format.
    /// <param name="value">The string value to write. The string is encoded in Modified UTF-8 and its length in bytes must not exceed the maximum allowed length.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A task representing the asynchronous write operation.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the writer has already been disposed.</exception>
    /// <exception cref="ArgumentNullException">Thrown if the provided string is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the encoded string length in bytes exceeds the maximum allowed length.</exception>
    public async ValueTask WriteStringAsync(string value, CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        byte[] stringBytes = ModifiedUtf8.GetBytes(value);
        if (stringBytes.Length > short.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(value), $"String length in bytes ({stringBytes.Length}) exceeds maximum allowed ({short.MaxValue}).");
        await WriteShortAsync((short)stringBytes.Length, cancellationToken).ConfigureAwait(false);
        await WriteAsync(stringBytes, cancellationToken).ConfigureAwait(false);
    }

    /// Asynchronously writes a 32-bit integer to the underlying stream in network byte order.
    /// <param name="value">The 32-bit integer value to write.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A ValueTask that represents the asynchronous write operation.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the writer has already been disposed.</exception>
    public ValueTask WriteIntAsync(int value, CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        int networkValue = IPAddress.HostToNetworkOrder(value);
        byte[] bytes = BitConverter.GetBytes(networkValue);
        return WriteAsync(bytes, cancellationToken);
    }

    /// Writes a single-precision floating-point number (float) to the underlying stream asynchronously.
    /// <param name="value">The floating-point value to write.</param>
    /// <param name="cancellationToken">A token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A ValueTask that represents the asynchronous write operation.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the writer has already been disposed.</exception>
    public ValueTask WriteFloatAsync(float value, CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        byte[] bytes = BitConverter.GetBytes(HostToNetworkOrder(value));
        return WriteAsync(bytes, cancellationToken);
    }

    /// Asynchronously writes a double value to the underlying stream in network byte order.
    /// <param name="value">The double value to write.</param>
    /// <param name="cancellationToken">A token to monitor for cancellation requests.</param>
    /// <returns>A ValueTask representing the asynchronous write operation.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the writer has already been disposed.</exception>
    public ValueTask WriteDoubleAsync(double value, CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        byte[] bytes = HostToNetworkOrder(value);
        return WriteAsync(bytes, cancellationToken);
    }

    /// Writes a 16-bit signed integer to the underlying stream in network byte order asynchronously.
    /// <param name="value">The 16-bit signed integer value to write.</param>
    /// <param name="cancellationToken">The cancellation token to monitor for cancellation requests.</param>
    /// <returns>A ValueTask that represents the asynchronous write operation.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the writer has already been disposed.</exception>
    public ValueTask WriteShortAsync(short value, CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        short networkValue = IPAddress.HostToNetworkOrder(value);
        byte[] bytes = BitConverter.GetBytes(networkValue);
        return WriteAsync(bytes, cancellationToken);
    }

    /// Writes a long integer to the underlying stream asynchronously in network byte order.
    /// <param name="value">The long integer value to write.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the asynchronous operation.</param>
    /// <returns>A ValueTask representing the asynchronous write operation.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the writer has already been disposed.</exception>
    public ValueTask WriteLongAsync(long value, CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        long networkValue = IPAddress.HostToNetworkOrder(value);
        byte[] bytes = BitConverter.GetBytes(networkValue);
        return WriteAsync(bytes, cancellationToken);
    }

    /// Writes a single byte asynchronously to the underlying stream.
    /// <param name="value">The byte value to write to the stream.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A task that represents the asynchronous write operation.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the writer has already been disposed.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the underlying stream does not support writing.</exception>
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

    /// Writes the specified data asynchronously to the underlying stream.
    /// <param name="data">The byte array to write to the stream.</param>
    /// <param name="cancellationToken">A cancellation token that can be used to cancel the operation.</param>
    /// <returns>A task representing the asynchronous write operation.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the writer has already been disposed.</exception>
    /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via the provided cancellation token.</exception>
    public ValueTask WriteAsync(byte[] data, CancellationToken cancellationToken = default) => WriteAsync(data.AsMemory(), cancellationToken);

    /// Writes the specified data to the underlying stream asynchronously.
    /// <param name="data">The data to write to the stream, represented as a read-only memory block of bytes.</param>
    /// <param name="cancellationToken">The token to monitor for cancellation requests.</param>
    /// <returns>A value task that represents the asynchronous write operation.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the writer has already been disposed.</exception>
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

    /// Releases all resources used by the NbtWriter instance.
    /// Ensures that any resources allocated by the writer, such as the underlying stream, are properly released.
    /// If the `leaveOpen` parameter in the constructor is false, the underlying stream will also be closed.
    /// After calling this method, the NbtWriter instance should no longer be used.
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// Asynchronously releases the unmanaged and optionally the managed resources used by the NbtWriter.
    /// Ensures that all pending asynchronous operations are completed before the object is disposed.
    /// Should be called when the NbtWriter instance is no longer needed.
    /// <returns>A ValueTask that represents the asynchronous dispose operation.</returns>
    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore().ConfigureAwait(false);
        Dispose(false);
        GC.SuppressFinalize(this);
    }

    /// Releases the unmanaged resources used by the NbtWriter and optionally releases the managed resources.
    /// <param name="disposing">Indicates whether to release both managed and unmanaged resources (true) or only unmanaged resources (false).</param>
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

    /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources asynchronously.
    /// This method can be overridden in a derived class to provide custom asynchronous disposal logic.
    /// <return>A ValueTask that represents the asynchronous dispose operation.</return>
    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (_disposed) return;
        if (!leaveOpen && _stream != null)
        {
            await _stream.DisposeAsync().ConfigureAwait(false);
        }
    }

    /// <summary>
    /// Implements the finalizer method for the NbtWriter, ensuring that unmanaged resources are released if <see cref="Dispose()"/> is not explicitly called.
    /// </summary>
    /// <remarks>
    /// This method is called by the garbage collector. It invokes the <see cref="Dispose(bool)"/> method with the <c>disposing</c> parameter set to <c>false</c>.
    /// </remarks>
    ~NbtWriter() => Dispose(false);
}