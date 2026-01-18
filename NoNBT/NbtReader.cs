using System.Buffers;
using System.Buffers.Binary;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using NoNBT.Tags;

namespace NoNBT;

/// <summary>
/// Provides functionality for reading NBT data from a stream.
/// </summary>
/// <remarks>
/// Reads NBT data according to the specification (Big Endian).
/// Supports synchronous and asynchronous operations. Async operations utilize Task.Run
/// for improved performance over granular async IO for small reads.
/// </remarks>
public sealed class NbtReader(Stream stream, bool leaveOpen = false) : IDisposable, IAsyncDisposable
{
    private readonly Stream _stream = stream ?? throw new ArgumentNullException(nameof(stream));
    private bool _disposed;

    private static readonly bool s_isLittleEndian = BitConverter.IsLittleEndian;
    private static ReadOnlySpan<byte> ShuffleMaskInt32 => [3, 2, 1, 0, 7, 6, 5, 4, 11, 10, 9, 8, 15, 14, 13, 12];

    /// <summary>
    /// Reads the next NBT tag from the stream.
    /// </summary>
    /// <param name="named">
    /// If true, expects a tag type byte, then a name (length-prefixed string), then the tag payload.
    /// If false, expects only the tag payload - the root compound is unnamed in newer versions.
    /// </param>
    /// <returns>
    /// The deserialized <see cref="NbtTag"/>, or null if <paramref name="named"/> is true and a TAG_End was encountered (signaling the end of a CompoundTag).
    /// </returns>
    /// <exception cref="EndOfStreamException">If the stream ends unexpectedly.</exception>
    /// <exception cref="IOException">If the data format is invalid (e.g., bad tag type, negative length, unexpected TAG_End).</exception>
    /// <exception cref="ObjectDisposedException"> If the reader is disposed. </exception>
    public NbtTag? ReadTag(bool named = true)
    {
        CheckDisposed();

        var tagType = (NbtTagType)ReadByte();
        if (named && tagType == NbtTagType.End)
            return null;

        string? name = null;

        if (named)
        {
            name = ReadStringInternal();
        }

        NbtTag tag = ReadTagPayload(tagType, name);
        return tag;
    }

    private NbtTag ReadTagPayload(NbtTagType type, string? name)
    {
        return type switch
        {
            NbtTagType.Byte => new ByteTag(name, ReadByteChecked()),
            NbtTagType.Short => new ShortTag(name, ReadShortInternal()),
            NbtTagType.Int => new IntTag(name, ReadIntInternal()),
            NbtTagType.Long => new LongTag(name, ReadLongInternal()),
            NbtTagType.Float => new FloatTag(name, ReadFloatInternal()),
            NbtTagType.Double => new DoubleTag(name, ReadDoubleInternal()),
            NbtTagType.ByteArray => ReadByteArrayPayload(name),
            NbtTagType.String => new StringTag(name, ReadStringInternal()),
            NbtTagType.List => ReadListPayload(name),
            NbtTagType.Compound => ReadCompoundPayload(name),
            NbtTagType.IntArray => ReadIntArrayPayload(name),
            NbtTagType.LongArray => ReadLongArrayPayload(name),
            NbtTagType.End => throw new IOException(
                "TAG_End encountered where a tag was expected. This should not happen."),
            _ => throw new IOException($"Unsupported tag type payload: {type}")
        };
    }

    private ListTag ReadListPayload(string? name)
    {
        var listType = (NbtTagType)ReadByteChecked();
        int count = ReadIntCheckedLength();
        if (listType == NbtTagType.End && count > 0)
            throw new IOException("TAG_List specifies TAG_End as element type but has count > 0.");

        var list = new ListTag(name, listType);
        if (count == 0) return list;

        if (listType is NbtTagType.Byte or NbtTagType.Short or NbtTagType.Int or NbtTagType.Long or NbtTagType.Float
            or NbtTagType.Double)
        {
            int elementSize = GetPrimitiveSize(listType);
            bool needsSwap = s_isLittleEndian;

            int byteCount = count * elementSize;
            if ((uint)byteCount > 512 * 1024 * 1024)
            {
                throw new IOException($"List size ({byteCount} bytes) exceeds safety limits.");
            }

            byte[] rentedBuffer = ArrayPool<byte>.Shared.Rent(byteCount);
            try
            {
                Span<byte> bufferSpan = rentedBuffer.AsSpan(0, byteCount);
                _stream.ReadExactly(bufferSpan);
                for (var i = 0; i < count; i++)
                {
                    ReadOnlySpan<byte> elementSpan = bufferSpan.Slice(i * elementSize, elementSize);
                    NbtTag element = CreatePrimitiveTag(listType, null, elementSpan, needsSwap);
                    list.Add(element);
                }
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(rentedBuffer);
            }
        }
        else
        {
            for (nint i = 0; i < count; i++)
            {
                NbtTag element = ReadTagPayload(listType, null);
                list.Add(element);
            }
        }

        return list;
    }

