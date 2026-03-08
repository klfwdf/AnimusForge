using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Security;
using System.Security.Permissions;

namespace System.Threading
{
	// Token: 0x0200050D RID: 1293
	internal sealed class PinnableBufferCache
	{
		// Token: 0x06003CCB RID: 15563 RVA: 0x000E5304 File Offset: 0x000E3504
		public PinnableBufferCache(string cacheName, int numberOfElements)
			: this(cacheName, () => new byte[numberOfElements])
		{
		}

		// Token: 0x06003CCC RID: 15564 RVA: 0x000E5331 File Offset: 0x000E3531
		public byte[] AllocateBuffer()
		{
			return (byte[])this.Allocate();
		}

		// Token: 0x06003CCD RID: 15565 RVA: 0x000E533E File Offset: 0x000E353E
		public void FreeBuffer(byte[] buffer)
		{
			this.Free(buffer);
		}

		// Token: 0x06003CCE RID: 15566 RVA: 0x000E5348 File Offset: 0x000E3548
		[SecuritySafeCritical]
		[EnvironmentPermission(SecurityAction.Assert, Unrestricted = true)]
		internal PinnableBufferCache(string cacheName, Func<object> factory)
		{
			this.m_NotGen2 = new List<object>(16);
			this.m_factory = factory;
			string variable = "PinnableBufferCache_" + cacheName + "_Disabled";
			try
			{
				string environmentVariable = Environment.GetEnvironmentVariable(variable);
				if (environmentVariable != null)
				{
					PinnableBufferCacheEventSource.Log.DebugMessage("Creating " + cacheName + " PinnableBufferCacheDisabled=" + environmentVariable);
					int num = environmentVariable.IndexOf(cacheName, StringComparison.OrdinalIgnoreCase);
					if (0 <= num)
					{
						PinnableBufferCacheEventSource.Log.DebugMessage("Disabling " + cacheName);
						return;
					}
				}
			}
			catch
			{
			}
			string variable2 = "PinnableBufferCache_" + cacheName + "_MinCount";
			try
			{
				string environmentVariable2 = Environment.GetEnvironmentVariable(variable2);
				if (environmentVariable2 != null && int.TryParse(environmentVariable2, out this.m_minBufferCount))
				{
					this.CreateNewBuffers();
				}
			}
			catch
			{
			}
			PinnableBufferCacheEventSource.Log.Create(cacheName);
			this.m_CacheName = cacheName;
		}

		// Token: 0x06003CCF RID: 15567 RVA: 0x000E5440 File Offset: 0x000E3640
		[SecuritySafeCritical]
		internal object Allocate()
		{
			if (this.m_CacheName == null)
			{
				return this.m_factory();
			}
			object obj;
			if (!this.m_FreeList.TryPop(out obj))
			{
				this.Restock(out obj);
			}
			if (PinnableBufferCacheEventSource.Log.IsEnabled())
			{
				int num = Interlocked.Increment(ref this.m_numAllocCalls);
				if (num >= 1024)
				{
					lock (this)
					{
						int num2 = Interlocked.Exchange(ref this.m_numAllocCalls, 0);
						if (num2 >= 1024)
						{
							int num3 = 0;
							foreach (object obj2 in this.m_FreeList)
							{
								if (GC.GetGeneration(obj2) < GC.MaxGeneration)
								{
									num3++;
								}
							}
							PinnableBufferCacheEventSource.Log.WalkFreeListResult(this.m_CacheName, this.m_FreeList.Count, num3);
						}
					}
				}
				PinnableBufferCacheEventSource.Log.AllocateBuffer(this.m_CacheName, PinnableBufferCacheEventSource.AddressOf(obj), obj.GetHashCode(), GC.GetGeneration(obj), this.m_FreeList.Count);
			}
			return obj;
		}

		// Token: 0x06003CD0 RID: 15568 RVA: 0x000E557C File Offset: 0x000E377C
		[SecuritySafeCritical]
		internal void Free(object buffer)
		{
			if (this.m_CacheName == null)
			{
				return;
			}
			if (PinnableBufferCacheEventSource.Log.IsEnabled())
			{
				PinnableBufferCacheEventSource.Log.FreeBuffer(this.m_CacheName, PinnableBufferCacheEventSource.AddressOf(buffer), buffer.GetHashCode(), this.m_FreeList.Count);
			}
			if (buffer == null)
			{
				if (PinnableBufferCacheEventSource.Log.IsEnabled())
				{
					PinnableBufferCacheEventSource.Log.FreeBufferNull(this.m_CacheName, this.m_FreeList.Count);
				}
				return;
			}
			if (this.m_gen1CountAtLastRestock + 3 > GC.CollectionCount(GC.MaxGeneration - 1))
			{
				lock (this)
				{
					if (GC.GetGeneration(buffer) < GC.MaxGeneration)
					{
						this.m_moreThanFreeListNeeded = true;
						PinnableBufferCacheEventSource.Log.FreeBufferStillTooYoung(this.m_CacheName, this.m_NotGen2.Count);
						this.m_NotGen2.Add(buffer);
						this.m_gen1CountAtLastRestock = GC.CollectionCount(GC.MaxGeneration - 1);
						return;
					}
				}
			}
			this.m_FreeList.Push(buffer);
		}

