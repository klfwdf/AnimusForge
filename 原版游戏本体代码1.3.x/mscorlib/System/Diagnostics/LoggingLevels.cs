using System;

namespace System.Diagnostics
{
	// Token: 0x020003F5 RID: 1013
	[Serializable]
	internal enum LoggingLevels
	{
		// Token: 0x040016BD RID: 5821
		TraceLevel0,
		// Token: 0x040016BE RID: 5822
		TraceLevel1,
		// Token: 0x040016BF RID: 5823
		TraceLevel2,
		// Token: 0x040016C0 RID: 5824
		TraceLevel3,
		// Token: 0x040016C1 RID: 5825
		TraceLevel4,
		// Token: 0x040016C2 RID: 5826
		StatusLevel0 = 20,
		// Token: 0x040016C3 RID: 5827
		StatusLevel1,
		// Token: 0x040016C4 RID: 5828
		StatusLevel2,
		// Token: 0x040016C5 RID: 5829
		StatusLevel3,
		// Token: 0x040016C6 RID: 5830
		StatusLevel4,
		// Token: 0x040016C7 RID: 5831
		WarningLevel = 40,
		// Token: 0x040016C8 RID: 5832
		ErrorLevel = 50,
		// Token: 0x040016C9 RID: 5833
		PanicLevel = 100
	}
}
