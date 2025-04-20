using System.Buffers;
using System.Net;
using NoNBT.Tags;

namespace NoNBT;

/// <summary>
/// Provides functionality for reading NBT data from a stream.
/// </summary>
/// <remarks>
/// The <see cref="NbtReader"/> class supports both synchronous and asynchronous methods
/// for reading NBT tags and their associated data types, including strings, integers,
/// floats, doubles, shorts, longs, bytes, and variable-length integers. It allows
/// reading named or unnamed tags, based on the provided parameters.
/// </remarks>
/// <example>
/// The class should be used with a provided <see cref="Stream"/> object to read
/// NBT data. Make sure to properly release resources using its synchronous or
/// asynchronous dispose methods.
/// </example>
/// <threadsafety>
/// Instances of <see cref="NbtReader"/> are not thread-safe. Ensure usage is confined
/// to a single thread or implement your own synchronization mechanism.
/// </threadsafety>
/// <see cref="IDisposable"/> and <see cref="IAsyncDisposable"/> are implemented to handle the proper
/// disposal of resources tied to the stream instance being read from.
public class NbtReader(Stream stream, bool leaveOpen = false) : IDisposable, IAsyncDisposable
{
    private readonly Stream _stream = stream ?? throw new ArgumentNullException(nameof(stream));
    private bool _disposed;

    private const int MaxVarIntSize = 5;

    /// <summary>
    /// Reads an NBT tag from the stream with an optional name.
    /// </summary>
    /// <param name="named">
    /// A boolean value determining whether the tag should include a name.
    /// If true, the tag will be read with its name; otherwise, the tag will be read without a name.
    /// </param>
    /// <returns>
    /// An <see cref="NbtTag"/> instance representing the read tag, or null if the tag type is End.
    /// </returns>
    /// <exception cref="EndOfStreamException">
    /// Thrown if the stream ends unexpectedly while reading the tag type.
    /// </exception>
    /// <exception cref="IOException">
    /// Thrown if an unsupported tag type is encountered or an unexpected TAG_End is found during tag reading.
    /// </exception>
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
            NbtTagType.Byte => new ByteTag(name, ReadByteChecked()),
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
                NbtTagType.Byte => new ByteTag(null, ReadByteChecked()),
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

    /// <summary>
    /// Reads a string from the stream encoded in Modified UTF-8 format.
    /// </summary>
    /// <returns>
    /// A string instance representing the value read from the stream.
    /// </returns>
    /// <exception cref="IOException">
    /// Thrown if the string length is invalid (negative) or an error occurs while reading bytes from the stream.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// Thrown if the stream has been disposed before calling this method.
    /// </exception>
    public string ReadString()
    {
        CheckDisposed();
        short length = ReadShort();
        if (length < 0) throw new IOException($"Invalid string length: {length}");
        byte[] stringValue = Read(length);
        return ModifiedUtf8.GetString(stringValue);
    }

    /// <summary>
    /// Reads a 32-bit signed integer from the stream in big-endian format.
    /// </summary>
    /// <returns>
    /// The 32-bit signed integer read from the stream.
    /// </returns>
    /// <exception cref="ObjectDisposedException">
    /// Thrown if the method is called on a disposed instance.
    /// </exception>
    /// <exception cref="IOException">
    /// Thrown if the stream ends unexpectedly or an error occurs during reading.
    /// </exception>
    public int ReadInt()
    {
        CheckDisposed();
        return IPAddress.NetworkToHostOrder(BitConverter.ToInt32(Read(sizeof(int)), 0));
    }

    /// <summary>
    /// Reads a 32-bit floating-point number from the stream in network byte order.
    /// </summary>
    /// <returns>
    /// A <see cref="float"/> representing the value read from the stream.
    /// </returns>
    /// <exception cref="ObjectDisposedException">
    /// Thrown if the reader has been disposed when attempting to read from the stream.
    /// </exception>
    /// <exception cref="EndOfStreamException">
    /// Thrown if the stream ends unexpectedly while attempting to read the required number of bytes.
    /// </exception>
    /// <exception cref="IOException">
    /// Thrown if an I/O error occurs during the reading process.
    /// </exception>
    public float ReadFloat()
    {
        CheckDisposed();
        return NetworkToHostOrder(BitConverter.ToSingle(Read(sizeof(float)), 0));
    }

    /// <summary>
    /// Reads a double-precision floating-point number from the stream in network byte order.
    /// </summary>
    /// <returns>
    /// A double value read from the stream.
    /// </returns>
    /// <exception cref="ObjectDisposedException">
    /// Thrown if the reader has been disposed.
    /// </exception>
    /// <exception cref="EndOfStreamException">
    /// Thrown if the stream ends unexpectedly while attempting to read the double value.
    /// </exception>
    /// <exception cref="IOException">
    /// Thrown if an error occurs while reading from the stream.
    /// </exception>
    public double ReadDouble()
    {
        CheckDisposed();
        return NetworkToHostOrder(Read(sizeof(double)));
    }

