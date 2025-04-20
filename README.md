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

## Features
*   **Read & Write NBT Data:** Supports reading from and writing to streams.
*   **Full Tag Support:** Implements all standard NBT tags:
    *   `TAG_End` (handled implicitly)
    *   `TAG_Byte`
    *   `TAG_Short`
    *   `TAG_Int`
    *   `TAG_Long`
    *   `TAG_Float`
    *   `TAG_Double`
    *   `TAG_Byte_Array`
    *   `TAG_String` (handles Modified UTF-8)
    *   `TAG_List`
    *   `TAG_Compound`
    *   `TAG_Int_Array`
    *   `TAG_Long_Array`
*   **Modified UTF-8:** Correctly encodes and decodes strings using Minecraft's MUTF-8 specification.
*   **Simple API:** Easy-to-use `NbtReader` and `NbtWriter` classes.
*   **Object Model:** Represents NBT data using a clear hierarchy of `NbtTag` derived classes.
*   **JSON Conversion:** Basic `ToJson()` method on tags for representation (primarily for debugging/testing).
*   **Modern .NET:** Targets `net9.0` (update as needed) with nullable reference types enabled.

## Installation

Install the package via NuGet Package Manager or the .NET CLI:

```bash
dotnet add package NoNBT
```

## Usage

### Reading NBT Data

```csharp

```

> [!NOTE]  
> `NbtReader` expects a raw, decompressed NBT stream. You need to handle GZip/ZLib decompression *before* passing the stream.

### Writing NBT Data

```csharp

```

> [!NOTE]
> `NbtWriter` writes raw NBT data. Apply GZip/ZLib compression *after* writing if needed.*

## Supported NBT Tag Types

The library fully supports the standard NBT tag types via the `NbtTagType` enum and corresponding classes in the `NoNBT.Tags` namespace:

*   `End` = 0 (Implicitly handled by `NbtCompound` and `NbtReader`)
*   `Byte` = 1 (`NbtByte`)
*   `Short` = 2 (`NbtShort`)
*   `Int` = 3 (`NbtInt`)
*   `Long` = 4 (`NbtLong`)
*   `Float` = 5 (`NbtFloat`)
*   `Double` = 6 (`NbtDouble`)
*   `ByteArray` = 7 (`NbtByteArray`)
*   `String` = 8 (`NbtString`)
*   `List` = 9 (`NbtList`)
*   `Compound` = 10 (`NbtCompound`)
*   `IntArray` = 11 (`NbtIntArray`)
*   `LongArray` = 12 (`NbtLongArray`)