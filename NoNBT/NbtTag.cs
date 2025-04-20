namespace NoNBT;

/// <summary>
/// Represents the base class for all NBT types.
/// </summary>
public abstract class NbtTag(string? name)
{
    /// <summary>
    /// Gets the name of the NBT tag. It can be null if no name is assigned.
    /// </summary>
    public string? Name { get; init; } = name;

    /// <summary>
    /// Gets the specific type of the NBT tag, represented as an <see cref="NbtTagType"/>.
    /// This property is implemented by derived classes to indicate their respective NBT tag type.
    /// </summary>
    public abstract NbtTagType TagType { get; }

    /// <summary>
    /// Creates a deep copy of the current NBT tag and its associated data.
    /// </summary>
    /// <returns>A new <see cref="NbtTag"/> instance that is a deep copy of the current tag.</returns>
    public abstract NbtTag Clone();

    /// <summary>
    /// Returns a string representation of the current NBT tag, including its type and name.
    /// </summary>
    /// <returns>A string that represents the NBT tag with its type and name.</returns>
    public override string ToString()
    {
        return $"[{TagType}] {Name ?? "''"}";
    }
    
    protected static string EscapeString(string s)
    {
        if (s == null) return "null";

        var sb = new System.Text.StringBuilder();
        foreach (char c in s)
        {
            switch (c)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append(@"\\"); break;
                case '\b': sb.Append("\\b"); break;
                case '\f': sb.Append("\\f"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default:
                    if (c < ' ')
                    {
                        sb.Append($"\\u{(int)c:x4}");
                    }
                    else
                    {
                        sb.Append(c);
                    }
                    break;
            }
        }
        return sb.ToString();
    }

    /// <summary>
    /// Converts the current NBT tag and its associated data to a JSON-formatted string representation.
    /// Useful for debugging or testing purposes, not for serialization.
    /// </summary>
    /// <param name="indentLevel">The level of indentation applied to the JSON output. Default is 0.</param>
    /// <returns>A JSON-formatted string representation of the current NBT tag.</returns>
    public abstract string ToJson(int indentLevel = 0);

    protected static string GetIndent(int indentLevel)
    {
        return new string(' ', indentLevel * 2);
    }

    protected string FormatPropertyName(bool requireQuotes = true)
    {
        if (string.IsNullOrEmpty(Name)) return "";

        string formattedName = requireQuotes ? $"\"{EscapeString(Name)}\"" : EscapeString(Name);
        return $"{formattedName}: ";
    }
}