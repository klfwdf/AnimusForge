using System;
using System.Diagnostics.Tracing;
using System.Security;

namespace System.Threading
{
	// Token: 0x0200050F RID: 1295
	[EventSource(Name = "Microsoft-DotNETRuntime-PinnableBufferCache")]
	internal sealed class PinnableBufferCacheEventSource : EventSource
	{
		// Token: 0x06003CDA RID: 15578 RVA: 0x000E5CC4 File Offset: 0x000E3EC4
		[Event(1, Level = EventLevel.Verbose)]
		public void DebugMessage(string message)
		{
			if (base.IsEnabled())
			{
				base.WriteEvent(1, message);
			}
		}

		// Token: 0x06003CDB RID: 15579 RVA: 0x000E5CD6 File Offset: 0x000E3ED6
		[Event(2, Level = EventLevel.Verbose)]
		public void DebugMessage1(string message, long value)
		{
			if (base.IsEnabled())
			{
				base.WriteEvent(2, message, value);
			}
		}

		// Token: 0x06003CDC RID: 15580 RVA: 0x000E5CE9 File Offset: 0x000E3EE9
		[Event(3, Level = EventLevel.Verbose)]
		public void DebugMessage2(string message, long value1, long value2)
		{
			if (base.IsEnabled())
			{
				base.WriteEvent(3, new object[] { message, value1, value2 });
			}
		}

		// Token: 0x06003CDD RID: 15581 RVA: 0x000E5D16 File Offset: 0x000E3F16
		[Event(18, Level = EventLevel.Verbose)]
		public void DebugMessage3(string message, long value1, long value2, long value3)
		{
			if (base.IsEnabled())
			{
				base.WriteEvent(18, new object[] { message, value1, value2, value3 });
			}
		}

		// Token: 0x06003CDE RID: 15582 RVA: 0x000E5D4E File Offset: 0x000E3F4E
		[Event(4)]
		public void Create(string cacheName)
		{
			if (base.IsEnabled())
			{
				base.WriteEvent(4, cacheName);
			}
		}

		// Token: 0x06003CDF RID: 15583 RVA: 0x000E5D60 File Offset: 0x000E3F60
		[Event(5, Level = EventLevel.Verbose)]
		public void AllocateBuffer(string cacheName, ulong objectId, int objectHash, int objectGen, int freeCountAfter)
		{
			if (base.IsEnabled())
			{
				base.WriteEvent(5, new object[] { cacheName, objectId, objectHash, objectGen, freeCountAfter });
			}
		}

		// Token: 0x06003CE0 RID: 15584 RVA: 0x000E5DAC File Offset: 0x000E3FAC
		[Event(6)]
		public void AllocateBufferFromNotGen2(string cacheName, int notGen2CountAfter)
		{
			if (base.IsEnabled())
			{
				base.WriteEvent(6, cacheName, notGen2CountAfter);
			}
		}

		// Token: 0x06003CE1 RID: 15585 RVA: 0x000E5DBF File Offset: 0x000E3FBF
		[Event(7)]
		public void AllocateBufferCreatingNewBuffers(string cacheName, int totalBuffsBefore, int objectCount)
		{
			if (base.IsEnabled())
			{
				base.WriteEvent(7, cacheName, totalBuffsBefore, objectCount);
			}
		}

		// Token: 0x06003CE2 RID: 15586 RVA: 0x000E5DD3 File Offset: 0x000E3FD3
		[Event(8)]
		public void AllocateBufferAged(string cacheName, int agedCount)
		{
			if (base.IsEnabled())
			{
				base.WriteEvent(8, cacheName, agedCount);
			}
		}

		// Token: 0x06003CE3 RID: 15587 RVA: 0x000E5DE6 File Offset: 0x000E3FE6
		[Event(9)]
		public void AllocateBufferFreeListEmpty(string cacheName, int notGen2CountBefore)
		{
			if (base.IsEnabled())
			{
				base.WriteEvent(9, cacheName, notGen2CountBefore);
			}
		}

		// Token: 0x06003CE4 RID: 15588 RVA: 0x000E5DFA File Offset: 0x000E3FFA
		[Event(10, Level = EventLevel.Verbose)]
		public void FreeBuffer(string cacheName, ulong objectId, int objectHash, int freeCountBefore)
		{
			if (base.IsEnabled())
			{
				base.WriteEvent(10, new object[] { cacheName, objectId, objectHash, freeCountBefore });
			}
		}

