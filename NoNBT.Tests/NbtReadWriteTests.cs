using Microsoft.VisualStudio.TestTools.UnitTesting;
using NoNBT.Tags;

namespace NoNBT.Tests;

[TestClass]
public class NbtReadWriteTests
{
    private static (T?, T?) ReadWriteTag<T>(T tag) where T : NbtTag
    {
        using var ms = new MemoryStream();
        var writer = new NbtWriter(ms);
        writer.WriteTag(tag, named: true);

        ms.Position = 0;

        var reader = new NbtReader(ms);
        var readTag = reader.ReadTag(named: true) as T;

        return (tag, readTag);
    }

    private static void AssertTagEquals(NbtTag? expected, NbtTag? actual)
    {
        Assert.IsNotNull(actual, $"Read tag was null, expected {expected?.GetType().Name}");
        Assert.AreEqual(expected!.TagType, actual.TagType, "Tag types do not match.");
        Assert.AreEqual(expected.Name, actual.Name, "Tag names do not match.");

        switch (expected.TagType)
        {
            case NbtTagType.Byte: Assert.AreEqual(((NbtByte)expected).Value, ((NbtByte)actual).Value); break;
            case NbtTagType.Short: Assert.AreEqual(((NbtShort)expected).Value, ((NbtShort)actual).Value); break;
            case NbtTagType.Int: Assert.AreEqual(((NbtInt)expected).Value, ((NbtInt)actual).Value); break;
            case NbtTagType.Long: Assert.AreEqual(((NbtLong)expected).Value, ((NbtLong)actual).Value); break;
            case NbtTagType.Float: Assert.AreEqual(((NbtFloat)expected).Value, ((NbtFloat)actual).Value); break;
            case NbtTagType.Double: Assert.AreEqual(((NbtDouble)expected).Value, ((NbtDouble)actual).Value); break;
            case NbtTagType.String: Assert.AreEqual(((NbtString)expected).Value, ((NbtString)actual).Value); break;
            case NbtTagType.ByteArray:
                CollectionAssert.AreEqual(((NbtByteArray)expected).Value, ((NbtByteArray)actual).Value); break;
            case NbtTagType.IntArray:
                CollectionAssert.AreEqual(((NbtIntArray)expected).Value, ((NbtIntArray)actual).Value); break;
            case NbtTagType.LongArray:
                CollectionAssert.AreEqual(((NbtLongArray)expected).Value, ((NbtLongArray)actual).Value); break;
            case NbtTagType.List:
                var expectedList = (NbtList)expected;
                var actualList = (NbtList)actual;
                Assert.AreEqual(expectedList.ListType, actualList.ListType, "List types do not match.");
                Assert.AreEqual(expectedList.Count, actualList.Count, "List counts do not match.");
                for (var i = 0; i < expectedList.Count; i++)
                {
                    NbtTag expectedElement = expectedList[i];
                    expectedElement.Name = null;
                    AssertTagEquals(expectedElement, actualList[i]);
                }

                break;
            case NbtTagType.Compound:
                var expectedCompound = (NbtCompound)expected;
                var actualCompound = (NbtCompound)actual;
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
        (NbtByte? original, NbtByte? read) = ReadWriteTag(new NbtByte("TestByte", 123));
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public void TestShortTag()
    {
        (NbtShort? original, NbtShort? read) = ReadWriteTag(new NbtShort("TestShort", -12345));
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public void TestIntTag()
    {
        (NbtInt? original, NbtInt? read) = ReadWriteTag(new NbtInt("TestInt", 1234567890));
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public void TestLongTag()
    {
        (NbtLong? original, NbtLong? read) = ReadWriteTag(new NbtLong("TestLong", -1234567890123456789L));
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public void TestFloatTag()
    {
        (NbtFloat? original, NbtFloat? read) = ReadWriteTag(new NbtFloat("TestFloat", 123.456f));
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public void TestDoubleTag()
    {
        (NbtDouble? original, NbtDouble? read) = ReadWriteTag(new NbtDouble("TestDouble", -12345.67890));
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public void TestStringTag()
    {
        (NbtString? original, NbtString? read) =
            ReadWriteTag(new NbtString("TestString", "Hello, World! 🔥 \u00A9\u2122"));
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public void TestEmptyStringTag()
    {
        (NbtString? original, NbtString? read) = ReadWriteTag(new NbtString("TestEmptyString", ""));
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public void TestByteArrayTag()
    {
        byte[] bytes = Enumerable.Range(0, 100).Select(i => (byte)i).ToArray();
        (NbtByteArray? original, NbtByteArray? read) = ReadWriteTag(new NbtByteArray("TestByteArray", bytes));
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public void TestEmptyByteArrayTag()
    {
        (NbtByteArray? original, NbtByteArray? read) = ReadWriteTag(new NbtByteArray("TestEmptyByteArray", []));
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public void TestIntArrayTag()
    {
        int[] ints = Enumerable.Range(0, 50).Select(i => i * 1000 - 25000).ToArray();
        (NbtIntArray? original, NbtIntArray? read) = ReadWriteTag(new NbtIntArray("TestIntArray", ints));
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public void TestEmptyIntArrayTag()
    {
        (NbtIntArray? original, NbtIntArray? read) = ReadWriteTag(new NbtIntArray("TestEmptyIntArray", []));
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public void TestLongArrayTag()
    {
        long[] longs = Enumerable.Range(0, 50).Select(i => i * 1000000000L - 25000000000L).ToArray();
        (NbtLongArray? original, NbtLongArray? read) = ReadWriteTag(new NbtLongArray("TestLongArray", longs));
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public void TestEmptyLongArrayTag()
    {
        (NbtLongArray? original, NbtLongArray? read) = ReadWriteTag(new NbtLongArray("TestEmptyLongArray", []));
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public void TestListTag_Int()
    {
        var list = new NbtList("TestListInt", NbtTagType.Int)
        {
            new NbtInt(null, 1),
            new NbtInt(null, 2),
            new NbtInt(null, 3)
        };
        (NbtList? original, NbtList? read) = ReadWriteTag(list);
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public void TestListTag_String()
    {
        var list = new NbtList("TestListString", NbtTagType.String)
        {
            new NbtString(null, "A"),
            new NbtString(null, "B"),
            new NbtString(null, "C")
        };
        (NbtList? original, NbtList? read) = ReadWriteTag(list);
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public void TestListTag_Empty()
    {
        var list = new NbtList("TestListEmpty", NbtTagType.End);
        (NbtList? original, NbtList? read) = ReadWriteTag(list);
        AssertTagEquals(original, read);

        var list2 = new NbtList("TestListEmptyTyped", NbtTagType.Byte);
        (NbtList? original2, NbtList? read2) = ReadWriteTag(list2);
        AssertTagEquals(original2, read2);
    }

    [TestMethod]
    public void TestListTag_Compound()
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

        (NbtList? original, NbtList? read) = ReadWriteTag(list);
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public void TestCompoundTag_Simple()
    {
        var compound = new NbtCompound("TestCompoundSimple")
        {
            new NbtByte("MyByte", 1),
            new NbtString("MyString", "Value"),
            new NbtLong("MyLong", 9876543210L)
        };

        (NbtCompound? original, NbtCompound? read) = ReadWriteTag(compound);
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public void TestCompoundTag_Empty()
    {
        var compound = new NbtCompound("TestCompoundEmpty");
        (NbtCompound? original, NbtCompound? read) = ReadWriteTag(compound);
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public void TestCompoundTag_Nested()
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

        (NbtCompound? original, NbtCompound? read) = ReadWriteTag(root);
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public void TestBigCompound()
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


        (NbtCompound? original, NbtCompound? read) = ReadWriteTag(root);
        AssertTagEquals(original, read);
    }
}