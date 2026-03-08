using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Security.Permissions;
using System.Threading;

namespace System.Collections.Concurrent
{
	// Token: 0x020004AA RID: 1194
	[DebuggerDisplay("Count = {Count}")]
	[DebuggerTypeProxy(typeof(SystemCollectionsConcurrent_ProducerConsumerCollectionDebugView<>))]
	[__DynamicallyInvokable]
	[HostProtection(SecurityAction.LinkDemand, Synchronization = true, ExternalThreading = true)]
	[Serializable]
	public class ConcurrentStack<T> : IProducerConsumerCollection<T>, IEnumerable<!0>, IEnumerable, ICollection, IReadOnlyCollection<T>
	{
		// Token: 0x06003904 RID: 14596 RVA: 0x000DA65B File Offset: 0x000D885B
		[__DynamicallyInvokable]
		public ConcurrentStack()
		{
		}

		// Token: 0x06003905 RID: 14597 RVA: 0x000DA663 File Offset: 0x000D8863
		[__DynamicallyInvokable]
		public ConcurrentStack(IEnumerable<T> collection)
		{
			if (collection == null)
			{
				throw new ArgumentNullException("collection");
			}
			this.InitializeFromCollection(collection);
		}

		// Token: 0x06003906 RID: 14598 RVA: 0x000DA680 File Offset: 0x000D8880
		private void InitializeFromCollection(IEnumerable<T> collection)
		{
			ConcurrentStack<T>.Node node = null;
			foreach (T value in collection)
			{
				node = new ConcurrentStack<T>.Node(value)
				{
					m_next = node
				};
			}
			this.m_head = node;
		}

		// Token: 0x06003907 RID: 14599 RVA: 0x000DA6DC File Offset: 0x000D88DC
		[OnSerializing]
		private void OnSerializing(StreamingContext context)
		{
			this.m_serializationArray = this.ToArray();
		}

		// Token: 0x06003908 RID: 14600 RVA: 0x000DA6EC File Offset: 0x000D88EC
		[OnDeserialized]
		private void OnDeserialized(StreamingContext context)
		{
			ConcurrentStack<T>.Node node = null;
			ConcurrentStack<T>.Node head = null;
			for (int i = 0; i < this.m_serializationArray.Length; i++)
			{
				ConcurrentStack<T>.Node node2 = new ConcurrentStack<T>.Node(this.m_serializationArray[i]);
				if (node == null)
				{
					head = node2;
				}
				else
				{
					node.m_next = node2;
				}
				node = node2;
			}
			this.m_head = head;
			this.m_serializationArray = null;
		}

		// Token: 0x17000889 RID: 2185
		// (get) Token: 0x06003909 RID: 14601 RVA: 0x000DA742 File Offset: 0x000D8942
		[__DynamicallyInvokable]
		public bool IsEmpty
		{
			[__DynamicallyInvokable]
			get
			{
				return this.m_head == null;
			}
		}

		// Token: 0x1700088A RID: 2186
		// (get) Token: 0x0600390A RID: 14602 RVA: 0x000DA750 File Offset: 0x000D8950
		[__DynamicallyInvokable]
		public int Count
		{
			[__DynamicallyInvokable]
			get
			{
				int num = 0;
				for (ConcurrentStack<T>.Node node = this.m_head; node != null; node = node.m_next)
				{
					num++;
				}
				return num;
			}
		}

		// Token: 0x1700088B RID: 2187
		// (get) Token: 0x0600390B RID: 14603 RVA: 0x000DA779 File Offset: 0x000D8979
		[__DynamicallyInvokable]
		bool ICollection.IsSynchronized
		{
			[__DynamicallyInvokable]
			get
			{
				return false;
			}
		}

