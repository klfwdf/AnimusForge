using System.Collections.Generic;

namespace TaleWorlds.Library;

public class MBQueue<T> : MBReadOnlyQueue<T>, IMBCollection
{
	public MBQueue()
	{
	}

	public MBQueue(int capacity)
		: base(capacity)
	{
	}

	public MBQueue(Queue<T> queue)
		: base(queue)
	{
	}

	public MBQueue(IEnumerable<T> collection)
		: base(collection)
	{
	}

	public bool Remove(T item)
	{
		EqualityComparer<T> equalityComparer = EqualityComparer<T>.Default;
		int count = base.Count;
		bool flag = false;
		for (int i = 0; i < count; i++)
		{
			T val = Dequeue();
			if (!flag && equalityComparer.Equals(val, item))
			{
				flag = true;
			}
			else
			{
				Enqueue(val);
			}
		}
		return flag;
	}

	void IMBCollection.Clear()
	{
		Clear();
	}
}
