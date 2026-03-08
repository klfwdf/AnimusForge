using System;
using System.Security;

namespace System
{
	// Token: 0x0200016A RID: 362
	internal struct DateTimeRawInfo
	{
		// Token: 0x06001668 RID: 5736 RVA: 0x00046E7F File Offset: 0x0004507F
		[SecurityCritical]
		internal unsafe void Init(int* numberBuffer)
		{
			this.month = -1;
			this.year = -1;
			this.dayOfWeek = -1;
			this.era = -1;
			this.timeMark = DateTimeParse.TM.NotSet;
			this.fraction = -1.0;
			this.num = numberBuffer;
		}

		// Token: 0x06001669 RID: 5737 RVA: 0x00046EBC File Offset: 0x000450BC
		[SecuritySafeCritical]
		internal unsafe void AddNumber(int value)
		{
			ref int ptr = ref *this.num;
			int num = this.numCount;
			this.numCount = num + 1;
			*((ref ptr) + (IntPtr)num * 4) = value;
		}

		// Token: 0x0600166A RID: 5738 RVA: 0x00046EE6 File Offset: 0x000450E6
		[SecuritySafeCritical]
		internal unsafe int GetNumber(int index)
		{
			return this.num[index];
		}

		// Token: 0x0400077A RID: 1914
		[SecurityCritical]
		private unsafe int* num;

		// Token: 0x0400077B RID: 1915
		internal int numCount;

		// Token: 0x0400077C RID: 1916
		internal int month;

		// Token: 0x0400077D RID: 1917
		internal int year;

		// Token: 0x0400077E RID: 1918
		internal int dayOfWeek;

		// Token: 0x0400077F RID: 1919
		internal int era;

		// Token: 0x04000780 RID: 1920
		internal DateTimeParse.TM timeMark;

		// Token: 0x04000781 RID: 1921
		internal double fraction;

		// Token: 0x04000782 RID: 1922
		internal bool hasSameDateAndTimeSeparators;

		// Token: 0x04000783 RID: 1923
		internal bool timeZone;
	}
}
