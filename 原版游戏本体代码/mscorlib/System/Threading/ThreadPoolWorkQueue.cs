using System;
using System.Diagnostics.Tracing;
using System.Runtime.ConstrainedExecution;
using System.Security;

namespace System.Threading
{
	// Token: 0x0200051B RID: 1307
	internal sealed class ThreadPoolWorkQueue
	{
		// Token: 0x06003DC1 RID: 15809 RVA: 0x000E6EE4 File Offset: 0x000E50E4
		public ThreadPoolWorkQueue()
		{
			this.queueTail = (this.queueHead = new ThreadPoolWorkQueue.QueueSegment());
			this.loggingEnabled = FrameworkEventSource.Log.IsEnabled(EventLevel.Verbose, (EventKeywords)18L);
		}

		// Token: 0x06003DC2 RID: 15810 RVA: 0x000E6F23 File Offset: 0x000E5123
		[SecurityCritical]
		public ThreadPoolWorkQueueThreadLocals EnsureCurrentThreadHasQueue()
		{
			if (ThreadPoolWorkQueueThreadLocals.threadLocals == null)
			{
				ThreadPoolWorkQueueThreadLocals.threadLocals = new ThreadPoolWorkQueueThreadLocals(this);
			}
			return ThreadPoolWorkQueueThreadLocals.threadLocals;
		}

		// Token: 0x06003DC3 RID: 15811 RVA: 0x000E6F3C File Offset: 0x000E513C
		[SecurityCritical]
		internal void EnsureThreadRequested()
		{
			int num;
			for (int i = this.numOutstandingThreadRequests; i < ThreadPoolGlobals.processorCount; i = num)
			{
				num = Interlocked.CompareExchange(ref this.numOutstandingThreadRequests, i + 1, i);
				if (num == i)
				{
					ThreadPool.RequestWorkerThread();
					return;
				}
			}
		}

		// Token: 0x06003DC4 RID: 15812 RVA: 0x000E6F7C File Offset: 0x000E517C
		[SecurityCritical]
		internal void MarkThreadRequestSatisfied()
		{
			int num;
			for (int i = this.numOutstandingThreadRequests; i > 0; i = num)
			{
				num = Interlocked.CompareExchange(ref this.numOutstandingThreadRequests, i - 1, i);
				if (num == i)
				{
					break;
				}
			}
		}

		// Token: 0x06003DC5 RID: 15813 RVA: 0x000E6FB0 File Offset: 0x000E51B0
		[SecurityCritical]
		public void Enqueue(IThreadPoolWorkItem callback, bool forceGlobal)
		{
			ThreadPoolWorkQueueThreadLocals threadPoolWorkQueueThreadLocals = null;
			if (!forceGlobal)
			{
				threadPoolWorkQueueThreadLocals = ThreadPoolWorkQueueThreadLocals.threadLocals;
			}
			if (this.loggingEnabled)
			{
				FrameworkEventSource.Log.ThreadPoolEnqueueWorkObject(callback);
			}
			if (threadPoolWorkQueueThreadLocals != null)
			{
				threadPoolWorkQueueThreadLocals.workStealingQueue.LocalPush(callback);
			}
			else
			{
				ThreadPoolWorkQueue.QueueSegment queueSegment = this.queueHead;
				while (!queueSegment.TryEnqueue(callback))
				{
					Interlocked.CompareExchange<ThreadPoolWorkQueue.QueueSegment>(ref queueSegment.Next, new ThreadPoolWorkQueue.QueueSegment(), null);
					while (queueSegment.Next != null)
					{
						Interlocked.CompareExchange<ThreadPoolWorkQueue.QueueSegment>(ref this.queueHead, queueSegment.Next, queueSegment);
						queueSegment = this.queueHead;
					}
				}
			}
			this.EnsureThreadRequested();
		}

		// Token: 0x06003DC6 RID: 15814 RVA: 0x000E7044 File Offset: 0x000E5244
		[SecurityCritical]
		internal bool LocalFindAndPop(IThreadPoolWorkItem callback)
		{
			ThreadPoolWorkQueueThreadLocals threadLocals = ThreadPoolWorkQueueThreadLocals.threadLocals;
			return threadLocals != null && threadLocals.workStealingQueue.LocalFindAndPop(callback);
		}

