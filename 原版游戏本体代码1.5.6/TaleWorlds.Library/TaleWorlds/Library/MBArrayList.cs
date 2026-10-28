using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace TaleWorlds.Library;

public class MBArrayList<T> : IMBCollection, ICollection, IEnumerable, IEnumerable<T>
{
	private T[] _data;

	public int Count { get; private set; }

	public int Capacity => _data.Length;

	public T[] RawArray => _data;

	public bool IsSynchronized => false;

	public object SyncRoot => null;

	public T this[int index]
	{
		get
		{
			return _data[index];
		}
		set
		{
			_data[index] = value;
		}
	}

	public MBArrayList()
	{
		_data = new T[1];
		Count = 0;
	}

	public MBArrayList(List<T> list)
	{
		_data = list.ToArray();
		Count = _data.Length;
	}

	public MBArrayList(IEnumerable<T> list)
	{
		_data = list.ToArray();
		Count = _data.Length;
	}

	public int IndexOf(T item)
	{
		int result = -1;
		EqualityComparer<T> equalityComparer = EqualityComparer<T>.Default;
		for (int i = 0; i < Count; i++)
		{
			if (equalityComparer.Equals(_data[i], item))
			{
				result = i;
				break;
			}
		}
		return result;
	}

	public bool Contains(T item)
	{
		EqualityComparer<T> equalityComparer = EqualityComparer<T>.Default;
		for (int i = 0; i < Count; i++)
		{
			if (equalityComparer.Equals(_data[i], item))
			{
				return true;
			}
		}
		return false;
	}

	public IEnumerator<T> GetEnumerator()
	{
		for (int i = 0; i < Count; i++)
		{
			yield return _data[i];
		}
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}

	public void Clear()
	{
		for (int i = 0; i < Count; i++)
		{
			_data[i] = default(T);
		}
		Count = 0;
	}

	private void EnsureCapacity(int newMinimumCapacity)
	{
		if (newMinimumCapacity > Capacity)
		{
			T[] array = new T[MathF.Max(Capacity * 2, newMinimumCapacity)];
			CopyTo(array, 0);
			_data = array;
		}
	}

	public void Add(T item)
	{
		EnsureCapacity(Count + 1);
		_data[Count] = item;
		Count++;
	}

	public void AddRange(IEnumerable<T> list)
	{
		foreach (T item in list)
		{
			EnsureCapacity(Count + 1);
			_data[Count] = item;
			Count++;
		}
	}

	public bool Remove(T item)
	{
		int num = IndexOf(item);
		if (num >= 0)
		{
			for (int i = num; i < Count - 1; i++)
			{
				_data[num] = _data[num + 1];
			}
			Count--;
			_data[Count] = default(T);
			return true;
		}
		return false;
	}

	public void CopyTo(Array array, int index)
	{
		if (array is T[] array2)
		{
			for (int i = 0; i < Count; i++)
			{
				array2[i + index] = _data[i];
			}
			return;
		}
		array.GetType().GetElementType();
		object[] array3 = array as object[];
		try
		{
			for (int j = 0; j < Count; j++)
			{
				array3[index++] = _data[j];
			}
		}
		catch (ArrayTypeMismatchException)
		{
			Debug.FailedAssert("Invalid array type", "C:\\BuildAgent\\work\\mb3\\TaleWorlds.Shared\\Source\\Base\\TaleWorlds.Library\\MBArrayList.cs", "CopyTo", 210);
		}
	}
}
