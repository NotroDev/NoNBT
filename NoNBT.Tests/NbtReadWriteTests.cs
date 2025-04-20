using Microsoft.VisualStudio.TestTools.UnitTesting;
using NoNBT.Tags;

namespace NoNBT.Tests;

[TestClass]
public class NbtReadWriteTests
{
    private static (T?, T?) ReadWriteTag<T>(T tag) where T : NbtTag
    {
        using var ms = new MemoryStream();
        var writer = new NbtWriter(ms, true);
        writer.WriteTag(tag, named: true);

        ms.Position = 0;

        var reader = new NbtReader(ms, true);
        var readTag = reader.ReadTag(named: true) as T;

        return (tag, readTag);
    }

    public static void AssertTagEquals(NbtTag? expected, NbtTag? actual)
    {
        Assert.IsNotNull(actual, $"Read tag was null, expected {expected?.GetType().Name}");
        Assert.AreEqual(expected!.TagType, actual.TagType, "Tag types do not match.");
        Assert.AreEqual(expected.Name, actual.Name, "Tag names do not match.");

        switch (expected.TagType)
        {
            case NbtTagType.Byte: Assert.AreEqual(((ByteTag)expected).Value, ((ByteTag)actual).Value); break;
            case NbtTagType.Short: Assert.AreEqual(((ShortTag)expected).Value, ((ShortTag)actual).Value); break;
            case NbtTagType.Int: Assert.AreEqual(((IntTag)expected).Value, ((IntTag)actual).Value); break;
            case NbtTagType.Long: Assert.AreEqual(((LongTag)expected).Value, ((LongTag)actual).Value); break;
            case NbtTagType.Float: Assert.AreEqual(((FloatTag)expected).Value, ((FloatTag)actual).Value); break;
            case NbtTagType.Double: Assert.AreEqual(((DoubleTag)expected).Value, ((DoubleTag)actual).Value); break;
            case NbtTagType.String: Assert.AreEqual(((StringTag)expected).Value, ((StringTag)actual).Value); break;
            case NbtTagType.ByteArray:
                CollectionAssert.AreEqual(((ByteArrayTag)expected).Value, ((ByteArrayTag)actual).Value); break;
            case NbtTagType.IntArray:
                CollectionAssert.AreEqual(((IntArrayTag)expected).Value, ((IntArrayTag)actual).Value); break;
            case NbtTagType.LongArray:
                CollectionAssert.AreEqual(((LongArrayTag)expected).Value, ((LongArrayTag)actual).Value); break;
            case NbtTagType.List:
                var expectedList = (ListTag)expected;
                var actualList = (ListTag)actual;
                Assert.AreEqual(expectedList.ListType, actualList.ListType, "List types do not match.");
                Assert.AreEqual(expectedList.Count, actualList.Count, "List counts do not match.");
                for (var i = 0; i < expectedList.Count; i++)
                {
                    NbtTag expectedElement = expectedList[i];
                    AssertTagEquals(expectedElement, actualList[i]);
                }

                break;
            case NbtTagType.Compound:
                var expectedCompound = (CompoundTag)expected;
                var actualCompound = (CompoundTag)actual;
                Assert.AreEqual(expectedCompound.Count, actualCompound.Count, "Compound tag counts do not match.");
                break;
            case NbtTagType.End:
                Assert.Fail("TAG_End should not be compared directly.");
                break;
            default:
                Assert.Fail($"Unsupported tag type for comparison: {expected.TagType}");
                break;
        }
    }