		// Token: 0x06003DC7 RID: 15815 RVA: 0x000E7068 File Offset: 0x000E5268
		[SecurityCritical]
		public void Dequeue(ThreadPoolWorkQueueThreadLocals tl, out IThreadPoolWorkItem callback, out bool missedSteal)
		{
			callback = null;
			missedSteal = false;
			ThreadPoolWorkQueue.WorkStealingQueue workStealingQueue = tl.workStealingQueue;
			workStealingQueue.LocalPop(out callback);
			if (callback == null)
			{
				ThreadPoolWorkQueue.QueueSegment queueSegment = this.queueTail;
				while (!queueSegment.TryDequeue(out callback) && queueSegment.Next != null && queueSegment.IsUsedUp())
				{
					Interlocked.CompareExchange<ThreadPoolWorkQueue.QueueSegment>(ref this.queueTail, queueSegment.Next, queueSegment);
					queueSegment = this.queueTail;
				}
			}
			if (callback == null)
			{
				ThreadPoolWorkQueue.WorkStealingQueue[] array = ThreadPoolWorkQueue.allThreadQueues.Current;
				int num = tl.random.Next(array.Length);
				for (int i = array.Length; i > 0; i--)
				{
					ThreadPoolWorkQueue.WorkStealingQueue workStealingQueue2 = Volatile.Read<ThreadPoolWorkQueue.WorkStealingQueue>(ref array[num % array.Length]);
					if (workStealingQueue2 != null && workStealingQueue2 != workStealingQueue && workStealingQueue2.TrySteal(out callback, ref missedSteal))
					{
						break;
					}
					num++;
				}
			}
		}

		// Token: 0x06003DC8 RID: 15816 RVA: 0x000E712C File Offset: 0x000E532C
		[SecurityCritical]
		internal static bool Dispatch()
		{
			ThreadPoolWorkQueue workQueue = ThreadPoolGlobals.workQueue;
			int tickCount = Environment.TickCount;
			workQueue.MarkThreadRequestSatisfied();
			workQueue.loggingEnabled = FrameworkEventSource.Log.IsEnabled(EventLevel.Verbose, (EventKeywords)18L);
			bool flag = true;
			IThreadPoolWorkItem threadPoolWorkItem = null;
			try
			{
				ThreadPoolWorkQueueThreadLocals tl = workQueue.EnsureCurrentThreadHasQueue();
				while ((long)(Environment.TickCount - tickCount) < (long)((ulong)ThreadPoolGlobals.tpQuantum))
				{
					try
					{
					}
					finally
					{
						bool flag2 = false;
						workQueue.Dequeue(tl, out threadPoolWorkItem, out flag2);
						if (threadPoolWorkItem == null)
						{
							flag = flag2;
						}
						else
						{
							workQueue.EnsureThreadRequested();
						}
					}
					if (threadPoolWorkItem == null)
					{
						return true;
					}
					if (workQueue.loggingEnabled)
					{
						FrameworkEventSource.Log.ThreadPoolDequeueWorkObject(threadPoolWorkItem);
					}
					if (ThreadPoolGlobals.enableWorkerTracking)
					{
						bool flag3 = false;
						try
						{
							try
							{
							}
							finally
							{
								ThreadPool.ReportThreadStatus(true);
								flag3 = true;
							}
							threadPoolWorkItem.ExecuteWorkItem();
							threadPoolWorkItem = null;
							goto IL_A6;
						}
						finally
						{
							if (flag3)
							{
								ThreadPool.ReportThreadStatus(false);
							}
						}
						goto IL_9E;
					}
					goto IL_9E;
					IL_A6:
					if (!ThreadPool.NotifyWorkItemComplete())
					{
						return false;
					}
					continue;
					IL_9E:
					threadPoolWorkItem.ExecuteWorkItem();
					threadPoolWorkItem = null;
					goto IL_A6;
				}
				return true;
			}
			catch (ThreadAbortException tae)
			{
				if (threadPoolWorkItem != null)
				{
					threadPoolWorkItem.MarkAborted(tae);
				}
				flag = false;
			}
			finally
			{
				if (flag)
				{
					workQueue.EnsureThreadRequested();
				}
			}
			return true;
		}

