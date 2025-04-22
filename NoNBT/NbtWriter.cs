using System.Buffers;
using System.Buffers.Binary;
using System.Runtime.InteropServices;
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
/// It ensures compliance with data representation and ordering requirements of the NBT format (Big Endian).
/// Async operations utilize Task.Run for improved performance over granular async IO for small writes.
/// </remarks>
public class NbtWriter(Stream stream, bool leaveOpen = false) : IDisposable, IAsyncDisposable
{
    private readonly Stream _stream = stream ?? throw new ArgumentNullException(nameof(stream));
    private bool _disposed;

    /// Writes an NBT tag to the underlying stream.
    /// <param name="tag">The NBT tag to write. The tag must not be of type End, as it is written automatically at the end of a compound tag.</param>
    /// <param name="named">Indicates whether the tag is named. When set to true, the tag's type and name will be included in the output. Set to false for list elements.</param>
    /// <exception cref="ArgumentException">Thrown if the tag is of type End or if trying to write a named tag as a list element.</exception>
    /// <exception cref="ArgumentNullException">Thrown if the tag is named but its name is null.</exception>
    /// <exception cref="ObjectDisposedException">Thrown if the writer has already been disposed.</exception>
    public void WriteTag(NbtTag tag, bool named = true)
    {
        CheckDisposed();
        if (tag.TagType == NbtTagType.End)
            throw new ArgumentException(
                "Cannot write TAG_End directly. It is written automatically at the end of a TAG_Compound.",
                nameof(tag));

        if (named)
        {
            WriteByte((byte)tag.TagType);
            if (tag.Name == null)
                throw new ArgumentNullException(nameof(tag.Name), "Tag name cannot be null when writing a named tag.");
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
            default: throw new NotImplementedException($"Unsupported tag type for writing payload: {tag.TagType}");
        }
    }

    private void WriteListPayload(ListTag tag)
    {
        WriteByte((byte)tag.ListType);
        WriteInt(tag.Count);
        foreach (NbtTag item in tag)
        {
            WriteTagPayload(item);
        }
    }

    private void WriteCompoundPayload(CompoundTag tag)
    {
        foreach (KeyValuePair<string, NbtTag> childTagPair in tag)
        {
            WriteTag(childTagPair.Value, named: true);
        }

        WriteByte((byte)NbtTagType.End);
    }

    private void WriteByteArrayPayload(ByteArrayTag tag)
    {
        WriteInt(tag.Value.Length);
        Write(tag.Value.AsSpan());
    }

