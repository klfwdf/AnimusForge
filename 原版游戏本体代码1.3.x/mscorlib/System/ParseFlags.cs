using System;

namespace System
{
	// Token: 0x0200016C RID: 364
	[Flags]
	internal enum ParseFlags
	{
		// Token: 0x0400078B RID: 1931
		HaveYear = 1,
		// Token: 0x0400078C RID: 1932
		HaveMonth = 2,
		// Token: 0x0400078D RID: 1933
		HaveDay = 4,
		// Token: 0x0400078E RID: 1934
		HaveHour = 8,
		// Token: 0x0400078F RID: 1935
		HaveMinute = 16,
		// Token: 0x04000790 RID: 1936
		HaveSecond = 32,
		// Token: 0x04000791 RID: 1937
		HaveTime = 64,
		// Token: 0x04000792 RID: 1938
		HaveDate = 128,
		// Token: 0x04000793 RID: 1939
		TimeZoneUsed = 256,
		// Token: 0x04000794 RID: 1940
		TimeZoneUtc = 512,
		// Token: 0x04000795 RID: 1941
		ParsedMonthName = 1024,
		// Token: 0x04000796 RID: 1942
		CaptureOffset = 2048,
		// Token: 0x04000797 RID: 1943
		YearDefault = 4096,
		// Token: 0x04000798 RID: 1944
		Rfc1123Pattern = 8192,
		// Token: 0x04000799 RID: 1945
		UtcSortPattern = 16384
	}
}
