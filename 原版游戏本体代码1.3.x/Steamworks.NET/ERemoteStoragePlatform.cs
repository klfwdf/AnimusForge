using System;

namespace Steamworks
{
	// Token: 0x02000123 RID: 291
	[Flags]
	public enum ERemoteStoragePlatform
	{
		// Token: 0x04000684 RID: 1668
		k_ERemoteStoragePlatformNone = 0,
		// Token: 0x04000685 RID: 1669
		k_ERemoteStoragePlatformWindows = 1,
		// Token: 0x04000686 RID: 1670
		k_ERemoteStoragePlatformOSX = 2,
		// Token: 0x04000687 RID: 1671
		k_ERemoteStoragePlatformPS3 = 4,
		// Token: 0x04000688 RID: 1672
		k_ERemoteStoragePlatformLinux = 8,
		// Token: 0x04000689 RID: 1673
		k_ERemoteStoragePlatformSwitch = 16,
		// Token: 0x0400068A RID: 1674
		k_ERemoteStoragePlatformAndroid = 32,
		// Token: 0x0400068B RID: 1675
		k_ERemoteStoragePlatformIOS = 64,
		// Token: 0x0400068C RID: 1676
		k_ERemoteStoragePlatformAll = -1
	}
}
