using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Columns;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Filters;
using BenchmarkDotNet.Reports;
using Perfolizer.Horology;

namespace NoNBT.Benchmarks;

[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByCategory)]
[CategoriesColumn]
public class NbtBenchmarks
{
    // NoNBT objects
    private Tags.CompoundTag _noNbtSmallCompound = null!;
    private Tags.CompoundTag _noNbtMediumCompound = null!;
    private Tags.CompoundTag _noNbtLargeCompound = null!;
    private byte[] _noNbtSmallCompoundBytes = null!;
    private byte[] _noNbtMediumCompoundBytes = null!;
    private byte[] _noNbtLargeCompoundBytes = null!;

    // SharpNBT objects
    private SharpNBT.CompoundTag _sharpNbtSmallCompound = null!;
    private SharpNBT.CompoundTag _sharpNbtMediumCompound = null!;
    private SharpNBT.CompoundTag _sharpNbtLargeCompound = null!;
    private byte[] _sharpNbtSmallCompoundBytes = null!;
    private byte[] _sharpNbtMediumCompoundBytes = null!;
    private byte[] _sharpNbtLargeCompoundBytes = null!;

    [GlobalSetup]
    public void Setup()
    {
        SetupNoNbt();
        SetupSharpNbt();
    }

    private void SetupNoNbt()
    {
        // Create a small compound tag
        _noNbtSmallCompound = new Tags.CompoundTag("SmallCompound")
        {
            new Tags.IntTag("IntValue", 12345),
            new Tags.StringTag("StringValue", "Hello World"),
            new Tags.ByteTag("ByteValue", 127)
        };

        // Create a medium compound tag with nested structures
        _noNbtMediumCompound = new Tags.CompoundTag("MediumCompound")
        {
            new Tags.IntTag("IntValue", int.MaxValue),
            new Tags.StringTag("StringValue", "This is a medium-sized compound tag"),
            new Tags.ByteTag("ByteValue", byte.MaxValue),
            new Tags.DoubleTag("DoubleValue", 3.14159265359),
            new Tags.ListTag("ListOfInts", NbtTagType.Int)
            {
                new Tags.IntTag(null, 1),
                new Tags.IntTag(null, 2),
                new Tags.IntTag(null, 3),
                new Tags.IntTag(null, 4),
                new Tags.IntTag(null, 5)
            }
        };

        var nestedCompound = new Tags.CompoundTag("NestedCompound")
        {
            new Tags.StringTag("Name", "Nested Value"),
            new Tags.FloatTag("FloatValue", 2.71828f)
        };
        _noNbtMediumCompound.Add(nestedCompound);

        // Create a large compound tag similar to the big test in the test files
        _noNbtLargeCompound = CreateNoNbtLargeCompoundTag();

        // Pre-serialize tags to bytes for read benchmarks
        _noNbtSmallCompoundBytes = SerializeNoNbtTag(_noNbtSmallCompound);
        _noNbtMediumCompoundBytes = SerializeNoNbtTag(_noNbtMediumCompound);
        _noNbtLargeCompoundBytes = SerializeNoNbtTag(_noNbtLargeCompound);
    }

