using System.Collections;
using System.Text;

namespace NoNBT.Tags;

/// <summary>
/// Represents an NBT list tag containing a sequence of tags of a single, consistent type.
/// </summary>
/// <param name="name">The name of the tag.</param>
/// <param name="listType">The type of the tags contained within the list.</param>
public class ListTag(string? name, NbtTagType listType) : NbtTag(name), IList<NbtTag>
{
    private readonly List<NbtTag> _tags = [];
    
    /// <summary>
    /// Gets the type of this NBT tag, which is always <see cref="NbtTagType.List"/>.
    /// </summary>
    public override NbtTagType TagType => NbtTagType.List;

    /// <summary>
    /// Gets the type of the tags contained within this list.
    /// </summary>
    public NbtTagType ListType { get; init; } = listType;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="ListTag"/> class with initial tags.
    /// </summary>
    /// <param name="name">The name of the tag.</param>
    /// <param name="listType">The type of the tags contained within the list.</param>
    /// <param name="initialTags">The initial collection of tags to add.</param>
    public ListTag(string? name, NbtTagType listType, IEnumerable<NbtTag>? initialTags) : this(name, listType)
    {
        if (initialTags == null) return;
        foreach (NbtTag tag in initialTags)
        {
            Add(tag);
        }
    }
    
    /// <summary>
    /// Gets or sets the tag at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index of the tag to get or set.</param>
    /// <returns>The tag at the specified index.</returns>
    public NbtTag this[int index]
    {
        get => _tags[index];
        set
        {
            ValidateTag(value);
            _tags[index] = value;
        }
    }
    
    /// <summary>
    /// Gets the number of tags in the list.
    /// </summary>
    public int Count => _tags.Count;

    /// <summary>
    /// Gets a value indicating whether the list is read-only.
    /// </summary>
    public bool IsReadOnly => false;
    
    /// <summary>
    /// Adds an item to the list.
    /// </summary>
    /// <param name="item">The object to add to the list.</param>
    /// <exception cref="ArgumentNullException">Thrown when the item is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the item has a non-null name or its type doesn't match the list type.</exception>
    public void Add(NbtTag item)
    {
        ValidateTag(item);
        _tags.Add(item);
    }
    
    /// <summary>
    /// Removes all tags from the list.
    /// </summary>
    public void Clear() => _tags.Clear();

    /// <summary>
    /// Determines whether the list contains a specific tag.
    /// </summary>
    /// <param name="item">The tag to locate in the list.</param>
    /// <returns>true if the tag is found in the list; otherwise, false.</returns>
    public bool Contains(NbtTag item) => _tags.Contains(item);

    /// <summary>
    /// Copies the elements of the list to an array, starting at a particular array index.
    /// </summary>
    /// <param name="array">The one-dimensional array that is the destination of the elements copied from the list.</param>
    /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
    public void CopyTo(NbtTag[] array, int arrayIndex) => _tags.CopyTo(array, arrayIndex);

    /// <summary>
    /// Returns an enumerator that iterates through the list.
    /// </summary>
    /// <returns>An enumerator for the list.</returns>
    public IEnumerator<NbtTag> GetEnumerator() => _tags.GetEnumerator();

    /// <summary>
    /// Determines the index of a specific tag in the list.
    /// </summary>
    /// <param name="item">The tag to locate in the list.</param>
    /// <returns>The index of the tag if found in the list; otherwise, -1.</returns>
    public int IndexOf(NbtTag item) => _tags.IndexOf(item);
    
    /// <summary>
    /// Inserts an item into the list at the specified index.
    /// </summary>
    /// <param name="index">The zero-based index at which item should be inserted.</param>
    /// <param name="item">The object to insert into the list.</param>
    /// <exception cref="ArgumentNullException">Thrown when the item is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the item has a non-null name or its type doesn't match the list type.</exception>
    public void Insert(int index, NbtTag item)
    {
        ValidateTag(item);
        _tags.Insert(index, item);
    }
    
    /// <summary>
    /// Removes the first occurrence of a specific tag from the list.
    /// </summary>
    /// <param name="item">The tag to remove from the list.</param>
    /// <returns>true if item is successfully removed; otherwise, false. This method also returns false if item was not found in the list.</returns>
    public bool Remove(NbtTag item) => _tags.Remove(item);

    /// <summary>
    /// Removes the tag at the specified index of the list.
    /// </summary>
    /// <param name="index">The zero-based index of the tag to remove.</param>
    public void RemoveAt(int index) => _tags.RemoveAt(index);
    
    /// <summary>
    /// Returns an enumerator that iterates through the list.
    /// </summary>
    /// <returns>An enumerator for the list.</returns>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    
    /// <summary>
    /// Validates a tag before adding or inserting it into the list.
    /// </summary>
    /// <param name="tag">The tag to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when the tag is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the tag has a non-null name or its type doesn't match the list type.</exception>
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
    
    /// <summary>
    /// Creates a deep copy of this tag.
    /// </summary>
    /// <returns>A new <see cref="ListTag"/> with the same name, list type, and copies of all contained tags.</returns>
    public override NbtTag Clone()
    {
        IEnumerable<NbtTag> clonedTags = _tags.Select(tag => tag.Clone());
        return new ListTag(Name, ListType, clonedTags);
    }
    
    /// <summary>
    /// Returns a string representation of this tag.
    /// </summary>
    /// <returns>A string representing this tag, its list type, and the number of entries it contains.</returns>
    public override string ToString()
    {
        return $"{base.ToString()}<{ListType}>[{Count} entries]";
    }
    
    /// <summary>
    /// Converts this tag to a JSON string representation.
    /// </summary>
    /// <param name="indentLevel">The indentation level for formatting.</param>
    /// <returns>A JSON string representing this tag and its contents.</returns>
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