		// Token: 0x04001A07 RID: 6663
		internal volatile ThreadPoolWorkQueue.QueueSegment queueHead;

		// Token: 0x04001A08 RID: 6664
		internal volatile ThreadPoolWorkQueue.QueueSegment queueTail;

		// Token: 0x04001A09 RID: 6665
		internal bool loggingEnabled;

		// Token: 0x04001A0A RID: 6666
		internal static ThreadPoolWorkQueue.SparseArray<ThreadPoolWorkQueue.WorkStealingQueue> allThreadQueues = new ThreadPoolWorkQueue.SparseArray<ThreadPoolWorkQueue.WorkStealingQueue>(16);

		// Token: 0x04001A0B RID: 6667
		private volatile int numOutstandingThreadRequests;

		// Token: 0x02000BF5 RID: 3061
		internal class SparseArray<T> where T : class
		{
			// Token: 0x06006F6C RID: 28524 RVA: 0x0017FDBE File Offset: 0x0017DFBE
			internal SparseArray(int initialSize)
			{
				this.m_array = new T[initialSize];
			}

			// Token: 0x17001322 RID: 4898
			// (get) Token: 0x06006F6D RID: 28525 RVA: 0x0017FDD4 File Offset: 0x0017DFD4
			internal T[] Current
			{
				get
				{
					return this.m_array;
				}
			}

			// Token: 0x06006F6E RID: 28526 RVA: 0x0017FDE0 File Offset: 0x0017DFE0
			internal int Add(T e)
			{
				for (;;)
				{
					T[] array = this.m_array;
					T[] obj = array;
					lock (obj)
					{
						for (int i = 0; i < array.Length; i++)
						{
							if (array[i] == null)
							{
								Volatile.Write<T>(ref array[i], e);
								return i;
							}
							if (i == array.Length - 1 && array == this.m_array)
							{
								T[] array2 = new T[array.Length * 2];
								Array.Copy(array, array2, i + 1);
								array2[i + 1] = e;
								this.m_array = array2;
								return i + 1;
							}
						}
						continue;
					}
					break;
				}
				int result;
				return result;
			}

			// Token: 0x06006F6F RID: 28527 RVA: 0x0017FE98 File Offset: 0x0017E098
			internal void Remove(T e)
			{
				T[] array = this.m_array;
				T[] obj = array;
				lock (obj)
				{
					for (int i = 0; i < this.m_array.Length; i++)
					{
						if (this.m_array[i] == e)
						{
							Volatile.Write<T>(ref this.m_array[i], default(T));
							break;
						}
					}
				}
			}

			// Token: 0x04003626 RID: 13862
			private volatile T[] m_array;
		}

		// Token: 0x02000BF6 RID: 3062
		internal class WorkStealingQueue
		{
			// Token: 0x06006F70 RID: 28528 RVA: 0x0017FF28 File Offset: 0x0017E128
			public void LocalPush(IThreadPoolWorkItem obj)
			{
				int num = this.m_tailIndex;
				if (num == 2147483647)
				{
					bool flag = false;
					try
					{
						this.m_foreignLock.Enter(ref flag);
						if (this.m_tailIndex == 2147483647)
						{
							this.m_headIndex &= this.m_mask;
							num = (this.m_tailIndex &= this.m_mask);
						}
					}
					finally
					{
						if (flag)
						{
							this.m_foreignLock.Exit(true);
						}
					}
				}
				if (num < this.m_headIndex + this.m_mask)
				{
					Volatile.Write<IThreadPoolWorkItem>(ref this.m_array[num & this.m_mask], obj);
					this.m_tailIndex = num + 1;
					return;
				}
				bool flag2 = false;
				try
				{
					this.m_foreignLock.Enter(ref flag2);
					int headIndex = this.m_headIndex;
					int num2 = this.m_tailIndex - this.m_headIndex;
					if (num2 >= this.m_mask)
					{
						IThreadPoolWorkItem[] array = new IThreadPoolWorkItem[this.m_array.Length << 1];
						for (int i = 0; i < this.m_array.Length; i++)
						{
							array[i] = this.m_array[(i + headIndex) & this.m_mask];
						}
						this.m_array = array;
						this.m_headIndex = 0;
						num = (this.m_tailIndex = num2);
						this.m_mask = (this.m_mask << 1) | 1;
					}
					Volatile.Write<IThreadPoolWorkItem>(ref this.m_array[num & this.m_mask], obj);
					this.m_tailIndex = num + 1;
				}
				finally
				{
					if (flag2)
					{
						this.m_foreignLock.Exit(false);
					}
				}
			}

