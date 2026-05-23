using System;
using System.Runtime.InteropServices;

namespace System.Reflection
{
	// Token: 0x020005F4 RID: 1524
	[Flags]
	[ComVisible(true)]
	[__DynamicallyInvokable]
	[Serializable]
	public enum ResourceLocation
	{
		// Token: 0x04001CDD RID: 7389
		[__DynamicallyInvokable]
		Embedded = 1,
		// Token: 0x04001CDE RID: 7390
		[__DynamicallyInvokable]
		ContainedInAnotherAssembly = 2,
		// Token: 0x04001CDF RID: 7391
		[__DynamicallyInvokable]
		ContainedInManifestFile = 4
	}
}
