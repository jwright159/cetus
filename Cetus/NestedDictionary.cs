using System.Collections;
using System.Diagnostics.CodeAnalysis;

namespace Cetus;

public class NestedDictionary<TKey, TValue> : IDictionary<TKey, TValue> where TKey : notnull
{
	public IDictionary<TKey, TValue> ThisDict { get; }
	public IDictionary<TKey, TValue> SuperDict { get; }
	public IDictionary<TKey, TValue> ContextDict { get; }
	
	public NestedDictionary(NestedDictionary<TKey, TValue> superDict, IDictionary<TKey, TValue> contextDict)
	{
		ThisDict = new Dictionary<TKey, TValue>();
		SuperDict = new NestedDictionary<TKey, TValue>(superDict.ThisDict, superDict.SuperDict, new Dictionary<TKey, TValue>());
		ContextDict = contextDict;
	}
	
	public NestedDictionary(IDictionary<TKey, TValue> contextDict)
	{
		ThisDict = new Dictionary<TKey, TValue>();
		SuperDict = new Dictionary<TKey, TValue>();
		ContextDict = contextDict;
	}
	
	private NestedDictionary(IDictionary<TKey, TValue> thisDict, IDictionary<TKey, TValue> superDict, IDictionary<TKey, TValue> contextDict)
	{
		ThisDict = thisDict;
		SuperDict = superDict;
		ContextDict = contextDict;
	}
	
	public NestedDictionary()
	{
		ThisDict = new Dictionary<TKey, TValue>();
		SuperDict = new Dictionary<TKey, TValue>();
		ContextDict = new Dictionary<TKey, TValue>();
	}
	
	public ICollection<TKey> Keys => ThisDict.Keys.Concat(SuperDict.Keys).Concat(ContextDict.Keys).ToList();
	public ICollection<TValue> Values => ThisDict.Values.Concat(SuperDict.Values).Concat(ContextDict.Values).ToList();
	
	public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => ThisDict.Concat(SuperDict).Concat(ContextDict).GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	
	public void Add(TKey key, TValue value) => ThisDict.Add(key, value);
	
	public bool ContainsKey(TKey key) => ThisDict.ContainsKey(key) || SuperDict.ContainsKey(key) || ContextDict.ContainsKey(key);
	
	public bool Remove(TKey key) => ThisDict.Remove(key);
	
	public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) => ThisDict.TryGetValue(key, out value) || SuperDict.TryGetValue(key, out value) || ContextDict.TryGetValue(key, out value);
	
	public TValue this[TKey key]
	{
		get => ThisDict.TryGetValue(key, out TValue? value) ? value : SuperDict.TryGetValue(key, out value) ? value : ContextDict.TryGetValue(key, out value) ? value : throw new KeyNotFoundException();
		set => ThisDict[key] = value;
	}
	
	public void Add(KeyValuePair<TKey, TValue> item) => ThisDict.Add(item.Key, item.Value);
	
	public void Clear() => ThisDict.Clear();
	
	public bool Contains(KeyValuePair<TKey, TValue> item) => ThisDict.Contains(item) || SuperDict.Contains(item) || ContextDict.Contains(item);
	
	public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex)
	{
		ThisDict.CopyTo(array, arrayIndex);
		SuperDict.CopyTo(array, arrayIndex + ThisDict.Count);
		ContextDict.CopyTo(array, arrayIndex + ThisDict.Count + SuperDict.Count);
	}
	
	public bool Remove(KeyValuePair<TKey, TValue> item) => ThisDict.Remove(item);
	
	public int Count => ThisDict.Count + SuperDict.Count + ContextDict.Count;
	public bool IsReadOnly => false;
}