using System.Collections;
using System.Text;

namespace NoNBT.Tags;

public class NbtCompound(string? name) : NbtTag(name), IDictionary<string, NbtTag>
{
    private readonly Dictionary<string, NbtTag> _tags = new();
    
    public override NbtTagType TagType => NbtTagType.Compound;
    
    public NbtCompound(string? name, IEnumerable<KeyValuePair<string, NbtTag>>? initialTags) : this(name)
    {
        if (initialTags == null) return;
        
        foreach (KeyValuePair<string, NbtTag> pair in initialTags)
        {
            Add(pair.Key, pair.Value);
        }
    }
    
    public NbtCompound() : this(null) { }
    
    public NbtTag this[string key]
    {
        get => _tags[key];
        set
        {
            ValidateTag(key, value);
            _tags[key] = value;
        }
    }
    
    public ICollection<string> Keys => _tags.Keys;
    public ICollection<NbtTag> Values => _tags.Values;
    public int Count => _tags.Count;
    public bool IsReadOnly => false;
    
    public void Add(string key, NbtTag value)
    {
        ValidateTag(key, value);
        _tags.Add(key, value);
    }

    public void Add(NbtTag tag)
    {
        if (tag.Name == null)
        {
            throw new ArgumentException("Tag added to NbtCompound cannot have a null name.", nameof(tag));
        }
        
        _tags.Add(tag.Name, tag);
    }
    
    public void Add(string key, object value)
    {
        if (value is NbtTag tag)
        {
            if (tag.Name != null && tag.Name != key)
            {
                throw new ArgumentException($"Tag name '{tag.Name}' does not match the provided key '{key}'.", nameof(key));
            }

            Add(key, tag);
        }
        else
        {
            throw new ArgumentException($"Value must be of type {nameof(NbtTag)}.", nameof(value));
        }
    }
    
    public bool ContainsKey(string key) => _tags.ContainsKey(key);
    public bool Remove(string key) => _tags.Remove(key);
    public bool TryGetValue(string key, out NbtTag value) => _tags.TryGetValue(key, out value!);
    public void Add(KeyValuePair<string, NbtTag> item) => Add(item.Key, item.Value);
    public void Clear() => _tags.Clear();
    public bool Contains(KeyValuePair<string, NbtTag> item) => _tags.Contains(item);
    public void CopyTo(KeyValuePair<string, NbtTag>[] array, int arrayIndex) => ((ICollection<KeyValuePair<string, NbtTag>>)_tags).CopyTo(array, arrayIndex);
    public bool Remove(KeyValuePair<string, NbtTag> item) => ((ICollection<KeyValuePair<string, NbtTag>>)_tags).Remove(item);
    public IEnumerator<KeyValuePair<string, NbtTag>> GetEnumerator() => _tags.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    
    private static void ValidateTag(string key, NbtTag value)
    {
        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentException("Key cannot be null or empty.", nameof(key));
        }
        ArgumentNullException.ThrowIfNull(value);
    }
    
    public override NbtTag Clone()
    {
        IEnumerable<KeyValuePair<string, NbtTag>> clonedTags = _tags.Select(kvp => new KeyValuePair<string, NbtTag>(kvp.Key, kvp.Value.Clone()));
        return new NbtCompound(Name, clonedTags);
    }
    
    public override string ToString()
    {
        return $"{base.ToString()} {{{Count} entries}}";
    }
    
    public override string ToJson(int indentLevel = 0)
    {
        var sb = new StringBuilder();
        string currentIndent = GetIndent(indentLevel);

        sb.Append(currentIndent);
        sb.Append(FormatPropertyName());

        if (Count == 0)
        {
            sb.Append("{}");
            return sb.ToString();
        }

        sb.Append("{\n");

        List<KeyValuePair<string, NbtTag>> tagList = _tags.ToList();
        for (var i = 0; i < tagList.Count; i++)
        {
            KeyValuePair<string, NbtTag> kvp = tagList[i];
            sb.Append(kvp.Value.ToJson(indentLevel + 1));
            if (i < tagList.Count - 1)
            {
                sb.Append(',');
            }
            sb.Append('\n');
        }

        sb.Append(currentIndent);
        sb.Append('}');
        return sb.ToString();
    }
}