		// Token: 0x1700088C RID: 2188
		// (get) Token: 0x0600390C RID: 14604 RVA: 0x000DA77C File Offset: 0x000D897C
		[__DynamicallyInvokable]
		object ICollection.SyncRoot
		{
			[__DynamicallyInvokable]
			get
			{
				throw new NotSupportedException(Environment.GetResourceString("ConcurrentCollection_SyncRoot_NotSupported"));
			}
		}

		// Token: 0x0600390D RID: 14605 RVA: 0x000DA78D File Offset: 0x000D898D
		[__DynamicallyInvokable]
		public void Clear()
		{
			this.m_head = null;
		}

		// Token: 0x0600390E RID: 14606 RVA: 0x000DA798 File Offset: 0x000D8998
		[__DynamicallyInvokable]
		void ICollection.CopyTo(Array array, int index)
		{
			if (array == null)
			{
				throw new ArgumentNullException("array");
			}
			((ICollection)this.ToList()).CopyTo(array, index);
		}

		// Token: 0x0600390F RID: 14607 RVA: 0x000DA7B5 File Offset: 0x000D89B5
		[__DynamicallyInvokable]
		public void CopyTo(T[] array, int index)
		{
			if (array == null)
			{
				throw new ArgumentNullException("array");
			}
			this.ToList().CopyTo(array, index);
		}

		// Token: 0x06003910 RID: 14608 RVA: 0x000DA7D4 File Offset: 0x000D89D4
		[__DynamicallyInvokable]
		public void Push(T item)
		{
			ConcurrentStack<T>.Node node = new ConcurrentStack<T>.Node(item);
			node.m_next = this.m_head;
			if (Interlocked.CompareExchange<ConcurrentStack<T>.Node>(ref this.m_head, node, node.m_next) == node.m_next)
			{
				return;
			}
			this.PushCore(node, node);
		}

		// Token: 0x06003911 RID: 14609 RVA: 0x000DA819 File Offset: 0x000D8A19
		[__DynamicallyInvokable]
		public void PushRange(T[] items)
		{
			if (items == null)
			{
				throw new ArgumentNullException("items");
			}
			this.PushRange(items, 0, items.Length);
		}

		// Token: 0x06003912 RID: 14610 RVA: 0x000DA834 File Offset: 0x000D8A34
		[__DynamicallyInvokable]
		public void PushRange(T[] items, int startIndex, int count)
		{
			this.ValidatePushPopRangeInput(items, startIndex, count);
			if (count == 0)
			{
				return;
			}
			ConcurrentStack<T>.Node node2;
			ConcurrentStack<T>.Node node = (node2 = new ConcurrentStack<T>.Node(items[startIndex]));
			for (int i = startIndex + 1; i < startIndex + count; i++)
			{
				node2 = new ConcurrentStack<T>.Node(items[i])
				{
					m_next = node2
				};
			}
			node.m_next = this.m_head;
			if (Interlocked.CompareExchange<ConcurrentStack<T>.Node>(ref this.m_head, node2, node.m_next) == node.m_next)
			{
				return;
			}
			this.PushCore(node2, node);
		}

		// Token: 0x06003913 RID: 14611 RVA: 0x000DA8B4 File Offset: 0x000D8AB4
		private void PushCore(ConcurrentStack<T>.Node head, ConcurrentStack<T>.Node tail)
		{
			SpinWait spinWait = default(SpinWait);
			do
			{
				spinWait.SpinOnce();
				tail.m_next = this.m_head;
			}
			while (Interlocked.CompareExchange<ConcurrentStack<T>.Node>(ref this.m_head, head, tail.m_next) != tail.m_next);
			if (CDSCollectionETWBCLProvider.Log.IsEnabled())
			{
				CDSCollectionETWBCLProvider.Log.ConcurrentStack_FastPushFailed(spinWait.Count);
			}
		}

