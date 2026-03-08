using System;
using System.Globalization;

namespace System
{
	// Token: 0x0200016D RID: 365
	internal struct DateTimeResult
	{
		// Token: 0x0600166B RID: 5739 RVA: 0x00046EF4 File Offset: 0x000450F4
		internal void Init()
		{
			this.Year = -1;
			this.Month = -1;
			this.Day = -1;
			this.fraction = -1.0;
			this.era = -1;
		}

		// Token: 0x0600166C RID: 5740 RVA: 0x00046F21 File Offset: 0x00045121
		internal void SetDate(int year, int month, int day)
		{
			this.Year = year;
			this.Month = month;
			this.Day = day;
		}

		// Token: 0x0600166D RID: 5741 RVA: 0x00046F38 File Offset: 0x00045138
		internal void SetFailure(ParseFailureKind failure, string failureMessageID, object failureMessageFormatArgument)
		{
			this.failure = failure;
			this.failureMessageID = failureMessageID;
			this.failureMessageFormatArgument = failureMessageFormatArgument;
		}

		// Token: 0x0600166E RID: 5742 RVA: 0x00046F4F File Offset: 0x0004514F
		internal void SetFailure(ParseFailureKind failure, string failureMessageID, object failureMessageFormatArgument, string failureArgumentName)
		{
			this.failure = failure;
			this.failureMessageID = failureMessageID;
			this.failureMessageFormatArgument = failureMessageFormatArgument;
			this.failureArgumentName = failureArgumentName;
		}

		// Token: 0x0400079A RID: 1946
		internal int Year;

		// Token: 0x0400079B RID: 1947
		internal int Month;

		// Token: 0x0400079C RID: 1948
		internal int Day;

		// Token: 0x0400079D RID: 1949
		internal int Hour;

		// Token: 0x0400079E RID: 1950
		internal int Minute;

		// Token: 0x0400079F RID: 1951
		internal int Second;

		// Token: 0x040007A0 RID: 1952
		internal double fraction;

		// Token: 0x040007A1 RID: 1953
		internal int era;

		// Token: 0x040007A2 RID: 1954
		internal ParseFlags flags;

		// Token: 0x040007A3 RID: 1955
		internal TimeSpan timeZoneOffset;

		// Token: 0x040007A4 RID: 1956
		internal Calendar calendar;

		// Token: 0x040007A5 RID: 1957
		internal DateTime parsedDate;

		// Token: 0x040007A6 RID: 1958
		internal ParseFailureKind failure;

		// Token: 0x040007A7 RID: 1959
		internal string failureMessageID;

		// Token: 0x040007A8 RID: 1960
		internal object failureMessageFormatArgument;

		// Token: 0x040007A9 RID: 1961
		internal string failureArgumentName;
	}
}