		// Token: 0x06003CE5 RID: 15589 RVA: 0x000E5E32 File Offset: 0x000E4032
		[Event(11)]
		public void FreeBufferStillTooYoung(string cacheName, int notGen2CountBefore)
		{
			if (base.IsEnabled())
			{
				base.WriteEvent(11, cacheName, notGen2CountBefore);
			}
		}

		// Token: 0x06003CE6 RID: 15590 RVA: 0x000E5E46 File Offset: 0x000E4046
		[Event(13)]
		public void TrimCheck(string cacheName, int totalBuffs, bool neededMoreThanFreeList, int deltaMSec)
		{
			if (base.IsEnabled())
			{
				base.WriteEvent(13, new object[] { cacheName, totalBuffs, neededMoreThanFreeList, deltaMSec });
			}
		}

		// Token: 0x06003CE7 RID: 15591 RVA: 0x000E5E7E File Offset: 0x000E407E
		[Event(14)]
		public void TrimFree(string cacheName, int totalBuffs, int freeListCount, int toBeFreed)
		{
			if (base.IsEnabled())
			{
				base.WriteEvent(14, new object[] { cacheName, totalBuffs, freeListCount, toBeFreed });
			}
		}

		// Token: 0x06003CE8 RID: 15592 RVA: 0x000E5EB6 File Offset: 0x000E40B6
		[Event(15)]
		public void TrimExperiment(string cacheName, int totalBuffs, int freeListCount, int numTrimTrial)
		{
			if (base.IsEnabled())
			{
				base.WriteEvent(15, new object[] { cacheName, totalBuffs, freeListCount, numTrimTrial });
			}
		}

		// Token: 0x06003CE9 RID: 15593 RVA: 0x000E5EEE File Offset: 0x000E40EE
		[Event(16)]
		public void TrimFreeSizeOK(string cacheName, int totalBuffs, int freeListCount)
		{
			if (base.IsEnabled())
			{
				base.WriteEvent(16, cacheName, totalBuffs, freeListCount);
			}
		}

		// Token: 0x06003CEA RID: 15594 RVA: 0x000E5F03 File Offset: 0x000E4103
		[Event(17)]
		public void TrimFlush(string cacheName, int totalBuffs, int freeListCount, int notGen2CountBefore)
		{
			if (base.IsEnabled())
			{
				base.WriteEvent(17, new object[] { cacheName, totalBuffs, freeListCount, notGen2CountBefore });
			}
		}

		// Token: 0x06003CEB RID: 15595 RVA: 0x000E5F3B File Offset: 0x000E413B
		[Event(20)]
		public void AgePendingBuffersResults(string cacheName, int promotedToFreeListCount, int heldBackCount)
		{
			if (base.IsEnabled())
			{
				base.WriteEvent(20, cacheName, promotedToFreeListCount, heldBackCount);
			}
		}

		// Token: 0x06003CEC RID: 15596 RVA: 0x000E5F50 File Offset: 0x000E4150
		[Event(21)]
		public void WalkFreeListResult(string cacheName, int freeListCount, int gen0BuffersInFreeList)
		{
			if (base.IsEnabled())
			{
				base.WriteEvent(21, cacheName, freeListCount, gen0BuffersInFreeList);
			}
		}

		// Token: 0x06003CED RID: 15597 RVA: 0x000E5F65 File Offset: 0x000E4165
		[Event(22)]
		public void FreeBufferNull(string cacheName, int freeCountBefore)
		{
			if (base.IsEnabled())
			{
				base.WriteEvent(22, cacheName, freeCountBefore);
			}
		}

		// Token: 0x06003CEE RID: 15598 RVA: 0x000E5F7C File Offset: 0x000E417C
		internal static ulong AddressOf(object obj)
		{
			byte[] array = obj as byte[];
			if (array != null)
			{
				return (ulong)PinnableBufferCacheEventSource.AddressOfByteArray(array);
			}
			return 0UL;
		}

		// Token: 0x06003CEF RID: 15599 RVA: 0x000E5F9C File Offset: 0x000E419C
		[SecuritySafeCritical]
		internal unsafe static long AddressOfByteArray(byte[] array)
		{
			if (array == null)
			{
				return 0L;
			}
			byte* ptr;
			if (array == null || array.Length == 0)
			{
				ptr = null;
			}
			else
			{
				ptr = &array[0];
			}
			return ptr - 2 * sizeof(void*);
		}

		// Token: 0x040019DA RID: 6618
		public static readonly PinnableBufferCacheEventSource Log = new PinnableBufferCacheEventSource();
	}
}