    private CompoundTag ReadCompoundPayload(string? name)
    {
        var compound = new CompoundTag(name);

        while (true)
        {
            int tagTypeByte = _stream.ReadByte();

            if (tagTypeByte == -1)
                ThrowUnexpectedEndOfStream();
            if (tagTypeByte == (int)NbtTagType.End)
                break;

            string childName = ReadStringInternal();
            compound.Add(ReadTagPayload((NbtTagType)tagTypeByte, childName));
        }

        return compound;

        [DoesNotReturn]
        static void ThrowUnexpectedEndOfStream() =>
            throw new EndOfStreamException("Unexpected end of stream within TAG_Compound.");
    }

    private ByteArrayTag ReadByteArrayPayload(string? name)
    {
        int length = ReadIntCheckedLength();
        byte[] data = ReadBytes(length);
        return new ByteArrayTag(name, data);
    }

    private IntArrayTag ReadIntArrayPayload(string? name)
    {
        int length = ReadIntCheckedLength();
        if (length == 0) return new IntArrayTag(name, []);

        int byteCount = length * sizeof(int);
        if ((uint)byteCount > 512 * 1024 * 1024)
            throw new IOException($"IntArray size ({byteCount} bytes) exceeds safety limits.");

        byte[] rentedBuffer = ArrayPool<byte>.Shared.Rent(byteCount);
        try
        {
            Span<byte> bufferSpan = rentedBuffer.AsSpan(0, byteCount);
            _stream.ReadExactly(bufferSpan);

            var result = new int[length];

            if (s_isLittleEndian)
            {
                ReverseEndiannessInt32(bufferSpan, result);
            }
            else
            {
                MemoryMarshal.Cast<byte, int>(bufferSpan).CopyTo(result);
            }

            return new IntArrayTag(name, result);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rentedBuffer);
        }
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void ReverseEndiannessInt32(ReadOnlySpan<byte> source, Span<int> destination)
    {
        ref byte srcRef = ref MemoryMarshal.GetReference(source);
        ref int dstRef = ref MemoryMarshal.GetReference(destination);
        nint length = destination.Length;
        nint i = 0;

        if (Avx2.IsSupported)
        {
            var mask = Vector256.Create(
                (byte)3, 2, 1, 0, 7, 6, 5, 4, 11, 10, 9, 8, 15, 14, 13, 12,
                3, 2, 1, 0, 7, 6, 5, 4, 11, 10, 9, 8, 15, 14, 13, 12);

            for (; i <= length - 16; i += 16)
            {
                Vector256<byte> v0 = Vector256.LoadUnsafe(ref srcRef, (nuint)(i * 4));
                Vector256<byte> v1 = Vector256.LoadUnsafe(ref srcRef, (nuint)(i * 4 + 32));
                Avx2.Shuffle(v0, mask).As<byte, int>().StoreUnsafe(ref dstRef, (nuint)i);
                Avx2.Shuffle(v1, mask).As<byte, int>().StoreUnsafe(ref dstRef, (nuint)(i + 8));
            }
        }

        if (Ssse3.IsSupported)
        {
            var mask = Vector128.Create((byte)3, 2, 1, 0, 7, 6, 5, 4, 11, 10, 9, 8, 15, 14, 13, 12);
            for (; i <= length - 4; i += 4)
            {
                Vector128<byte> v = Vector128.LoadUnsafe(ref srcRef, (nuint)(i * 4));
                Ssse3.Shuffle(v, mask).As<byte, int>().StoreUnsafe(ref dstRef, (nuint)i);
            }
        }

        for (; i < length; i++)
        {
            Unsafe.Add(ref dstRef, i) = BinaryPrimitives.ReverseEndianness(
                Unsafe.ReadUnaligned<int>(ref Unsafe.Add(ref srcRef, (nint)(i * 4))));
        }
    }