    private void SetupSharpNbt()
    {
        // Create a small compound tag
        _sharpNbtSmallCompound = new SharpNBT.CompoundTag("SmallCompound")
        {
            new SharpNBT.IntTag("IntValue", 12345),
            new SharpNBT.StringTag("StringValue", "Hello World"),
            new SharpNBT.ByteTag("ByteValue", 127)
        };

        // Create a medium compound tag with nested structures
        _sharpNbtMediumCompound = new SharpNBT.CompoundTag("MediumCompound")
        {
            new SharpNBT.IntTag("IntValue", int.MaxValue),
            new SharpNBT.StringTag("StringValue", "This is a medium-sized compound tag"),
            new SharpNBT.ByteTag("ByteValue", byte.MaxValue),
            new SharpNBT.DoubleTag("DoubleValue", 3.14159265359)
        };

        var sharpListOfInts = new SharpNBT.ListTag("ListOfInts", SharpNBT.TagType.Int)
        {
            new SharpNBT.IntTag(null, 1),
            new SharpNBT.IntTag(null, 2),
            new SharpNBT.IntTag(null, 3),
            new SharpNBT.IntTag(null, 4),
            new SharpNBT.IntTag(null, 5)
        };
        _sharpNbtMediumCompound.Add(sharpListOfInts);

        var sharpNestedCompound = new SharpNBT.CompoundTag("NestedCompound")
        {
            new SharpNBT.StringTag("Name", "Nested Value"),
            new SharpNBT.FloatTag("FloatValue", 2.71828f)
        };
        _sharpNbtMediumCompound.Add(sharpNestedCompound);

        // Create a large compound tag similar to the big test in the test files
        _sharpNbtLargeCompound = CreateSharpNbtLargeCompoundTag();

        // Pre-serialize tags to bytes for read benchmarks
        _sharpNbtSmallCompoundBytes = SerializeSharpNbtTag(_sharpNbtSmallCompound);
        _sharpNbtMediumCompoundBytes = SerializeSharpNbtTag(_sharpNbtMediumCompound);
        _sharpNbtLargeCompoundBytes = SerializeSharpNbtTag(_sharpNbtLargeCompound);
    }

    private static byte[] SerializeNoNbtTag(NbtTag tag)
    {
        using var ms = new MemoryStream();
        var writer = new NbtWriter(ms, true);
        writer.WriteTag(tag);
        return ms.ToArray();
    }

    private static byte[] SerializeSharpNbtTag(SharpNBT.Tag tag)
    {
        using var ms = new MemoryStream();
        var writer = new SharpNBT.TagWriter(ms, SharpNBT.FormatOptions.BigEndian);
        writer.WriteTag(tag);
        return ms.ToArray();
    }

