using System;

namespace System.Security.Util
{
	// Token: 0x02000379 RID: 889
	[Flags]
	[Serializable]
	internal enum QuickCacheEntryType
	{
		// Token: 0x040011BE RID: 4542
		FullTrustZoneMyComputer = 16777216,
		// Token: 0x040011BF RID: 4543
		FullTrustZoneIntranet = 33554432,
		// Token: 0x040011C0 RID: 4544
		FullTrustZoneInternet = 67108864,
		// Token: 0x040011C1 RID: 4545
		FullTrustZoneTrusted = 134217728,
		// Token: 0x040011C2 RID: 4546
		FullTrustZoneUntrusted = 268435456,
		// Token: 0x040011C3 RID: 4547
		FullTrustAll = 536870912
	}
}
