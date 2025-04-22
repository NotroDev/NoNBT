using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using NoNBT.Tags;

namespace NoNBT.Benchmarks;

[MemoryDiagnoser]
public class NbtBenchmarks
{
    private CompoundTag _smallCompound = null!;
    private CompoundTag _mediumCompound = null!;
    private CompoundTag _largeCompound = null!;
    private byte[] _smallCompoundBytes = null!;
    private byte[] _mediumCompoundBytes = null!;
    private byte[] _largeCompoundBytes = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Create a small compound tag
        _smallCompound = new CompoundTag("SmallCompound")
        {
            new IntTag("IntValue", 12345),
            new StringTag("StringValue", "Hello World"),
            new ByteTag("ByteValue", 127)
        };

        // Create a medium compound tag with nested structures
        _mediumCompound = new CompoundTag("MediumCompound")
        {
            new IntTag("IntValue", int.MaxValue),
            new StringTag("StringValue", "This is a medium-sized compound tag"),
            new ByteTag("ByteValue", byte.MaxValue),
            new DoubleTag("DoubleValue", 3.14159265359),
            new ListTag("ListOfInts", NbtTagType.Int)
            {
                new IntTag(null, 1),
                new IntTag(null, 2),
                new IntTag(null, 3),
                new IntTag(null, 4),
                new IntTag(null, 5)
            }
        };

        var nestedCompound = new CompoundTag("NestedCompound")
        {
            new StringTag("Name", "Nested Value"),
            new FloatTag("FloatValue", 2.71828f)
        };
        _mediumCompound.Add(nestedCompound);

        // Create a large compound tag similar to the big test in the test files
        _largeCompound = CreateLargeCompoundTag();

        // Pre-serialize tags to bytes for read benchmarks
        _smallCompoundBytes = SerializeTag(_smallCompound);
        _mediumCompoundBytes = SerializeTag(_mediumCompound);
        _largeCompoundBytes = SerializeTag(_largeCompound);
    }

    private static byte[] SerializeTag(NbtTag tag)
    {
        using var ms = new MemoryStream();
        var writer = new NbtWriter(ms, true);
        writer.WriteTag(tag);
        return ms.ToArray();
    }

    private static CompoundTag CreateLargeCompoundTag()
    {
        var root = new CompoundTag("Level")
        {
            new IntTag("intTest", int.MaxValue),
            new ByteTag("byteTest", byte.MaxValue),
            new StringTag("stringTest", "HELLO WORLD THIS IS A TEST STRING ÅÄÖ!"),
            new DoubleTag("doubleTest", 0.49312871321823148d),
            new FloatTag("floatTest", 0.49823147f),
            new LongTag("longTest", long.MaxValue),
            new ShortTag("shortTest", short.MaxValue)
        };

        var byteArr = new byte[1000];
        for (var n = 0; n < 1000; n++)
        {
            byteArr[n] = (byte)((n * n * 255 + n * 7) % 100);
        }

        root.Add(new ByteArrayTag("byteArrayTest", byteArr));

        var listLong = new ListTag("listTest (long)", NbtTagType.Long);
        for (long i = 0; i < 5; i++) listLong.Add(new LongTag(null, 11 + i));
        root.Add(listLong);

        var listCompound = new ListTag("listTest (compound)", NbtTagType.Compound);
        for (var i = 0; i < 10; i++)
        {
            var c = new CompoundTag(null)
            {
                new LongTag("created-on", 1264099775885L + i),
                new StringTag("name", $"Compound tag #{i}"),
                new IntTag("value", i * 100)
            };
            listCompound.Add(c);
        }

        root.Add(listCompound);

        // Add some array data
        var intArray = new int[100];
        for (var i = 0; i < 100; i++)
        {
            intArray[i] = i * 1000 - 50000;
        }
        root.Add(new IntArrayTag("intArrayTest", intArray));

        var longArray = new long[100];
        for (var i = 0; i < 100; i++)
        {
            longArray[i] = i * 1000000000L - 50000000000L;
        }
        root.Add(new LongArrayTag("longArrayTest", longArray));

        // Add 5 levels of nested compounds
        CompoundTag current = root;
        for (var i = 0; i < 5; i++)
        {
            var nested = new CompoundTag($"level{i}")
            {
                new StringTag("name", $"Level {i}"),
                new IntTag("depth", i)
            };
            current.Add(nested);
            current = nested;
        }

        return root;
    }

    // Write benchmarks

    [Benchmark]
    public void WriteSmallCompound()
    {
        using var ms = new MemoryStream();
        var writer = new NbtWriter(ms, true);
        writer.WriteTag(_smallCompound);
    }

    [Benchmark]
    public async Task WriteSmallCompoundAsync()
    {
        using var ms = new MemoryStream();
        var writer = new NbtWriter(ms, true);
        await writer.WriteTagAsync(_smallCompound);
    }

    [Benchmark]
    public void WriteMediumCompound()
    {
        using var ms = new MemoryStream();
        var writer = new NbtWriter(ms, true);
        writer.WriteTag(_mediumCompound);
    }

    [Benchmark]
    public async Task WriteMediumCompoundAsync()
    {
        using var ms = new MemoryStream();
        var writer = new NbtWriter(ms, true);
        await writer.WriteTagAsync(_mediumCompound);
    }

    [Benchmark]
    public void WriteLargeCompound()
    {
        using var ms = new MemoryStream();
        var writer = new NbtWriter(ms, true);
        writer.WriteTag(_largeCompound);
    }

    [Benchmark]
    public async Task WriteLargeCompoundAsync()
    {
        using var ms = new MemoryStream();
        var writer = new NbtWriter(ms, true);
        await writer.WriteTagAsync(_largeCompound);
    }

    // Read benchmarks

    [Benchmark]
    public void ReadSmallCompound()
    {
        using var ms = new MemoryStream(_smallCompoundBytes);
        var reader = new NbtReader(ms, true);
        _ = reader.ReadTag();
    }

    [Benchmark]
    public async Task ReadSmallCompoundAsync()
    {
        using var ms = new MemoryStream(_smallCompoundBytes);
        var reader = new NbtReader(ms, true);
        _ = await reader.ReadTagAsync();
    }

    [Benchmark]
    public void ReadMediumCompound()
    {
        using var ms = new MemoryStream(_mediumCompoundBytes);
        var reader = new NbtReader(ms, true);
        _ = reader.ReadTag();
    }

    [Benchmark]
    public async Task ReadMediumCompoundAsync()
    {
        using var ms = new MemoryStream(_mediumCompoundBytes);
        var reader = new NbtReader(ms, true);
        _ = await reader.ReadTagAsync();
    }

    [Benchmark]
    public void ReadLargeCompound()
    {
        using var ms = new MemoryStream(_largeCompoundBytes);
        var reader = new NbtReader(ms, true);
        _ = reader.ReadTag();
    }

    [Benchmark]
    public async Task ReadLargeCompoundAsync()
    {
        using var ms = new MemoryStream(_largeCompoundBytes);
        var reader = new NbtReader(ms, true);
        _ = await reader.ReadTagAsync();
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        // dont use the config yet
        //BenchmarkConfig config = new();
        BenchmarkRunner.Run<NbtBenchmarks>();
    }
}