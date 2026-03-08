using System;
using System.Runtime.InteropServices;

namespace System.Reflection
{
	// Token: 0x020005C9 RID: 1481
	[Flags]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public enum AssemblyNameFlags
	{
		// Token: 0x04001C21 RID: 7201
		[__DynamicallyInvokable]
		None = 0,
		// Token: 0x04001C22 RID: 7202
		[__DynamicallyInvokable]
		PublicKey = 1,
		// Token: 0x04001C23 RID: 7203
		EnableJITcompileOptimizer = 16384,
		// Token: 0x04001C24 RID: 7204
		EnableJITcompileTracking = 32768,
		// Token: 0x04001C25 RID: 7205
		[__DynamicallyInvokable]
		Retargetable = 256
	}
}
