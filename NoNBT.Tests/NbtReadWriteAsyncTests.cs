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
        (NbtByte? original, NbtByte? read) = await ReadWriteTagAsync(new NbtByte("TestByte", 123));
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public async Task TestShortTagAsync()
    {
        (NbtShort? original, NbtShort? read) = await ReadWriteTagAsync(new NbtShort("TestShort", -12345));
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
        (NbtLong? original, NbtLong? read) = await ReadWriteTagAsync(new NbtLong("TestLong", -1234567890123456789L));
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public async Task TestFloatTagAsync()
    {
        (NbtFloat? original, NbtFloat? read) = await ReadWriteTagAsync(new NbtFloat("TestFloat", 123.456f));
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public async Task TestDoubleTagAsync()
    {
        (NbtDouble? original, NbtDouble? read) = await ReadWriteTagAsync(new NbtDouble("TestDouble", -12345.67890));
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public async Task TestStringTagAsync()
    {
        (NbtString? original, NbtString? read) = await ReadWriteTagAsync(new NbtString("TestString", "Hello, World! 🔥 \u00A9\u2122"));
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public async Task TestEmptyStringTagAsync()
    {
        (NbtString? original, NbtString? read) = await ReadWriteTagAsync(new NbtString("TestEmptyString", ""));
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public async Task TestByteArrayTagAsync()
    {
        byte[] bytes = Enumerable.Range(0, 100).Select(i => (byte)i).ToArray();
        (NbtByteArray? original, NbtByteArray? read) = await ReadWriteTagAsync(new NbtByteArray("TestByteArray", bytes));
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public async Task TestEmptyByteArrayTagAsync()
    {
        (NbtByteArray? original, NbtByteArray? read) = await ReadWriteTagAsync(new NbtByteArray("TestEmptyByteArray", []));
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public async Task TestIntArrayTagAsync()
    {
        int[] ints = Enumerable.Range(0, 50).Select(i => i * 1000 - 25000).ToArray();
        (NbtIntArray? original, NbtIntArray? read) = await ReadWriteTagAsync(new NbtIntArray("TestIntArray", ints));
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public async Task TestEmptyIntArrayTagAsync()
    {
        (NbtIntArray? original, NbtIntArray? read) = await ReadWriteTagAsync(new NbtIntArray("TestEmptyIntArray", []));
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public async Task TestLongArrayTagAsync()
    {
        long[] longs = Enumerable.Range(0, 50).Select(i => i * 1000000000L - 25000000000L).ToArray();
        (NbtLongArray? original, NbtLongArray? read) = await ReadWriteTagAsync(new NbtLongArray("TestLongArray", longs));
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public async Task TestEmptyLongArrayTagAsync()
    {
        (NbtLongArray? original, NbtLongArray? read) = await ReadWriteTagAsync(new NbtLongArray("TestEmptyLongArray", []));
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public async Task TestListTag_IntAsync()
    {
        var list = new NbtList("TestListInt", NbtTagType.Int)
        {
            new NbtInt(null, 1),
            new NbtInt(null, 2),
            new NbtInt(null, 3)
        };
        (NbtList? original, NbtList? read) = await ReadWriteTagAsync(list);
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public async Task TestListTag_StringAsync()
    {
        var list = new NbtList("TestListString", NbtTagType.String)
        {
            new NbtString(null, "A"),
            new NbtString(null, "B"),
            new NbtString(null, "C")
        };
        (NbtList? original, NbtList? read) = await ReadWriteTagAsync(list);
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public async Task TestListTag_EmptyAsync()
    {
        var list = new NbtList("TestListEmpty", NbtTagType.End);
        (NbtList? original, NbtList? read) = await ReadWriteTagAsync(list);
        AssertTagEquals(original, read);

        var list2 = new NbtList("TestListEmptyTyped", NbtTagType.Byte);
        (NbtList? original2, NbtList? read2) = await ReadWriteTagAsync(list2);
        AssertTagEquals(original2, read2);
    }

    [TestMethod]
    public async Task TestListTag_CompoundAsync()
    {
        var list = new NbtList("TestListCompound", NbtTagType.Compound);
        var c1 = new NbtCompound(null)
        {
            new NbtString("ItemName", "First"),
            new NbtInt("ItemValue", 10)
        };
        list.Add(c1);

        var c2 = new NbtCompound(null)
        {
            new NbtString("ItemName", "Second"),
            new NbtInt("ItemValue", 20)
        };
        list.Add(c2);

        (NbtList? original, NbtList? read) = await ReadWriteTagAsync(list);
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public async Task TestCompoundTag_SimpleAsync()
    {
        var compound = new NbtCompound("TestCompoundSimple")
        {
            new NbtByte("MyByte", 1),
            new NbtString("MyString", "Value"),
            new NbtLong("MyLong", 9876543210L)
        };

        (NbtCompound? original, NbtCompound? read) = await ReadWriteTagAsync(compound);
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public async Task TestCompoundTag_EmptyAsync()
    {
        var compound = new NbtCompound("TestCompoundEmpty");
        (NbtCompound? original, NbtCompound? read) = await ReadWriteTagAsync(compound);
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public async Task TestCompoundTag_NestedAsync()
    {
        var root = new NbtCompound("Root") { new NbtInt("RootInt", 100) };

        var nested = new NbtCompound("Nested") { new NbtFloat("NestedFloat", 3.14f) };

        var list = new NbtList("NestedList", NbtTagType.Short)
        {
            new NbtShort(null, 10),
            new NbtShort(null, 20)
        };
        nested.Add(list);

        root.Add(nested);
        root.Add(new NbtString("RootString", "End"));

        (NbtCompound? original, NbtCompound? read) = await ReadWriteTagAsync(root);
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public async Task TestBigCompoundAsync()
    {
        var root = new NbtCompound("Level")
        {
            new NbtInt("intTest", int.MaxValue),
            new NbtByte("byteTest", byte.MaxValue),
            new NbtString("stringTest", "HELLO WORLD THIS IS A TEST STRING ÅÄÖ!"),
            new NbtDouble("doubleTest", 0.49312871321823148d),
            new NbtFloat("floatTest", 0.49823147f),
            new NbtLong("longTest", long.MaxValue),
            new NbtShort("shortTest", short.MaxValue)
        };

        var byteArr = new byte[1000];
        for (var n = 0; n < 1000; n++)
        {
            byteArr[n] = (byte)((n * n * 255 + n * 7) % 100);
        }

        root.Add(new NbtByteArray("byteArrayTest", byteArr));

        var listLong = new NbtList("listTest (long)", NbtTagType.Long);
        for (long i = 0; i < 5; i++) listLong.Add(new NbtLong(null, 11 + i));
        root.Add(listLong);

        var listCompound = new NbtList("listTest (compound)", NbtTagType.Compound);
        for (var i = 0; i < 2; i++)
        {
            var c = new NbtCompound(null)
            {
                new NbtLong("created-on", 1264099775885L + i),
                new NbtString("name", $"Compound tag #{i}")
            };
            listCompound.Add(c);
        }

        root.Add(listCompound);

        var nestedCompound = new NbtCompound("nested compound test");
        var egg = new NbtCompound("egg")
        {
            new NbtString("name", "Eggbert"),
            new NbtFloat("value", 0.5f)
        };
        nestedCompound.Add(egg);
        var ham = new NbtCompound("ham")
        {
            new NbtString("name", "Hampus"),
            new NbtFloat("value", 0.75f)
        };
        nestedCompound.Add(ham);
        root.Add(nestedCompound);


        (NbtCompound? original, NbtCompound? read) = await ReadWriteTagAsync(root);
        AssertTagEquals(original, read);
    }
}