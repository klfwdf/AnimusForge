using System.Collections;
using System.Collections.Generic;

namespace TaleWorlds.DotNet;

public sealed class NativeArrayEnumerator<T> : IReadOnlyList<T>, IEnumerable<T>, IEnumerable, IReadOnlyCollection<T> where T : struct
{
	private readonly NativeArray _nativeArray;

	public T this[int index] => _nativeArray.GetElementAt<T>(index);

	public int Count => _nativeArray.GetLength<T>();

	public NativeArrayEnumerator(NativeArray nativeArray)
	{
		_nativeArray = nativeArray;
	}

	IEnumerator<T> IEnumerable<T>.GetEnumerator()
	{
		return _nativeArray.GetEnumerator<T>().GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return _nativeArray.GetEnumerator<T>().GetEnumerator();
	}
}