    /// <summary>
    /// Reads a 16-bit signed integer from the stream in big-endian format.
    /// </summary>
    /// <returns>
    /// A short value representing the 16-bit signed integer read from the stream.
    /// </returns>
    /// <exception cref="ObjectDisposedException">
    /// Thrown if the reader has been disposed before this method is called.
    /// </exception>
    /// <exception cref="EndOfStreamException">
    /// Thrown if the stream ends unexpectedly while attempting to read the required data.
    /// </exception>
    /// <exception cref="IOException">
    /// Thrown if an error occurs while accessing the underlying stream.
    /// </exception>
    public short ReadShort()
    {
        CheckDisposed();
        return IPAddress.NetworkToHostOrder(BitConverter.ToInt16(Read(sizeof(short)), 0));
    }

    /// <summary>
    /// Reads a 64-bit signed integer (long) from the stream in network byte order and converts it to the host byte order.
    /// </summary>
    /// <returns>
    /// A long value read from the stream.
    /// </returns>
    /// <exception cref="ObjectDisposedException">
    /// Thrown if the method is called after the stream has been disposed.
    /// </exception>
    /// <exception cref="IOException">
    /// Thrown if an error occurs while reading from the stream.
    /// </exception>
    /// <exception cref="EndOfStreamException">
    /// Thrown if the stream ends unexpectedly while attempting to read the long value.
    /// </exception>
    public long ReadLong()
    {
        CheckDisposed();
        return IPAddress.NetworkToHostOrder(BitConverter.ToInt64(Read(sizeof(long)), 0));
    }

    /// <summary>
    /// Reads a single byte from the underlying stream.
    /// </summary>
    /// <returns>
    /// An integer representing the next byte in the stream, or -1 if the end of the stream is reached.
    /// </returns>
    /// <exception cref="ObjectDisposedException">
    /// Thrown if the stream has already been disposed when attempting to read.
    /// </exception>
    public int ReadByte()
    {
        CheckDisposed();
        return _stream.ReadByte();
    }

    /// <summary>
    /// Gets the number of elements within the collection or sequence.
    /// </summary>
    /// <returns>
    /// An integer representing the total count of elements.
    /// </returns>
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

    /// <summary>
    /// Reads a variable-length integer (VarInt) from the stream.
    /// </summary>
    /// <param name="bytesRead">
    /// An output parameter that returns the number of bytes read while decoding the VarInt.
    /// </param>
    /// <returns>
    /// The decoded integer value as a VarInt.
    /// </returns>
    /// <exception cref="EndOfStreamException">
    /// Thrown if the stream ends unexpectedly while reading the VarInt.
    /// </exception>
    /// <exception cref="IOException">
    /// Thrown if the VarInt is too large or an unexpected condition occurs during reading.
    /// </exception>
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

    /// <summary>
    /// Reads a variable-length integer (VarInt) from the stream.
    /// </summary>
    /// <returns>
    /// The integer value decoded from the VarInt format.
    /// </returns>
    /// <exception cref="EndOfStreamException">
    /// Thrown if the stream ends unexpectedly while reading the VarInt.
    /// </exception>
    /// <exception cref="IOException">
    /// Thrown if an invalid VarInt encoding is encountered.
    /// </exception>
    public int ReadVarInt() => ReadVarInt(out int _);

    /// <summary>
    /// Asynchronously reads an NBT tag from the stream with an optional name.
    /// </summary>
    /// <param name="named">
    /// A boolean value indicating whether the tag should include a name.
    /// If true, the tag will be read with its name; otherwise, the tag will be read without a name.
    /// </param>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> that can be used to cancel the asynchronous operation.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation. The task result contains an <see cref="NbtTag"/> instance representing the read tag,
    /// or null if the tag type is End.
    /// </returns>
    /// <exception cref="EndOfStreamException">
    /// Thrown if the stream ends unexpectedly while reading the tag type.
    /// </exception>
    /// <exception cref="IOException">
    /// Thrown if an unsupported tag type is encountered or an unexpected TAG_End is found during tag reading.
    /// </exception>
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

