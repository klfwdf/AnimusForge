using System;
using System.Runtime.InteropServices;

namespace System.Runtime.CompilerServices
{
	// Token: 0x020008BD RID: 2237
	[Flags]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public enum MethodImplOptions
	{
		// Token: 0x04002A1C RID: 10780
		Unmanaged = 4,
		// Token: 0x04002A1D RID: 10781
		ForwardRef = 16,
		// Token: 0x04002A1E RID: 10782
		[__DynamicallyInvokable]
		PreserveSig = 128,
		// Token: 0x04002A1F RID: 10783
		InternalCall = 4096,
		// Token: 0x04002A20 RID: 10784
		Synchronized = 32,
		// Token: 0x04002A21 RID: 10785
		[__DynamicallyInvokable]
		NoInlining = 8,
		// Token: 0x04002A22 RID: 10786
		[ComVisible(false)]
		[__DynamicallyInvokable]
		AggressiveInlining = 256,
		// Token: 0x04002A23 RID: 10787
		[__DynamicallyInvokable]
		NoOptimization = 64,
		// Token: 0x04002A24 RID: 10788
		SecurityMitigations = 1024
	}
}
