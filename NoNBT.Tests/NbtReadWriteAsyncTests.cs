using Microsoft.VisualStudio.TestTools.UnitTesting;
using NoNBT.Tags;

namespace NoNBT.Tests;

[TestClass]
public class NbtReadWriteAsyncTests
{
    private static void AssertTagEquals(NbtTag? expected, NbtTag? actual) => NbtReadWriteTests.AssertTagEquals(expected, actual);

    private static async Task<(T?, T?)> ReadWriteTagAsync<T>(T tag) where T : NbtTag
    {
        using var ms = new MemoryStream();
        var writer = new NbtWriter(ms, true);
        await writer.WriteTagAsync(tag, named: true);
        await writer.DisposeAsync();

        ms.Position = 0;

        var reader = new NbtReader(ms, true);
        var readTag = await reader.ReadTagAsync(named: true) as T;
        await reader.DisposeAsync();

        return (tag, readTag);
    }

    [TestMethod]
    public async Task TestByteTagAsync()
    {
        (ByteTag? original, ByteTag? read) = await ReadWriteTagAsync(new ByteTag("TestByte", 123));
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public async Task TestShortTagAsync()
    {
        (ShortTag? original, ShortTag? read) = await ReadWriteTagAsync(new ShortTag("TestShort", -12345));
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public async Task TestIntTagAsync()
    {
        (NbtInt? original, NbtInt? read) = await ReadWriteTagAsync(new NbtInt("TestInt", 1234567890));
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public async Task TestLongTagAsync()
    {
        (LongTag? original, LongTag? read) = await ReadWriteTagAsync(new LongTag("TestLong", -1234567890123456789L));
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public async Task TestFloatTagAsync()
    {
        (FloatTag? original, FloatTag? read) = await ReadWriteTagAsync(new FloatTag("TestFloat", 123.456f));
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public async Task TestDoubleTagAsync()
    {
        (DoubleTag? original, DoubleTag? read) = await ReadWriteTagAsync(new DoubleTag("TestDouble", -12345.67890));
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public async Task TestStringTagAsync()
    {
        (StringTag? original, StringTag? read) = await ReadWriteTagAsync(new StringTag("TestString", "Hello, World! 🔥 \u00A9\u2122"));
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public async Task TestEmptyStringTagAsync()
    {
        (StringTag? original, StringTag? read) = await ReadWriteTagAsync(new StringTag("TestEmptyString", ""));
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public async Task TestByteArrayTagAsync()
    {
        byte[] bytes = Enumerable.Range(0, 100).Select(i => (byte)i).ToArray();
        (ByteArrayTag? original, ByteArrayTag? read) = await ReadWriteTagAsync(new ByteArrayTag("TestByteArray", bytes));
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public async Task TestEmptyByteArrayTagAsync()
    {
        (ByteArrayTag? original, ByteArrayTag? read) = await ReadWriteTagAsync(new ByteArrayTag("TestEmptyByteArray", []));
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public async Task TestIntArrayTagAsync()
    {
        int[] ints = Enumerable.Range(0, 50).Select(i => i * 1000 - 25000).ToArray();
        (IntArrayTag? original, IntArrayTag? read) = await ReadWriteTagAsync(new IntArrayTag("TestIntArray", ints));
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public async Task TestEmptyIntArrayTagAsync()
    {
        (IntArrayTag? original, IntArrayTag? read) = await ReadWriteTagAsync(new IntArrayTag("TestEmptyIntArray", []));
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public async Task TestLongArrayTagAsync()
    {
        long[] longs = Enumerable.Range(0, 50).Select(i => i * 1000000000L - 25000000000L).ToArray();
        (LongArrayTag? original, LongArrayTag? read) = await ReadWriteTagAsync(new LongArrayTag("TestLongArray", longs));
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public async Task TestEmptyLongArrayTagAsync()
    {
        (LongArrayTag? original, LongArrayTag? read) = await ReadWriteTagAsync(new LongArrayTag("TestEmptyLongArray", []));
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public async Task TestListTag_IntAsync()
    {
        var list = new ListTag("TestListInt", NbtTagType.Int)
        {
            new NbtInt(null, 1),
            new NbtInt(null, 2),
            new NbtInt(null, 3)
        };
        (ListTag? original, ListTag? read) = await ReadWriteTagAsync(list);
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public async Task TestListTag_StringAsync()
    {
        var list = new ListTag("TestListString", NbtTagType.String)
        {
            new StringTag(null, "A"),
            new StringTag(null, "B"),
            new StringTag(null, "C")
        };
        (ListTag? original, ListTag? read) = await ReadWriteTagAsync(list);
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public async Task TestListTag_EmptyAsync()
    {
        var list = new ListTag("TestListEmpty", NbtTagType.End);
        (ListTag? original, ListTag? read) = await ReadWriteTagAsync(list);
        AssertTagEquals(original, read);

        var list2 = new ListTag("TestListEmptyTyped", NbtTagType.Byte);
        (ListTag? original2, ListTag? read2) = await ReadWriteTagAsync(list2);
        AssertTagEquals(original2, read2);
    }

    [TestMethod]
    public async Task TestListTag_CompoundAsync()
    {
        var list = new ListTag("TestListCompound", NbtTagType.Compound);
        var c1 = new CompoundTag(null)
        {
            new StringTag("ItemName", "First"),
            new NbtInt("ItemValue", 10)
        };
        list.Add(c1);

        var c2 = new CompoundTag(null)
        {
            new StringTag("ItemName", "Second"),
            new NbtInt("ItemValue", 20)
        };
        list.Add(c2);

        (ListTag? original, ListTag? read) = await ReadWriteTagAsync(list);
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public async Task TestCompoundTag_SimpleAsync()
    {
        var compound = new CompoundTag("TestCompoundSimple")
        {
            new ByteTag("MyByte", 1),
            new StringTag("MyString", "Value"),
            new LongTag("MyLong", 9876543210L)
        };

        (CompoundTag? original, CompoundTag? read) = await ReadWriteTagAsync(compound);
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public async Task TestCompoundTag_EmptyAsync()
    {
        var compound = new CompoundTag("TestCompoundEmpty");
        (CompoundTag? original, CompoundTag? read) = await ReadWriteTagAsync(compound);
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public async Task TestCompoundTag_NestedAsync()
    {
        var root = new CompoundTag("Root") { new NbtInt("RootInt", 100) };

        var nested = new CompoundTag("Nested") { new FloatTag("NestedFloat", 3.14f) };

        var list = new ListTag("NestedList", NbtTagType.Short)
        {
            new ShortTag(null, 10),
            new ShortTag(null, 20)
        };
        nested.Add(list);

        root.Add(nested);
        root.Add(new StringTag("RootString", "End"));

        (CompoundTag? original, CompoundTag? read) = await ReadWriteTagAsync(root);
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public async Task TestBigCompoundAsync()
    {
        var root = new CompoundTag("Level")
        {
            new NbtInt("intTest", int.MaxValue),
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
        for (var i = 0; i < 2; i++)
        {
            var c = new CompoundTag(null)
            {
                new LongTag("created-on", 1264099775885L + i),
                new StringTag("name", $"Compound tag #{i}")
            };
            listCompound.Add(c);
        }

        root.Add(listCompound);

        var nestedCompound = new CompoundTag("nested compound test");
        var egg = new CompoundTag("egg")
        {
            new StringTag("name", "Eggbert"),
            new FloatTag("value", 0.5f)
        };
        nestedCompound.Add(egg);
        var ham = new CompoundTag("ham")
        {
            new StringTag("name", "Hampus"),
            new FloatTag("value", 0.75f)
        };
        nestedCompound.Add(ham);
        root.Add(nestedCompound);


        (CompoundTag? original, CompoundTag? read) = await ReadWriteTagAsync(root);
        AssertTagEquals(original, read);
    }
}