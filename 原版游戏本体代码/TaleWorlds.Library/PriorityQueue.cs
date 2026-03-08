using System;
using System.Collections;
using System.Collections.Generic;

namespace TaleWorlds.Library
{
	// Token: 0x02000082 RID: 130
	public class PriorityQueue<TPriority, TValue> : ICollection<KeyValuePair<TPriority, TValue>>, IEnumerable<KeyValuePair<TPriority, TValue>>, IEnumerable
	{
		// Token: 0x17000077 RID: 119
		// (get) Token: 0x06000487 RID: 1159 RVA: 0x00010002 File Offset: 0x0000E202
		private IComparer<TPriority> Comparer
		{
			get
			{
				if (this._customComparer == null)
				{
					return Comparer<TPriority>.Default;
				}
				return this._customComparer;
			}
		}

		// Token: 0x06000488 RID: 1160 RVA: 0x00010018 File Offset: 0x0000E218
		public PriorityQueue()
		{
			this._baseHeap = new List<KeyValuePair<TPriority, TValue>>();
		}

		// Token: 0x06000489 RID: 1161 RVA: 0x0001002B File Offset: 0x0000E22B
		public PriorityQueue(int capacity)
		{
			this._baseHeap = new List<KeyValuePair<TPriority, TValue>>(capacity);
		}

		// Token: 0x0600048A RID: 1162 RVA: 0x0001003F File Offset: 0x0000E23F
		public PriorityQueue(int capacity, IComparer<TPriority> comparer)
		{
			if (comparer == null)
			{
				throw new ArgumentNullException();
			}
			this._baseHeap = new List<KeyValuePair<TPriority, TValue>>(capacity);
			this._customComparer = comparer;
		}

		// Token: 0x0600048B RID: 1163 RVA: 0x00010063 File Offset: 0x0000E263
		public PriorityQueue(IComparer<TPriority> comparer)
		{
			if (comparer == null)
			{
				throw new ArgumentNullException();
			}
			this._baseHeap = new List<KeyValuePair<TPriority, TValue>>();
			this._customComparer = comparer;
		}

		// Token: 0x0600048C RID: 1164 RVA: 0x00010086 File Offset: 0x0000E286
		public PriorityQueue(IEnumerable<KeyValuePair<TPriority, TValue>> data)
			: this(data, Comparer<TPriority>.Default)
		{
		}

		// Token: 0x0600048D RID: 1165 RVA: 0x00010094 File Offset: 0x0000E294
		public PriorityQueue(IEnumerable<KeyValuePair<TPriority, TValue>> data, IComparer<TPriority> comparer)
		{
			if (data == null || comparer == null)
			{
				throw new ArgumentNullException();
			}
			this._customComparer = comparer;
			this._baseHeap = new List<KeyValuePair<TPriority, TValue>>(data);
			for (int i = this._baseHeap.Count / 2 - 1; i >= 0; i--)
			{
				this.HeapifyFromBeginningToEnd(i);
			}
		}

		// Token: 0x0600048E RID: 1166 RVA: 0x000100E7 File Offset: 0x0000E2E7
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
			return PriorityQueue<TPriority, TValue>.MergeQueues(pq1, pq2, pq1.Comparer);
		}

		// Token: 0x0600048F RID: 1167 RVA: 0x0001011C File Offset: 0x0000E31C
		public static PriorityQueue<TPriority, TValue> MergeQueues(PriorityQueue<TPriority, TValue> pq1, PriorityQueue<TPriority, TValue> pq2, IComparer<TPriority> comparer)
		{
			if (pq1 == null || pq2 == null || comparer == null)
			{
				throw new ArgumentNullException();
			}
			PriorityQueue<TPriority, TValue> priorityQueue = new PriorityQueue<TPriority, TValue>(pq1.Count + pq2.Count, comparer);
			priorityQueue._baseHeap.AddRange(pq1._baseHeap);
			priorityQueue._baseHeap.AddRange(pq2._baseHeap);
			for (int i = priorityQueue._baseHeap.Count / 2 - 1; i >= 0; i--)
			{
				priorityQueue.HeapifyFromBeginningToEnd(i);
			}
			return priorityQueue;
		}

		// Token: 0x06000490 RID: 1168 RVA: 0x00010190 File Offset: 0x0000E390
		public void Enqueue(TPriority priority, TValue value)
		{
			this.Insert(priority, value);
		}

		// Token: 0x06000491 RID: 1169 RVA: 0x0001019A File Offset: 0x0000E39A
		public KeyValuePair<TPriority, TValue> Dequeue()
		{
			if (!this.IsEmpty)
			{
				KeyValuePair<TPriority, TValue> result = this._baseHeap[0];
				this.DeleteRoot();
				return result;
			}
			throw new InvalidOperationException("Priority queue is empty");
		}

		// Token: 0x06000492 RID: 1170 RVA: 0x000101C4 File Offset: 0x0000E3C4
		public TValue DequeueValue()
		{
			return this.Dequeue().Value;
		}

		// Token: 0x06000493 RID: 1171 RVA: 0x000101DF File Offset: 0x0000E3DF
		public KeyValuePair<TPriority, TValue> Peek()
		{
			if (!this.IsEmpty)
			{
				return this._baseHeap[0];
			}
			throw new InvalidOperationException("Priority queue is empty");
		}

		// Token: 0x06000494 RID: 1172 RVA: 0x00010200 File Offset: 0x0000E400
		public TValue PeekValue()
		{
			return this.Peek().Value;
		}

