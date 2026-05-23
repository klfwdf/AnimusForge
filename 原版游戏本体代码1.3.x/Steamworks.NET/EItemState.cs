using System;

namespace Steamworks
{
	// Token: 0x02000133 RID: 307
	[Flags]
	public enum EItemState
	{
		// Token: 0x04000709 RID: 1801
		k_EItemStateNone = 0,
		// Token: 0x0400070A RID: 1802
		k_EItemStateSubscribed = 1,
		// Token: 0x0400070B RID: 1803
		k_EItemStateLegacyItem = 2,
		// Token: 0x0400070C RID: 1804
		k_EItemStateInstalled = 4,
		// Token: 0x0400070D RID: 1805
		k_EItemStateNeedsUpdate = 8,
		// Token: 0x0400070E RID: 1806
		k_EItemStateDownloading = 16,
		// Token: 0x0400070F RID: 1807
		k_EItemStateDownloadPending = 32
	}
}
