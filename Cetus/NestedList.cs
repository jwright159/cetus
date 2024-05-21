using System.Collections;

namespace Cetus;

public class NestedCollection<T>(ICollection<T> superList) : ICollection<T>
{
	public List<T> ThisList = [];
	public ICollection<T> SuperList => superList;
	
	public IEnumerator<T> GetEnumerator() => ThisList.Concat(superList).GetEnumerator();
	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
	
	public void Add(T item) => ThisList.Add(item);
	
	public void Clear() => ThisList.Clear();
	
	public bool Contains(T item) => ThisList.Contains(item) || superList.Contains(item);
	
	public void CopyTo(T[] array, int arrayIndex)
	{
		ThisList.CopyTo(array, arrayIndex);
		superList.CopyTo(array, arrayIndex + ThisList.Count);
	}
	
	public bool Remove(T item) => ThisList.Remove(item);
	
	public int Count => ThisList.Count + superList.Count;
	public bool IsReadOnly => false;
}