    [TestMethod]
    public void TestByteTag()
    {
        (ByteTag? original, ByteTag? read) = ReadWriteTag(new ByteTag("TestByte", 123));
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public void TestShortTag()
    {
        (ShortTag? original, ShortTag? read) = ReadWriteTag(new ShortTag("TestShort", -12345));
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public void TestIntTag()
    {
        (IntTag? original, IntTag? read) = ReadWriteTag(new IntTag("TestInt", 1234567890));
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public void TestLongTag()
    {
        (LongTag? original, LongTag? read) = ReadWriteTag(new LongTag("TestLong", -1234567890123456789L));
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public void TestFloatTag()
    {
        (FloatTag? original, FloatTag? read) = ReadWriteTag(new FloatTag("TestFloat", 123.456f));
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public void TestDoubleTag()
    {
        (DoubleTag? original, DoubleTag? read) = ReadWriteTag(new DoubleTag("TestDouble", -12345.67890));
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public void TestStringTag()
    {
        (StringTag? original, StringTag? read) =
            ReadWriteTag(new StringTag("TestString", "Hello, World! 🔥 \u00A9\u2122"));
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public void TestEmptyStringTag()
    {
        (StringTag? original, StringTag? read) = ReadWriteTag(new StringTag("TestEmptyString", ""));
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public void TestByteArrayTag()
    {
        byte[] bytes = Enumerable.Range(0, 100).Select(i => (byte)i).ToArray();
        (ByteArrayTag? original, ByteArrayTag? read) = ReadWriteTag(new ByteArrayTag("TestByteArray", bytes));
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public void TestEmptyByteArrayTag()
    {
        (ByteArrayTag? original, ByteArrayTag? read) = ReadWriteTag(new ByteArrayTag("TestEmptyByteArray", []));
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public void TestIntArrayTag()
    {
        int[] ints = Enumerable.Range(0, 50).Select(i => i * 1000 - 25000).ToArray();
        (IntArrayTag? original, IntArrayTag? read) = ReadWriteTag(new IntArrayTag("TestIntArray", ints));
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public void TestEmptyIntArrayTag()
    {
        (IntArrayTag? original, IntArrayTag? read) = ReadWriteTag(new IntArrayTag("TestEmptyIntArray", []));
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public void TestLongArrayTag()
    {
        long[] longs = Enumerable.Range(0, 50).Select(i => i * 1000000000L - 25000000000L).ToArray();
        (LongArrayTag? original, LongArrayTag? read) = ReadWriteTag(new LongArrayTag("TestLongArray", longs));
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public void TestEmptyLongArrayTag()
    {
        (LongArrayTag? original, LongArrayTag? read) = ReadWriteTag(new LongArrayTag("TestEmptyLongArray", []));
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public void TestListTag_Int()
    {
        var list = new ListTag("TestListInt", NbtTagType.Int)
        {
            new IntTag(null, 1),
            new IntTag(null, 2),
            new IntTag(null, 3)
        };
        (ListTag? original, ListTag? read) = ReadWriteTag(list);
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public void TestListTag_String()
    {
        var list = new ListTag("TestListString", NbtTagType.String)
        {
            new StringTag(null, "A"),
            new StringTag(null, "B"),
            new StringTag(null, "C")
        };
        (ListTag? original, ListTag? read) = ReadWriteTag(list);
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public void TestListTag_Empty()
    {
        var list = new ListTag("TestListEmpty", NbtTagType.End);
        (ListTag? original, ListTag? read) = ReadWriteTag(list);
        AssertTagEquals(original, read);

        var list2 = new ListTag("TestListEmptyTyped", NbtTagType.Byte);
        (ListTag? original2, ListTag? read2) = ReadWriteTag(list2);
        AssertTagEquals(original2, read2);
    }

    [TestMethod]
    public void TestListTag_Compound()
    {
        var list = new ListTag("TestListCompound", NbtTagType.Compound);
        var c1 = new CompoundTag(null)
        {
            new StringTag("ItemName", "First"),
            new IntTag("ItemValue", 10)
        };
        list.Add(c1);

        var c2 = new CompoundTag(null)
        {
            new StringTag("ItemName", "Second"),
            new IntTag("ItemValue", 20)
        };
        list.Add(c2);

        (ListTag? original, ListTag? read) = ReadWriteTag(list);
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public void TestCompoundTag_Simple()
    {
        var compound = new CompoundTag("TestCompoundSimple")
        {
            new ByteTag("MyByte", 1),
            new StringTag("MyString", "Value"),
            new LongTag("MyLong", 9876543210L)
        };

        (CompoundTag? original, CompoundTag? read) = ReadWriteTag(compound);
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public void TestCompoundTag_Empty()
    {
        var compound = new CompoundTag("TestCompoundEmpty");
        (CompoundTag? original, CompoundTag? read) = ReadWriteTag(compound);
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public void TestCompoundTag_Nested()
    {
        var root = new CompoundTag("Root") { new IntTag("RootInt", 100) };

        var nested = new CompoundTag("Nested") { new FloatTag("NestedFloat", 3.14f) };

        var list = new ListTag("NestedList", NbtTagType.Short)
        {
            new ShortTag(null, 10),
            new ShortTag(null, 20)
        };
        nested.Add(list);

        root.Add(nested);
        root.Add(new StringTag("RootString", "End"));

        (CompoundTag? original, CompoundTag? read) = ReadWriteTag(root);
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public void TestBigCompound()
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


        (CompoundTag? original, CompoundTag? read) = ReadWriteTag(root);
        AssertTagEquals(original, read);
    }
}