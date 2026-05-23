using System;

namespace System.Globalization
{
	// Token: 0x020003AE RID: 942
	[Flags]
	internal enum DateTimeFormatFlags
	{
		// Token: 0x04001372 RID: 4978
		None = 0,
		// Token: 0x04001373 RID: 4979
		UseGenitiveMonth = 1,
		// Token: 0x04001374 RID: 4980
		UseLeapYearMonth = 2,
		// Token: 0x04001375 RID: 4981
		UseSpacesInMonthNames = 4,
		// Token: 0x04001376 RID: 4982
		UseHebrewRule = 8,
		// Token: 0x04001377 RID: 4983
		UseSpacesInDayNames = 16,
		// Token: 0x04001378 RID: 4984
		UseDigitPrefixInTokens = 32,
		// Token: 0x04001379 RID: 4985
		NotInitialized = -1
	}
}