    /// <summary>
    /// Reads a UTF-8 string from the stream asynchronously.
    /// </summary>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> that can be used to cancel the asynchronous operation.
    /// The default value is <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>
    /// A <see cref="string"/> representing the UTF-8 string read from the stream.
    /// </returns>
    /// <exception cref="IOException">
    /// Thrown if the string length is invalid or the stream ends unexpectedly while reading the string.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// Thrown if the reader has already been disposed.
    /// </exception>
    public async ValueTask<string> ReadStringAsync(CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        short length = await ReadShortAsync(cancellationToken).ConfigureAwait(false);
        if (length < 0) throw new IOException($"Invalid string length: {length}");
        byte[] stringValue = await ReadAsync(length, cancellationToken).ConfigureAwait(false);
        return ModifiedUtf8.GetString(stringValue);
    }

    /// <summary>
    /// Asynchronously reads a 4-byte integer from the stream in big-endian order.
    /// </summary>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> that can be used to cancel the asynchronous operation.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the integer value read from the stream.
    /// </returns>
    /// <exception cref="ObjectDisposedException">
    /// Thrown if the reader has already been disposed.
    /// </exception>
    /// <exception cref="EndOfStreamException">
    /// Thrown if the stream ends unexpectedly while reading the required bytes.
    /// </exception>
    /// <exception cref="IOException">
    /// Thrown if an I/O error occurs while reading from the stream.
    /// </exception>
    public async ValueTask<int> ReadIntAsync(CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        byte[] buffer = await ReadAsync(sizeof(int), cancellationToken).ConfigureAwait(false);
        return IPAddress.NetworkToHostOrder(BitConverter.ToInt32(buffer, 0));
    }

    /// <summary>
    /// Reads a single-precision floating-point value from the stream asynchronously.
    /// </summary>
    /// <param name="cancellationToken">
    /// A token to monitor for cancellation requests. The default value is <see cref="CancellationToken.None"/>.
    /// </param>
    /// <returns>
    /// A single-precision floating-point value read from the underlying stream.
    /// </returns>
    /// <exception cref="ObjectDisposedException">
    /// Thrown if the reader has been disposed.
    /// </exception>
    /// <exception cref="EndOfStreamException">
    /// Thrown if the stream ends unexpectedly while reading the required data.
    /// </exception>
    /// <exception cref="IOException">
    /// Thrown if an input/output error occurs during the read operation.
    /// </exception>
    public async ValueTask<float> ReadFloatAsync(CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        byte[] buffer = await ReadAsync(sizeof(float), cancellationToken).ConfigureAwait(false);
        return NetworkToHostOrder(BitConverter.ToSingle(buffer, 0));
    }

    /// <summary>
    /// Asynchronously reads a double value from the underlying stream.
    /// </summary>
    /// <param name="cancellationToken">
    /// A token to monitor for cancellation requests. If cancellation is requested, the operation will be aborted.
    /// </param>
    /// <returns>
    /// A <see cref="double"/> value that was read from the stream.
    /// </returns>
    /// <exception cref="ObjectDisposedException">
    /// Thrown if the reader has been disposed prior to calling this method.
    /// </exception>
    /// <exception cref="EndOfStreamException">
    /// Thrown if the stream ends unexpectedly while attempting to read the bytes for the double value.
    /// </exception>
    /// <exception cref="IOException">
    /// Thrown if an I/O error occurs during the asynchronous read operation.
    /// </exception>
    public async ValueTask<double> ReadDoubleAsync(CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        byte[] buffer = await ReadAsync(sizeof(double), cancellationToken).ConfigureAwait(false);
        return NetworkToHostOrder(buffer);
    }

    /// <summary>
    /// Asynchronously reads a 16-bit signed integer (short) from the stream in network byte order.
    /// </summary>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> that can be used to cancel the asynchronous operation.
    /// </param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> representing the asynchronous operation,
    /// with a result of the 16-bit signed integer (short) read from the stream.
    /// </returns>
    /// <exception cref="ObjectDisposedException">
    /// Thrown if the method is called on a disposed stream.
    /// </exception>
    /// <exception cref="EndOfStreamException">
    /// Thrown if the stream ends unexpectedly while reading.
    /// </exception>
    /// <exception cref="IOException">
    /// Thrown if there is an error during the read operation.
    /// </exception>
    public async ValueTask<short> ReadShortAsync(CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        byte[] buffer = await ReadAsync(sizeof(short), cancellationToken).ConfigureAwait(false);
        return IPAddress.NetworkToHostOrder(BitConverter.ToInt16(buffer, 0));
    }