    private LongArrayTag ReadLongArrayPayload(string? name)
    {
        int length = ReadIntCheckedLength();
        if (length == 0) return new LongArrayTag(name, []);

        int byteCount = length * sizeof(long);
        if (byteCount is < 0 or > 1024 * 1024 * 512)
            throw new IOException($"LongArray size ({byteCount} bytes) is invalid or exceeds safety limits.");

        byte[] rentedBuffer = ArrayPool<byte>.Shared.Rent(byteCount);
        try
        {
            Span<byte> bufferSpan = rentedBuffer.AsSpan(0, byteCount);
            _stream.ReadExactly(bufferSpan);

            var result = new long[length];

            if (s_isLittleEndian)
            {
                ReverseEndiannessInt64(bufferSpan, result);
            }
            else
            {
                MemoryMarshal.Cast<byte, long>(bufferSpan).CopyTo(result);
            }

            return new LongArrayTag(name, result);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(rentedBuffer);
        }
    }

    private static void ReverseEndiannessInt64(ReadOnlySpan<byte> source, Span<long> destination)
    {
        ref byte srcRef = ref MemoryMarshal.GetReference(source);
        ref long dstRef = ref MemoryMarshal.GetReference(destination);
        nint length = destination.Length;
        nint i = 0;

        if (Avx2.IsSupported)
        {
            var mask = Vector256.Create(
                (byte)7, 6, 5, 4, 3, 2, 1, 0, 15, 14, 13, 12, 11, 10, 9, 8,
                7, 6, 5, 4, 3, 2, 1, 0, 15, 14, 13, 12, 11, 10, 9, 8);

            for (; i <= length - 4; i += 4)
            {
                Vector256<byte> v = Vector256.LoadUnsafe(ref srcRef, (nuint)(i * 8));
                Avx2.Shuffle(v, mask).As<byte, long>().StoreUnsafe(ref dstRef, (nuint)i);
            }
        }

        for (; i < length; i++)
        {
            Unsafe.Add(ref dstRef, i) = BinaryPrimitives.ReverseEndianness(
                Unsafe.ReadUnaligned<long>(ref Unsafe.Add(ref srcRef, (nint)(i * 8))));
        }
    }

    /// <summary>
    /// Reads a string from the stream (length-prefixed short, Modified UTF-8).
    /// Used for tag names and StringTag values.
    /// </summary>
    public string ReadString() => ReadStringInternal();

    [SkipLocalsInit]
    private string ReadStringInternal()
    {
        short length = ReadShortInternal();

        switch (length)
        {
            case < 0:
                throw new IOException($"Invalid string length: {length}");
            case 0:
                return string.Empty;
        }

        byte[]? rentedBuffer = null;
        Span<byte> stringBytes = length <= 256
            ? stackalloc byte[length]
            : (rentedBuffer = ArrayPool<byte>.Shared.Rent(length)).AsSpan(0, length);

        try
        {
            _stream.ReadExactly(stringBytes);
            return ModifiedUtf8.GetString(stringBytes);
        }
        finally
        {
            if (rentedBuffer != null)
                ArrayPool<byte>.Shared.Return(rentedBuffer);
        }
    }

    /// Reads a 32-bit signed integer (Big Endian).
    public int ReadInt() => ReadIntInternal();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private int ReadIntInternal()
    {
        Span<byte> buffer = stackalloc byte[4];
        _stream.ReadExactly(buffer);
        return BinaryPrimitives.ReadInt32BigEndian(buffer);
    }

    /// Reads a 32-bit float (Big Endian).
    public float ReadFloat() => ReadFloatInternal();

    private float ReadFloatInternal()
    {
        Span<byte> buffer = stackalloc byte[4];
        _stream.ReadExactly(buffer);
        return BinaryPrimitives.ReadSingleBigEndian(buffer);
    }

    /// Reads a 64-bit double (Big Endian).
    public double ReadDouble() => ReadDoubleInternal();