		// Token: 0x17000078 RID: 120
		// (get) Token: 0x06000495 RID: 1173 RVA: 0x0001021B File Offset: 0x0000E41B
		public bool IsEmpty
		{
			get
			{
				return this._baseHeap.Count == 0;
			}
		}

		// Token: 0x06000496 RID: 1174 RVA: 0x0001022C File Offset: 0x0000E42C
		private void ExchangeElements(int pos1, int pos2)
		{
			KeyValuePair<TPriority, TValue> value = this._baseHeap[pos1];
			this._baseHeap[pos1] = this._baseHeap[pos2];
			this._baseHeap[pos2] = value;
		}

		// Token: 0x06000497 RID: 1175 RVA: 0x0001026C File Offset: 0x0000E46C
		private void Insert(TPriority priority, TValue value)
		{
			KeyValuePair<TPriority, TValue> item = new KeyValuePair<TPriority, TValue>(priority, value);
			this._baseHeap.Add(item);
			this.HeapifyFromEndToBeginning(this._baseHeap.Count - 1);
		}

		// Token: 0x06000498 RID: 1176 RVA: 0x000102A4 File Offset: 0x0000E4A4
		private int HeapifyFromEndToBeginning(int pos)
		{
			if (pos >= this._baseHeap.Count)
			{
				return -1;
			}
			IComparer<TPriority> comparer = this.Comparer;
			while (pos > 0)
			{
				int num = (pos - 1) / 2;
				if (comparer.Compare(this._baseHeap[num].Key, this._baseHeap[pos].Key) >= 0)
				{
					break;
				}
				this.ExchangeElements(num, pos);
				pos = num;
			}
			return pos;
		}

		// Token: 0x06000499 RID: 1177 RVA: 0x00010314 File Offset: 0x0000E514
		private void DeleteRoot()
		{
			if (this._baseHeap.Count <= 1)
			{
				this._baseHeap.Clear();
				return;
			}
			this._baseHeap[0] = this._baseHeap[this._baseHeap.Count - 1];
			this._baseHeap.RemoveAt(this._baseHeap.Count - 1);
			this.HeapifyFromBeginningToEnd(0);
		}

		// Token: 0x0600049A RID: 1178 RVA: 0x00010380 File Offset: 0x0000E580
		private void HeapifyFromBeginningToEnd(int pos)
		{
			if (pos >= this._baseHeap.Count)
			{
				return;
			}
			IComparer<TPriority> comparer = this.Comparer;
			for (;;)
			{
				int num = pos;
				int num2 = 2 * pos + 1;
				int num3 = 2 * pos + 2;
				if (num2 < this._baseHeap.Count && comparer.Compare(this._baseHeap[num].Key, this._baseHeap[num2].Key) < 0)
				{
					num = num2;
				}
				if (num3 < this._baseHeap.Count && comparer.Compare(this._baseHeap[num].Key, this._baseHeap[num3].Key) < 0)
				{
					num = num3;
				}
				if (num == pos)
				{
					break;
				}
				this.ExchangeElements(num, pos);
				pos = num;
			}
		}

		// Token: 0x0600049B RID: 1179 RVA: 0x0001044B File Offset: 0x0000E64B
		public void Add(KeyValuePair<TPriority, TValue> item)
		{
			this.Enqueue(item.Key, item.Value);
		}

		// Token: 0x0600049C RID: 1180 RVA: 0x00010461 File Offset: 0x0000E661
		public void Clear()
		{
			this._baseHeap.Clear();
		}

		// Token: 0x0600049D RID: 1181 RVA: 0x0001046E File Offset: 0x0000E66E
		public bool Contains(KeyValuePair<TPriority, TValue> item)
		{
			return this._baseHeap.Contains(item);
		}

		// Token: 0x17000079 RID: 121
		// (get) Token: 0x0600049E RID: 1182 RVA: 0x0001047C File Offset: 0x0000E67C
		public int Count
		{
			get
			{
				return this._baseHeap.Count;
			}
		}

		// Token: 0x0600049F RID: 1183 RVA: 0x00010489 File Offset: 0x0000E689
		public void CopyTo(KeyValuePair<TPriority, TValue>[] array, int arrayIndex)
		{
			this._baseHeap.CopyTo(array, arrayIndex);
		}

		// Token: 0x1700007A RID: 122
		// (get) Token: 0x060004A0 RID: 1184 RVA: 0x00010498 File Offset: 0x0000E698
		public bool IsReadOnly
		{
			get
			{
				return false;
			}
		}

		// Token: 0x060004A1 RID: 1185 RVA: 0x0001049C File Offset: 0x0000E69C
		public bool Remove(KeyValuePair<TPriority, TValue> item)
		{
			int num = this._baseHeap.IndexOf(item);
			if (num < 0)
			{
				return false;
			}
			this._baseHeap[num] = this._baseHeap[this._baseHeap.Count - 1];
			this._baseHeap.RemoveAt(this._baseHeap.Count - 1);
			if (this.HeapifyFromEndToBeginning(num) == num)
			{
				this.HeapifyFromBeginningToEnd(num);
			}
			return true;
		}

		// Token: 0x060004A2 RID: 1186 RVA: 0x0001050A File Offset: 0x0000E70A
		public IEnumerator<KeyValuePair<TPriority, TValue>> GetEnumerator()
		{
			return this._baseHeap.GetEnumerator();
		}

		// Token: 0x060004A3 RID: 1187 RVA: 0x0001051C File Offset: 0x0000E71C
		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		// Token: 0x0400016C RID: 364
		private readonly List<KeyValuePair<TPriority, TValue>> _baseHeap;

		// Token: 0x0400016D RID: 365
		private readonly IComparer<TPriority> _customComparer;
	}
}
