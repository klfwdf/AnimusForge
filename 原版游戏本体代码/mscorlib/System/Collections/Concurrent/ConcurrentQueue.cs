using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Threading;

namespace System.Collections.Concurrent
{
	// Token: 0x020004AF RID: 1199
	[ComVisible(false)]
	[DebuggerDisplay("Count = {Count}")]
	[DebuggerTypeProxy(typeof(SystemCollectionsConcurrent_ProducerConsumerCollectionDebugView<>))]
	[__DynamicallyInvokable]
	[HostProtection(SecurityAction.LinkDemand, Synchronization = true, ExternalThreading = true)]
	[Serializable]
	public class ConcurrentQueue<T> : IProducerConsumerCollection<!0>, IEnumerable<!0>, IEnumerable, ICollection, IReadOnlyCollection<T>
	{
		// Token: 0x06003978 RID: 14712 RVA: 0x000DC440 File Offset: 0x000DA640
		[__DynamicallyInvokable]
		public ConcurrentQueue()
		{
			this.m_head = (this.m_tail = new ConcurrentQueue<T>.Segment(0L, this));
		}

		// Token: 0x06003979 RID: 14713 RVA: 0x000DC470 File Offset: 0x000DA670
		private void InitializeFromCollection(IEnumerable<T> collection)
		{
			ConcurrentQueue<T>.Segment segment = new ConcurrentQueue<T>.Segment(0L, this);
			this.m_head = segment;
			int num = 0;
			foreach (T value in collection)
			{
				segment.UnsafeAdd(value);
				num++;
				if (num >= 32)
				{
					segment = segment.UnsafeGrow();
					num = 0;
				}
			}
			this.m_tail = segment;
		}

		// Token: 0x0600397A RID: 14714 RVA: 0x000DC4E8 File Offset: 0x000DA6E8
		[__DynamicallyInvokable]
		public ConcurrentQueue(IEnumerable<T> collection)
		{
			if (collection == null)
			{
				throw new ArgumentNullException("collection");
			}
			this.InitializeFromCollection(collection);
		}

		// Token: 0x0600397B RID: 14715 RVA: 0x000DC505 File Offset: 0x000DA705
		[OnSerializing]
		private void OnSerializing(StreamingContext context)
		{
			this.m_serializationArray = this.ToArray();
		}

		// Token: 0x0600397C RID: 14716 RVA: 0x000DC513 File Offset: 0x000DA713
		[OnDeserialized]
		private void OnDeserialized(StreamingContext context)
		{
			this.InitializeFromCollection(this.m_serializationArray);
			this.m_serializationArray = null;
		}

		// Token: 0x0600397D RID: 14717 RVA: 0x000DC528 File Offset: 0x000DA728
		[__DynamicallyInvokable]
		void ICollection.CopyTo(Array array, int index)
		{
			if (array == null)
			{
				throw new ArgumentNullException("array");
			}
			((ICollection)this.ToList()).CopyTo(array, index);
		}

		// Token: 0x1700089E RID: 2206
		// (get) Token: 0x0600397E RID: 14718 RVA: 0x000DC545 File Offset: 0x000DA745
		[__DynamicallyInvokable]
		bool ICollection.IsSynchronized
		{
			[__DynamicallyInvokable]
			get
			{
				return false;
			}
		}

		// Token: 0x1700089F RID: 2207
		// (get) Token: 0x0600397F RID: 14719 RVA: 0x000DC548 File Offset: 0x000DA748
		[__DynamicallyInvokable]
		object ICollection.SyncRoot
		{
			[__DynamicallyInvokable]
			get
			{
				throw new NotSupportedException(Environment.GetResourceString("ConcurrentCollection_SyncRoot_NotSupported"));
			}
		}

