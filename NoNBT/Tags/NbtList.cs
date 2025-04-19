using System.Collections;
using System.Text;

namespace NoNBT.Tags;

public class NbtList(string? name, NbtTagType listType) : NbtTag(name), IList<NbtTag>
{
    private readonly List<NbtTag> _tags = [];
    
    public override NbtTagType TagType => NbtTagType.List;

    public NbtTagType ListType { get; init; } = listType;
    
    public NbtList(string? name, NbtTagType listType, IEnumerable<NbtTag>? initialTags) : this(name, listType)
    {
        if (initialTags == null) return;
        foreach (NbtTag tag in initialTags)
        {
            Add(tag);
        }
    }
    
    public NbtTag this[int index]
    {
        get => _tags[index];
        set
        {
            ValidateTag(value);
            _tags[index] = value;
        }
    }
    
    public int Count => _tags.Count;
    public bool IsReadOnly => false;
    
    public void Add(NbtTag item)
    {
        ValidateTag(item);
        _tags.Add(item);
    }
    
    public void Clear() => _tags.Clear();
    public bool Contains(NbtTag item) => _tags.Contains(item);
    public void CopyTo(NbtTag[] array, int arrayIndex) => _tags.CopyTo(array, arrayIndex);
    public IEnumerator<NbtTag> GetEnumerator() => _tags.GetEnumerator();
    public int IndexOf(NbtTag item) => _tags.IndexOf(item);
    
    public void Insert(int index, NbtTag item)
    {
        ValidateTag(item);
        _tags.Insert(index, item);
    }
    
    public bool Remove(NbtTag item) => _tags.Remove(item);
    public void RemoveAt(int index) => _tags.RemoveAt(index);
    
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    
    public void ValidateTag(NbtTag tag)
    {
        ArgumentNullException.ThrowIfNull(tag);
        
        if (tag.Name != null)
        {
            throw new ArgumentException("Tag name must be null for list items.", nameof(tag));
        }
        
        if (ListType == NbtTagType.End && _tags.Count == 0 && tag.TagType == NbtTagType.End)
        {
            return;
        }
        if (tag.TagType == NbtTagType.End && ListType != NbtTagType.End)
        {
            throw new ArgumentException("Cannot add an End tag to a non-empty list.", nameof(tag));
        }
        if (ListType != NbtTagType.End && tag.TagType != ListType)
        {
            throw new ArgumentException($"Tag type {tag.TagType} does not match list type {ListType}.", nameof(tag));
        }
    }
    
    public override NbtTag Clone()
    {
        IEnumerable<NbtTag> clonedTags = _tags.Select(tag => tag.Clone());
        return new NbtList(Name, ListType, clonedTags);
    }
    
    public override string ToString()
    {
        return $"{base.ToString()}<{ListType}>[{Count} entries]";
    }
    
    public override string ToJson(int indentLevel = 0)
    {
        var sb = new StringBuilder();
        string currentIndent = GetIndent(indentLevel);

        sb.Append(currentIndent);
        sb.Append(FormatPropertyName());
        if (Count == 0)
        {
            sb.Append("[]");
            return sb.ToString();
        }

        sb.Append("[\n");

        for (var i = 0; i < _tags.Count; i++)
        {
            sb.Append(_tags[i].ToJson(indentLevel + 1));
            if (i < _tags.Count - 1)
            {
                sb.Append(',');
            }
            sb.Append('\n');
        }

        sb.Append(currentIndent);
        sb.Append(']');
        return sb.ToString();
    }
}