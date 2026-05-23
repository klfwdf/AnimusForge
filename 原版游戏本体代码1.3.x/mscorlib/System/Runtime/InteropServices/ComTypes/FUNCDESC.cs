using System;

namespace System.Runtime.InteropServices.ComTypes
{
	// Token: 0x02000A3E RID: 2622
	[__DynamicallyInvokable]
	public struct FUNCDESC
	{
		// Token: 0x04002D95 RID: 11669
		[__DynamicallyInvokable]
		public int memid;

		// Token: 0x04002D96 RID: 11670
		public IntPtr lprgscode;

		// Token: 0x04002D97 RID: 11671
		public IntPtr lprgelemdescParam;

		// Token: 0x04002D98 RID: 11672
		[__DynamicallyInvokable]
		public FUNCKIND funckind;

		// Token: 0x04002D99 RID: 11673
		[__DynamicallyInvokable]
		public INVOKEKIND invkind;

		// Token: 0x04002D9A RID: 11674
		[__DynamicallyInvokable]
		public CALLCONV callconv;

		// Token: 0x04002D9B RID: 11675
		[__DynamicallyInvokable]
		public short cParams;

		// Token: 0x04002D9C RID: 11676
		[__DynamicallyInvokable]
		public short cParamsOpt;

		// Token: 0x04002D9D RID: 11677
		[__DynamicallyInvokable]
		public short oVft;

		// Token: 0x04002D9E RID: 11678
		[__DynamicallyInvokable]
		public short cScodes;

		// Token: 0x04002D9F RID: 11679
		[__DynamicallyInvokable]
		public ELEMDESC elemdescFunc;

		// Token: 0x04002DA0 RID: 11680
		[__DynamicallyInvokable]
		public short wFuncFlags;
	}
}
