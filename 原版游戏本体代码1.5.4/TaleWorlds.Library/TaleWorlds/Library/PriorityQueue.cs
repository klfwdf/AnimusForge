using System;
using System.Collections;
using System.Collections.Generic;

namespace TaleWorlds.Library;

public class PriorityQueue<TPriority, TValue> : ICollection<KeyValuePair<TPriority, TValue>>, IEnumerable<KeyValuePair<TPriority, TValue>>, IEnumerable
{
	private readonly List<KeyValuePair<TPriority, TValue>> _baseHeap;

	private readonly IComparer<TPriority> _customComparer;

	private IComparer<TPriority> Comparer
	{
		get
		{
			if (_customComparer == null)
			{
				return Comparer<TPriority>.Default;
			}
			return _customComparer;
		}
	}

	public bool IsEmpty => _baseHeap.Count == 0;

	public int Count => _baseHeap.Count;

	public bool IsReadOnly => false;

	public PriorityQueue()
	{
		_baseHeap = new List<KeyValuePair<TPriority, TValue>>();
	}

	public PriorityQueue(int capacity)
	{
		_baseHeap = new List<KeyValuePair<TPriority, TValue>>(capacity);
	}

	public PriorityQueue(int capacity, IComparer<TPriority> comparer)
	{
		if (comparer == null)
		{
			throw new ArgumentNullException();
		}
		_baseHeap = new List<KeyValuePair<TPriority, TValue>>(capacity);
		_customComparer = comparer;
	}

	public PriorityQueue(IComparer<TPriority> comparer)
	{
		if (comparer == null)
		{
			throw new ArgumentNullException();
		}
		_baseHeap = new List<KeyValuePair<TPriority, TValue>>();
		_customComparer = comparer;
	}

	public PriorityQueue(IEnumerable<KeyValuePair<TPriority, TValue>> data)
		: this(data, (IComparer<TPriority>)Comparer<TPriority>.Default)
	{
	}

	public PriorityQueue(IEnumerable<KeyValuePair<TPriority, TValue>> data, IComparer<TPriority> comparer)
	{
		if (data == null || comparer == null)
		{
			throw new ArgumentNullException();
		}
		_customComparer = comparer;
		_baseHeap = new List<KeyValuePair<TPriority, TValue>>(data);
		for (int num = _baseHeap.Count / 2 - 1; num >= 0; num--)
		{
			HeapifyFromBeginningToEnd(num);
		}
	}

	public static PriorityQueue<TPriority, TValue> MergeQueues(PriorityQueue<TPriority, TValue> pq1, PriorityQueue<TPriority, TValue> pq2)
	{
		if (pq1 == null || pq2 == null)
		{
			throw new ArgumentNullException();
		}
		if (pq1.Comparer != pq2.Comparer)
		{
			throw new InvalidOperationException("Priority queues to be merged must have equal comparers");
		}
		return MergeQueues(pq1, pq2, pq1.Comparer);
	}

	public static PriorityQueue<TPriority, TValue> MergeQueues(PriorityQueue<TPriority, TValue> pq1, PriorityQueue<TPriority, TValue> pq2, IComparer<TPriority> comparer)
	{
		if (pq1 == null || pq2 == null || comparer == null)
		{
			throw new ArgumentNullException();
		}
		PriorityQueue<TPriority, TValue> priorityQueue = new PriorityQueue<TPriority, TValue>(pq1.Count + pq2.Count, comparer);
		priorityQueue._baseHeap.AddRange(pq1._baseHeap);
		priorityQueue._baseHeap.AddRange(pq2._baseHeap);
		for (int num = priorityQueue._baseHeap.Count / 2 - 1; num >= 0; num--)
		{
			priorityQueue.HeapifyFromBeginningToEnd(num);
		}
		return priorityQueue;
	}

	public void Enqueue(TPriority priority, TValue value)
	{
		Insert(priority, value);
	}

