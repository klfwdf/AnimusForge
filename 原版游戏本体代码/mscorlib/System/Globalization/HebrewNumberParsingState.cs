using System;

namespace System.Globalization
{
	// Token: 0x020003DE RID: 990
	internal enum HebrewNumberParsingState
	{
		// Token: 0x0400168F RID: 5775
		InvalidHebrewNumber,
		// Token: 0x04001690 RID: 5776
		NotHebrewDigit,
		// Token: 0x04001691 RID: 5777
		FoundEndOfHebrewNumber,
		// Token: 0x04001692 RID: 5778
		ContinueParsing
	}
}
