using System;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;
using System.Security;

namespace System.Threading
{
	// Token: 0x0200053C RID: 1340
	[FriendAccessAllowed]
	[EventSource(Name = "System.Threading.SynchronizationEventSource", Guid = "EC631D38-466B-4290-9306-834971BA0217", LocalizationResources = "mscorlib")]
	internal sealed class CdsSyncEtwBCLProvider : EventSource
	{
		// Token: 0x06003EDC RID: 16092 RVA: 0x000E9E9D File Offset: 0x000E809D
		private CdsSyncEtwBCLProvider()
		{
		}

		// Token: 0x06003EDD RID: 16093 RVA: 0x000E9EA5 File Offset: 0x000E80A5
		[Event(1, Level = EventLevel.Warning)]
		public void SpinLock_FastPathFailed(int ownerID)
		{
			if (base.IsEnabled(EventLevel.Warning, EventKeywords.All))
			{
				base.WriteEvent(1, ownerID);
			}
		}

		// Token: 0x06003EDE RID: 16094 RVA: 0x000E9EBA File Offset: 0x000E80BA
		[Event(2, Level = EventLevel.Informational)]
		public void SpinWait_NextSpinWillYield()
		{
			if (base.IsEnabled(EventLevel.Informational, EventKeywords.All))
			{
				base.WriteEvent(2);
			}
		}

		// Token: 0x06003EDF RID: 16095 RVA: 0x000E9ED0 File Offset: 0x000E80D0
		[SecuritySafeCritical]
		[Event(3, Level = EventLevel.Verbose, Version = 1)]
		public unsafe void Barrier_PhaseFinished(bool currentSense, long phaseNum)
		{
			if (base.IsEnabled(EventLevel.Verbose, EventKeywords.All))
			{
				EventSource.EventData* ptr = stackalloc EventSource.EventData[checked(unchecked((UIntPtr)2) * (UIntPtr)sizeof(EventSource.EventData))];
				int num = (currentSense ? 1 : 0);
				ptr->Size = 4;
				ptr->DataPointer = (IntPtr)((void*)(&num));
				ptr[1].Size = 8;
				ptr[1].DataPointer = (IntPtr)((void*)(&phaseNum));
				base.WriteEventCore(3, 2, ptr);
			}
		}

		// Token: 0x04001A6E RID: 6766
		public static CdsSyncEtwBCLProvider Log = new CdsSyncEtwBCLProvider();

		// Token: 0x04001A6F RID: 6767
		private const EventKeywords ALL_KEYWORDS = EventKeywords.All;

		// Token: 0x04001A70 RID: 6768
		private const int SPINLOCK_FASTPATHFAILED_ID = 1;

		// Token: 0x04001A71 RID: 6769
		private const int SPINWAIT_NEXTSPINWILLYIELD_ID = 2;

		// Token: 0x04001A72 RID: 6770
		private const int BARRIER_PHASEFINISHED_ID = 3;
	}
}
