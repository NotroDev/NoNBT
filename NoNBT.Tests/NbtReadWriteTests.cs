using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using NoNBT;
using NoNBT.Tags;
using System.Linq;

namespace NoNBT.Tests;

[TestClass]
public class NbtReadWriteTests
{
    private (T?, T?) ReadWriteTag<T>(T tag) where T : NbtTag
    {
        using var ms = new MemoryStream();
        var writer = new NbtWriter(ms);
        writer.WriteTag(tag, named: true); // Always write root tag as named

        ms.Position = 0;

        var reader = new NbtReader(ms);
        var readTag = reader.ReadTag(named: true) as T; // Always read root tag as named

        return (tag, readTag);
    }

    private void AssertTagEquals(NbtTag? expected, NbtTag? actual)
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
            case NbtTagType.ByteArray: CollectionAssert.AreEqual(((NbtByteArray)expected).Value, ((NbtByteArray)actual).Value); break;
            case NbtTagType.IntArray: CollectionAssert.AreEqual(((NbtIntArray)expected).Value, ((NbtIntArray)actual).Value); break;
            case NbtTagType.LongArray: CollectionAssert.AreEqual(((NbtLongArray)expected).Value, ((NbtLongArray)actual).Value); break;
            case NbtTagType.List:
                var expectedList = (NbtList)expected;
                var actualList = (NbtList)actual;
                Assert.AreEqual(expectedList.ListType, actualList.ListType, "List types do not match.");
                Assert.AreEqual(expectedList.Count, actualList.Count, "List counts do not match.");
                for (int i = 0; i < expectedList.Count; i++)
                {
                    // List elements are nameless, so pass null for expected name comparison within AssertTagEquals
                    var expectedElement = expectedList[i];
                    expectedElement.Name = null; // Ensure name is null for comparison
                    AssertTagEquals(expectedElement, actualList[i]);
                }
                break;
            case NbtTagType.Compound:
                var expectedCompound = (NbtCompound)expected;
                var actualCompound = (NbtCompound)actual;
                Assert.AreEqual(expectedCompound.Count, actualCompound.Count, "Compound tag counts do not match.");
                foreach (var tagPair in expectedCompound)
                {
                    var expectedTag = tagPair.Value;
                    var actualTag = actualCompound[tagPair.Key];
                    Assert.IsNotNull(actualTag, $"Expected tag '{tagPair.Key}' not found in actual compound.");
                    AssertTagEquals(expectedTag, actualTag);
                }
                break;
            case NbtTagType.End:
                // End tags are not directly read/written as standalone root tags
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
        var (original, read) = ReadWriteTag(new NbtByte("TestByte", 123));
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public void TestShortTag()
    {
        var (original, read) = ReadWriteTag(new NbtShort("TestShort", -12345));
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public void TestIntTag()
    {
        var (original, read) = ReadWriteTag(new NbtInt("TestInt", 1234567890));
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public void TestLongTag()
    {
        var (original, read) = ReadWriteTag(new NbtLong("TestLong", -1234567890123456789L));
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public void TestFloatTag()
    {
        var (original, read) = ReadWriteTag(new NbtFloat("TestFloat", 123.456f));
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public void TestDoubleTag()
    {
        var (original, read) = ReadWriteTag(new NbtDouble("TestDouble", -12345.67890));
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public void TestStringTag()
    {
        var (original, read) = ReadWriteTag(new NbtString("TestString", "Hello, World! \u00A9\u2122"));
        AssertTagEquals(original, read);
    }

     [TestMethod]
    public void TestEmptyStringTag()
    {
        var (original, read) = ReadWriteTag(new NbtString("TestEmptyString", ""));
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public void TestByteArrayTag()
    {
        var bytes = Enumerable.Range(0, 100).Select(i => (byte)i).ToArray();
        var (original, read) = ReadWriteTag(new NbtByteArray("TestByteArray", bytes));
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public void TestEmptyByteArrayTag()
    {
        var (original, read) = ReadWriteTag(new NbtByteArray("TestEmptyByteArray", []));
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public void TestIntArrayTag()
    {
        var ints = Enumerable.Range(0, 50).Select(i => i * 1000 - 25000).ToArray();
        var (original, read) = ReadWriteTag(new NbtIntArray("TestIntArray", ints));
        AssertTagEquals(original, read);
    }

     [TestMethod]
    public void TestEmptyIntArrayTag()
    {
        var (original, read) = ReadWriteTag(new NbtIntArray("TestEmptyIntArray", []));
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public void TestLongArrayTag()
    {
        var longs = Enumerable.Range(0, 50).Select(i => (long)i * 1000000000L - 25000000000L).ToArray();
        var (original, read) = ReadWriteTag(new NbtLongArray("TestLongArray", longs));
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public void TestEmptyLongArrayTag()
    {
        var (original, read) = ReadWriteTag(new NbtLongArray("TestEmptyLongArray", []));
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public void TestListTag_Int()
    {
        var list = new NbtList("TestListInt", NbtTagType.Int);
        list.Add(new NbtInt(null, 1));
        list.Add(new NbtInt(null, 2));
        list.Add(new NbtInt(null, 3));
        var (original, read) = ReadWriteTag(list);
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public void TestListTag_String()
    {
        var list = new NbtList("TestListString", NbtTagType.String);
        list.Add(new NbtString(null, "A"));
        list.Add(new NbtString(null, "B"));
        list.Add(new NbtString(null, "C"));
        var (original, read) = ReadWriteTag(list);
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public void TestListTag_Empty()
    {
        // Spec allows TAG_End as type for empty lists
        var list = new NbtList("TestListEmpty", NbtTagType.End);
        var (original, read) = ReadWriteTag(list);
        AssertTagEquals(original, read);

        // Also test with a non-End type but zero elements
        var list2 = new NbtList("TestListEmptyTyped", NbtTagType.Byte);
         var (original2, read2) = ReadWriteTag(list2);
        AssertTagEquals(original2, read2);
    }

    [TestMethod]
    public void TestListTag_Compound()
    {
        var list = new NbtList("TestListCompound", NbtTagType.Compound);
        var c1 = new NbtCompound(null); // Compounds in lists are nameless
        c1.Add(new NbtString("ItemName", "First"));
        c1.Add(new NbtInt("ItemValue", 10));
        list.Add(c1);

        var c2 = new NbtCompound(null);
        c2.Add(new NbtString("ItemName", "Second"));
        c2.Add(new NbtInt("ItemValue", 20));
        list.Add(c2);

        var (original, read) = ReadWriteTag(list);
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public void TestCompoundTag_Simple()
    {
        var compound = new NbtCompound("TestCompoundSimple");
        compound.Add(new NbtByte("MyByte", 1));
        compound.Add(new NbtString("MyString", "Value"));
        compound.Add(new NbtLong("MyLong", 9876543210L));

        var (original, read) = ReadWriteTag(compound);
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public void TestCompoundTag_Empty()
    {
        var compound = new NbtCompound("TestCompoundEmpty");
        var (original, read) = ReadWriteTag(compound);
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public void TestCompoundTag_Nested()
    {
        var root = new NbtCompound("Root");
        root.Add(new NbtInt("RootInt", 100));

        var nested = new NbtCompound("Nested");
        nested.Add(new NbtFloat("NestedFloat", 3.14f));

        var list = new NbtList("NestedList", NbtTagType.Short);
        list.Add(new NbtShort(null, 10));
        list.Add(new NbtShort(null, 20));
        nested.Add(list);

        root.Add(nested);
        root.Add(new NbtString("RootString", "End"));

        var (original, read) = ReadWriteTag(root);
        AssertTagEquals(original, read);
    }

    [TestMethod]
    public void TestBigCompound() // Based loosely on bigtest.nbt structure
    {
        var root = new NbtCompound("Level");

        root.Add(new NbtInt("intTest", int.MaxValue));
        root.Add(new NbtByte("byteTest", byte.MaxValue));
        root.Add(new NbtString("stringTest", "HELLO WORLD THIS IS A TEST STRING ÅÄÖ!"));
        root.Add(new NbtDouble("doubleTest", 0.49312871321823148d));
        root.Add(new NbtFloat("floatTest", 0.49823147f));
        root.Add(new NbtLong("longTest", long.MaxValue));
        root.Add(new NbtShort("shortTest", short.MaxValue));

        var byteArr = new byte[1000];
        for (int n = 0; n < 1000; n++)
        {
            byteArr[n] = (byte)((n * n * 255 + n * 7) % 100);
        }
        root.Add(new NbtByteArray("byteArrayTest", byteArr));

        var listLong = new NbtList("listTest (long)", NbtTagType.Long);
        for (long i = 0; i < 5; i++) listLong.Add(new NbtLong(null, 11 + i));
        root.Add(listLong);

        var listCompound = new NbtList("listTest (compound)", NbtTagType.Compound);
        for (int i = 0; i < 2; i++)
        {
            var c = new NbtCompound(null);
            c.Add(new NbtLong("created-on", 1264099775885L + i));
            c.Add(new NbtString("name", $"Compound tag #{i}"));
            listCompound.Add(c);
        }
        root.Add(listCompound);

        var nestedCompound = new NbtCompound("nested compound test");
        var egg = new NbtCompound("egg");
        egg.Add(new NbtString("name", "Eggbert"));
        egg.Add(new NbtFloat("value", 0.5f));
        nestedCompound.Add(egg);
        var ham = new NbtCompound("ham");
        ham.Add(new NbtString("name", "Hampus"));
        ham.Add(new NbtFloat("value", 0.75f));
        nestedCompound.Add(ham);
        root.Add(nestedCompound);


        var (original, read) = ReadWriteTag(root);
        AssertTagEquals(original, read);
    }
}
