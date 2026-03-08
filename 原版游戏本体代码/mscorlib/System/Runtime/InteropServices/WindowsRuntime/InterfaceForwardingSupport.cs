using System;

namespace System.Runtime.InteropServices.WindowsRuntime
{
	// Token: 0x02000A0D RID: 2573
	[Flags]
	internal enum InterfaceForwardingSupport
	{
		// Token: 0x04002D3C RID: 11580
		None = 0,
		// Token: 0x04002D3D RID: 11581
		IBindableVector = 1,
		// Token: 0x04002D3E RID: 11582
		IVector = 2,
		// Token: 0x04002D3F RID: 11583
		IBindableVectorView = 4,
		// Token: 0x04002D40 RID: 11584
		IVectorView = 8,
		// Token: 0x04002D41 RID: 11585
		IBindableIterableOrIIterable = 16
	}
}
