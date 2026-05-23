using System;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;

namespace System.Collections.Concurrent
{
	// Token: 0x020004AD RID: 1197
	[FriendAccessAllowed]
	[EventSource(Name = "System.Collections.Concurrent.ConcurrentCollectionsEventSource", Guid = "35167F8E-49B2-4b96-AB86-435B59336B5E", LocalizationResources = "mscorlib")]
	internal sealed class CDSCollectionETWBCLProvider : EventSource
	{
		// Token: 0x06003929 RID: 14633 RVA: 0x000DAC46 File Offset: 0x000D8E46
		private CDSCollectionETWBCLProvider()
		{
		}

		// Token: 0x0600392A RID: 14634 RVA: 0x000DAC4E File Offset: 0x000D8E4E
		[Event(1, Level = EventLevel.Warning)]
		public void ConcurrentStack_FastPushFailed(int spinCount)
		{
			if (base.IsEnabled(EventLevel.Warning, EventKeywords.All))
			{
				base.WriteEvent(1, spinCount);
			}
		}

		// Token: 0x0600392B RID: 14635 RVA: 0x000DAC63 File Offset: 0x000D8E63
		[Event(2, Level = EventLevel.Warning)]
		public void ConcurrentStack_FastPopFailed(int spinCount)
		{
			if (base.IsEnabled(EventLevel.Warning, EventKeywords.All))
			{
				base.WriteEvent(2, spinCount);
			}
		}

		// Token: 0x0600392C RID: 14636 RVA: 0x000DAC78 File Offset: 0x000D8E78
		[Event(3, Level = EventLevel.Warning)]
		public void ConcurrentDictionary_AcquiringAllLocks(int numOfBuckets)
		{
			if (base.IsEnabled(EventLevel.Warning, EventKeywords.All))
			{
				base.WriteEvent(3, numOfBuckets);
			}
		}

		// Token: 0x0600392D RID: 14637 RVA: 0x000DAC8D File Offset: 0x000D8E8D
		[Event(4, Level = EventLevel.Verbose)]
		public void ConcurrentBag_TryTakeSteals()
		{
			if (base.IsEnabled(EventLevel.Verbose, EventKeywords.All))
			{
				base.WriteEvent(4);
			}
		}

		// Token: 0x0600392E RID: 14638 RVA: 0x000DACA1 File Offset: 0x000D8EA1
		[Event(5, Level = EventLevel.Verbose)]
		public void ConcurrentBag_TryPeekSteals()
		{
			if (base.IsEnabled(EventLevel.Verbose, EventKeywords.All))
			{
				base.WriteEvent(5);
			}
		}

		// Token: 0x04001915 RID: 6421
		public static CDSCollectionETWBCLProvider Log = new CDSCollectionETWBCLProvider();

		// Token: 0x04001916 RID: 6422
		private const EventKeywords ALL_KEYWORDS = EventKeywords.All;

		// Token: 0x04001917 RID: 6423
		private const int CONCURRENTSTACK_FASTPUSHFAILED_ID = 1;

		// Token: 0x04001918 RID: 6424
		private const int CONCURRENTSTACK_FASTPOPFAILED_ID = 2;

		// Token: 0x04001919 RID: 6425
		private const int CONCURRENTDICTIONARY_ACQUIRINGALLLOCKS_ID = 3;

		// Token: 0x0400191A RID: 6426
		private const int CONCURRENTBAG_TRYTAKESTEALS_ID = 4;

		// Token: 0x0400191B RID: 6427
		private const int CONCURRENTBAG_TRYPEEKSTEALS_ID = 5;
	}
}
