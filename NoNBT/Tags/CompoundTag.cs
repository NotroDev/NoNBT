using System.Collections;
using System.Text;

namespace NoNBT.Tags;

/// <summary>
/// Represents an NBT compound tag that contains a collection of named tags.
/// </summary>
/// <param name="name">The name of the tag.</param>
public class CompoundTag(string? name) : NbtTag(name), IDictionary<string, NbtTag>, IEnumerable<NbtTag>
{
    private readonly Dictionary<string, NbtTag> _tags = new();
    
    /// <summary>
    /// Gets the type of this NBT tag.
    /// </summary>
    public override NbtTagType TagType => NbtTagType.Compound;
    
    /// <summary>
    /// Initializes a new instance of the <see cref="CompoundTag"/> class with initial tags.
    /// </summary>
    /// <param name="name">The name of the tag.</param>
    /// <param name="initialTags">The initial collection of tags to add.</param>
    public CompoundTag(string? name, IEnumerable<KeyValuePair<string, NbtTag>>? initialTags) : this(name)
    {
        if (initialTags == null) return;
        
        foreach (KeyValuePair<string, NbtTag> pair in initialTags)
        {
            Add(pair.Key, pair.Value);
        }
    }
    
    /// <summary>
    /// Initializes a new instance of the <see cref="CompoundTag"/> class with a null name.
    /// </summary>
    public CompoundTag() : this(null) { }
    
    /// <summary>
    /// Gets or sets the tag with the specified key.
    /// </summary>
    /// <param name="key">The key of the tag to get or set.</param>
    /// <returns>The tag with the specified key.</returns>
    public NbtTag this[string key]
    {
        get => _tags[key];
        set
        {
            ValidateTag(key, value);
            _tags[key] = value;
        }
    }
    
    /// <summary>
    /// Gets a tag of the specified type with the given key, or null if not found or not of the correct type.
    /// </summary>
    /// <typeparam name="T">The type of tag to retrieve.</typeparam>
    /// <param name="key">The key of the tag to get.</param>
    /// <returns>The tag with the specified key and type, or null if not found or not of the correct type.</returns>
    public T? Get<T>(string key) where T : NbtTag
    {
        if (TryGetValue(key, out NbtTag tag) && tag is T typedTag)
        {
            return typedTag;
        }
    
        return null;
    }
    
    /// <summary>
    /// Gets the collection of keys in the compound.
    /// </summary>
    public ICollection<string> Keys => _tags.Keys;
    
    /// <summary>
    /// Gets the collection of values in the compound.
    /// </summary>
    public ICollection<NbtTag> Values => _tags.Values;
    
    /// <summary>
    /// Gets the number of tags in the compound.
    /// </summary>
    public int Count => _tags.Count;
    
    /// <summary>
    /// Gets a value indicating whether the compound is read-only.
    /// </summary>
    public bool IsReadOnly => false;
    
    /// <summary>
    /// Adds a tag with the specified key to the compound.
    /// </summary>
    /// <param name="key">The key for the tag.</param>
    /// <param name="value">The tag to add.</param>
    public void Add(string key, NbtTag value)
    {
        ValidateTag(key, value);
        _tags.Add(key, value);
    }

    /// <summary>
    /// Adds a tag to the compound using its name as the key.
    /// </summary>
    /// <param name="tag">The tag to add.</param>
    /// <exception cref="ArgumentException">Thrown when the tag has a null name.</exception>
    public void Add(NbtTag tag)
    {
        if (tag.Name == null)
        {
            throw new ArgumentException("Tag added to NbtCompound cannot have a null name.", nameof(tag));
        }
        
        _tags.Add(tag.Name, tag);
    }
    
    /// <summary>
    /// Adds a tag with the specified key to the compound.
    /// </summary>
    /// <param name="key">The key for the tag.</param>
    /// <param name="value">The tag to add or an object that can be converted to a tag.</param>
    /// <exception cref="ArgumentException">Thrown when the value is not an NbtTag or the tag name doesn't match the key.</exception>
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
    
    /// <summary>
    /// Determines whether the compound contains a tag with the specified key.
    /// </summary>
    /// <param name="key">The key to locate in the compound.</param>
    /// <returns>true if the compound contains a tag with the key; otherwise, false.</returns>
    public bool ContainsKey(string key) => _tags.ContainsKey(key);
    
    /// <summary>
    /// Removes the tag with the specified key from the compound.
    /// </summary>
    /// <param name="key">The key of the tag to remove.</param>
    /// <returns>true if the tag is successfully removed; otherwise, false.</returns>
    public bool Remove(string key) => _tags.Remove(key);
    