			// Token: 0x06006F71 RID: 28529 RVA: 0x001800F0 File Offset: 0x0017E2F0
			public bool LocalFindAndPop(IThreadPoolWorkItem obj)
			{
				if (this.m_array[(this.m_tailIndex - 1) & this.m_mask] == obj)
				{
					IThreadPoolWorkItem threadPoolWorkItem;
					return this.LocalPop(out threadPoolWorkItem);
				}
				for (int i = this.m_tailIndex - 2; i >= this.m_headIndex; i--)
				{
					if (this.m_array[i & this.m_mask] == obj)
					{
						bool flag = false;
						try
						{
							this.m_foreignLock.Enter(ref flag);
							if (this.m_array[i & this.m_mask] == null)
							{
								return false;
							}
							Volatile.Write<IThreadPoolWorkItem>(ref this.m_array[i & this.m_mask], null);
							if (i == this.m_tailIndex)
							{
								this.m_tailIndex--;
							}
							else if (i == this.m_headIndex)
							{
								this.m_headIndex++;
							}
							return true;
						}
						finally
						{
							if (flag)
							{
								this.m_foreignLock.Exit(false);
							}
						}
					}
				}
				return false;
			}

			// Token: 0x06006F72 RID: 28530 RVA: 0x00180210 File Offset: 0x0017E410
			public bool LocalPop(out IThreadPoolWorkItem obj)
			{
				int num3;
				for (;;)
				{
					int num = this.m_tailIndex;
					if (this.m_headIndex >= num)
					{
						break;
					}
					num--;
					Interlocked.Exchange(ref this.m_tailIndex, num);
					if (this.m_headIndex > num)
					{
						bool flag = false;
						bool result;
						try
						{
							this.m_foreignLock.Enter(ref flag);
							if (this.m_headIndex <= num)
							{
								int num2 = num & this.m_mask;
								obj = Volatile.Read<IThreadPoolWorkItem>(ref this.m_array[num2]);
								if (obj == null)
								{
									continue;
								}
								this.m_array[num2] = null;
								result = true;
							}
							else
							{
								this.m_tailIndex = num + 1;
								obj = null;
								result = false;
							}
						}
						finally
						{
							if (flag)
							{
								this.m_foreignLock.Exit(false);
							}
						}
						return result;
					}
					num3 = num & this.m_mask;
					obj = Volatile.Read<IThreadPoolWorkItem>(ref this.m_array[num3]);
					if (obj != null)
					{
						goto Block_2;
					}
				}
				obj = null;
				return false;
				Block_2:
				this.m_array[num3] = null;
				return true;
			}

			// Token: 0x06006F73 RID: 28531 RVA: 0x0018030C File Offset: 0x0017E50C
			public bool TrySteal(out IThreadPoolWorkItem obj, ref bool missedSteal)
			{
				return this.TrySteal(out obj, ref missedSteal, 0);
			}

			// Token: 0x06006F74 RID: 28532 RVA: 0x00180318 File Offset: 0x0017E518
			private bool TrySteal(out IThreadPoolWorkItem obj, ref bool missedSteal, int millisecondsTimeout)
			{
				obj = null;
				while (this.m_headIndex < this.m_tailIndex)
				{
					bool flag = false;
					try
					{
						this.m_foreignLock.TryEnter(millisecondsTimeout, ref flag);
						if (flag)
						{
							int headIndex = this.m_headIndex;
							Interlocked.Exchange(ref this.m_headIndex, headIndex + 1);
							if (headIndex < this.m_tailIndex)
							{
								int num = headIndex & this.m_mask;
								obj = Volatile.Read<IThreadPoolWorkItem>(ref this.m_array[num]);
								if (obj == null)
								{
									continue;
								}
								this.m_array[num] = null;
								return true;
							}
							else
							{
								this.m_headIndex = headIndex;
								obj = null;
								missedSteal = true;
							}
						}
						else
						{
							missedSteal = true;
						}
					}
					finally
					{
						if (flag)
						{
							this.m_foreignLock.Exit(false);
						}
					}
					return false;
				}
				return false;
			}