		// Token: 0x06003CD1 RID: 15569 RVA: 0x000E568C File Offset: 0x000E388C
		[SecuritySafeCritical]
		private void Restock(out object returnBuffer)
		{
			lock (this)
			{
				if (!this.m_FreeList.TryPop(out returnBuffer))
				{
					if (this.m_restockSize == 0)
					{
						Gen2GcCallback.Register(new Func<object, bool>(PinnableBufferCache.Gen2GcCallbackFunc), this);
					}
					this.m_moreThanFreeListNeeded = true;
					PinnableBufferCacheEventSource.Log.AllocateBufferFreeListEmpty(this.m_CacheName, this.m_NotGen2.Count);
					if (this.m_NotGen2.Count == 0)
					{
						this.CreateNewBuffers();
					}
					int index = this.m_NotGen2.Count - 1;
					if (GC.GetGeneration(this.m_NotGen2[index]) < GC.MaxGeneration && GC.GetGeneration(this.m_NotGen2[0]) == GC.MaxGeneration)
					{
						index = 0;
					}
					returnBuffer = this.m_NotGen2[index];
					this.m_NotGen2.RemoveAt(index);
					if (PinnableBufferCacheEventSource.Log.IsEnabled() && GC.GetGeneration(returnBuffer) < GC.MaxGeneration)
					{
						PinnableBufferCacheEventSource.Log.AllocateBufferFromNotGen2(this.m_CacheName, this.m_NotGen2.Count);
					}
					if (!this.AgePendingBuffers() && this.m_NotGen2.Count == this.m_restockSize / 2)
					{
						PinnableBufferCacheEventSource.Log.DebugMessage("Proactively adding more buffers to aging pool");
						this.CreateNewBuffers();
					}
				}
			}
		}

		// Token: 0x06003CD2 RID: 15570 RVA: 0x000E57F0 File Offset: 0x000E39F0
		[SecuritySafeCritical]
		private bool AgePendingBuffers()
		{
			if (this.m_gen1CountAtLastRestock < GC.CollectionCount(GC.MaxGeneration - 1))
			{
				int num = 0;
				List<object> list = new List<object>();
				PinnableBufferCacheEventSource.Log.AllocateBufferAged(this.m_CacheName, this.m_NotGen2.Count);
				for (int i = 0; i < this.m_NotGen2.Count; i++)
				{
					object obj = this.m_NotGen2[i];
					if (GC.GetGeneration(obj) >= GC.MaxGeneration)
					{
						this.m_FreeList.Push(obj);
						num++;
					}
					else
					{
						list.Add(obj);
					}
				}
				PinnableBufferCacheEventSource.Log.AgePendingBuffersResults(this.m_CacheName, num, list.Count);
				this.m_NotGen2 = list;
				return true;
			}
			return false;
		}

		// Token: 0x06003CD3 RID: 15571 RVA: 0x000E58A4 File Offset: 0x000E3AA4
		private void CreateNewBuffers()
		{
			if (this.m_restockSize == 0)
			{
				this.m_restockSize = 4;
			}
			else if (this.m_restockSize < 16)
			{
				this.m_restockSize = 16;
			}
			else if (this.m_restockSize < 256)
			{
				this.m_restockSize *= 2;
			}
			else if (this.m_restockSize < 4096)
			{
				this.m_restockSize = this.m_restockSize * 3 / 2;
			}
			else
			{
				this.m_restockSize = 4096;
			}
			if (this.m_minBufferCount > this.m_buffersUnderManagement)
			{
				this.m_restockSize = Math.Max(this.m_restockSize, this.m_minBufferCount - this.m_buffersUnderManagement);
			}
			PinnableBufferCacheEventSource.Log.AllocateBufferCreatingNewBuffers(this.m_CacheName, this.m_buffersUnderManagement, this.m_restockSize);
			for (int i = 0; i < this.m_restockSize; i++)
			{
				object item = this.m_factory();
				object obj = new object();
				this.m_NotGen2.Add(item);
			}
			this.m_buffersUnderManagement += this.m_restockSize;
			this.m_gen1CountAtLastRestock = GC.CollectionCount(GC.MaxGeneration - 1);
		}

