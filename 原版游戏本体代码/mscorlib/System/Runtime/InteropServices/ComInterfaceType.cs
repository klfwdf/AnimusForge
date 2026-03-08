using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000913 RID: 2323
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public enum ComInterfaceType
	{
		// Token: 0x04002A67 RID: 10855
		[__DynamicallyInvokable]
		InterfaceIsDual,
		// Token: 0x04002A68 RID: 10856
		[__DynamicallyInvokable]
		InterfaceIsIUnknown,
		// Token: 0x04002A69 RID: 10857
		[__DynamicallyInvokable]
		InterfaceIsIDispatch,
		// Token: 0x04002A6A RID: 10858
		[ComVisible(false)]
		[__DynamicallyInvokable]
		InterfaceIsIInspectable
	}
}
