using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace System.Threading.Tasks
{
	// Token: 0x02000584 RID: 1412
	[DebuggerDisplay("Count = {Count}")]
	[DebuggerTypeProxy(typeof(SingleProducerSingleConsumerQueue<>.SingleProducerSingleConsumerQueue_DebugView))]
	internal sealed class SingleProducerSingleConsumerQueue<T> : IProducerConsumerQueue<T>, IEnumerable<!0>, IEnumerable
	{
		// Token: 0x0600426D RID: 17005 RVA: 0x000F7370 File Offset: 0x000F5570
		internal SingleProducerSingleConsumerQueue()
		{
			this.m_head = (this.m_tail = new SingleProducerSingleConsumerQueue<T>.Segment(32));
		}

		// Token: 0x0600426E RID: 17006 RVA: 0x000F73A0 File Offset: 0x000F55A0
		public void Enqueue(T item)
		{
			SingleProducerSingleConsumerQueue<T>.Segment tail = this.m_tail;
			T[] array = tail.m_array;
			int last = tail.m_state.m_last;
			int num = (last + 1) & (array.Length - 1);
			if (num != tail.m_state.m_firstCopy)
			{
				array[last] = item;
				tail.m_state.m_last = num;
				return;
			}
			this.EnqueueSlow(item, ref tail);
		}

		// Token: 0x0600426F RID: 17007 RVA: 0x000F7404 File Offset: 0x000F5604
		private void EnqueueSlow(T item, ref SingleProducerSingleConsumerQueue<T>.Segment segment)
		{
			if (segment.m_state.m_firstCopy != segment.m_state.m_first)
			{
				segment.m_state.m_firstCopy = segment.m_state.m_first;
				this.Enqueue(item);
				return;
			}
			int num = this.m_tail.m_array.Length << 1;
			if (num > 16777216)
			{
				num = 16777216;
			}
			SingleProducerSingleConsumerQueue<T>.Segment segment2 = new SingleProducerSingleConsumerQueue<T>.Segment(num);
			segment2.m_array[0] = item;
			segment2.m_state.m_last = 1;
			segment2.m_state.m_lastCopy = 1;
			try
			{
			}
			finally
			{
				Volatile.Write<SingleProducerSingleConsumerQueue<T>.Segment>(ref this.m_tail.m_next, segment2);
				this.m_tail = segment2;
			}
		}

		// Token: 0x06004270 RID: 17008 RVA: 0x000F74CC File Offset: 0x000F56CC
		public bool TryDequeue(out T result)
		{
			SingleProducerSingleConsumerQueue<T>.Segment head = this.m_head;
			T[] array = head.m_array;
			int first = head.m_state.m_first;
			if (first != head.m_state.m_lastCopy)
			{
				result = array[first];
				array[first] = default(T);
				head.m_state.m_first = (first + 1) & (array.Length - 1);
				return true;
			}
			return this.TryDequeueSlow(ref head, ref array, out result);
		}

		// Token: 0x06004271 RID: 17009 RVA: 0x000F7548 File Offset: 0x000F5748
		private bool TryDequeueSlow(ref SingleProducerSingleConsumerQueue<T>.Segment segment, ref T[] array, out T result)
		{
			if (segment.m_state.m_last != segment.m_state.m_lastCopy)
			{
				segment.m_state.m_lastCopy = segment.m_state.m_last;
				return this.TryDequeue(out result);
			}
			if (segment.m_next != null && segment.m_state.m_first == segment.m_state.m_last)
			{
				segment = segment.m_next;
				array = segment.m_array;
				this.m_head = segment;
			}
			int first = segment.m_state.m_first;
			if (first == segment.m_state.m_last)
			{
				result = default(T);
				return false;
			}
			result = array[first];
			array[first] = default(T);
			segment.m_state.m_first = (first + 1) & (segment.m_array.Length - 1);
			segment.m_state.m_lastCopy = segment.m_state.m_last;
			return true;
		}

		// Token: 0x06004272 RID: 17010 RVA: 0x000F7658 File Offset: 0x000F5858
		public bool TryPeek(out T result)
		{
			SingleProducerSingleConsumerQueue<T>.Segment head = this.m_head;
			T[] array = head.m_array;
			int first = head.m_state.m_first;
			if (first != head.m_state.m_lastCopy)
			{
				result = array[first];
				return true;
			}
			return this.TryPeekSlow(ref head, ref array, out result);
		}

		// Token: 0x06004273 RID: 17011 RVA: 0x000F76AC File Offset: 0x000F58AC
		private bool TryPeekSlow(ref SingleProducerSingleConsumerQueue<T>.Segment segment, ref T[] array, out T result)
		{
			if (segment.m_state.m_last != segment.m_state.m_lastCopy)
			{
				segment.m_state.m_lastCopy = segment.m_state.m_last;
				return this.TryPeek(out result);
			}
			if (segment.m_next != null && segment.m_state.m_first == segment.m_state.m_last)
			{
				segment = segment.m_next;
				array = segment.m_array;
				this.m_head = segment;
			}
			int first = segment.m_state.m_first;
			if (first == segment.m_state.m_last)
			{
				result = default(T);
				return false;
			}
			result = array[first];
			return true;
		}

		// Token: 0x06004274 RID: 17012 RVA: 0x000F7774 File Offset: 0x000F5974
		public bool TryDequeueIf(Predicate<T> predicate, out T result)
		{
			SingleProducerSingleConsumerQueue<T>.Segment head = this.m_head;
			T[] array = head.m_array;
			int first = head.m_state.m_first;
			if (first == head.m_state.m_lastCopy)
			{
				return this.TryDequeueIfSlow(predicate, ref head, ref array, out result);
			}
			result = array[first];
			if (predicate == null || predicate(result))
			{
				array[first] = default(T);
				head.m_state.m_first = (first + 1) & (array.Length - 1);
				return true;
			}
			result = default(T);
			return false;
		}

		// Token: 0x06004275 RID: 17013 RVA: 0x000F7808 File Offset: 0x000F5A08
		private bool TryDequeueIfSlow(Predicate<T> predicate, ref SingleProducerSingleConsumerQueue<T>.Segment segment, ref T[] array, out T result)
		{
			if (segment.m_state.m_last != segment.m_state.m_lastCopy)
			{
				segment.m_state.m_lastCopy = segment.m_state.m_last;
				return this.TryDequeueIf(predicate, out result);
			}
			if (segment.m_next != null && segment.m_state.m_first == segment.m_state.m_last)
			{
				segment = segment.m_next;
				array = segment.m_array;
				this.m_head = segment;
			}
			int first = segment.m_state.m_first;
			if (first == segment.m_state.m_last)
			{
				result = default(T);
				return false;
			}
			result = array[first];
			if (predicate == null || predicate(result))
			{
				array[first] = default(T);
				segment.m_state.m_first = (first + 1) & (segment.m_array.Length - 1);
				segment.m_state.m_lastCopy = segment.m_state.m_last;
				return true;
			}
			result = default(T);
			return false;
		}

		// Token: 0x06004276 RID: 17014 RVA: 0x000F7938 File Offset: 0x000F5B38
		public void Clear()
		{
			T t;
			while (this.TryDequeue(out t))
			{
			}
		}

		// Token: 0x170009E3 RID: 2531
		// (get) Token: 0x06004277 RID: 17015 RVA: 0x000F7950 File Offset: 0x000F5B50
		public bool IsEmpty
		{
			get
			{
				SingleProducerSingleConsumerQueue<T>.Segment head = this.m_head;
				return head.m_state.m_first == head.m_state.m_lastCopy && head.m_state.m_first == head.m_state.m_last && head.m_next == null;
			}
		}

		// Token: 0x06004278 RID: 17016 RVA: 0x000F79A9 File Offset: 0x000F5BA9
		public IEnumerator<T> GetEnumerator()
		{
			SingleProducerSingleConsumerQueue<T>.Segment segment;
			for (segment = this.m_head; segment != null; segment = segment.m_next)
			{
				for (int pt = segment.m_state.m_first; pt != segment.m_state.m_last; pt = (pt + 1) & (segment.m_array.Length - 1))
				{
					yield return segment.m_array[pt];
				}
			}
			segment = null;
			yield break;
		}

		// Token: 0x06004279 RID: 17017 RVA: 0x000F79B8 File Offset: 0x000F5BB8
		IEnumerator IEnumerable.GetEnumerator()
		{
			return this.GetEnumerator();
		}

		// Token: 0x170009E4 RID: 2532
		// (get) Token: 0x0600427A RID: 17018 RVA: 0x000F79C0 File Offset: 0x000F5BC0
		public int Count
		{
			get
			{
				int num = 0;
				for (SingleProducerSingleConsumerQueue<T>.Segment segment = this.m_head; segment != null; segment = segment.m_next)
				{
					int num2 = segment.m_array.Length;
					int first;
					int last;
					do
					{
						first = segment.m_state.m_first;
						last = segment.m_state.m_last;
					}
					while (first != segment.m_state.m_first);
					num += (last - first) & (num2 - 1);
				}
				return num;
			}
		}

		// Token: 0x0600427B RID: 17019 RVA: 0x000F7A28 File Offset: 0x000F5C28
		int IProducerConsumerQueue<!0>.GetCountSafe(object syncObj)
		{
			int count;
			lock (syncObj)
			{
				count = this.Count;
			}
			return count;
		}

		// Token: 0x04001B9C RID: 7068
		private const int INIT_SEGMENT_SIZE = 32;

		// Token: 0x04001B9D RID: 7069
		private const int MAX_SEGMENT_SIZE = 16777216;

		// Token: 0x04001B9E RID: 7070
		private volatile SingleProducerSingleConsumerQueue<T>.Segment m_head;

		// Token: 0x04001B9F RID: 7071
		private volatile SingleProducerSingleConsumerQueue<T>.Segment m_tail;

		// Token: 0x02000C2B RID: 3115
		[StructLayout(LayoutKind.Sequential)]
		private sealed class Segment
		{
			// Token: 0x06007025 RID: 28709 RVA: 0x00182BAA File Offset: 0x00180DAA
			internal Segment(int size)
			{
				this.m_array = new T[size];
			}

			// Token: 0x040036F6 RID: 14070
			internal SingleProducerSingleConsumerQueue<T>.Segment m_next;

			// Token: 0x040036F7 RID: 14071
			internal readonly T[] m_array;

			// Token: 0x040036F8 RID: 14072
			internal SingleProducerSingleConsumerQueue<T>.SegmentState m_state;
		}

		// Token: 0x02000C2C RID: 3116
		private struct SegmentState
		{
			// Token: 0x040036F9 RID: 14073
			internal PaddingFor32 m_pad0;

			// Token: 0x040036FA RID: 14074
			internal volatile int m_first;

			// Token: 0x040036FB RID: 14075
			internal int m_lastCopy;

			// Token: 0x040036FC RID: 14076
			internal PaddingFor32 m_pad1;

			// Token: 0x040036FD RID: 14077
			internal int m_firstCopy;

			// Token: 0x040036FE RID: 14078
			internal volatile int m_last;

			// Token: 0x040036FF RID: 14079
			internal PaddingFor32 m_pad2;
		}

		// Token: 0x02000C2D RID: 3117
		private sealed class SingleProducerSingleConsumerQueue_DebugView
		{
			// Token: 0x06007026 RID: 28710 RVA: 0x00182BBE File Offset: 0x00180DBE
			public SingleProducerSingleConsumerQueue_DebugView(SingleProducerSingleConsumerQueue<T> queue)
			{
				this.m_queue = queue;
			}

			// Token: 0x17001337 RID: 4919
			// (get) Token: 0x06007027 RID: 28711 RVA: 0x00182BD0 File Offset: 0x00180DD0
			[DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
			public T[] Items
			{
				get
				{
					List<T> list = new List<T>();
					foreach (T item in this.m_queue)
					{
						list.Add(item);
					}
					return list.ToArray();
				}
			}

			// Token: 0x04003700 RID: 14080
			private readonly SingleProducerSingleConsumerQueue<T> m_queue;
		}
	}
}
