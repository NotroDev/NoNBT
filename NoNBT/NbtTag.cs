namespace NoNBT;

/// <summary>
/// Represents the base class for all NBT tags.
/// </summary>
/// <param name="name">The optional name of the tag.</param>
public abstract class NbtTag(string? name)
{
    /// <summary>
    /// Gets the optional name of the tag.
    /// </summary>
    public string? Name { get; init; } = name;
    
    /// <summary>
    /// Gets the type of this NBT tag.
    /// </summary>
    public abstract NbtTagType TagType { get; }
    
    /// <summary>
    /// Creates a deep copy of this tag.
    /// </summary>
    /// <returns>A new NBT tag that is a copy of this instance.</returns>
    public abstract NbtTag Clone();
    
    /// <summary>
    /// Returns a string representation of this tag.
    /// </summary>
    /// <returns>A string representing this tag, including its type and name.</returns>
    public override string ToString()
    {
        return $"[{TagType}] {Name ?? "''"}";
    }
    
    /// <summary>
    /// Escapes special characters in a string for JSON representation.
    /// </summary>
    /// <param name="s">The string to escape.</param>
    /// <returns>The escaped string.</returns>
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
    /// Converts this tag to a JSON string representation.
    /// </summary>
    /// <param name="indentLevel">The indentation level for formatting.</param>
    /// <returns>A JSON string representing this tag.</returns>
    public abstract string ToJson(int indentLevel = 0);

    /// <summary>
    /// Gets the indentation string for a given level.
    /// </summary>
    /// <param name="indentLevel">The desired indentation level (number of tab equivalents).</param>
    /// <returns>A string consisting of spaces representing the indentation.</returns>
    protected static string GetIndent(int indentLevel)
    {
        return new string(' ', indentLevel * 2);
    }

    /// <summary>
    /// Formats the tag's name into a JSON property string (e.g., "TagName": ).
    /// </summary>
    /// <param name="requireQuotes">Whether to always enclose the name in quotes (standard JSON behavior). If false, only special characters might be escaped.</param>
    /// <returns>The formatted property name string, or an empty string if the tag has no name.</returns>
    protected string FormatPropertyName(bool requireQuotes = true)
    {
        if (string.IsNullOrEmpty(Name)) return "";

        string formattedName = requireQuotes ? $"\"{EscapeString(Name)}\"" : EscapeString(Name);
        return $"{formattedName}: ";
    }
}