		// Token: 0x06003CD4 RID: 15572 RVA: 0x000E59B9 File Offset: 0x000E3BB9
		[SecuritySafeCritical]
		private static bool Gen2GcCallbackFunc(object targetObj)
		{
			return ((PinnableBufferCache)targetObj).TrimFreeListIfNeeded();
		}

		// Token: 0x06003CD5 RID: 15573 RVA: 0x000E59C8 File Offset: 0x000E3BC8
		[SecuritySafeCritical]
		private bool TrimFreeListIfNeeded()
		{
			int tickCount = Environment.TickCount;
			int num = tickCount - this.m_msecNoUseBeyondFreeListSinceThisTime;
			PinnableBufferCacheEventSource.Log.TrimCheck(this.m_CacheName, this.m_buffersUnderManagement, this.m_moreThanFreeListNeeded, num);
			if (this.m_moreThanFreeListNeeded)
			{
				this.m_moreThanFreeListNeeded = false;
				this.m_trimmingExperimentInProgress = false;
				this.m_msecNoUseBeyondFreeListSinceThisTime = tickCount;
				return true;
			}
			if (0 <= num && num < 10000)
			{
				return true;
			}
			lock (this)
			{
				if (this.m_moreThanFreeListNeeded)
				{
					this.m_moreThanFreeListNeeded = false;
					this.m_trimmingExperimentInProgress = false;
					this.m_msecNoUseBeyondFreeListSinceThisTime = tickCount;
					return true;
				}
				int count = this.m_FreeList.Count;
				if (this.m_NotGen2.Count > 0)
				{
					if (!this.m_trimmingExperimentInProgress)
					{
						PinnableBufferCacheEventSource.Log.TrimFlush(this.m_CacheName, this.m_buffersUnderManagement, count, this.m_NotGen2.Count);
						this.AgePendingBuffers();
						this.m_trimmingExperimentInProgress = true;
						return true;
					}
					PinnableBufferCacheEventSource.Log.TrimFree(this.m_CacheName, this.m_buffersUnderManagement, count, this.m_NotGen2.Count);
					this.m_buffersUnderManagement -= this.m_NotGen2.Count;
					int num2 = this.m_buffersUnderManagement / 4;
					if (num2 < this.m_restockSize)
					{
						this.m_restockSize = Math.Max(num2, 16);
					}
					this.m_NotGen2.Clear();
					this.m_trimmingExperimentInProgress = false;
					return true;
				}
				else
				{
					int num3 = count / 4 + 1;
					if (count * 15 <= this.m_buffersUnderManagement || this.m_buffersUnderManagement - num3 <= this.m_minBufferCount)
					{
						PinnableBufferCacheEventSource.Log.TrimFreeSizeOK(this.m_CacheName, this.m_buffersUnderManagement, count);
						return true;
					}
					PinnableBufferCacheEventSource.Log.TrimExperiment(this.m_CacheName, this.m_buffersUnderManagement, count, num3);
					for (int i = 0; i < num3; i++)
					{
						object item;
						if (this.m_FreeList.TryPop(out item))
						{
							this.m_NotGen2.Add(item);
						}
					}
					this.m_msecNoUseBeyondFreeListSinceThisTime = tickCount;
					this.m_trimmingExperimentInProgress = true;
				}
			}
			return true;
		}

		// Token: 0x040019CB RID: 6603
		private const int DefaultNumberOfBuffers = 16;

		// Token: 0x040019CC RID: 6604
		private string m_CacheName;

		// Token: 0x040019CD RID: 6605
		private Func<object> m_factory;

		// Token: 0x040019CE RID: 6606
		private ConcurrentStack<object> m_FreeList = new ConcurrentStack<object>();

		// Token: 0x040019CF RID: 6607
		private List<object> m_NotGen2;

		// Token: 0x040019D0 RID: 6608
		private int m_gen1CountAtLastRestock;

		// Token: 0x040019D1 RID: 6609
		private int m_msecNoUseBeyondFreeListSinceThisTime;

		// Token: 0x040019D2 RID: 6610
		private bool m_moreThanFreeListNeeded;

		// Token: 0x040019D3 RID: 6611
		private int m_buffersUnderManagement;

		// Token: 0x040019D4 RID: 6612
		private int m_restockSize;

		// Token: 0x040019D5 RID: 6613
		private bool m_trimmingExperimentInProgress;

		// Token: 0x040019D6 RID: 6614
		private int m_minBufferCount;

		// Token: 0x040019D7 RID: 6615
		private int m_numAllocCalls;
	}
}
