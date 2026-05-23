using System;

namespace System
{
	// Token: 0x0200016F RID: 367
	internal enum TokenType
	{
		// Token: 0x040007B4 RID: 1972
		NumberToken = 1,
		// Token: 0x040007B5 RID: 1973
		YearNumberToken,
		// Token: 0x040007B6 RID: 1974
		Am,
		// Token: 0x040007B7 RID: 1975
		Pm,
		// Token: 0x040007B8 RID: 1976
		MonthToken,
		// Token: 0x040007B9 RID: 1977
		EndOfString,
		// Token: 0x040007BA RID: 1978
		DayOfWeekToken,
		// Token: 0x040007BB RID: 1979
		TimeZoneToken,
		// Token: 0x040007BC RID: 1980
		EraToken,
		// Token: 0x040007BD RID: 1981
		DateWordToken,
		// Token: 0x040007BE RID: 1982
		UnknownToken,
		// Token: 0x040007BF RID: 1983
		HebrewNumber,
		// Token: 0x040007C0 RID: 1984
		JapaneseEraToken,
		// Token: 0x040007C1 RID: 1985
		TEraToken,
		// Token: 0x040007C2 RID: 1986
		IgnorableSymbol,
		// Token: 0x040007C3 RID: 1987
		SEP_Unk = 256,
		// Token: 0x040007C4 RID: 1988
		SEP_End = 512,
		// Token: 0x040007C5 RID: 1989
		SEP_Space = 768,
		// Token: 0x040007C6 RID: 1990
		SEP_Am = 1024,
		// Token: 0x040007C7 RID: 1991
		SEP_Pm = 1280,
		// Token: 0x040007C8 RID: 1992
		SEP_Date = 1536,
		// Token: 0x040007C9 RID: 1993
		SEP_Time = 1792,
		// Token: 0x040007CA RID: 1994
		SEP_YearSuff = 2048,
		// Token: 0x040007CB RID: 1995
		SEP_MonthSuff = 2304,
		// Token: 0x040007CC RID: 1996
		SEP_DaySuff = 2560,
		// Token: 0x040007CD RID: 1997
		SEP_HourSuff = 2816,
		// Token: 0x040007CE RID: 1998
		SEP_MinuteSuff = 3072,
		// Token: 0x040007CF RID: 1999
		SEP_SecondSuff = 3328,
		// Token: 0x040007D0 RID: 2000
		SEP_LocalTimeMark = 3584,
		// Token: 0x040007D1 RID: 2001
		SEP_DateOrOffset = 3840,
		// Token: 0x040007D2 RID: 2002
		RegularTokenMask = 255,
		// Token: 0x040007D3 RID: 2003
		SeparatorTokenMask = 65280
	}
}
