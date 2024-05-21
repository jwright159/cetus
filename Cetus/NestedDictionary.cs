using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Cetus;

public class NestedDictionary<TKey, TValue>(IDictionary<TKey, TValue> superDict) : IDictionary<TKey, TValue> where TKey : notnull
{
	public Dictionary<TKey, TValue> ThisDict = new();
	public IDictionary<TKey, TValue> SuperDict => superDict;
	
	public ICollection<TKey> Keys => ThisDict.Keys.Concat(superDict.Keys).ToList();
	public ICollection<TValue> Values => ThisDict.Values.Concat(superDict.Values).ToList();
	
	public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => ThisDict.Concat(superDict).GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	
	public void Add(TKey key, TValue value) => ThisDict.Add(key, value);
	
	public bool ContainsKey(TKey key) => ThisDict.ContainsKey(key) || superDict.ContainsKey(key);
	
	public bool Remove(TKey key) => ThisDict.Remove(key);
	
	public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) => ThisDict.TryGetValue(key, out value) || superDict.TryGetValue(key, out value);
	
	public TValue this[TKey key]
	{
		get => ThisDict.TryGetValue(key, out TValue? value) ? value : superDict.TryGetValue(key, out value) ? value : throw new KeyNotFoundException();
		set => ThisDict[key] = value;
	}
	
	public void Add(KeyValuePair<TKey, TValue> item) => ThisDict.Add(item.Key, item.Value);
	
	public void Clear() => ThisDict.Clear();
	
	public bool Contains(KeyValuePair<TKey, TValue> item) => ThisDict.Contains(item) || superDict.Contains(item);
	
	public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
	{
		(ThisDict as IDictionary).CopyTo(array, arrayIndex);
		superDict.CopyTo(array, arrayIndex + ThisDict.Count);
	}
	
	public bool Remove(KeyValuePair<TKey, TValue> item) => (ThisDict as ICollection<KeyValuePair<TKey, TValue>>).Remove(item);
	
	public int Count => ThisDict.Count + superDict.Count;
	public bool IsReadOnly => false;
}