	public KeyValuePair<TPriority, TValue> Dequeue()
	{
		if (!IsEmpty)
		{
			KeyValuePair<TPriority, TValue> result = _baseHeap[0];
			DeleteRoot();
			return result;
		}
		throw new InvalidOperationException("Priority queue is empty");
	}

	public TValue DequeueValue()
	{
		return Dequeue().Value;
	}

	public KeyValuePair<TPriority, TValue> Peek()
	{
		if (!IsEmpty)
		{
			return _baseHeap[0];
		}
		throw new InvalidOperationException("Priority queue is empty");
	}

	public TValue PeekValue()
	{
		return Peek().Value;
	}

	private void ExchangeElements(int pos1, int pos2)
	{
		KeyValuePair<TPriority, TValue> value = _baseHeap[pos1];
		_baseHeap[pos1] = _baseHeap[pos2];
		_baseHeap[pos2] = value;
	}

	private void Insert(TPriority priority, TValue value)
	{
		KeyValuePair<TPriority, TValue> item = new KeyValuePair<TPriority, TValue>(priority, value);
		_baseHeap.Add(item);
		HeapifyFromEndToBeginning(_baseHeap.Count - 1);
	}

	private int HeapifyFromEndToBeginning(int pos)
	{
		if (pos >= _baseHeap.Count)
		{
			return -1;
		}
		IComparer<TPriority> comparer = Comparer;
		while (pos > 0)
		{
			int num = (pos - 1) / 2;
			if (comparer.Compare(_baseHeap[num].Key, _baseHeap[pos].Key) >= 0)
			{
				break;
			}
			ExchangeElements(num, pos);
			pos = num;
		}
		return pos;
	}

	private void DeleteRoot()
	{
		if (_baseHeap.Count <= 1)
		{
			_baseHeap.Clear();
			return;
		}
		_baseHeap[0] = _baseHeap[_baseHeap.Count - 1];
		_baseHeap.RemoveAt(_baseHeap.Count - 1);
		HeapifyFromBeginningToEnd(0);
	}

	private void HeapifyFromBeginningToEnd(int pos)
	{
		if (pos >= _baseHeap.Count)
		{
			return;
		}
		IComparer<TPriority> comparer = Comparer;
		while (true)
		{
			int num = pos;
			int num2 = 2 * pos + 1;
			int num3 = 2 * pos + 2;
			if (num2 < _baseHeap.Count && comparer.Compare(_baseHeap[num].Key, _baseHeap[num2].Key) < 0)
			{
				num = num2;
			}
			if (num3 < _baseHeap.Count && comparer.Compare(_baseHeap[num].Key, _baseHeap[num3].Key) < 0)
			{
				num = num3;
			}
			if (num != pos)
			{
				ExchangeElements(num, pos);
				pos = num;
				continue;
			}
			break;
		}
	}

	public void Add(KeyValuePair<TPriority, TValue> item)
	{
		Enqueue(item.Key, item.Value);
	}

	public void Clear()
	{
		_baseHeap.Clear();
	}

	public bool Contains(KeyValuePair<TPriority, TValue> item)
	{
		return _baseHeap.Contains(item);
	}

	public void CopyTo(KeyValuePair<TPriority, TValue>[] array, int arrayIndex)
	{
		_baseHeap.CopyTo(array, arrayIndex);
	}

	public bool Remove(KeyValuePair<TPriority, TValue> item)
	{
		int num = _baseHeap.IndexOf(item);
		if (num < 0)
		{
			return false;
		}
		_baseHeap[num] = _baseHeap[_baseHeap.Count - 1];
		_baseHeap.RemoveAt(_baseHeap.Count - 1);
		if (HeapifyFromEndToBeginning(num) == num)
		{
			HeapifyFromBeginningToEnd(num);
		}
		return true;
	}

	public IEnumerator<KeyValuePair<TPriority, TValue>> GetEnumerator()
	{
		return _baseHeap.GetEnumerator();
	}

	IEnumerator IEnumerable.GetEnumerator()
	{
		return GetEnumerator();
	}
}
