using System;
using System.Runtime.InteropServices;

namespace System.Globalization
{
	// Token: 0x020003A7 RID: 935
	[Flags]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public enum CompareOptions
	{
		// Token: 0x0400130F RID: 4879
		[__DynamicallyInvokable]
		None = 0,
		// Token: 0x04001310 RID: 4880
		[__DynamicallyInvokable]
		IgnoreCase = 1,
		// Token: 0x04001311 RID: 4881
		[__DynamicallyInvokable]
		IgnoreNonSpace = 2,
		// Token: 0x04001312 RID: 4882
		[__DynamicallyInvokable]
		IgnoreSymbols = 4,
		// Token: 0x04001313 RID: 4883
		[__DynamicallyInvokable]
		IgnoreKanaType = 8,
		// Token: 0x04001314 RID: 4884
		[__DynamicallyInvokable]
		IgnoreWidth = 16,
		// Token: 0x04001315 RID: 4885
		[__DynamicallyInvokable]
		OrdinalIgnoreCase = 268435456,
		// Token: 0x04001316 RID: 4886
		[__DynamicallyInvokable]
		StringSort = 536870912,
		// Token: 0x04001317 RID: 4887
		[__DynamicallyInvokable]
		Ordinal = 1073741824
	}
}
