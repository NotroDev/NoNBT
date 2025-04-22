# NoNBT

[![NuGet version](https://img.shields.io/nuget/v/NoNBT.svg)](https://www.nuget.org/packages/NoNBT/)
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT)

NoNBT is a lightweight, straightforward .NET library for reading and writing Minecraft's Named Binary Tag (NBT) data format.

## Features

- **Simple and Easy to Use**: Created specifically for my Minecraft server implementation
- **Full Support for Modified UTF-8**: Properly handles emojis and special characters (for some reason other libraries don't)
- **Java Edition Compatible**: Currently supports the Big Endian format used in Minecraft Java Edition

While NoNBT isn't designed to be a comprehensive NBT library, it covers all the essentials you need for basic NBT operations with minimal overhead. I built it because I needed something reliable and focused for my own projects.

There is **no** SNBT (Stringified NBT) support in this library. If you need to parse SNBT, consider using a different library or implement it yourself.

## Capabilities
*   **Read & Write NBT Data:** Supports reading from and writing to streams
*   **Full Tag Support:** Implements all standard NBT tags
*   **Modified UTF-8:** Correctly encodes and decodes strings using MUTF-8 used by Minecraft
*   **Simple API:** Easy-to-use `NbtReader` and `NbtWriter` classes
*   **Asynchronous API**: Supports async methods
*   **Object Model:** Represents NBT data using a clear hierarchy of `NbtTag` derived classes
*   **JSON Conversion:** Basic `ToJson()` method on tags for debugging/testing purposes

## Installation

Install the package via NuGet Package Manager or the .NET CLI:

```bash
dotnet add package NoNBT
```

## Usage

### Reading NBT Data

```csharp
using var reader = new NbtReader(stream);

NbtTag? tag = reader.ReadTag();

if (tag is CompoundTag rootTag)
{
    string? name = rootTag.Get<StringTag>("name")?.Value;
    int? score = rootTag.Get<IntTag>("score")?.Value;
    Console.WriteLine($"Player {name} has score {score}");
}

// or use the indexer
string? name = (rootTag["name"] as StringTag)?.Value;
```

> [!NOTE]  
> `NbtReader` expects a raw, decompressed NBT stream. You need to handle GZip/ZLib decompression *before* passing the stream.

### Writing NBT Data

```csharp
using var writer = new NbtWriter(stream);

writer.WriteTag(new StringTag("message", "Hello, World!"));

var compound = new CompoundTag("root")
{
    new StringTag("name", "Player1"),
    new IntTag("score", 42)
};
writer.WriteTag(compound);

public void WriteTextComponent(TextComponent? component)
{
    if (component == null) return;
    using NbtWriter writer = new(this);

    CompoundTag tag = component.ToNbt();
    writer.WriteTag(tag, false); // text component root tag is unnamed
}
```

> [!NOTE]
> `NbtWriter` writes raw NBT data. Apply GZip/ZLib compression *after* writing if needed.

## Supported NBT Tag Types

The library fully supports the standard NBT tag types via the `NbtTagType` enum and corresponding classes in the `NoNBT.Tags` namespace:

*   `End` = 0 (Implicitly handled by `CompoundTag` and `NbtReader`)
*   `Byte` = 1 (`ByteTag`)
*   `Short` = 2 (`ShortTag`)
*   `Int` = 3 (`IntTag`)
*   `Long` = 4 (`LongTag`)
*   `Float` = 5 (`FloatTag`)
*   `Double` = 6 (`DoubleTag`)
*   `ByteArray` = 7 (`ByteArrayTag`)
*   `String` = 8 (`StringTag`)
*   `List` = 9 (`ListTag`)
*   `Compound` = 10 (`CompoundTag`)
*   `IntArray` = 11 (`IntArrayTag`)
*   `LongArray` = 12 (`LongArrayTag`)
