using System;
using System.Globalization;

namespace System
{
	// Token: 0x0200016E RID: 366
	internal struct ParsingInfo
	{
		// Token: 0x0600166F RID: 5743 RVA: 0x00046F6E File Offset: 0x0004516E
		internal void Init()
		{
			this.dayOfWeek = -1;
			this.timeMark = DateTimeParse.TM.NotSet;
		}

		// Token: 0x040007AA RID: 1962
		internal Calendar calendar;

		// Token: 0x040007AB RID: 1963
		internal int dayOfWeek;

		// Token: 0x040007AC RID: 1964
		internal DateTimeParse.TM timeMark;

		// Token: 0x040007AD RID: 1965
		internal bool fUseHour12;

		// Token: 0x040007AE RID: 1966
		internal bool fUseTwoDigitYear;

		// Token: 0x040007AF RID: 1967
		internal bool fAllowInnerWhite;

		// Token: 0x040007B0 RID: 1968
		internal bool fAllowTrailingWhite;

		// Token: 0x040007B1 RID: 1969
		internal bool fCustomNumberParser;

		// Token: 0x040007B2 RID: 1970
		internal DateTimeParse.MatchNumberDelegate parseNumberDelegate;
	}
}