    /// <summary>
    /// Gets the tag associated with the specified key.
    /// </summary>
    /// <param name="key">The key of the tag to get.</param>
    /// <param name="value">When this method returns, contains the tag associated with the specified key, if the key is found; otherwise, the default value for the type of the value parameter.</param>
    /// <returns>true if the compound contains a tag with the specified key; otherwise, false.</returns>
    public bool TryGetValue(string key, out NbtTag value) => _tags.TryGetValue(key, out value!);
    
    /// <summary>
    /// Gets the tag of the specified type associated with the specified key.
    /// </summary>
    /// <typeparam name="T">The type of tag to retrieve.</typeparam>
    /// <param name="key">The key of the tag to get.</param>
    /// <param name="value">When this method returns, contains the tag associated with the specified key if the key is found and the tag is of the correct type; otherwise, the default value for the type parameter.</param>
    /// <returns>true if the compound contains a tag with the specified key and the tag is of the specified type; otherwise, false.</returns>
    public bool TryGetValue<T>(string key, out T value) where T : NbtTag
    {
        if (_tags.TryGetValue(key, out NbtTag? tag) && tag is T typedTag)
        {
            value = typedTag;
            return true;
        }

        value = null!;
        return false;
    }
    
    /// <summary>
    /// Adds a key/tag pair to the compound.
    /// </summary>
    /// <param name="item">The key/tag pair to add.</param>
    public void Add(KeyValuePair<string, NbtTag> item) => Add(item.Key, item.Value);
    
    /// <summary>
    /// Removes all tags from the compound.
    /// </summary>
    public void Clear() => _tags.Clear();
    
    /// <summary>
    /// Determines whether the compound contains a specific key/tag pair.
    /// </summary>
    /// <param name="item">The key/tag pair to locate in the compound.</param>
    /// <returns>true if the compound contains the specified key/tag pair; otherwise, false.</returns>
    public bool Contains(KeyValuePair<string, NbtTag> item) => _tags.Contains(item);
    
    /// <summary>
    /// Copies the elements of the compound to an array, starting at a particular array index.
    /// </summary>
    /// <param name="array">The one-dimensional array that is the destination of the elements copied from the compound.</param>
    /// <param name="arrayIndex">The zero-based index in array at which copying begins.</param>
    public void CopyTo(KeyValuePair<string, NbtTag>[] array, int arrayIndex) => ((ICollection<KeyValuePair<string, NbtTag>>)_tags).CopyTo(array, arrayIndex);
    
    /// <summary>
    /// Removes a specific key/tag pair from the compound.
    /// </summary>
    /// <param name="item">The key/tag pair to remove from the compound.</param>
    /// <returns>true if the key/tag pair was successfully removed from the compound; otherwise, false.</returns>
    public bool Remove(KeyValuePair<string, NbtTag> item) => ((ICollection<KeyValuePair<string, NbtTag>>)_tags).Remove(item);
    
    /// <summary>
    /// Returns an enumerator that iterates through the key/tag pairs in the compound.
    /// </summary>
    /// <returns>An enumerator for the compound.</returns>
    public IEnumerator<KeyValuePair<string, NbtTag>> GetEnumerator() => _tags.GetEnumerator();
    
    /// <summary>
    /// Returns an enumerator that iterates through the compound.
    /// </summary>
    /// <returns>An enumerator for the compound.</returns>
    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    
    /// <summary>
    /// Returns an enumerator that iterates through the tags in the compound.
    /// </summary>
    /// <returns>An enumerator for the tags in the compound.</returns>
    IEnumerator<NbtTag> IEnumerable<NbtTag>.GetEnumerator() => _tags.Values.GetEnumerator();
    
    /// <summary>
    /// Validates a tag before adding it to the compound.
    /// </summary>
    /// <param name="key">The key for the tag.</param>
    /// <param name="value">The tag to validate.</param>
    /// <exception cref="ArgumentException">Thrown when the key is null or empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when the value is null.</exception>
    private static void ValidateTag(string key, NbtTag value)
    {
        if (string.IsNullOrEmpty(key))
        {
            throw new ArgumentException("Key cannot be null or empty.", nameof(key));
        }
        ArgumentNullException.ThrowIfNull(value);
    }
    
    /// <summary>
    /// Creates a deep copy of this tag.
    /// </summary>
    /// <returns>A new <see cref="CompoundTag"/> with the same name and copies of all contained tags.</returns>
    public override NbtTag Clone()
    {
        IEnumerable<KeyValuePair<string, NbtTag>> clonedTags = _tags.Select(kvp => new KeyValuePair<string, NbtTag>(kvp.Key, kvp.Value.Clone()));
        return new CompoundTag(Name, clonedTags);
    }
    
    /// <summary>
    /// Returns a string representation of this tag.
    /// </summary>
    /// <returns>A string representing this tag and the number of entries it contains.</returns>
    public override string ToString()
    {
        return $"{base.ToString()} {{{Count} entries}}";
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