    private double ReadDoubleInternal()
    {
        Span<byte> buffer = stackalloc byte[8];
        _stream.ReadExactly(buffer);
        return BinaryPrimitives.ReadDoubleBigEndian(buffer);
    }

    /// Reads a 16-bit short (Big Endian).
    public short ReadShort() => ReadShortInternal();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private short ReadShortInternal()
    {
        Span<byte> buffer = stackalloc byte[2];
        _stream.ReadExactly(buffer);
        return BinaryPrimitives.ReadInt16BigEndian(buffer);
    }

    /// Reads a 64-bit long (Big Endian).
    public long ReadLong() => ReadLongInternal();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private long ReadLongInternal()
    {
        Span<byte> buffer = stackalloc byte[8];
        _stream.ReadExactly(buffer);
        return BinaryPrimitives.ReadInt64BigEndian(buffer);
    }

    /// Reads a single byte. Returns -1 if end of stream.
    public int ReadByte()
    {
        CheckDisposed();
        return _stream.ReadByte();
    }

    /// <summary>
    /// Reads exactly `length` bytes from the stream into a new byte array.
    /// Uses efficient block reading.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">If length is negative.</exception>
    /// <exception cref="EndOfStreamException">If stream ends before reading `length` bytes.</exception>
    /// <exception cref="ObjectDisposedException">If reader is disposed.</exception>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public byte[] ReadBytes(int length)
    {
        CheckDisposed();

        switch (length)
        {
            case < 0:
                ThrowNegativeLength(length);
                break;
            case 0:
                return [];
        }

        byte[] buffer = GC.AllocateUninitializedArray<byte>(length);
        _stream.ReadExactly(buffer);
        return buffer;

        [DoesNotReturn]
        static void ThrowNegativeLength(int length) =>
            throw new ArgumentOutOfRangeException(nameof(length), length, "Length cannot be negative.");
    }

