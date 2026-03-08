using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000942 RID: 2370
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public enum CallingConvention
	{
		// Token: 0x04002B35 RID: 11061
		[__DynamicallyInvokable]
		Winapi = 1,
		// Token: 0x04002B36 RID: 11062
		[__DynamicallyInvokable]
		Cdecl,
		// Token: 0x04002B37 RID: 11063
		[__DynamicallyInvokable]
		StdCall,
		// Token: 0x04002B38 RID: 11064
		[__DynamicallyInvokable]
		ThisCall,
		// Token: 0x04002B39 RID: 11065
		FastCall
	}
}
