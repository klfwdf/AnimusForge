using System;

namespace System.Runtime.InteropServices
{
	// Token: 0x02000947 RID: 2375
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public enum GCHandleType
	{
		// Token: 0x04002B42 RID: 11074
		[__DynamicallyInvokable]
		Weak,
		// Token: 0x04002B43 RID: 11075
		[__DynamicallyInvokable]
		WeakTrackResurrection,
		// Token: 0x04002B44 RID: 11076
		[__DynamicallyInvokable]
		Normal,
		// Token: 0x04002B45 RID: 11077
		[__DynamicallyInvokable]
		Pinned
	}
}