    /// <summary>
    /// Reads a 64-bit signed integer (long) from the stream asynchronously.
    /// </summary>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> used to monitor for cancellation requests
    /// while performing the asynchronous operation.
    /// </param>
    /// <returns>
    /// A task that represents the asynchronous operation. The task result contains the 64-bit signed integer
    /// read from the stream in host byte order.
    /// </returns>
    /// <exception cref="ObjectDisposedException">
    /// Thrown if the method is called after the <see cref="NbtReader"/> is disposed.
    /// </exception>
    /// <exception cref="EndOfStreamException">
    /// Thrown if the stream does not contain enough bytes to read a 64-bit signed integer.
    /// </exception>
    /// <exception cref="IOException">
    /// Thrown if an I/O error occurs while reading from the stream.
    /// </exception>
    public async ValueTask<long> ReadLongAsync(CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        byte[] buffer = await ReadAsync(sizeof(long), cancellationToken).ConfigureAwait(false);
        return IPAddress.NetworkToHostOrder(BitConverter.ToInt64(buffer, 0));
    }

    /// <summary>
    /// Reads a variable-length integer (VarInt) asynchronously from the stream.
    /// </summary>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> that can be used to cancel the read operation.
    /// </param>
    /// <returns>
    /// A task representing the asynchronous operation. The result contains the integer value read.
    /// </returns>
    /// <exception cref="ObjectDisposedException">
    /// Thrown if the NbtReader instance has been disposed.
    /// </exception>
    /// <exception cref="IOException">
    /// Thrown if an I/O error occurs during the read operation.
    /// </exception>
    /// <exception cref="EndOfStreamException">
    /// Thrown if the end of the stream is reached before the full VarInt can be read.
    /// </exception>
    public async ValueTask<int> ReadVarIntAsync(CancellationToken cancellationToken = default)
    {
        CheckDisposed();
        (_, int value) = await ReadVarIntWithBytesReadAsync(cancellationToken).ConfigureAwait(false);
        return value;
    }

    /// <summary>
    /// Reads a variable-length integer from the stream asynchronously,
    /// returning both the value and the total number of bytes read.
    /// </summary>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> that can be used to cancel the operation.
    /// </param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> that represents the asynchronous operation, containing a value tuple.
    /// The tuple consists of an integer representing the number of bytes read and an integer representing the value of the variable-length integer.
    /// </returns>
    /// <exception cref="ObjectDisposedException">
    /// Thrown if the underlying stream has already been disposed.
    /// </exception>
    /// <exception cref="IOException">
    /// Thrown if an I/O error occurs while reading the stream.
    /// </exception>
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

    /// <summary>
    /// Reads a specified number of bytes asynchronously from the underlying stream.
    /// </summary>
    /// <param name="length">
    /// The number of bytes to read. Must be a non-negative integer.
    /// If zero, an empty byte array is returned.
    /// </param>
    /// <param name="cancellationToken">
    /// A token to monitor for cancellation requests. If the operation is canceled, a <see cref="OperationCanceledException"/> may be thrown.
    /// </param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> containing a byte array with the requested number of bytes.
    /// If the stream ends prematurely or the length is invalid, appropriate exceptions may be thrown.
    /// </returns>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if the length is negative.
    /// </exception>
    /// <exception cref="EndOfStreamException">
    /// Thrown if the stream ends unexpectedly before the requested number of bytes can be read.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// Thrown if the stream has already been disposed.
    /// </exception>
    /// <exception cref="IOException">
    /// Thrown if an I/O error occurs during the async read operation.
    /// </exception>
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

    /// <summary>
    /// Asynchronously reads a single byte from the underlying stream, ensuring that the operation completes successfully.
    /// </summary>
    /// <param name="cancellationToken">
    /// A <see cref="CancellationToken"/> that can be used to cancel the asynchronous operation.
    /// </param>
    /// <returns>
    /// A <see cref="ValueTask{TResult}"/> that represents the asynchronous operation.
    /// The result is the byte that was read from the stream.
    /// </returns>
    /// <exception cref="EndOfStreamException">
    /// Thrown if the end of the stream is reached unexpectedly while attempting to read the byte.
    /// </exception>
    /// <exception cref="ObjectDisposedException">
    /// Thrown if the method is called on a disposed reader.
    /// </exception>
    /// <exception cref="IOException">
    /// Thrown if an I/O error occurs during the read operation.
    /// </exception>
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

    /// <summary>
    /// Releases all resources used by the <see cref="NbtReader"/> instance.
    /// </summary>
    /// <remarks>
    /// This method invokes the internal <see cref="Dispose(bool)"/> method to finalize the cleanup process and suppresses the finalization for garbage collection.
    /// </remarks>
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Asynchronously disposes the resources used by the <see cref="NbtReader"/> instance.
    /// </summary>
    /// <returns>
    /// A <see cref="ValueTask"/> representing the asynchronous operation of releasing unmanaged resources and optionally other resources.
    /// </returns>
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

    /// <summary>
    /// Executes core asynchronous disposal logic for the instance.
    /// </summary>
    /// <returns>
    /// A <see cref="ValueTask"/> that represents the asynchronous disposal operation.
    /// </returns>
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