    private static Tags.CompoundTag CreateNoNbtLargeCompoundTag()
    {
        var root = new Tags.CompoundTag("Level")
        {
            new Tags.IntTag("intTest", int.MaxValue),
            new Tags.ByteTag("byteTest", byte.MaxValue),
            new Tags.StringTag("stringTest", "HELLO WORLD THIS IS A TEST STRING ÅÄÖ!"),
            new Tags.DoubleTag("doubleTest", 0.49312871321823148d),
            new Tags.FloatTag("floatTest", 0.49823147f),
            new Tags.LongTag("longTest", long.MaxValue),
            new Tags.ShortTag("shortTest", short.MaxValue)
        };

        var byteArr = new byte[1000];
        for (var n = 0; n < 1000; n++)
        {
            byteArr[n] = (byte)((n * n * 255 + n * 7) % 100);
        }

        root.Add(new Tags.ByteArrayTag("byteArrayTest", byteArr));

        var listLong = new Tags.ListTag("listTest (long)", NbtTagType.Long);
        for (long i = 0; i < 5; i++) listLong.Add(new Tags.LongTag(null, 11 + i));
        root.Add(listLong);

        var listCompound = new Tags.ListTag("listTest (compound)", NbtTagType.Compound);
        for (var i = 0; i < 10; i++)
        {
            var c = new Tags.CompoundTag(null)
            {
                new Tags.LongTag("created-on", 1264099775885L + i),
                new Tags.StringTag("name", $"Compound tag #{i}"),
                new Tags.IntTag("value", i * 100)
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
        root.Add(new Tags.IntArrayTag("intArrayTest", intArray));

        var longArray = new long[100];
        for (var i = 0; i < 100; i++)
        {
            longArray[i] = i * 1000000000L - 50000000000L;
        }
        root.Add(new Tags.LongArrayTag("longArrayTest", longArray));

        // Add 5 levels of nested compounds
        Tags.CompoundTag current = root;
        for (var i = 0; i < 5; i++)
        {
            var nested = new Tags.CompoundTag($"level{i}")
            {
                new Tags.StringTag("name", $"Level {i}"),
                new Tags.IntTag("depth", i)
            };
            current.Add(nested);
            current = nested;
        }

        return root;
    }

    private static SharpNBT.CompoundTag CreateSharpNbtLargeCompoundTag()
    {
        var root = new SharpNBT.CompoundTag("Level")
        {
            new SharpNBT.IntTag("intTest", int.MaxValue),
            new SharpNBT.ByteTag("byteTest", byte.MaxValue),
            new SharpNBT.StringTag("stringTest", "HELLO WORLD THIS IS A TEST STRING ÅÄÖ!"),
            new SharpNBT.DoubleTag("doubleTest", 0.49312871321823148d),
            new SharpNBT.FloatTag("floatTest", 0.49823147f),
            new SharpNBT.LongTag("longTest", long.MaxValue),
            new SharpNBT.ShortTag("shortTest", short.MaxValue)
        };

        var byteArr = new byte[1000];
        for (var n = 0; n < 1000; n++)
        {
            byteArr[n] = (byte)((n * n * 255 + n * 7) % 100);
        }

        root.Add(new SharpNBT.ByteArrayTag("byteArrayTest", byteArr));

        var listLong = new SharpNBT.ListTag("listTest (long)", SharpNBT.TagType.Long);
        for (long i = 0; i < 5; i++) listLong.Add(new SharpNBT.LongTag(null, 11 + i));
        root.Add(listLong);

        var listCompound = new SharpNBT.ListTag("listTest (compound)", SharpNBT.TagType.Compound);
        for (var i = 0; i < 10; i++)
        {
            var c = new SharpNBT.CompoundTag(null)
            {
                new SharpNBT.LongTag("created-on", 1264099775885L + i),
                new SharpNBT.StringTag("name", $"Compound tag #{i}"),
                new SharpNBT.IntTag("value", i * 100)
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
        root.Add(new SharpNBT.IntArrayTag("intArrayTest", intArray));

        var longArray = new long[100];
        for (var i = 0; i < 100; i++)
        {
            longArray[i] = i * 1000000000L - 50000000000L;
        }
        root.Add(new SharpNBT.LongArrayTag("longArrayTest", longArray));

        // Add 5 levels of nested compounds
        SharpNBT.CompoundTag current = root;
        for (var i = 0; i < 5; i++)
        {
            var nested = new SharpNBT.CompoundTag($"level{i}")
            {
                new SharpNBT.StringTag("name", $"Level {i}"),
                new SharpNBT.IntTag("depth", i)
            };
            current.Add(nested);
            current = nested;
        }

        return root;
    }

    // NoNBT Write benchmarks
    [BenchmarkCategory("Write", "Small", "NoNBT")]
    [Benchmark(Baseline = true)]
    public void NoNBT_WriteSmallCompound()
    {
        using var ms = new MemoryStream();
        var writer = new NbtWriter(ms, true);
        writer.WriteTag(_noNbtSmallCompound);
    }

    [BenchmarkCategory("Write", "Small", "NoNBT")]
    [Benchmark]
    public async Task NoNBT_WriteSmallCompoundAsync()
    {
        using var ms = new MemoryStream();
        var writer = new NbtWriter(ms, true);
        await writer.WriteTagAsync(_noNbtSmallCompound);
    }

    [BenchmarkCategory("Write", "Medium", "NoNBT")]
    [Benchmark(Baseline = true)]
    public void NoNBT_WriteMediumCompound()
    {
        using var ms = new MemoryStream();
        var writer = new NbtWriter(ms, true);
        writer.WriteTag(_noNbtMediumCompound);
    }

    [BenchmarkCategory("Write", "Medium", "NoNBT")]
    [Benchmark]
    public async Task NoNBT_WriteMediumCompoundAsync()
    {
        using var ms = new MemoryStream();
        var writer = new NbtWriter(ms, true);
        await writer.WriteTagAsync(_noNbtMediumCompound);
    }

    [BenchmarkCategory("Write", "Large", "NoNBT")]
    [Benchmark(Baseline = true)]
    public void NoNBT_WriteLargeCompound()
    {
        using var ms = new MemoryStream();
        var writer = new NbtWriter(ms, true);
        writer.WriteTag(_noNbtLargeCompound);
    }

    [BenchmarkCategory("Write", "Large", "NoNBT")]
    [Benchmark]
    public async Task NoNBT_WriteLargeCompoundAsync()
    {
        using var ms = new MemoryStream();
        var writer = new NbtWriter(ms, true);
        await writer.WriteTagAsync(_noNbtLargeCompound);
    }

    // SharpNBT Write benchmarks
    [BenchmarkCategory("Write", "Small", "SharpNBT")]
    [Benchmark]
    public void SharpNBT_WriteSmallCompound()
    {
        using var ms = new MemoryStream();
        var writer = new SharpNBT.TagWriter(ms, SharpNBT.FormatOptions.BigEndian);
        writer.WriteTag(_sharpNbtSmallCompound);
    }

    [BenchmarkCategory("Write", "Small", "SharpNBT")]
    [Benchmark]
    public async Task SharpNBT_WriteSmallCompoundAsync()
    {
        using var ms = new MemoryStream();
        var writer = new SharpNBT.TagWriter(ms, SharpNBT.FormatOptions.BigEndian);
        await writer.WriteTagAsync(_sharpNbtSmallCompound);
    }

    [BenchmarkCategory("Write", "Medium", "SharpNBT")]
    [Benchmark]
    public void SharpNBT_WriteMediumCompound()
    {
        using var ms = new MemoryStream();
        var writer = new SharpNBT.TagWriter(ms, SharpNBT.FormatOptions.BigEndian);
        writer.WriteTag(_sharpNbtMediumCompound);
    }

    [BenchmarkCategory("Write", "Medium", "SharpNBT")]
    [Benchmark]
    public async Task SharpNBT_WriteMediumCompoundAsync()
    {
        using var ms = new MemoryStream();
        var writer = new SharpNBT.TagWriter(ms, SharpNBT.FormatOptions.BigEndian);
        await writer.WriteTagAsync(_sharpNbtMediumCompound);
    }

    [BenchmarkCategory("Write", "Large", "SharpNBT")]
    [Benchmark]
    public void SharpNBT_WriteLargeCompound()
    {
        using var ms = new MemoryStream();
        var writer = new SharpNBT.TagWriter(ms, SharpNBT.FormatOptions.BigEndian);
        writer.WriteTag(_sharpNbtLargeCompound);
    }

    [BenchmarkCategory("Write", "Large", "SharpNBT")]
    [Benchmark]
    public async Task SharpNBT_WriteLargeCompoundAsync()
    {
        using var ms = new MemoryStream();
        var writer = new SharpNBT.TagWriter(ms, SharpNBT.FormatOptions.BigEndian);
        await writer.WriteTagAsync(_sharpNbtLargeCompound);
    }

    // NoNBT Read benchmarks
    [BenchmarkCategory("Read", "Small", "NoNBT")]
    [Benchmark(Baseline = true)]
    public void NoNBT_ReadSmallCompound()
    {
        using var ms = new MemoryStream(_noNbtSmallCompoundBytes);
        var reader = new NbtReader(ms, true);
        _ = reader.ReadTag();
    }

    [BenchmarkCategory("Read", "Small", "NoNBT")]
    [Benchmark]
    public async Task NoNBT_ReadSmallCompoundAsync()
    {
        using var ms = new MemoryStream(_noNbtSmallCompoundBytes);
        var reader = new NbtReader(ms, true);
        _ = await reader.ReadTagAsync();
    }

    [BenchmarkCategory("Read", "Medium", "NoNBT")]
    [Benchmark(Baseline = true)]
    public void NoNBT_ReadMediumCompound()
    {
        using var ms = new MemoryStream(_noNbtMediumCompoundBytes);
        var reader = new NbtReader(ms, true);
        _ = reader.ReadTag();
    }

    [BenchmarkCategory("Read", "Medium", "NoNBT")]
    [Benchmark]
    public async Task NoNBT_ReadMediumCompoundAsync()
    {
        using var ms = new MemoryStream(_noNbtMediumCompoundBytes);
        var reader = new NbtReader(ms, true);
        _ = await reader.ReadTagAsync();
    }

    [BenchmarkCategory("Read", "Large", "NoNBT")]
    [Benchmark(Baseline = true)]
    public void NoNBT_ReadLargeCompound()
    {
        using var ms = new MemoryStream(_noNbtLargeCompoundBytes);
        var reader = new NbtReader(ms, true);
        _ = reader.ReadTag();
    }

    [BenchmarkCategory("Read", "Large", "NoNBT")]
    [Benchmark]
    public async Task NoNBT_ReadLargeCompoundAsync()
    {
        using var ms = new MemoryStream(_noNbtLargeCompoundBytes);
        var reader = new NbtReader(ms, true);
        _ = await reader.ReadTagAsync();
    }

    // SharpNBT Read benchmarks
    [BenchmarkCategory("Read", "Small", "SharpNBT")]
    [Benchmark]
    public void SharpNBT_ReadSmallCompound()
    {
        using var ms = new MemoryStream(_sharpNbtSmallCompoundBytes);
        var reader = new SharpNBT.TagReader(ms, SharpNBT.FormatOptions.BigEndian);
        _ = reader.ReadTag();
    }

    [BenchmarkCategory("Read", "Small", "SharpNBT")]
    [Benchmark]
    public async Task SharpNBT_ReadSmallCompoundAsync()
    {
        using var ms = new MemoryStream(_sharpNbtSmallCompoundBytes);
        var reader = new SharpNBT.TagReader(ms, SharpNBT.FormatOptions.BigEndian);
        _ = await reader.ReadTagAsync();
    }

    [BenchmarkCategory("Read", "Medium", "SharpNBT")]
    [Benchmark]
    public void SharpNBT_ReadMediumCompound()
    {
        using var ms = new MemoryStream(_sharpNbtMediumCompoundBytes);
        var reader = new SharpNBT.TagReader(ms, SharpNBT.FormatOptions.BigEndian);
        _ = reader.ReadTag();
    }

    [BenchmarkCategory("Read", "Medium", "SharpNBT")]
    [Benchmark]
    public async Task SharpNBT_ReadMediumCompoundAsync()
    {
        using var ms = new MemoryStream(_sharpNbtMediumCompoundBytes);
        var reader = new SharpNBT.TagReader(ms, SharpNBT.FormatOptions.BigEndian);
        _ = await reader.ReadTagAsync();
    }

    [BenchmarkCategory("Read", "Large", "SharpNBT")]
    [Benchmark]
    public void SharpNBT_ReadLargeCompound()
    {
        using var ms = new MemoryStream(_sharpNbtLargeCompoundBytes);
        var reader = new SharpNBT.TagReader(ms, SharpNBT.FormatOptions.BigEndian);
        _ = reader.ReadTag();
    }

    [BenchmarkCategory("Read", "Large", "SharpNBT")]
    [Benchmark]
    public async Task SharpNBT_ReadLargeCompoundAsync()
    {
        using var ms = new MemoryStream(_sharpNbtLargeCompoundBytes);
        var reader = new SharpNBT.TagReader(ms, SharpNBT.FormatOptions.BigEndian);
        _ = await reader.ReadTagAsync();
    }
}

public class Program
{
    public static void Main(string[] args)
    {
        ManualConfig config = ManualConfig.Create(DefaultConfig.Instance)
            .WithOptions(ConfigOptions.DisableOptimizationsValidator)
            .WithSummaryStyle(SummaryStyle.Default.WithTimeUnit(TimeUnit.Nanosecond))
            .AddFilter(new SyncOnlyFilter());
            
        BenchmarkRunner.Run<NbtBenchmarks>(config);
    }
}

public class SyncOnlyFilter : IFilter
{
    public bool Predicate(BenchmarkCase benchmarkCase)
    {
        return !benchmarkCase.Descriptor.WorkloadMethod.Name.Contains("Async");
    }
}