using System;
using System.Runtime.InteropServices;

namespace System.Reflection.Emit
{
	// Token: 0x0200065A RID: 1626
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public enum FlowControl
	{
		// Token: 0x04002182 RID: 8578
		[__DynamicallyInvokable]
		Branch,
		// Token: 0x04002183 RID: 8579
		[__DynamicallyInvokable]
		Break,
		// Token: 0x04002184 RID: 8580
		[__DynamicallyInvokable]
		Call,
		// Token: 0x04002185 RID: 8581
		[__DynamicallyInvokable]
		Cond_Branch,
		// Token: 0x04002186 RID: 8582
		[__DynamicallyInvokable]
		Meta,
		// Token: 0x04002187 RID: 8583
		[__DynamicallyInvokable]
		Next,
		// Token: 0x04002188 RID: 8584
		[Obsolete("This API has been deprecated. http://go.microsoft.com/fwlink/?linkid=14202")]
		Phi,
		// Token: 0x04002189 RID: 8585
		[__DynamicallyInvokable]
		Return,
		// Token: 0x0400218A RID: 8586
		[__DynamicallyInvokable]
		Throw
	}
}
