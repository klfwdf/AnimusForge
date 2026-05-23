using System;

namespace System
{
	// Token: 0x02000168 RID: 360
	internal struct DTSubString
	{
		// Token: 0x17000271 RID: 625
		internal char this[int relativeIndex]
		{
			get
			{
				return this.s[this.index + relativeIndex];
			}
		}

		// Token: 0x04000772 RID: 1906
		internal string s;

		// Token: 0x04000773 RID: 1907
		internal int index;

		// Token: 0x04000774 RID: 1908
		internal int length;

		// Token: 0x04000775 RID: 1909
		internal DTSubStringType type;

		// Token: 0x04000776 RID: 1910
		internal int value;
	}
}