		// Token: 0x06003980 RID: 14720 RVA: 0x000DC559 File Offset: 0x000DA759
		[__DynamicallyInvokable]
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<!0>)this).GetEnumerator();
		}

		// Token: 0x06003981 RID: 14721 RVA: 0x000DC561 File Offset: 0x000DA761
		[__DynamicallyInvokable]
		bool IProducerConsumerCollection<!0>.TryAdd(T item)
		{
			this.Enqueue(item);
			return true;
		}

		// Token: 0x06003982 RID: 14722 RVA: 0x000DC56B File Offset: 0x000DA76B
		[__DynamicallyInvokable]
		bool IProducerConsumerCollection<!0>.TryTake(out T item)
		{
			return this.TryDequeue(out item);
		}

		// Token: 0x170008A0 RID: 2208
		// (get) Token: 0x06003983 RID: 14723 RVA: 0x000DC574 File Offset: 0x000DA774
		[__DynamicallyInvokable]
		public bool IsEmpty
		{
			[__DynamicallyInvokable]
			get
			{
				ConcurrentQueue<T>.Segment head = this.m_head;
				if (!head.IsEmpty)
				{
					return false;
				}
				if (head.Next == null)
				{
					return true;
				}
				SpinWait spinWait = default(SpinWait);
				while (head.IsEmpty)
				{
					if (head.Next == null)
					{
						return true;
					}
					spinWait.SpinOnce();
					head = this.m_head;
				}
				return false;
			}
		}

		// Token: 0x06003984 RID: 14724 RVA: 0x000DC5CB File Offset: 0x000DA7CB
		[__DynamicallyInvokable]
		public T[] ToArray()
		{
			return this.ToList().ToArray();
		}

		// Token: 0x06003985 RID: 14725 RVA: 0x000DC5D8 File Offset: 0x000DA7D8
		private List<T> ToList()
		{
			Interlocked.Increment(ref this.m_numSnapshotTakers);
			List<T> list = new List<T>();
			try
			{
				ConcurrentQueue<T>.Segment segment;
				ConcurrentQueue<T>.Segment segment2;
				int start;
				int end;
				this.GetHeadTailPositions(out segment, out segment2, out start, out end);
				if (segment == segment2)
				{
					segment.AddToList(list, start, end);
				}
				else
				{
					segment.AddToList(list, start, 31);
					for (ConcurrentQueue<T>.Segment next = segment.Next; next != segment2; next = next.Next)
					{
						next.AddToList(list, 0, 31);
					}
					segment2.AddToList(list, 0, end);
				}
			}
			finally
			{
				Interlocked.Decrement(ref this.m_numSnapshotTakers);
			}
			return list;
		}

		// Token: 0x06003986 RID: 14726 RVA: 0x000DC66C File Offset: 0x000DA86C
		private void GetHeadTailPositions(out ConcurrentQueue<T>.Segment head, out ConcurrentQueue<T>.Segment tail, out int headLow, out int tailHigh)
		{
			head = this.m_head;
			tail = this.m_tail;
			headLow = head.Low;
			tailHigh = tail.High;
			SpinWait spinWait = default(SpinWait);
			while (head != this.m_head || tail != this.m_tail || headLow != head.Low || tailHigh != tail.High || head.m_index > tail.m_index)
			{
				spinWait.SpinOnce();
				head = this.m_head;
				tail = this.m_tail;
				headLow = head.Low;
				tailHigh = tail.High;
			}
		}

		// Token: 0x170008A1 RID: 2209
		// (get) Token: 0x06003987 RID: 14727 RVA: 0x000DC718 File Offset: 0x000DA918
		[__DynamicallyInvokable]
		public int Count
		{
			[__DynamicallyInvokable]
			get
			{
				ConcurrentQueue<T>.Segment segment;
				ConcurrentQueue<T>.Segment segment2;
				int num;
				int num2;
				this.GetHeadTailPositions(out segment, out segment2, out num, out num2);
				if (segment == segment2)
				{
					return num2 - num + 1;
				}
				int num3 = 32 - num;
				num3 += 32 * (int)(segment2.m_index - segment.m_index - 1L);
				return num3 + (num2 + 1);
			}
		}

		// Token: 0x06003988 RID: 14728 RVA: 0x000DC766 File Offset: 0x000DA966
		[__DynamicallyInvokable]
		public void CopyTo(T[] array, int index)
		{
			if (array == null)
			{
				throw new ArgumentNullException("array");
			}
			this.ToList().CopyTo(array, index);
		}

		// Token: 0x06003989 RID: 14729 RVA: 0x000DC784 File Offset: 0x000DA984
		[__DynamicallyInvokable]
		public IEnumerator<T> GetEnumerator()
		{
			Interlocked.Increment(ref this.m_numSnapshotTakers);
			ConcurrentQueue<T>.Segment head;
			ConcurrentQueue<T>.Segment tail;
			int headLow;
			int tailHigh;
			this.GetHeadTailPositions(out head, out tail, out headLow, out tailHigh);
			return this.GetEnumerator(head, tail, headLow, tailHigh);
		}

		// Token: 0x0600398A RID: 14730 RVA: 0x000DC7B5 File Offset: 0x000DA9B5
		private IEnumerator<T> GetEnumerator(ConcurrentQueue<T>.Segment head, ConcurrentQueue<T>.Segment tail, int headLow, int tailHigh)
		{
			try
			{
				SpinWait spin = default(SpinWait);
				if (head == tail)
				{
					int num;
					for (int i = headLow; i <= tailHigh; i = num + 1)
					{
						spin.Reset();
						while (!head.m_state[i].m_value)
						{
							spin.SpinOnce();
						}
						yield return head.m_array[i];
						num = i;
					}
				}
				else
				{
					int num;
					for (int i = headLow; i < 32; i = num + 1)
					{
						spin.Reset();
						while (!head.m_state[i].m_value)
						{
							spin.SpinOnce();
						}
						yield return head.m_array[i];
						num = i;
					}
					ConcurrentQueue<T>.Segment curr;
					for (curr = head.Next; curr != tail; curr = curr.Next)
					{
						for (int i = 0; i < 32; i = num + 1)
						{
							spin.Reset();
							while (!curr.m_state[i].m_value)
							{
								spin.SpinOnce();
							}
							yield return curr.m_array[i];
							num = i;
						}
					}
					for (int i = 0; i <= tailHigh; i = num + 1)
					{
						spin.Reset();
						while (!tail.m_state[i].m_value)
						{
							spin.SpinOnce();
						}
						yield return tail.m_array[i];
						num = i;
					}
					curr = null;
				}
			}
			finally
			{
				Interlocked.Decrement(ref this.m_numSnapshotTakers);
			}
			yield break;
			yield break;
		}

		// Token: 0x0600398B RID: 14731 RVA: 0x000DC7E4 File Offset: 0x000DA9E4
		[__DynamicallyInvokable]
		public void Enqueue(T item)
		{
			SpinWait spinWait = default(SpinWait);
			for (;;)
			{
				ConcurrentQueue<T>.Segment tail = this.m_tail;
				if (tail.TryAppend(item))
				{
					break;
				}
				spinWait.SpinOnce();
			}
		}

		// Token: 0x0600398C RID: 14732 RVA: 0x000DC814 File Offset: 0x000DAA14
		[__DynamicallyInvokable]
		public bool TryDequeue(out T result)
		{
			while (!this.IsEmpty)
			{
				ConcurrentQueue<T>.Segment head = this.m_head;
				if (head.TryRemove(out result))
				{
					return true;
				}
			}
			result = default(T);
			return false;
		}

		// Token: 0x0600398D RID: 14733 RVA: 0x000DC848 File Offset: 0x000DAA48
		[__DynamicallyInvokable]
		public bool TryPeek(out T result)
		{
			Interlocked.Increment(ref this.m_numSnapshotTakers);
			while (!this.IsEmpty)
			{
				ConcurrentQueue<T>.Segment head = this.m_head;
				if (head.TryPeek(out result))
				{
					Interlocked.Decrement(ref this.m_numSnapshotTakers);
					return true;
				}
			}
			result = default(T);
			Interlocked.Decrement(ref this.m_numSnapshotTakers);
			return false;
		}

		// Token: 0x04001927 RID: 6439
		[NonSerialized]
		private volatile ConcurrentQueue<T>.Segment m_head;

		// Token: 0x04001928 RID: 6440
		[NonSerialized]
		private volatile ConcurrentQueue<T>.Segment m_tail;

		// Token: 0x04001929 RID: 6441
		private T[] m_serializationArray;

		// Token: 0x0400192A RID: 6442
		private const int SEGMENT_SIZE = 32;

		// Token: 0x0400192B RID: 6443
		[NonSerialized]
		internal volatile int m_numSnapshotTakers;

		// Token: 0x02000BC8 RID: 3016
		private class Segment
		{
			// Token: 0x06006E81 RID: 28289 RVA: 0x0017D3D7 File Offset: 0x0017B5D7
			internal Segment(long index, ConcurrentQueue<T> source)
			{
				this.m_array = new T[32];
				this.m_state = new VolatileBool[32];
				this.m_high = -1;
				this.m_index = index;
				this.m_source = source;
			}

			// Token: 0x170012E3 RID: 4835
			// (get) Token: 0x06006E82 RID: 28290 RVA: 0x0017D416 File Offset: 0x0017B616
			internal ConcurrentQueue<T>.Segment Next
			{
				get
				{
					return this.m_next;
				}
			}

			// Token: 0x170012E4 RID: 4836
			// (get) Token: 0x06006E83 RID: 28291 RVA: 0x0017D420 File Offset: 0x0017B620
			internal bool IsEmpty
			{
				get
				{
					return this.Low > this.High;
				}
			}

			// Token: 0x06006E84 RID: 28292 RVA: 0x0017D430 File Offset: 0x0017B630
			internal void UnsafeAdd(T value)
			{
				this.m_high++;
				this.m_array[this.m_high] = value;
				this.m_state[this.m_high].m_value = true;
			}

			// Token: 0x06006E85 RID: 28293 RVA: 0x0017D484 File Offset: 0x0017B684
			internal ConcurrentQueue<T>.Segment UnsafeGrow()
			{
				ConcurrentQueue<T>.Segment segment = new ConcurrentQueue<T>.Segment(this.m_index + 1L, this.m_source);
				this.m_next = segment;
				return segment;
			}

			// Token: 0x06006E86 RID: 28294 RVA: 0x0017D4B4 File Offset: 0x0017B6B4
			internal void Grow()
			{
				ConcurrentQueue<T>.Segment next = new ConcurrentQueue<T>.Segment(this.m_index + 1L, this.m_source);
				this.m_next = next;
				this.m_source.m_tail = this.m_next;
			}

			// Token: 0x06006E87 RID: 28295 RVA: 0x0017D4F8 File Offset: 0x0017B6F8
			internal bool TryAppend(T value)
			{
				if (this.m_high >= 31)
				{
					return false;
				}
				int num = 32;
				try
				{
				}
				finally
				{
					num = Interlocked.Increment(ref this.m_high);
					if (num <= 31)
					{
						this.m_array[num] = value;
						this.m_state[num].m_value = true;
					}
					if (num == 31)
					{
						this.Grow();
					}
				}
				return num <= 31;
			}

			// Token: 0x06006E88 RID: 28296 RVA: 0x0017D574 File Offset: 0x0017B774
			internal bool TryRemove(out T result)
			{
				SpinWait spinWait = default(SpinWait);
				int i = this.Low;
				int high = this.High;
				while (i <= high)
				{
					if (Interlocked.CompareExchange(ref this.m_low, i + 1, i) == i)
					{
						SpinWait spinWait2 = default(SpinWait);
						while (!this.m_state[i].m_value)
						{
							spinWait2.SpinOnce();
						}
						result = this.m_array[i];
						if (this.m_source.m_numSnapshotTakers <= 0)
						{
							this.m_array[i] = default(T);
						}
						if (i + 1 >= 32)
						{
							spinWait2 = default(SpinWait);
							while (this.m_next == null)
							{
								spinWait2.SpinOnce();
							}
							this.m_source.m_head = this.m_next;
						}
						return true;
					}
					spinWait.SpinOnce();
					i = this.Low;
					high = this.High;
				}
				result = default(T);
				return false;
			}

			// Token: 0x06006E89 RID: 28297 RVA: 0x0017D678 File Offset: 0x0017B878
			internal bool TryPeek(out T result)
			{
				result = default(T);
				int low = this.Low;
				if (low > this.High)
				{
					return false;
				}
				SpinWait spinWait = default(SpinWait);
				while (!this.m_state[low].m_value)
				{
					spinWait.SpinOnce();
				}
				result = this.m_array[low];
				return true;
			}

			// Token: 0x06006E8A RID: 28298 RVA: 0x0017D6DC File Offset: 0x0017B8DC
			internal void AddToList(List<T> list, int start, int end)
			{
				for (int i = start; i <= end; i++)
				{
					SpinWait spinWait = default(SpinWait);
					while (!this.m_state[i].m_value)
					{
						spinWait.SpinOnce();
					}
					list.Add(this.m_array[i]);
				}
			}

			// Token: 0x170012E5 RID: 4837
			// (get) Token: 0x06006E8B RID: 28299 RVA: 0x0017D731 File Offset: 0x0017B931
			internal int Low
			{
				get
				{
					return Math.Min(this.m_low, 32);
				}
			}

			// Token: 0x170012E6 RID: 4838
			// (get) Token: 0x06006E8C RID: 28300 RVA: 0x0017D742 File Offset: 0x0017B942
			internal int High
			{
				get
				{
					return Math.Min(this.m_high, 31);
				}
			}

			// Token: 0x040035A8 RID: 13736
			internal volatile T[] m_array;

			// Token: 0x040035A9 RID: 13737
			internal volatile VolatileBool[] m_state;

			// Token: 0x040035AA RID: 13738
			private volatile ConcurrentQueue<T>.Segment m_next;

			// Token: 0x040035AB RID: 13739
			internal readonly long m_index;

			// Token: 0x040035AC RID: 13740
			private volatile int m_low;

			// Token: 0x040035AD RID: 13741
			private volatile int m_high;

			// Token: 0x040035AE RID: 13742
			private volatile ConcurrentQueue<T> m_source;
		}
	}
}
