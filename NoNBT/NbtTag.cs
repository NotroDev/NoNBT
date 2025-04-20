namespace NoNBT;

public abstract class NbtTag(string? name)
{
    public string? Name { get; set; } = name;

    public abstract NbtTagType TagType { get; }
    
    public abstract NbtTag Clone();
    
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