    private void WriteIntArrayPayload(IntArrayTag tag)
    {
        WriteInt(tag.Value.Length);
        int count = tag.Value.Length;
        if (count == 0) return;

        int byteCount = count * sizeof(int);
        bool needsSwap = BitConverter.IsLittleEndian;

        byte[] rentedBuffer = ArrayPool<byte>.Shared.Rent(byteCount);
        try
        {
            Span<byte> bufferSpan = rentedBuffer.AsSpan(0, byteCount);

            if (needsSwap)
            {
                for (var i = 0; i < count; i++)
                {
                    BinaryPrimitives.WriteInt32BigEndian(bufferSpan[(i * sizeof(int))..], tag.Value[i]);
                }
            }
            else
            {
                MemoryMarshal.AsBytes(tag.Value.AsSpan()).CopyTo(bufferSpan);
            }

            Write(bufferSpan);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rentedBuffer);
        }
    }

    private void WriteLongArrayPayload(LongArrayTag tag)
    {
        WriteInt(tag.Value.Length);
        int count = tag.Value.Length;
        if (count == 0) return;

        int byteCount = count * sizeof(long);
        bool needsSwap = BitConverter.IsLittleEndian;

        byte[] rentedBuffer = ArrayPool<byte>.Shared.Rent(byteCount);
        try
        {
            Span<byte> bufferSpan = rentedBuffer.AsSpan(0, byteCount);

            if (needsSwap)
            {
                for (var i = 0; i < count; i++)
                {
                    BinaryPrimitives.WriteInt64BigEndian(bufferSpan[(i * sizeof(long))..], tag.Value[i]);
                }
            }
            else
            {
                MemoryMarshal.AsBytes(tag.Value.AsSpan()).CopyTo(bufferSpan);
            }

            Write(bufferSpan);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rentedBuffer);
        }
    }

    /// Writes a string value to the underlying stream (length-prefixed short, Modified UTF-8).
    /// <param name="value">The string value to write. Cannot be null. Length in bytes must not exceed short.MaxValue.</param>
    /// <exception cref="ArgumentNullException">Thrown if value is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the byte length of the string exceeds short.MaxValue.</exception>
    /// <exception cref="ObjectDisposedException">Thrown if the writer has already been disposed.</exception>
    public void WriteString(string value)
    {
        CheckDisposed();
        ArgumentNullException.ThrowIfNull(value);

        byte[] stringBytes = ModifiedUtf8.GetBytes(value);
        if (stringBytes.Length > short.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(value),
                $"String length in bytes ({stringBytes.Length}) exceeds maximum allowed ({short.MaxValue}).");

        WriteShort((short)stringBytes.Length);
        if (stringBytes.Length > 0)
        {
            Write(stringBytes.AsSpan());
        }
    }

    /// Writes a 32-bit signed integer (Big Endian).
    public void WriteInt(int value)
    {
        CheckDisposed();
        Span<byte> buffer = stackalloc byte[sizeof(int)];
        BinaryPrimitives.WriteInt32BigEndian(buffer, value);
        _stream.Write(buffer);
    }

    /// Writes a 32-bit float (Big Endian).
    public void WriteFloat(float value)
    {
        CheckDisposed();
        Span<byte> buffer = stackalloc byte[sizeof(float)];
        BinaryPrimitives.WriteSingleBigEndian(buffer, value);
        _stream.Write(buffer);
    }

    /// Writes a 64-bit double (Big Endian).
    public void WriteDouble(double value)
    {
        CheckDisposed();
        Span<byte> buffer = stackalloc byte[sizeof(double)];
        BinaryPrimitives.WriteDoubleBigEndian(buffer, value);
        _stream.Write(buffer);
    }

    /// Writes a 16-bit short (Big Endian).
    public void WriteShort(short value)
    {
        CheckDisposed();
        Span<byte> buffer = stackalloc byte[sizeof(short)];
        BinaryPrimitives.WriteInt16BigEndian(buffer, value);
        _stream.Write(buffer);
    }

    /// Writes a 64-bit long (Big Endian).
    public void WriteLong(long value)
    {
        CheckDisposed();
        Span<byte> buffer = stackalloc byte[sizeof(long)];
        BinaryPrimitives.WriteInt64BigEndian(buffer, value);
        _stream.Write(buffer);
    }

    /// Writes a single byte.
    public void WriteByte(byte value)
    {
        CheckDisposed();
        _stream.WriteByte(value);
    }

    /// Writes a span of bytes.
    public void Write(ReadOnlySpan<byte> data)
    {
        CheckDisposed();
        if (!data.IsEmpty) _stream.Write(data);
    }

    /// Asynchronously writes an NBT tag to the underlying stream using Task.Run for efficiency.
    /// <param name="tag">The NBT tag to write. Must not be TAG_End.</param>
    /// <param name="named">Whether to include the tag type and name (true) or write only the payload (false, for list elements).</param>
    /// <param name="cancellationToken">Cancellation token (Note: cooperative cancellation within the synchronous method is not implemented).</param>
    /// <returns>A task representing the asynchronous write operation.</returns>
    /// <exception cref="ArgumentException">Thrown if the tag is of type End or other invalid arguments.</exception>
    /// <exception cref="ArgumentNullException">Thrown if the tag is named but its name is null.</exception>
    /// <exception cref="ObjectDisposedException">Thrown if the writer has already been disposed.</exception>
    /// <exception cref="OperationCanceledException">Thrown if the operation is cancelled via CancellationToken.</exception>
    public async ValueTask WriteTagAsync(NbtTag tag, bool named = true, CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        if (tag.TagType == NbtTagType.End)
            throw new ArgumentException("Cannot write TAG_End directly.", nameof(tag));
        if (named && tag.Name == null)
            throw new ArgumentNullException(nameof(tag.Name), "Tag name cannot be null when writing a named tag.");

        try
        {
            await Task.Run(() => WriteTag(tag, named), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException && ex is not ObjectDisposedException)
        {
            throw new AggregateException(
                $"Error during async NBT write operation for tag '{tag.Name ?? "<unnamed>"}' ({tag.TagType}).", ex);
        }
    }

    /// Asynchronously writes a string value (length-prefixed short, Modified UTF-8) using Task.Run.
    /// <param name="value">The string value to write. Cannot be null.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task representing the asynchronous write operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown if value is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if the byte length exceeds short.MaxValue.</exception>
    /// <exception cref="ObjectDisposedException">Thrown if the writer has already been disposed.</exception>
    /// <exception cref="OperationCanceledException">Thrown if the operation is cancelled.</exception>
    public async ValueTask WriteStringAsync(string value, CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        ArgumentNullException.ThrowIfNull(value);

        try
        {
            await Task.Run(() =>
            {
                byte[] stringBytes = ModifiedUtf8.GetBytes(value);
                if (stringBytes.Length > short.MaxValue)
                    throw new ArgumentOutOfRangeException(nameof(value),
                        $"String length in bytes ({stringBytes.Length}) exceeds maximum allowed ({short.MaxValue}).");

                WriteShort((short)stringBytes.Length);
                if (stringBytes.Length > 0)
                {
                    Write(stringBytes.AsSpan());
                }
            }, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException && ex is not ObjectDisposedException)
        {
            throw new AggregateException($"Error during async string write operation.", ex);
        }
    }


    private void CheckDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    /// Releases all resources used by the current instance of the NbtWriter class.
    /// This method disposes of the underlying stream if the writer was constructed
    /// with leaveOpen set to false. If leaveOpen was set to true, the stream remains open.
    /// Ensures proper cleanup of unmanaged resources and prepares the writer for garbage collection.
    /// <exception cref="ObjectDisposedException">Thrown if the writer is already disposed.</exception>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// Asynchronously releases the unmanaged and optionally managed resources used by the NbtWriter instance.
    /// Ensures proper cleanup of resources, including any underlying streams if not set to leave open.
    /// <returns>A ValueTask that represents the asynchronous operation of disposing resources.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if DisposeAsync is called on an already disposed instance.</exception>
    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore().ConfigureAwait(false);
        Dispose(disposing: true, calledFromAsync: true);
        GC.SuppressFinalize(this);
    }

    /// Releases all resources used by the NbtWriter instance.
    /// <param name="disposing">
    /// A Boolean value indicating whether the method has been called directly or indirectly by user code.
    /// If true, it indicates that managed and unmanaged resources should be released.
    /// If false, only unmanaged resources are released.
    /// </param>
    protected virtual void Dispose(bool disposing) => Dispose(disposing, calledFromAsync: false);

    private void Dispose(bool disposing, bool calledFromAsync)
    {
        if (_disposed) return;
        if (disposing)
        {
            if (!calledFromAsync)
            {
                try
                {
                    _stream?.Flush();
                }
                catch (ObjectDisposedException)
                {
                    /* Ignore */
                }
            }

            if (!calledFromAsync && !leaveOpen)
            {
                try
                {
                    _stream?.Dispose();
                }
                catch (ObjectDisposedException)
                {
                    /* Ignore */
                }
            }
        }

        _disposed = true;
    }


    /// Performs the core asynchronous disposal logic for the class.
    /// Ensures that the underlying stream is properly flushed and disposed based on the configuration.
    /// This method is called by DisposeAsync to handle the asynchronous cleanup operations.
    /// <returns>A ValueTask that represents the asynchronous dispose operation.</returns>
    /// <exception cref="ObjectDisposedException">Thrown if the underlying stream is already disposed when attempting to flush.</exception>
    protected virtual async ValueTask DisposeAsyncCore()
    {
        if (_disposed) return;
        try
        {
            if (_stream != null)
            {
                await _stream.FlushAsync().ConfigureAwait(false);
            }
        }
        catch (ObjectDisposedException)
        {
        }

        if (!leaveOpen && _stream != null)
        {
            await _stream.DisposeAsync().ConfigureAwait(false);
        }
    }

    ~NbtWriter() => Dispose(false);
}