			// Token: 0x04003627 RID: 13863
			private const int INITIAL_SIZE = 32;

			// Token: 0x04003628 RID: 13864
			internal volatile IThreadPoolWorkItem[] m_array = new IThreadPoolWorkItem[32];

			// Token: 0x04003629 RID: 13865
			private volatile int m_mask = 31;

			// Token: 0x0400362A RID: 13866
			private const int START_INDEX = 0;

			// Token: 0x0400362B RID: 13867
			private volatile int m_headIndex;

			// Token: 0x0400362C RID: 13868
			private volatile int m_tailIndex;

			// Token: 0x0400362D RID: 13869
			private SpinLock m_foreignLock = new SpinLock(false);
		}

		// Token: 0x02000BF7 RID: 3063
		internal class QueueSegment
		{
			// Token: 0x06006F76 RID: 28534 RVA: 0x00180410 File Offset: 0x0017E610
			private void GetIndexes(out int upper, out int lower)
			{
				int num = this.indexes;
				upper = (num >> 16) & 65535;
				lower = num & 65535;
			}

			// Token: 0x06006F77 RID: 28535 RVA: 0x0018043C File Offset: 0x0017E63C
			private bool CompareExchangeIndexes(ref int prevUpper, int newUpper, ref int prevLower, int newLower)
			{
				int num = (prevUpper << 16) | (prevLower & 65535);
				int value = (newUpper << 16) | (newLower & 65535);
				int num2 = Interlocked.CompareExchange(ref this.indexes, value, num);
				prevUpper = (num2 >> 16) & 65535;
				prevLower = num2 & 65535;
				return num2 == num;
			}

			// Token: 0x06006F78 RID: 28536 RVA: 0x0018048D File Offset: 0x0017E68D
			[ReliabilityContract(Consistency.WillNotCorruptState, Cer.MayFail)]
			public QueueSegment()
			{
				this.nodes = new IThreadPoolWorkItem[256];
			}

			// Token: 0x06006F79 RID: 28537 RVA: 0x001804A8 File Offset: 0x0017E6A8
			public bool IsUsedUp()
			{
				int num;
				int num2;
				this.GetIndexes(out num, out num2);
				return num == this.nodes.Length && num2 == this.nodes.Length;
			}

			// Token: 0x06006F7A RID: 28538 RVA: 0x001804D8 File Offset: 0x0017E6D8
			public bool TryEnqueue(IThreadPoolWorkItem node)
			{
				int num;
				int newLower;
				this.GetIndexes(out num, out newLower);
				while (num != this.nodes.Length)
				{
					if (this.CompareExchangeIndexes(ref num, num + 1, ref newLower, newLower))
					{
						Volatile.Write<IThreadPoolWorkItem>(ref this.nodes[num], node);
						return true;
					}
				}
				return false;
			}

			// Token: 0x06006F7B RID: 28539 RVA: 0x00180520 File Offset: 0x0017E720
			public bool TryDequeue(out IThreadPoolWorkItem node)
			{
				int num;
				int num2;
				this.GetIndexes(out num, out num2);
				while (num2 != num)
				{
					if (this.CompareExchangeIndexes(ref num, num, ref num2, num2 + 1))
					{
						SpinWait spinWait = default(SpinWait);
						for (;;)
						{
							IThreadPoolWorkItem threadPoolWorkItem;
							node = (threadPoolWorkItem = Volatile.Read<IThreadPoolWorkItem>(ref this.nodes[num2]));
							if (threadPoolWorkItem != null)
							{
								break;
							}
							spinWait.SpinOnce();
						}
						this.nodes[num2] = null;
						return true;
					}
				}
				node = null;
				return false;
			}

			// Token: 0x0400362E RID: 13870
			internal readonly IThreadPoolWorkItem[] nodes;

			// Token: 0x0400362F RID: 13871
			private const int QueueSegmentLength = 256;

			// Token: 0x04003630 RID: 13872
			private volatile int indexes;

			// Token: 0x04003631 RID: 13873
			public volatile ThreadPoolWorkQueue.QueueSegment Next;

			// Token: 0x04003632 RID: 13874
			private const int SixteenBits = 65535;
		}
	}
}