    /// <summary>
    /// Asynchronously reads the next NBT tag from the stream using Task.Run.
    /// </summary>
    /// <param name="named">If true, expects type, name, payload. If false, behavior is undefined (use ReadTagPayloadAsync).</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The read NbtTag or null if TAG_End was encountered.</returns>
    /// <exception cref="ObjectDisposedException">If reader is disposed.</exception>
    /// <exception cref="OperationCanceledException">If cancellation is requested.</exception>
    /// <exception cref="AggregateException">Wraps exceptions thrown during the underlying synchronous read.</exception>
    public async ValueTask<NbtTag?> ReadTagAsync(bool named = true, CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        if (!named)
            throw new ArgumentException(
                "Async reading of unnamed tags requires context. Use specific payload reading methods.", nameof(named));

        try
        {
            return await Task.Run(() => ReadTag(named: true), cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException && ex is not ObjectDisposedException)
        {
            throw new AggregateException("Error during async NBT read operation.", ex);
        }
    }

    /// Asynchronously reads a string (length-prefixed short, Modified UTF-8) using Task.Run.
    public async ValueTask<string> ReadStringAsync(CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        try
        {
            return await Task.Run(ReadStringInternal, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException && ex is not ObjectDisposedException)
        {
            throw new AggregateException("Error during async string read.", ex);
        }
    }

    /// Asynchronously reads exactly `length` bytes into a new byte array using stream's ReadExactlyAsync.
    public async ValueTask<byte[]> ReadBytesAsync(int length, CancellationToken cancellationToken = default)
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
        try
        {
            await _stream.ReadExactlyAsync(buffer, cancellationToken).ConfigureAwait(false);
        }
        catch (EndOfStreamException ex)
        {
            throw new EndOfStreamException($"Expected {length} bytes, but stream ended prematurely.", ex);
        }

        return buffer;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void CheckDisposed()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
    }

    private int ReadIntCheckedLength()
    {
        int length = ReadIntInternal();
        return length < 0 ? throw new IOException($"Invalid array/list/string length encountered: {length}") : length;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private byte ReadByteChecked()
    {
        int b = _stream.ReadByte();
        if (b == -1) ThrowEndOfStream();
        return (byte)b;

        [DoesNotReturn]
        static void ThrowEndOfStream() =>
            throw new EndOfStreamException("Unexpected end of stream while reading required byte.");
    }

    private static int GetPrimitiveSize(NbtTagType type) => type switch
    {
        NbtTagType.Byte => sizeof(byte),
        NbtTagType.Short => sizeof(short),
        NbtTagType.Int => sizeof(int),
        NbtTagType.Long => sizeof(long),
        NbtTagType.Float => sizeof(float),
        NbtTagType.Double => sizeof(double),
        _ => throw new ArgumentOutOfRangeException(nameof(type), "Not a fixed-size primitive type.")
    };

    private static NbtTag CreatePrimitiveTag(NbtTagType type, string? name, ReadOnlySpan<byte> data, bool needsSwap)
    {
        return type switch
        {
            NbtTagType.Byte => new ByteTag(name, data[0]),
            NbtTagType.Short => new ShortTag(name,
                needsSwap ? BinaryPrimitives.ReadInt16BigEndian(data) : MemoryMarshal.Read<short>(data)),
            NbtTagType.Int => new IntTag(name,
                needsSwap ? BinaryPrimitives.ReadInt32BigEndian(data) : MemoryMarshal.Read<int>(data)),
            NbtTagType.Long => new LongTag(name,
                needsSwap ? BinaryPrimitives.ReadInt64BigEndian(data) : MemoryMarshal.Read<long>(data)),
            NbtTagType.Float => new FloatTag(name,
                needsSwap ? BinaryPrimitives.ReadSingleBigEndian(data) : MemoryMarshal.Read<float>(data)),
            NbtTagType.Double => new DoubleTag(name,
                needsSwap ? BinaryPrimitives.ReadDoubleBigEndian(data) : MemoryMarshal.Read<double>(data)),
            _ => throw new ArgumentOutOfRangeException(nameof(type), "Not a primitive type.")
        };
    }


    /// <summary>
    /// Releases the resources used by the <see cref="NbtReader"/>.
    /// </summary>
    /// <remarks>
    /// This method calls <see cref="Dispose(bool)"/> to perform the actual resource cleanup and suppresses finalization of the object.
    /// Ensures proper cleanup of unmanaged resources and optionally disposes of managed resources depending on the implementation of the derived class.
    /// </remarks>
    public void Dispose()
    {
        Dispose(true);
    }

    /// <summary>
    /// Asynchronously releases the unmanaged resources used by the <see cref="NbtReader"/>
    /// and optionally disposes of the managed resources.
    /// </summary>
    /// <returns>
    /// A task that represents the asynchronous dispose operation.
    /// </returns>
    /// <remarks>
    /// After calling this method, the object is considered disposed and cannot be used further.
    /// </remarks>
    public async ValueTask DisposeAsync()
    {
        await DisposeAsyncCore().ConfigureAwait(false);
        Dispose(disposing: false);
    }

    /// <summary>
    /// Releases the resources used by the <see cref="NbtReader"/>.
    /// </summary>
    /// <param name="disposing">
    /// Indicates whether the method is being called from a disposing context.
    /// If true, managed and unmanaged resources will be released. If false, only unmanaged resources will be released.
    /// </param>
    /// <remarks>
    /// Ensures proper cleanup of unmanaged resources and optionally disposes of managed resources.
    /// If <paramref name="disposing"/> is true, this will release managed resources, but only when <c>leaveOpen</c> is false.
    /// Suppresses the finalizer to prevent further attempts to release resources.
    /// </remarks>
    private void Dispose(bool disposing)
    {
        if (_disposed) return;
        if (disposing)
        {
            if (!leaveOpen)
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

    /// <summary>
    /// Performs asynchronous cleanup operations for the NbtReader.
    /// </summary>
    /// <remarks>
    /// This method disposes the underlying stream of the NbtReader asynchronously if it was not specified to be left open.
    /// It is invoked by the <see cref="DisposeAsync"/> method during asynchronous disposal.
    /// </remarks>
    /// <returns>
    /// A <see cref="ValueTask"/> representing the asynchronous disposal operation.
    /// </returns>
    /// <exception cref="ObjectDisposedException">Thrown if this method is called after the NbtReader has already been disposed.</exception>
    private async ValueTask DisposeAsyncCore()
    {
        if (_disposed) return;
        if (!leaveOpen && _stream != null)
        {
            await _stream.DisposeAsync().ConfigureAwait(false);
        }
    }
}