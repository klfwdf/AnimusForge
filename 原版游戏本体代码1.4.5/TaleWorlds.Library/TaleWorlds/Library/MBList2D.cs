namespace TaleWorlds.Library;

public class MBList2D<T> : IMBCollection
{
	private T[] _data;

	public int Count1 { get; private set; }

	public int Count2 { get; private set; }

	private int Capacity => _data.Length;

	public T[] RawArray => _data;

	public T this[int index1, int index2]
	{
		get
		{
			return _data[index1 * Count2 + index2];
		}
		set
		{
			_data[index1 * Count2 + index2] = value;
		}
	}

	public MBList2D(int count1, int count2)
	{
		_data = new T[count1 * count2];
		Count1 = count1;
		Count2 = count2;
	}

	public bool Contains(T item)
	{
		for (int i = 0; i < Count1; i++)
		{
			for (int j = 0; j < Count2; j++)
			{
				ref readonly T reference = ref _data[i * Count2 + j];
				object obj = item;
				if (reference.Equals(obj))
				{
					return true;
				}
			}
		}
		return false;
	}

	public void Clear()
	{
		for (int i = 0; i < Count1 * Count2; i++)
		{
			_data[i] = default(T);
		}
	}

	public void ResetWithNewCount(int newCount1, int newCount2)
	{
		if (Count1 != newCount1 || Count2 != newCount2)
		{
			Count1 = newCount1;
			Count2 = newCount2;
			if (Capacity < newCount1 * newCount2)
			{
				_data = new T[MathF.Max(Capacity * 2, newCount1 * newCount2)];
			}
			else
			{
				Clear();
			}
		}
		else
		{
			Clear();
		}
	}

	public void CopyRowTo(int sourceIndex1, int sourceIndex2, MBList2D<T> destination, int destinationIndex1, int destinationIndex2, int copyCount)
	{
		for (int i = 0; i < copyCount; i++)
		{
			destination[destinationIndex1, destinationIndex2 + i] = this[sourceIndex1, sourceIndex2 + i];
		}
	}
}