		// Token: 0x06003914 RID: 14612 RVA: 0x000DA918 File Offset: 0x000D8B18
		private void ValidatePushPopRangeInput(T[] items, int startIndex, int count)
		{
			if (items == null)
			{
				throw new ArgumentNullException("items");
			}
			if (count < 0)
			{
				throw new ArgumentOutOfRangeException("count", Environment.GetResourceString("ConcurrentStack_PushPopRange_CountOutOfRange"));
			}
			int num = items.Length;
			if (startIndex >= num || startIndex < 0)
			{
				throw new ArgumentOutOfRangeException("startIndex", Environment.GetResourceString("ConcurrentStack_PushPopRange_StartOutOfRange"));
			}
			if (num - count < startIndex)
			{
				throw new ArgumentException(Environment.GetResourceString("ConcurrentStack_PushPopRange_InvalidCount"));
			}
		}

		// Token: 0x06003915 RID: 14613 RVA: 0x000DA983 File Offset: 0x000D8B83
		[__DynamicallyInvokable]
		bool IProducerConsumerCollection<!0>.TryAdd(T item)
		{
			this.Push(item);
			return true;
		}

		// Token: 0x06003916 RID: 14614 RVA: 0x000DA990 File Offset: 0x000D8B90
		[__DynamicallyInvokable]
		public bool TryPeek(out T result)
		{
			ConcurrentStack<T>.Node head = this.m_head;
			if (head == null)
			{
				result = default(T);
				return false;
			}
			result = head.m_value;
			return true;
		}

		// Token: 0x06003917 RID: 14615 RVA: 0x000DA9C0 File Offset: 0x000D8BC0
		[__DynamicallyInvokable]
		public bool TryPop(out T result)
		{
			ConcurrentStack<T>.Node head = this.m_head;
			if (head == null)
			{
				result = default(T);
				return false;
			}
			if (Interlocked.CompareExchange<ConcurrentStack<T>.Node>(ref this.m_head, head.m_next, head) == head)
			{
				result = head.m_value;
				return true;
			}
			return this.TryPopCore(out result);
		}

		// Token: 0x06003918 RID: 14616 RVA: 0x000DAA0C File Offset: 0x000D8C0C
		[__DynamicallyInvokable]
		public int TryPopRange(T[] items)
		{
			if (items == null)
			{
				throw new ArgumentNullException("items");
			}
			return this.TryPopRange(items, 0, items.Length);
		}

		// Token: 0x06003919 RID: 14617 RVA: 0x000DAA28 File Offset: 0x000D8C28
		[__DynamicallyInvokable]
		public int TryPopRange(T[] items, int startIndex, int count)
		{
			this.ValidatePushPopRangeInput(items, startIndex, count);
			if (count == 0)
			{
				return 0;
			}
			ConcurrentStack<T>.Node head;
			int num = this.TryPopCore(count, out head);
			if (num > 0)
			{
				this.CopyRemovedItems(head, items, startIndex, num);
			}
			return num;
		}

		// Token: 0x0600391A RID: 14618 RVA: 0x000DAA5C File Offset: 0x000D8C5C
		private bool TryPopCore(out T result)
		{
			ConcurrentStack<T>.Node node;
			if (this.TryPopCore(1, out node) == 1)
			{
				result = node.m_value;
				return true;
			}
			result = default(T);
			return false;
		}

