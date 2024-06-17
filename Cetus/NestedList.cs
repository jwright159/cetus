using System.Collections;

namespace Cetus;

public class NestedCollection<T> : ICollection<T>
{
	public ICollection<T> ThisList { get; }
	public ICollection<T> SuperList { get; }
	public ICollection<T> ContextList { get; }
	
	public NestedCollection(NestedCollection<T> superList, ICollection<T> contextList)
	{
		ThisList = new List<T>();
		SuperList = new NestedCollection<T>(superList.ThisList, superList.SuperList, new List<T>());
		ContextList = contextList;
	}
	
	public NestedCollection(ICollection<T> contextList)
	{
		ThisList = new List<T>();
		SuperList = new List<T>();
		ContextList = contextList;
	}
	
	private NestedCollection(ICollection<T> thisList, ICollection<T> superList, ICollection<T> contextList)
	{
		ThisList = thisList;
		SuperList = superList;
		ContextList = contextList;
	}
	
	public NestedCollection()
	{
		ThisList = new List<T>();
		SuperList = new List<T>();
		ContextList = new List<T>();
	}
	
	public IEnumerator<T> GetEnumerator() => ThisList.Concat(SuperList).Concat(ContextList).GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	
	public void Add(T item) => ThisList.Add(item);
	
	public void Clear() => ThisList.Clear();
	
	public bool Contains(T item) => ThisList.Contains(item) || SuperList.Contains(item) || ContextList.Contains(item);
	
	public void CopyTo(T[] array, int arrayIndex)
	{
		ThisList.CopyTo(array, arrayIndex);
		SuperList.CopyTo(array, arrayIndex + ThisList.Count);
		ContextList.CopyTo(array, arrayIndex + ThisList.Count + SuperList.Count);
	}
	
	public bool Remove(T item) => ThisList.Remove(item);
	
	public int Count => ThisList.Count + SuperList.Count + ContextList.Count;
	public bool IsReadOnly => false;
}