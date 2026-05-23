using System;
using System.Runtime.InteropServices;

namespace System.Globalization
{
	// Token: 0x020003AC RID: 940
	[Flags]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public enum DateTimeStyles
	{
		// Token: 0x04001363 RID: 4963
		[__DynamicallyInvokable]
		None = 0,
		// Token: 0x04001364 RID: 4964
		[__DynamicallyInvokable]
		AllowLeadingWhite = 1,
		// Token: 0x04001365 RID: 4965
		[__DynamicallyInvokable]
		AllowTrailingWhite = 2,
		// Token: 0x04001366 RID: 4966
		[__DynamicallyInvokable]
		AllowInnerWhite = 4,
		// Token: 0x04001367 RID: 4967
		[__DynamicallyInvokable]
		AllowWhiteSpaces = 7,
		// Token: 0x04001368 RID: 4968
		[__DynamicallyInvokable]
		NoCurrentDateDefault = 8,
		// Token: 0x04001369 RID: 4969
		[__DynamicallyInvokable]
		AdjustToUniversal = 16,
		// Token: 0x0400136A RID: 4970
		[__DynamicallyInvokable]
		AssumeLocal = 32,
		// Token: 0x0400136B RID: 4971
		[__DynamicallyInvokable]
		AssumeUniversal = 64,
		// Token: 0x0400136C RID: 4972
		[__DynamicallyInvokable]
		RoundtripKind = 128
	}
}