		// Token: 0x0600391B RID: 14619 RVA: 0x000DAA8C File Offset: 0x000D8C8C
		private int TryPopCore(int count, out ConcurrentStack<T>.Node poppedHead)
		{
			SpinWait spinWait = default(SpinWait);
			int num = 1;
			Random random = new Random(Environment.TickCount & int.MaxValue);
			ConcurrentStack<T>.Node head;
			int num2;
			for (;;)
			{
				head = this.m_head;
				if (head == null)
				{
					break;
				}
				ConcurrentStack<T>.Node node = head;
				num2 = 1;
				while (num2 < count && node.m_next != null)
				{
					node = node.m_next;
					num2++;
				}
				if (Interlocked.CompareExchange<ConcurrentStack<T>.Node>(ref this.m_head, node.m_next, head) == head)
				{
					goto Block_5;
				}
				for (int i = 0; i < num; i++)
				{
					spinWait.SpinOnce();
				}
				num = (spinWait.NextSpinWillYield ? random.Next(1, 8) : (num * 2));
			}
			if (count == 1 && CDSCollectionETWBCLProvider.Log.IsEnabled())
			{
				CDSCollectionETWBCLProvider.Log.ConcurrentStack_FastPopFailed(spinWait.Count);
			}
			poppedHead = null;
			return 0;
			Block_5:
			if (count == 1 && CDSCollectionETWBCLProvider.Log.IsEnabled())
			{
				CDSCollectionETWBCLProvider.Log.ConcurrentStack_FastPopFailed(spinWait.Count);
			}
			poppedHead = head;
			return num2;
		}

		// Token: 0x0600391C RID: 14620 RVA: 0x000DAB78 File Offset: 0x000D8D78
		private void CopyRemovedItems(ConcurrentStack<T>.Node head, T[] collection, int startIndex, int nodesCount)
		{
			ConcurrentStack<T>.Node node = head;
			for (int i = startIndex; i < startIndex + nodesCount; i++)
			{
				collection[i] = node.m_value;
				node = node.m_next;
			}
		}

		// Token: 0x0600391D RID: 14621 RVA: 0x000DABAA File Offset: 0x000D8DAA
		[__DynamicallyInvokable]
		bool IProducerConsumerCollection<!0>.TryTake(out T item)
		{
			return this.TryPop(out item);
		}

		// Token: 0x0600391E RID: 14622 RVA: 0x000DABB3 File Offset: 0x000D8DB3
		[__DynamicallyInvokable]
		public T[] ToArray()
		{
			return this.ToList().ToArray();
		}

		// Token: 0x0600391F RID: 14623 RVA: 0x000DABC0 File Offset: 0x000D8DC0
		private List<T> ToList()
		{
			List<T> list = new List<T>();
			for (ConcurrentStack<T>.Node node = this.m_head; node != null; node = node.m_next)
			{
				list.Add(node.m_value);
			}
			return list;
		}

		// Token: 0x06003920 RID: 14624 RVA: 0x000DABF5 File Offset: 0x000D8DF5
		[__DynamicallyInvokable]
		public IEnumerator<T> GetEnumerator()
		{
			return this.GetEnumerator(this.m_head);
		}

		// Token: 0x06003921 RID: 14625 RVA: 0x000DAC05 File Offset: 0x000D8E05
		private IEnumerator<T> GetEnumerator(ConcurrentStack<T>.Node head)
		{
			for (ConcurrentStack<T>.Node current = head; current != null; current = current.m_next)
			{
				yield return current.m_value;
			}
			yield break;
		}

		// Token: 0x06003922 RID: 14626 RVA: 0x000DAC14 File Offset: 0x000D8E14
		[__DynamicallyInvokable]
		IEnumerator IEnumerable.GetEnumerator()
		{
			return ((IEnumerable<!0>)this).GetEnumerator();
		}

		// Token: 0x04001911 RID: 6417
		[NonSerialized]
		private volatile ConcurrentStack<T>.Node m_head;

		// Token: 0x04001912 RID: 6418
		private T[] m_serializationArray;

		// Token: 0x04001913 RID: 6419
		private const int BACKOFF_MAX_YIELDS = 8;

		// Token: 0x02000BC2 RID: 3010
		private class Node
		{
			// Token: 0x06006E6B RID: 28267 RVA: 0x0017D0FC File Offset: 0x0017B2FC
			internal Node(T value)
			{
				this.m_value = value;
				this.m_next = null;
			}

			// Token: 0x04003593 RID: 13715
			internal readonly T m_value;

			// Token: 0x04003594 RID: 13716
			internal ConcurrentStack<T>.Node m_next;
		}
	}
}
